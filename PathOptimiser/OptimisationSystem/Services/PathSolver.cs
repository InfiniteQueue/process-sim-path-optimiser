using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PathOptimiser.DisplayGrid.OperationsList;
using PathOptimiser.Models;
using PathOptimiser.OptimisationSystem;
using PathOptimiser.OptimisationSystem.Models;
using PathOptimiser.OptimisationSystem.Services;
using PathOptimiser.OptimisationSystem.Services.EnvelopeRecorder;
using Tecnomatix.Engineering;
using static Tecnomatix.Engineering.TxApplication;
using CompoundOp = Tecnomatix.Engineering.ITxRoboticOrderedCompoundOperation;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;

namespace PathOptimiser
{
    public partial class PathSolver
    {

        #region Data
        SimPlayerTracker tracker;

        public SolverParams solverParams = new SolverParams();
        public HashSet<LocOp> ActiveVias => solverParams.ActiveVias;


        ITxCompoundOperation operation;
        ITxCompoundOperation Operation { get; set; }
        TxRobot Robot => Operation.SimulatedObjects.OfType<TxRobot>().First();

        double OriginalTime;
        double OptimisedTime;
        #endregion
        #region Init
        public PathSolver(SimPlayerTracker tracker)
        {
            this.tracker = tracker;
            this.Done += (s) => PathSolver_Done();
        }
        #endregion


        #region Iteration

        public void RunMain(IEnumerable<LocOp> IncludedVias, Dictionary<ITxCompoundOperation, CollectionOperationsDisplay.OperationData> OperationData)
        {
            DeviceResetter.Store();
            var operations = IncludedVias.GroupBy(x => x.Collection as ITxCompoundOperation);
            var results = new List<Result>();

            foreach (var pair in operations) {
                solverParams.NearMissDistance = OperationToNearMissRegistry.ContainsKey(pair.Key) ? OperationToNearMissRegistry.GetValue(pair.Key) : 5;
                solverParams.LoadData(OperationData[pair.Key]);

                Operation = pair.Key;
                var result = OptimiseWithSubEnvelopes(pair);

                results.Add(result);
                OperationFinished?.Invoke(new OperationOptimisedReport() { Operation = pair.Key, Success = result, originalTime = OriginalTime, optimisedTime = OptimisedTime });
                DeviceResetter.Reload();
            }

            Done?.Invoke(
                results.Any(x => x == Result.Cancelled) ? Result.Cancelled :
                results.All(x => x == Result.Success) ? Result.Success : Result.CouldNotComplete);
        }


        #region SubEnvelopes
        private Result OptimiseWithSubEnvelopes(IEnumerable<LocOp> IncludedVias)
        {
            #region Setup
            Result result = Result.Success;
            OriginalTime = 0;
            OptimisedTime = 0;

            var op = Operation as CompoundOp;
            //Set first and last cnt to fine to ensure path runs properly
            (op.GetChildAt(0) as ITxLocationOperation).SetCNT(null);
            (op.GetChildAt(op.Count - 1) as ITxLocationOperation).SetCNT(null);

            if (IsCancelRequested) { result = Result.Cancelled; return result; }

            solverParams.ActiveVias = IncludedVias.ToHashSet();

            var acceptableClearanceRecorder = new AcceptableClearanceRecorder(tracker, IncludedVias.First().Collection as ITxOperation, solverParams);
            var initialCollection = acceptableClearanceRecorder.Run(tracker);
            OriginalClearancesFound?.Invoke(solverParams.acceptedClearances);
            OriginalTime = initialCollection.FinalTime;


            if (IsCancelRequested) {
                result = Result.Cancelled; return result;
            }

            //This is handled by separate code in the enveloperecorder now
            //if (!solverParams.IgnoreExistingNearMisses && !solverParams.IgnoreExistingCollisions) { solverParams.acceptedClearances.Clear(); }

            //Exclude all welds from the input vias
            IncludedVias = IncludedVias.Where(x => x is TxWeldLocationOperation == false);
            this.solverParams.ActiveVias = new HashSet<LocOp>(IncludedVias);

            //Require input vias to run
            if (this.ActiveVias.Count == 0) {
                Debug.WriteLine($"{this}: No operation set");
                return Result.CouldNotComplete;
            }

            //Reset current operation
            tracker.SimPlayer.AskUserForReset(false);
            tracker.SimPlayer.SetOperation(op);
            ActiveDocument.CurrentOperation = op;
            #endregion

            //Debug.WriteLine("---------\n---DEBUG skip maximise speeds---\n---------");
            SetInitialViaSpeeds(op, solverParams.ResetToMax);

            ValuesUpdated?.Invoke();

            ClearAllSubEnvelopes(IncludedVias);


            if (!IsCancelRequested) {
                DebugLog();
                Debug.WriteLine($"\n - - - - Begin final pass - - - - \n");
                var finalSolver = new PathSolver(tracker) { solverParams = this.solverParams };
                finalSolver.RunAsSinglePath(ActiveVias, false);
                this.OptimisedTime = finalSolver.OptimisedTime;
                //Final pass to clean up any remaining collisions with the original method
            }


            tracker.SimPlayer?.Stop();

            Debug.WriteLine($"{nameof(PathSolver)}: Done");

            if (IsCancelRequested) result = Result.Cancelled;

            return result;
            //Done?.Invoke(result);
        }

        void ClearAllSubEnvelopes(IEnumerable<LocOp> InputVias)
        {
            using (var envelopeGetter = new EnvelopeRecorder(tracker, ActiveDocument.CurrentOperation, solverParams) { SimPlayerTracker = tracker }) {

                //Avoids repeating the same envelope by creating a hashset of the first via per envelope and checking if it is already present
                var StudiedVias = new HashSet<ITxLocationOperation>();

                EnvelopeCollection currentEnvCollection;
                //Build a collection of collision envelopes, take the first envelope found and optimise a copied version of the suboperation
                for (currentEnvCollection = envelopeGetter.Run(tracker); currentEnvCollection.Count > 0; currentEnvCollection = envelopeGetter.Run(tracker)) {

                    if (OriginalTime == 0) OriginalTime = currentEnvCollection.FinalTime;

                    if (currentEnvCollection.totalPenalty == 0) {
                        OptimisedTime = currentEnvCollection.FinalTime;
                        Debug.WriteLine($"{nameof(PathSolver)}: Breaking, no collisions found");
                        break; //No collisions, stop iterating
                    }


                    //Get the first envelope...
                    var testEnv = currentEnvCollection.FirstOrDefault(x => 

                    !StudiedVias.Contains(x.CollidingVias.First()) //Not starting with the same via as a previous envelope (avoids infinite loops)
                    && (x.CollidingVias.Any(y => InputVias.Contains(y)) //Must include an active via, or at least be a weld bordering one
                    || x.CollidingVias.FirstOrDefault() is TxWeldLocationOperation && x.ExtendBy(1,1).Any(y => InputVias.Contains(y))))
                    ;
                    if (testEnv == null) break;


                    envelopeGetter.paused = true;
                    PathSolveOnEnvelopeDuplicateOperation(InputVias, testEnv);

                    envelopeGetter.paused = false;
                    StudiedVias.Add(testEnv.CollidingVias.First());

                    RefreshDisplay();

                    ValuesUpdated?.Invoke();

                    tracker.SimPlayer = new TxSimulationPlayer(false, true);
                    tracker.SimPlayer.SetOperation(Operation);
                    if (IsCancelRequested) break;
                }
            }
        }

        #region DuplicateSetup
        private void PathSolveOnEnvelopeDuplicateOperation(IEnumerable<LocOp> InputVias, CollisionEnvelope testEnv)
        {

            using (var duplicate = new EnvelopeDuplicate(testEnv, (CompoundOp)Operation)) {

                var firstVia = duplicate.EnvCopyOperation.Vias().First() as ITxRoboticLocationOperation;
                if (firstVia != Operation.Vias().First()) new PlayToVia().Run(firstVia, tracker.SimPlayer);

                var solver = new PathSolver(tracker);

                solver.solverParams = new SolverParams()
                {
                    acceptedClearances = solverParams.acceptedClearances.GetEnvelopeDuplicateClone(duplicate),
                    thisEnvelopeDuplicate = duplicate,
                    NearMissDistance = solverParams.NearMissDistance
                };


                GetViaRolesForDuplicate(InputVias, testEnv, duplicate, solver.solverParams);


                solver.RunAsSinglePath(solver.solverParams.ActiveVias, false);
            }

            DeviceResetter.Reload();
        }

        private static void GetViaRolesForDuplicate(IEnumerable<LocOp> inputActiveVias, CollisionEnvelope testEnv, EnvelopeDuplicate duplicate, SolverParams duplicateSolverParams)
        {
            var duplicateVias = duplicate.EnvCopyOperation.Vias().OfType<LocOp>();

            var extension = EnvelopeDuplicate.EnvelopeViaExtensionCount;


            /*Explanation:
            Active vias are considered for speed adjustments
            Ignored vias do not report collisions

            All vias but the colliding vias, and their neighbours, are ignored
            All vias but the first and last are active
            */

            var ignoredVias = testEnv.ExtendBy(extension, extension).Except(testEnv.ExtendBy(1, 1)).Select(x => duplicate.OriginalToCopy(x));



            duplicateSolverParams.IgnoredVias = ignoredVias.OfType<LocOp>().ToHashSet();



            var duplicateActiveVias = testEnv.ExtendBy(EnvelopeDuplicate.EnvelopeViaExtensionCount - 1, EnvelopeDuplicate.EnvelopeViaExtensionCount - 1);

            //Active vias should not include the first and last vias of the duplicate, unless they themselves are involved in the collision
            //duplicateActiveVias = duplicateActiveVias.Append(testEnv.CollidingVias.First()).Append(testEnv.CollidingVias.Last()).Distinct();

            duplicateActiveVias = duplicateActiveVias.Intersect(inputActiveVias);

            duplicateActiveVias = duplicateActiveVias.Select(x => duplicate.OriginalToCopy(x)).OfType<LocOp>();


            duplicateSolverParams.ActiveVias = duplicateActiveVias.ToHashSet();
        }

        #endregion
        #endregion

        #region SinglePath
        void RunAsSinglePath(IEnumerable<LocOp> InputVias, bool resetToMaxSpeed = true)
        {
            InputVias = InputVias.Where(x => x is TxWeldLocationOperation == false);
            this.solverParams.ActiveVias = new HashSet<LocOp>(InputVias);


            if (this.ActiveVias.Count == 0) { Debug.WriteLine($"{this}: No operation set"); return; }

            tracker.SimPlayer.AskUserForReset(false);
            tracker.SimPlayer.SetOperation((ITxOperation)this.ActiveVias.First().Collection);
            tracker.SimPlayer.AskUserForReset(false);
            ActiveDocument.CurrentOperation = (ITxOperation)this.ActiveVias.First().Collection;

            var op = ActiveDocument.CurrentOperation as CompoundOp;

            SetInitialViaSpeeds(op, resetToMaxSpeed);

            ValuesUpdated?.Invoke();

            ClearSingleOperation();

            tracker.SimPlayer?.Stop();
            Debug.WriteLine($"Envelope Solved");
        }

        private void ClearSingleOperation()
        {
            using (var envelopeRecorder = new EnvelopeRecorder(tracker, ActiveDocument.CurrentOperation, solverParams) { SimPlayerTracker = tracker }) {

                var remainingEnvelopes = envelopeRecorder.Run(tracker);

                while (remainingEnvelopes.Envelopes.Count > 0) {

                    if (remainingEnvelopes.totalPenalty == 0) { Debug.WriteLine($"{nameof(PathSolver)}: Breaking, no collisions found"); break; }

                    var finder = new BestAdjustmentFinder(envelopeRecorder, remainingEnvelopes);
                    finder.Run();


                    if (finder.bestAdjustment is ViaAdjustment adjustment == false) {
                        Debug.WriteLine("No possible adjustments remaining"); break;
                    }

                    else {

                        var bestAdjustment = finder.bestAdjustment.Value;


                        ApplyAdjustment(bestAdjustment, finder);


                        if (bestAdjustment.AdjustmentType.HasFlag(ViaAdjustment.AdjustmentTypes.Speed) && bestAdjustment.original.Speed == bestAdjustment.Via.MaxSpeedAndCnt().newParams.Speed) {
                            DoSpeedSkip(finder.bestEnvCollection, bestAdjustment.Via);
                        }

                        foreach (var via in ActiveVias) Debug.Write($"{new ViaAdjustment(via, new ViaParameters(via), false)} ");
                        Debug.WriteLine(" ");


                        remainingEnvelopes = finder.bestEnvCollection;
                        ValuesUpdated?.Invoke();

                    }

                    if (IsCancelRequested) break;
                }

                OptimisedTime = remainingEnvelopes.FinalTime;

                if (remainingEnvelopes.Envelopes.Count == 0) Debug.WriteLine("No collisions found");
            }
        }

        void ApplyAdjustment(ViaAdjustment bestAdjustment, BestAdjustmentFinder finder = null)
        {
            if (finder != null) {
                Debug.WriteLine($"\nApplied {finder.bestAdjustment}, \nscore: {finder.bestEnvCollection.totalPenalty}, time: {finder.bestEnvCollection.FinalTime}, intersecting frames: {finder.bestEnvCollection.Envelopes.Sum(x => x.CollidingFrameCount)}");
            }


            bestAdjustment.Apply();

            var originalVia = solverParams.thisEnvelopeDuplicate is EnvelopeDuplicate ? solverParams.thisEnvelopeDuplicate.DuplicateToOriginal[bestAdjustment.Via] as LocOp : bestAdjustment.Via;

            AdjustmentApplied?.Invoke(new ViaAdjustment(originalVia, bestAdjustment.newParams, false));
        }

        /// <summary> Repeatedly applies speed step downs to this via until it creates a significant time impact </summary>
        void DoSpeedSkip(EnvelopeCollection bestAdjustment, LocOp locOp)
        {
            Debug.WriteLine($"Speed skip on {locOp.Name}");
            var originalTime = bestAdjustment.FinalTime;

            ViaAdjustment? adjust;
            for (adjust = locOp.StandardSpeedStepDown(1); adjust != null; adjust = locOp.StandardSpeedStepDown(1)) {

                adjust?.Apply();
                var getter = new EnvelopeRecorder(tracker, ActiveDocument.CurrentOperation, solverParams);
                var collection = getter.Run(tracker);

                if (collection.FinalTime - originalTime >= 0.02) {
                    adjust = new ViaAdjustment(adjust?.Via, adjust?.original, false);
                    break;
                }
            }

            if (adjust != null) ApplyAdjustment(adjust.Value);
            Debug.WriteLine($"Stepped down to {locOp.GetSpeed()}");
        }
        #endregion
        #endregion

        private void PathSolver_Done()
        {
            //Allows the cancel request to be set to true again next loop
            IsCancelRequested = false;
        }

        #region HelperMethods
        private void SetInitialViaSpeeds(CompoundOp op, bool setMaxSpeeds)
        {
            if (setMaxSpeeds) {
                foreach (var item in ActiveVias) {
                    if (item.IsFine() == false) ApplyAdjustment(item.MaxSpeedAndCnt());
                    else {
                        ApplyAdjustment(new ViaAdjustment(item, new ViaParameters() { Speed = item.MaxSpeedAndCnt().newParams.Speed, CNT = item.GetCNT() }, false));
                    }
                }

                Debug.WriteLine("Max speeds set");
            }


        }


        void DebugLog()
        {
            foreach (var item in ActiveVias) {
                Debug.Write(new ViaAdjustment(item, new ViaParameters(item), false).ToString() + " ");
            }
            Debug.WriteLine("");
        }


        #endregion

        static bool CancelRequested;


        public static bool IsCancelRequested
        {
            get => CancelRequested;
            set {
                if (CancelRequested == value) return;
                CancelRequested = value;
                CancelRequestedChanged?.Invoke(null, value);
            }
        }

        #region Events

        public static event EventHandler<bool> CancelRequestedChanged;

        public static event Action<ViaAdjustment> AdjustmentApplied;
        public static event Action<OperationOptimisedReport> OperationFinished;
        public static event Action<SolverParams.AcceptedClearancesCollection> OriginalClearancesFound;

        public event Action<Result> Done;

        public event Action ValuesUpdated;
        #endregion

        public enum Result
        {
            Success,
            Cancelled,
            CouldNotComplete
        }

        public class OperationOptimisedReport
        {
            public ITxCompoundOperation Operation;
            public Result Success;
            public double originalTime;
            public double optimisedTime;
        }

    }
}

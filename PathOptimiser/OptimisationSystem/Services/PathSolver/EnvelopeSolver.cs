using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PathOptimiser.OptimisationSystem.Models;
using PathOptimiser.OptimisationSystem.Services.EnvelopeRecorders;
using Tecnomatix.Engineering;
using static Tecnomatix.Engineering.TxApplication;
using CompoundOp = Tecnomatix.Engineering.ITxRoboticOrderedCompoundOperation;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;
using static  PathOptimiser.OptimisationSystem.Services.PathSolver.OptimisationOperation;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.PathSolver;
using PathOptimiser.OptimisationSystem.Services.PathSolver;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.PathSolverStatics;

namespace PathOptimiser.OptimisationSystem.Services.PathSolver
{
    public partial class EnvelopeSolver
    {
        public EnvelopeSolver(SimPlayerTracker tracker, SolverParams solverParams)
        {
            this.tracker = tracker;
            this.solverParams = solverParams;
        }



        SimPlayerTracker tracker;
        public SolverParams solverParams;
        public HashSet<LocOp> ActiveVias => solverParams.ActiveVias;

        CompoundOp op => solverParams.Operation;

        public double OriginalTime;
        public double OptimisedTime;

        public bool errorEncountered = false;

        #region SubEnvelopes
        public Result OptimiseWithSubEnvelopes(IEnumerable<LocOp> IncludedVias, CancellationToken allOpsToken)
        {
            solverParams.token = CancellationTokenSource.CreateLinkedTokenSource(allOpsToken, solverParams.tokenSource.Token).Token;

            if (solverParams.token.IsCancellationRequested) return Result.Cancelled;

            TecnomatixStatics.SimulationError += TecnomatixStatics_SimulationError;

            #region Setup
            Result result = Result.Success;
            OriginalTime = 0;
            OptimisedTime = 0;

            //Set first and last cnt to fine to ensure path runs properly
            (op.GetChildAt(0) as ITxLocationOperation).SetCNT(null);
            (op.GetChildAt(op.Count - 1) as ITxLocationOperation).SetCNT(null);

            //if (IsCancelRequested) { result = Result.Cancelled; return result; }

            solverParams.ActiveVias = IncludedVias.ToHashSet();

            var acceptableClearanceRecorder = new AcceptableClearanceRecorder(tracker, IncludedVias.First().Collection as ITxOperation, solverParams);
            var initialCollection = acceptableClearanceRecorder.Run(tracker);

            ReportOriginalClearancesFound(solverParams.acceptedClearances);
            OriginalTime = initialCollection.FinalTime;


            //if (IsCancelRequested) {
            //result = Result.Cancelled; return result;
            //}

            //Exclude all welds from the input vias
            IncludedVias = IncludedVias.Where(x => x is TxWeldLocationOperation == false);
            this.solverParams.ActiveVias = new HashSet<LocOp>(IncludedVias);

            //Require input vias to run
            if (solverParams.ActiveVias.Count == 0) {
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


            ClearAllSubEnvelopes(IncludedVias);


            //if (!IsCancelRequested) {
            //DebugLog();
            Debug.WriteLine($"\n - - - - Begin final pass - - - - \n");

            var finalSolver = new PathSolver(tracker, this.solverParams);
            finalSolver.Run(ActiveVias, false);
            OptimisedTime = finalSolver.OptimisedTime;
            //Final pass to clean up any remaining collisions with the original method
            //}


            tracker.SimPlayer?.Stop();

            Debug.WriteLine($"{nameof(Services.PathSolver)}: Done");


            if (solverParams.token.IsCancellationRequested) result = Result.Cancelled;
            if (OptimisedTime < tracker.SimPlayer.TimeInterval * 2) result = Result.RobotControllerFailed;
            if (errorEncountered) result = Result.RobotControllerFailed;

            ReportOperationFinished(new OperationOptimisedReport() { Operation = solverParams.Operation, Success = result, originalTime = OriginalTime, optimisedTime = OptimisedTime, finalEnvelopeCollection = finalSolver.FinalEnvelopeCollection });
            return result;
            //Done?.Invoke(result);
        }

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

        void ClearAllSubEnvelopes(IEnumerable<LocOp> InputVias)
        {
            if (solverParams.token.IsCancellationRequested) return;

            using (var envelopeGetter = new EnvelopeRecorder(tracker, ActiveDocument.CurrentOperation, solverParams) { SimPlayerTracker = tracker }) {

                //Avoids repeating the same envelope by creating a hashset of the first via per envelope and checking if it is already present
                var StudiedVias = new HashSet<ITxLocationOperation>();

                EnvelopeCollection currentEnvCollection;
                //Build a collection of collision envelopes, take the first envelope found and optimise a copied version of the suboperation
                for (currentEnvCollection = envelopeGetter.Run(tracker); currentEnvCollection.Count > 0; currentEnvCollection = envelopeGetter.Run(tracker)) {

                    if (solverParams.token.IsCancellationRequested) return;

                    if (OriginalTime == 0) OriginalTime = currentEnvCollection.FinalTime;

                    if (currentEnvCollection.totalPenalty == 0) {
                        OptimisedTime = currentEnvCollection.FinalTime;
                        Debug.WriteLine($"{nameof(Services.PathSolver)}: Breaking, no collisions found");
                        break; //No collisions, stop iterating
                    }


                    //Get the first envelope...
                    var testEnv = currentEnvCollection.FirstOrDefault(x =>

                    !StudiedVias.Contains(x.CollidingVias.First()) //Not starting with the same via as a previous envelope (avoids infinite loops)
                    && (x.CollidingVias.Any(y => InputVias.Contains(y)) //Must include an active via, or at least be a weld bordering one
                    || x.CollidingVias.FirstOrDefault() is TxWeldLocationOperation && x.ExtendBy(1, 1).Any(y => InputVias.Contains(y))))
                    ;
                    if (testEnv == null) break;


                    envelopeGetter.paused = true;
                    PathSolveOnEnvelopeDuplicateOperation(InputVias, testEnv);

                    envelopeGetter.paused = false;
                    StudiedVias.Add(testEnv.CollidingVias.First());

                    RefreshDisplay();


                    tracker.SimPlayer = new TxSimulationPlayer(false, true);
                    tracker.SimPlayer.SetOperation(op);
                    //if (IsCancelRequested) break;
                }
            }
        }


        #region DuplicateSetup
        private void PathSolveOnEnvelopeDuplicateOperation(IEnumerable<LocOp> InputVias, CollisionEnvelope testEnv)
        {

            using (var duplicate = new EnvelopeDuplicate(testEnv, op)) {

                var firstVia = duplicate.EnvCopyOperation.Vias().First() as ITxRoboticLocationOperation;
                if (firstVia != op.Vias().First()) new PlayToVia().Run(firstVia, tracker.SimPlayer);

                var newParams = new SolverParams()
                {
                    Operation = duplicate.EnvCopyOperation,
                    tokenSource = solverParams.tokenSource,
                    token = solverParams.token,
                    IgnoreExistingNearMisses = solverParams.IgnoreExistingNearMisses,
                    IgnoreExistingCollisions = solverParams.IgnoreExistingCollisions,
                    ResetToMax = solverParams.ResetToMax,
                    acceptedClearances = solverParams.acceptedClearances.GetEnvelopeDuplicateClone(duplicate),
                    thisEnvelopeDuplicate = duplicate,
                    NearMissDistance = solverParams.NearMissDistance,
                };


                GetViaRolesForDuplicate(InputVias, testEnv, duplicate, newParams);


                var solver = new PathSolver(tracker, newParams);


                solver.Run(solver.solverParams.ActiveVias, false);
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
            duplicateActiveVias = duplicateActiveVias.Append(testEnv.CollidingVias.First()).Append(testEnv.CollidingVias.Last()).Distinct();

            duplicateActiveVias = duplicateActiveVias.Intersect(inputActiveVias);

            duplicateActiveVias = duplicateActiveVias.Select(x => duplicate.OriginalToCopy(x)).OfType<LocOp>();


            duplicateSolverParams.ActiveVias = duplicateActiveVias.ToHashSet();
        }

        #endregion


        private void TecnomatixStatics_SimulationError(ITxOperation obj)
        {
            Debug.WriteLine($"Simulation error on {obj.Name}, cancelling optimisation");
            solverParams.tokenSource?.Cancel();
            errorEncountered = true;
        }
        #endregion

    }
}

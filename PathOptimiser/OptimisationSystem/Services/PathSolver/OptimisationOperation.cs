using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PathOptimiser.DisplayGrid.OperationsList;
using PathOptimiser.Models;
using PathOptimiser.OptimisationSystem;
using PathOptimiser.OptimisationSystem.Models;
using PathOptimiser.OptimisationSystem.Services;
using PathOptimiser.OptimisationSystem.Services.EnvelopeRecorders;
using Tecnomatix.Engineering;
using static Tecnomatix.Engineering.TxApplication;
using CompoundOp = Tecnomatix.Engineering.ITxRoboticOrderedCompoundOperation;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.PathSolverStatics;

namespace PathOptimiser.OptimisationSystem.Services.PathSolver
{
    public class OptimisationOperation
    {

        #region Data
        SimPlayerTracker tracker;

        //public SolverParams solverParams = new SolverParams();
        //public HashSet<LocOp> ActiveVias => solverParams.ActiveVias;


        EnvelopeCollection FinalEnvelopeCollection;
        #endregion
        #region Init
        public OptimisationOperation(SimPlayerTracker tracker)
        {
            this.tracker = tracker;
        }
        #endregion


        #region Iteration

        public void RunMain(IEnumerable<LocOp> IncludedVias, Dictionary<CompoundOp, CollectionOperationsDisplay.OperationData> OperationData)
        {
            DeviceResetter.Store();
            var operations = IncludedVias.GroupBy(x => x.Collection as CompoundOp);
            var results = new List<Result>();

            foreach (var pair in operations) {

                var newParams = new SolverParams() {
                    ActiveVias = pair.ToHashSet(),
                    Operation = pair.Key as CompoundOp
                };
                newParams.LoadData(OperationData[pair.Key]);


                var tokenCreator = new CancellationTokenSource();

                var envSolver = new EnvelopeSolver(tracker, newParams);
                var result = envSolver.OptimiseWithSubEnvelopes(pair, tokenCreator.Token);

                results.Add(result);
                ReportOperationFinished(new OperationOptimisedReport() { Operation = pair.Key, Success = result, originalTime = envSolver.OriginalTime, optimisedTime = envSolver.OptimisedTime, finalEnvelopeCollection = FinalEnvelopeCollection });
                DeviceResetter.Reload();
            }

            Done?.Invoke(
                results.Any(x => x == Result.Cancelled) ? Result.Cancelled :
                results.All(x => x == Result.Success) ? Result.Success : Result.CouldNotComplete);
        }

        #endregion

        #region HelperMethods
        //private void SetInitialViaSpeeds(CompoundOp op, bool setMaxSpeeds)
        //{
        //    if (setMaxSpeeds) {
        //        foreach (var item in ActiveVias) {
        //            if (item.IsFine() == false) ApplyAdjustment(item.MaxSpeedAndCnt());
        //            else {
        //                ApplyAdjustment(new ViaAdjustment(item, new ViaParameters() { Speed = item.MaxSpeedAndCnt().newParams.Speed, CNT = item.GetCNT() }, false));
        //            }
        //        }

        //        Debug.WriteLine("Max speeds set");
        //    }
        //}


        //void DebugLog()
        //{
        //    foreach (var item in ActiveVias) {
        //        Debug.Write(new ViaAdjustment(item, new ViaParameters(item), false).ToString() + " ");
        //    }
        //    Debug.WriteLine("");
        //}


        #endregion



        //public static bool IsCancelRequested
        //{
        //    get => CancelRequested;
        //    set {
        //        if (CancelRequested == value) return;
        //        CancelRequested = value;
        //        CancelRequestedChanged?.Invoke(null, value);
        //    }
        //}

        #region Events

        public event Action<Result> Done;

        #endregion

        public enum Result
        {
            Success,
            Cancelled,
            RobotControllerFailed,
            CouldNotComplete
        }

        public class OperationOptimisedReport
        {
            public ITxRoboticOrderedCompoundOperation Operation;
            public Result Success;
            public double originalTime;
            public double optimisedTime;

            public EnvelopeCollection finalEnvelopeCollection;

        }

    }
}

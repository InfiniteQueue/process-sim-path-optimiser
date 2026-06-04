using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PathOptimiser.OptimisationSystem.Models;
using Tecnomatix.Engineering;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.OptimisationOperation;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;

namespace PathOptimiser.OptimisationSystem.Services.PathSolver
{
    public static class PathSolverStatics
    {

        public static void ApplyAdjustment(SolverParams solverParams, ViaAdjustment bestAdjustment, EnvelopeSolver.BestAdjustmentFinder finder = null)
        {
            if (finder != null) {
                Debug.WriteLine($"\nApplied {finder.bestAdjustment}, \nscore: {finder.bestEnvCollection.totalPenalty}, time: {finder.bestEnvCollection.FinalTime}, intersecting frames: {finder.bestEnvCollection.Envelopes.Sum(x => x.CollidingFrameCount)}");
            }


            bestAdjustment.Apply();

            var originalVia = solverParams.thisEnvelopeDuplicate != null ? solverParams.thisEnvelopeDuplicate.DuplicateToOriginal[bestAdjustment.Via] as LocOp : bestAdjustment.Via;

            AdjustmentApplied?.Invoke(new ViaAdjustment(originalVia, bestAdjustment.newParams, false));
        }


        public static void RequestCancel()
        {
            CancelRequestedChanged?.Invoke(null, true);
        }

        public static void ReportSimError(SolverParams param)
        {
            if (param.thisEnvelopeDuplicate is PathSolver.EnvelopeDuplicate duplicate) {
                SimErrorReported?.Invoke(duplicate.OriginalOperation);
            }
            else {
                SimErrorReported?.Invoke(param.Operation);
            }
        }

        public static Action<ITxRoboticOrderedCompoundOperation> SimErrorReported;

        public static void ApplyAdjustment(ViaAdjustment adjustment)
        {
            adjustment.Apply();
            AdjustmentApplied?.Invoke(adjustment);
        }
        public static void ReportOperationFinished(OperationOptimisedReport report) => OperationFinished?.Invoke(report);
        public static void ReportOriginalClearancesFound(SolverParams.AcceptedClearancesCollection clearances) => OriginalClearancesFound?.Invoke(clearances);

        public static event EventHandler<bool> CancelRequestedChanged;
        public static event Action<ViaAdjustment> AdjustmentApplied;
        public static event Action<OperationOptimisedReport> OperationFinished;
        public static event Action<SolverParams.AcceptedClearancesCollection> OriginalClearancesFound;

    }
}

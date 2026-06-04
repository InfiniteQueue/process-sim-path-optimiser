using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PathOptimiser.OptimisationSystem.Models;
using PathOptimiser.OptimisationSystem.Services.EnvelopeRecorders;
using Tecnomatix.Engineering;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.PathSolver;
using static Tecnomatix.Engineering.TxApplication;
using CompoundOp = Tecnomatix.Engineering.ITxRoboticOrderedCompoundOperation;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;
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

        public double OriginalTime;
        public double OptimisedTime;
        public EnvelopeCollection FinalEnvelopeCollection;


        public void Run(IEnumerable<LocOp> InputVias, bool resetToMaxSpeed = true)
        {
            InputVias = InputVias.Where(x => x is TxWeldLocationOperation == false);
            this.solverParams.ActiveVias = new HashSet<LocOp>(InputVias);


            if (this.ActiveVias.Count == 0) { Debug.WriteLine($"{this}: No operation set"); return; }

            tracker.SimPlayer.AskUserForReset(false);
            tracker.SimPlayer.SetOperation((ITxOperation)this.ActiveVias.First().Collection);
            tracker.SimPlayer.AskUserForReset(false);
            ActiveDocument.CurrentOperation = (ITxOperation)this.ActiveVias.First().Collection;

            var op = ActiveDocument.CurrentOperation as CompoundOp;

            SetInitialViaSpeeds(solverParams, resetToMaxSpeed);


            ClearSingleOperation();

            tracker.SimPlayer?.Stop();
            Debug.WriteLine($"Envelope Solved");
        }


        private void ClearSingleOperation()
        {
            using (var envelopeRecorder = new EnvelopeRecorder(tracker, ActiveDocument.CurrentOperation, solverParams) { SimPlayerTracker = tracker }) {

                var remainingEnvelopes = envelopeRecorder.Run(tracker);

                while (remainingEnvelopes.Envelopes.Count > 0) {

                    if (remainingEnvelopes.totalPenalty == 0) { Debug.WriteLine($"{nameof(OptimisationOperation)}: Breaking, no collisions found"); break; }

                    var finder = new BestAdjustmentFinder(envelopeRecorder, remainingEnvelopes);
                    finder.Run();


                    if (finder.bestAdjustment is ViaAdjustment adjustment == false) {
                        Debug.WriteLine("No possible adjustments remaining"); break;
                    }

                    else {

                        var bestAdjustment = finder.bestAdjustment.Value;


                        ApplyAdjustment(solverParams, bestAdjustment, finder);


                        if (bestAdjustment.AdjustmentType.HasFlag(ViaAdjustment.AdjustmentTypes.Speed) && bestAdjustment.original.Speed == bestAdjustment.Via.MaxSpeedAndCnt().newParams.Speed) {
                            DoSpeedSkip(finder.bestEnvCollection, bestAdjustment.Via);
                        }

                        foreach (var via in ActiveVias) Debug.Write($"{new ViaAdjustment(via, new ViaParameters(via), false)} ");
                        Debug.WriteLine(" ");


                        remainingEnvelopes = finder.bestEnvCollection;

                    }

                    //if (IsCancelRequested) break;
                }

                OptimisedTime = remainingEnvelopes.FinalTime;
                FinalEnvelopeCollection = remainingEnvelopes;


                if (remainingEnvelopes.Envelopes.Count == 0) Debug.WriteLine("No collisions found");
            }
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

    }
}

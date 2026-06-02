using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PathOptimiser.OptimisationSystem;
using PathOptimiser.OptimisationSystem.Models;
using Tecnomatix.Engineering;

namespace PathOptimiser
{
    public partial class PathSolver
    {
        private class BestAdjustmentFinder
        {

            public EnvelopeRecorder envelopeRecorder { get; }
            public SimPlayerTracker tracker => envelopeRecorder?.SimPlayerTracker;
            public SolverParams solverParams => envelopeRecorder.Data.solverParams;

            public BestAdjustmentFinder(EnvelopeRecorder envelopeGetter,  EnvelopeCollection currentEnvCollection)
            {
                this.envelopeRecorder = envelopeGetter;
                this.currentEnvCollection = currentEnvCollection;
                this.bestEnvCollection = currentEnvCollection;
            }

            public void Run()
            {
                var envCollections = new Dictionary<ViaAdjustment, EnvelopeCollection>();

                Debug.WriteLine($"Considering adjustments from {solverParams.ActiveVias.FirstOrDefault()?.Name} to {solverParams.ActiveVias.LastOrDefault()?.Name}");

                var adjustments =  new AdjustmentGenerator(solverParams.ActiveVias).GetAllStandardAdjustments();

                ConsiderAdjustments(envCollections, adjustments);




                var improvingAdjustments = envCollections.Where(x => x.Value.totalPenalty < currentEnvCollection.totalPenalty);
                var worseningAdjustments = envCollections.Except(improvingAdjustments);

                KeyValuePair<ViaAdjustment, EnvelopeCollection>? bestAdjustmentPair = null;

                if (improvingAdjustments.Count() > 0) {
                    bestAdjustmentPair = improvingAdjustments.OrderByDescending(x => currentEnvCollection.GetRelativeScore(x.Value)).First();
                }
                else {
                    if (worseningAdjustments.Count() > 0) {
                        Debug.WriteLine("Forced score increase");
                        bestAdjustmentPair = worseningAdjustments.OrderBy(x => x.Value.totalPenalty).Last();
                        //Pick the adjustment that changes the collisions the most
                        //It's very rare that all adjustments increase the collision score. Picking the highest at least ensures that meaningful changes are being made
                    }
                }

                if (bestAdjustmentPair != null) {
                    bestRelativeScore = currentEnvCollection.GetRelativeScore(bestAdjustmentPair.Value.Value);
                    bestEnvCollection = bestAdjustmentPair.Value.Value;
                }

                this.bestAdjustment = bestAdjustmentPair?.Key ?? null;
            }

            private void ConsiderAdjustments(Dictionary<ViaAdjustment, EnvelopeCollection> envCollections, IEnumerable<ViaAdjustment> adjustments)
            {
                foreach (var adjustment in adjustments) {

                    if (!solverParams.ActiveVias.Contains(adjustment.Via)) { continue; } //We're not editing this via
                    if (consideredAdjustments.Contains(adjustment)) { Debug.WriteLine("Repeat adjustment skipped"); continue; } //Already considered this adjustment, so skip


                    adjustment.Apply();

                    var newEnvCollection = envelopeRecorder.Run(tracker);
                    envCollections.Add(adjustment, newEnvCollection);

                    adjustment.Reset();


                    consideredAdjustments.Add(adjustment);

                    if (bestRelativeScore == double.PositiveInfinity) break;

                    if (IsCancelRequested) break;
                }
            }

            public event EventHandler<EnvelopeCollection> BestAdjustmentFound;
            HashSet<ViaAdjustment> consideredAdjustments = new HashSet<ViaAdjustment>();
            public EnvelopeCollection bestEnvCollection;
            EnvelopeCollection currentEnvCollection;
            public double bestRelativeScore = double.NegativeInfinity;
            public ViaAdjustment? bestAdjustment = null;



            private class AdjustmentGenerator
            {
                public IEnumerable<ITxRoboticLocationOperation> Vias { get; }
                int multiplier { get; }
                public AdjustmentGenerator(IEnumerable<ITxRoboticLocationOperation> vias, int multiplier = 1)
                {
                    this.Vias = vias;
                    this.multiplier = multiplier;
                }

                public IEnumerable<ViaAdjustment> GetAllStandardAdjustments()
                {
                    foreach (var item in GetCntAdjustments().Concat(GetSpeedAdjustments())) yield return item;
                }


                public IEnumerable<ViaAdjustment> GetCntAdjustments()
                {
                    foreach (var via in Vias) {
                        var targetCNT = via.GetCNT() - 10 * multiplier;
                        if (targetCNT >= 0) {
                            yield return new ViaAdjustment(via, new ViaParameters() { CNT = targetCNT, Speed = via.GetSpeed() }, false);
                        }
                    }
                }

                public IEnumerable<ViaAdjustment> GetSpeedAdjustments()
                {
                    foreach (var via in Vias) {
                        if (via.StandardSpeedStepDown(multiplier) is ViaAdjustment speedAdjust) yield return speedAdjust;
                    }
                }


            }




        }
    }
}

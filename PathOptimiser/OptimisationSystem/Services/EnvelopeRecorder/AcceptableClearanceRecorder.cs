using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PathOptimiser.OptimisationSystem;
using PathOptimiser.OptimisationSystem.Models;
using Tecnomatix.Engineering;
using static PathOptimiser.TecnomatixStatics;
using static Tecnomatix.Engineering.TxApplication;

namespace PathOptimiser.OptimisationSystem.Services.EnvelopeRecorder
{
    internal class AcceptableClearanceRecorder : PathOptimiser.EnvelopeRecorder
    {
        SolverParams solverParams;

        public AcceptableClearanceRecorder(SimPlayerTracker tracker, ITxOperation operation, SolverParams solverParams) : base(tracker, operation, solverParams)
        {
            this.solverParams = solverParams;
            Data.CurrentVia = (operation as ITxOrderedCompoundOperation).GetChildAt(0) as ITxRoboticLocationOperation;
        }

        protected override void TimeIntervalReached(object sender, TxSimulationPlayer_TimeIntervalReachedEventArgs args)
        {
            if (paused) return;

            Data.Motion.UpdateMotionData();


            var collisionHandler = new CollisionFinder(Data);

            TxCollisionQueryResults results = collisionHandler.GetTxCollisionResults(ActiveDocument.CollisionRoot);


            Data.GetPathVias(out var movingFrom, out var movingTo);
            if (results.States.Count > 0 && collisionHandler.CollisionFound(results)) solverParams.acceptedClearances.Record(results.MinDistance(), movingFrom);
        }

        public override EnvelopeCollection Run(SimPlayerTracker tracker)
        {
            Debug.WriteLine("Get Acceptable Clearances");

            var result = base.Run(tracker);
            Debug.WriteLine($"{solverParams.acceptedClearances.Count} clearances found");

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecnomatix.Engineering;

namespace PathOptimiser.OptimisationSystem.Services
{
    internal class ReadyToRunChecks
    {
        public enum Failure
        {
            CollisionDetected
        }

        public static Dictionary<Failure, string> FailureStrings = new Dictionary<Failure, string>()
        {
            { Failure.CollisionDetected, "Ensure there are no collisions before starting" }
        };

        public static Failure? FindIssues()
        {
            return CollisionsFound() ?? null;
        }

        public static Failure? CollisionsFound()
        {
            var colRoot = TxApplication.ActiveDocument.CollisionRoot;

            return MyDispatchers.TxDispatcher.Invoke<Failure?>(() =>
            {

                colRoot.CheckNearMiss = true;
                colRoot.ReportLevel = TxCollisionQueryParams.TxCollisionReportLevel.ComponentLevel;
                //colRoot.CheckCollisions = true;
                var results = colRoot.GetCollidingObjectsAndDistances(new TxCollisionAndDistancesQueryParams() { UseNearMiss = true, NearMissDistance = EnvelopeRecordingData.DefaultCollisionDistance, Mode = TxCollisionQueryParams.TxCollisionQueryMode.DefinedPairs, FindPenetrationRegions = false });

                if (results.MinDistance() < EnvelopeRecordingData.DefaultCollisionDistance) return Failure.CollisionDetected;
                else return null;
            });
            
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PathOptimiser.DisplayGrid.OperationsList;
using PathOptimiser.OptimisationSystem.Services.PathSolver;
using Tecnomatix.Engineering;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.OptimisationOperation;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.PathSolver;
using static PathOptimiser.TecnomatixStatics;
using System.Security.Policy;
using System.Threading;

namespace PathOptimiser.OptimisationSystem.Models
{
    public class SolverParams
    {
        public SolverParams() { 
            this.acceptedClearances = new AcceptedClearancesCollection(this);
            token = tokenSource.Token;
        }

        public ITxRoboticOrderedCompoundOperation Operation { get; set; }

        public CancellationTokenSource tokenSource { get; set; } = new CancellationTokenSource();
        public CancellationToken token { get; set; }

        public bool IgnoreExistingNearMisses { get; set; } = true;
        public bool IgnoreExistingCollisions { get; set; } = true;

        public bool ResetToMax { get; set; } = true;


        /// <summary> Vias to be considered for adjustment </summary>
        public HashSet<ITxRoboticLocationOperation> ActiveVias { get; set; } = new HashSet<ITxRoboticLocationOperation>();

        /// <summary> Vias explicitly set to have their collisions ignored </summary>
        public HashSet<ITxRoboticLocationOperation> IgnoredVias { get; set; } = new HashSet<ITxRoboticLocationOperation>();

        public AcceptedClearancesCollection acceptedClearances;

        public EnvelopeDuplicate thisEnvelopeDuplicate;

        public double NearMissDistance;


        public void LoadData(CollectionOperationsDisplay.OperationData data)
        {
            NearMissDistance = double.TryParse(data.Clearance, out var result) ? result : 5;
            IgnoreExistingNearMisses = data.IgnoreNearMisses;
            IgnoreExistingCollisions = data.IgnoreCollisions;
            ResetToMax = data.SetToMax;
            Operation = data.compoundOp as ITxRoboticOrderedCompoundOperation;
        }

        public class AcceptedClearancesCollection : Dictionary<ITxLocationOperation,  AcceptedClearance>
        {
            public AcceptedClearancesCollection(SolverParams solverParams)
            {
                this.SolverParams = solverParams;
            }

            SolverParams SolverParams { get; set; }

            /// <summary> 
            /// Note that 'prevVia' here is not the same as the 'CurrentVia' property in the EnvelopeRecordingData.
            /// CurrentVia considers the 'closest' current via, while prevVia is the last via that was 'passed' by the robot
            /// </summary>
            public void Record(double clearance, ITxLocationOperation prevVia)
            {
                if (clearance > SolverParams.NearMissDistance) return;
                if (prevVia == null) return;
                if (!ContainsKey(prevVia)) this[prevVia] = new AcceptedClearance() { PrevVia = prevVia, AcceptableClearance = clearance, NextVia = (prevVia.Collection as ITxOrderedCompoundOperation).AtIndex(prevVia.Index() + 1) };
                else {
                    if (clearance < this[prevVia].AcceptableClearance) this[prevVia].AcceptableClearance = clearance;
                }
            }

            public AcceptedClearancesCollection GetEnvelopeDuplicateClone(EnvelopeDuplicate duplicate)
            {
                var newCollection = new AcceptedClearancesCollection(SolverParams);
                foreach(var item in this.Keys) {
                    if (duplicate.OriginalToCopy( item ) is ITxLocationOperation newPrevVia
                        && duplicate.OriginalToCopy(this[item].NextVia ) is ITxLocationOperation newNextVia) {
                        newCollection.Add(newPrevVia, new AcceptedClearance() { PrevVia = newPrevVia, NextVia = newNextVia, AcceptableClearance = this[item].AcceptableClearance });
                    }
                }
                return newCollection;
            }
        }

        public class AcceptedClearance
        {
            public ITxLocationOperation PrevVia;
            public ITxLocationOperation NextVia;

            public double AcceptableClearance { get; set; }


        }
    }
}

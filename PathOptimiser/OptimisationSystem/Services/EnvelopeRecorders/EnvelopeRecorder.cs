using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PathOptimiser.OptimisationSystem;
using PathOptimiser.OptimisationSystem.Models;
using Tecnomatix.Engineering;
using static PathOptimiser.TecnomatixStatics;
using static Tecnomatix.Engineering.TxApplication;

namespace PathOptimiser.OptimisationSystem.Services.EnvelopeRecorders
{
    public class EnvelopeRecorder : IDisposable
    {
        #region Data
        public SimPlayerTracker SimPlayerTracker;

        public EnvelopeRecordingData Data;

        public ITxOperation Operation => Data?.Operation;

        #endregion

        #region Properties
        TxSimulationPlayer _simPlayer;
        public TxSimulationPlayer SimPlayer
        {
            get => _simPlayer;

            set {
                if (_simPlayer != null) {
                    _simPlayer.TimeIntervalReached -= TimeIntervalReached;
                    _simPlayer.OperationStartedForward -= _simPlayer_OperationStartedForward;
                    _simPlayer.OperationEndedForward -= _simPlayer_OperationEndedForward;
                }
                _simPlayer = value;
                if (Data != null) Data.SimPlayer = value;
                if (_simPlayer != null) {
                    _simPlayer.TimeIntervalReached += TimeIntervalReached;
                    _simPlayer.OperationStartedForward += _simPlayer_OperationStartedForward;
                    _simPlayer.OperationEndedForward += _simPlayer_OperationEndedForward;
                }
            }
        }

        private void _simPlayer_OperationEndedForward(object sender, TxSimulationPlayer_OperationEndedForwardEventArgs args)
        {
            Data.SimulatingOperations.Remove(args.Operation);
        }

        private void _simPlayer_OperationStartedForward(object sender, TxSimulationPlayer_OperationStartedForwardEventArgs args)
        {
            Data.SimulatingOperations.Add(args.Operation);
        }

        #endregion


        public bool paused = false;

        public EnvelopeRecorder(SimPlayerTracker tracker, ITxOperation operation, SolverParams solverParams)
        {
            this.Data = new EnvelopeRecordingData(operation, solverParams);

            this.SimPlayerTracker = tracker;
            this.SimPlayer = tracker.SimPlayer;
            SimPlayerTracker.SimPlayerChanged += CurrentSimplayerChanged;

            Data.CurrentVia = (operation as ITxOrderedCompoundOperation).GetChildAt(0) as ITxRoboticLocationOperation;
        }



        public virtual EnvelopeCollection Run(SimPlayerTracker tracker)
        {
            Data.RecordedEnvelopes = new EnvelopeCollection(Operation as ITxCompoundOperation);
            if (Data.Operation == null) {
                Debug.WriteLine($"{nameof(EnvelopeRecorder)} No operation set");
                return Data.RecordedEnvelopes;
            }

            Data.CurrentVia = (Data.Operation as ITxOrderedCompoundOperation).GetChildAt(0) as ITxRoboticLocationOperation;
            var robot = Data.Operation.SimulatedObjects.OfType<TxRobot>().First();

            try { robot.CurrentPose = robot.GetPoseAtLocation(Data.CurrentVia); }
            catch { }

            if (SimPlayer == null) SimPlayer = new TxSimulationPlayer(false, true);

            TecnomatixStatics.SafePlaySim(SimPlayer, Data.Operation, true, Data);

            Dispose();

            //foreach (var envelope in Data.RecordedEnvelopes.ToArray()) {
            //    if (envelope.ExtendBy(1, 1).All(x => !Data.ActiveVias.Contains(x))) Data.RecordedEnvelopes.Remove(envelope);
            //}

            CollectionGot?.Invoke(this, Data.RecordedEnvelopes);

            return Data.RecordedEnvelopes;
        }

        

        protected virtual void TimeIntervalReached(object sender, TxSimulationPlayer_TimeIntervalReachedEventArgs args)
        {
            if (paused) return;
            Data.MotionCalc.UpdateMotionData();

            new CollisionFinder(Data).RecordCollisionState();

            ;
        }

        public void Dispose()
        {
            SimPlayerTracker.SimPlayerChanged -= CurrentSimplayerChanged;
            Data.CurrentEnvelope = null;
            SimPlayer = null; //Clears the time interval reached events
        }

        protected class CollisionFinder
        {
            EnvelopeRecordingData data;
            public CollisionFinder(EnvelopeRecordingData data)
            {
                this.data = data;
            }

            public virtual void RecordCollisionState()
            {
                TxCollisionQueryResults results = GetTxCollisionResults(ActiveDocument.CollisionRoot);

                if (!CollisionFound(results)) {
                    data.CurrentEnvelope = null;
                    return;
                }

                else {
                    if (data.CurrentEnvelope == null) data.CurrentEnvelope = new CollisionEnvelope();

                    data.CurrentEnvelope?.RecordCollisionFrame(data, results.MinDistance());
                }
            }

            public bool CollisionFound(TxCollisionQueryResults results)
            {
                //Debug.WriteLine(data.CurrentVia.Name);


                if (!results.CollisionDetected()) return false;

                if (!(data.CurrentVia is TxWeldLocationOperation) && !data.ActiveVias.Contains(data.CurrentVia)) return false;

                if (WeldWedgeCase(data)) return false;

                if (data.solverParams.IgnoredVias?.Contains(data.CurrentVia) == true) return false;

                if (data.Welds.Any(x => (x.AbsoluteLocation.Translation - data.MotionCalc.CurrentEffectiveLocation).Magnitude() < EnvelopeRecordingData.WeldIgnoreRadius)) return false;

                if (results.MinDistance() >= data.GetCurrentPermissibleClearance()) return false;


                return true;
            }

            /// <summary> Ignore collisions that cannot possibly be fixed, between a weld and a via that cannot be adjusted </summary>
            private bool WeldWedgeCase(EnvelopeRecordingData data)
            {
                data.GetPathVias(out var from, out var to);
                var bothVias = new ITxRoboticLocationOperation[] { from, to };
                return (bothVias.Any(x => x is TxWeldLocationOperation) && bothVias.Any(x => x is TxWeldLocationOperation == false && data.ActiveVias.Contains(x) == false));
            }

            public TxCollisionQueryResults GetTxCollisionResults(TxCollisionRoot colRoot)
            {
                colRoot.CheckNearMiss = true;
                colRoot.ReportLevel = TxCollisionQueryParams.TxCollisionReportLevel.ComponentLevel;
                colRoot.CheckCollisions = true;
                var results = colRoot.GetCollidingObjectsAndDistances(new TxCollisionAndDistancesQueryParams() { UseNearMiss = true, NearMissDistance = data.solverParams.NearMissDistance, Mode = TxCollisionQueryParams.TxCollisionQueryMode.DefinedPairs, FindPenetrationRegions = false });
                return results;
            }

        }

        #region Events and Handlers
        public static event EventHandler<EnvelopeCollection> CollectionGot;


        private void CurrentSimplayerChanged(TxSimulationPlayer oldPlayer, TxSimulationPlayer newPlayer) => SimPlayer = newPlayer;

        #endregion
    }

    //SW_WAIT_TIME
    //private void SafePlaySim()
    //{
    //    if (SimPlayer == null) SimPlayer = new TxSimulationPlayer(false, true);


    //    if (ActiveDocument.CurrentOperation != Data.Operation) {
    //        SimPlayer.AskUserForReset(false);
    //        ActiveDocument.CurrentOperation = Data.Operation;
    //    }

    //    //A lot of askuserforreset(false) here, just to be safe. Reset prompts would halt the entire program

    //    SimPlayer.AskUserForReset(false);
    //    SimPlayer.SetOperation(Data.Operation);
    //    SimPlayer.Pause();
    //    SimPlayer.Stop();
    //    SimPlayer.AskUserForReset(false);
    //    SimPlayer.Rewind();

    //    Data.Motion.Reset();

    //    var debugTime = DateTime.Now;
    //    SimPlayer.AskUserForReset(false);
    //    SimPlayer.PlayWithoutRefresh();

    //    Data.RecordedEnvelopes.FinalTime = SimPlayer.CurrentTime;

    //    SimPlayer.AskUserForReset(false);
    //    SimPlayer.Rewind();

    //    var errors = SimPlayer.GetErrorsAndTraces().Where(x => x.Contains("[Error]"));
    //    if (errors.Count() > 0) {
    //        Debug.WriteLine(string.Join("\n", errors));
    //        PathSolver.IsCancelRequested = true;
    //    }
    //}
}

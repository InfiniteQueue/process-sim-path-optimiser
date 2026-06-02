using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Markup;
using PathOptimiser.OptimisationSystem.Models;
using Tecnomatix.Engineering;
using static PathOptimiser.TecnomatixStatics;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;

namespace PathOptimiser.OptimisationSystem
{
    public class EnvelopeRecordingData
    {
        public EnvelopeRecordingData(ITxOperation operation, SolverParams solverParams)
        {
            Operation = operation;
            Welds = (operation as ITxOrderedCompoundOperation)
                .GetDirectDescendants(new TxTypeFilter(typeof(TxWeldLocationOperation)))
                .OfType<TxWeldLocationOperation>().ToArray();
            Robot = operation.SimulatedObjects.OfType<ITxRobot>().FirstOrDefault();
            this.solverParams = solverParams;

            MotionCalc =  new MotionData(this);
        }

        #region InitialData
        public ITxOperation Operation { get; }
        public ITxRobot Robot{get;}
        public TxWeldLocationOperation[] Welds{get;}
        public SolverParams solverParams{get;}
        #endregion

        #region Getters
        #region Constants
        public const double DefaultCollisionDistance = 5;
        public const double WeldIgnoreRadius = 10;
        #endregion
        public HashSet<LocOp> ActiveVias => solverParams.ActiveVias;
        #endregion

        #region WorkingData
        public List<ITxOperation> SimulatingOperations = new List<ITxOperation>();

        public EnvelopeCollection RecordedEnvelopes = null;


        public TxSimulationPlayer SimPlayer { get; set; }

        LocOp _currentVia;
        public LocOp CurrentVia
        {
            get => _currentVia;
            set {
                if (_currentVia == value) return;
                _currentVia = value;
                //Debug.WriteLine(_currentVia?.Name);
            }
        }

        CollisionEnvelope _currentEnvelope;
        public CollisionEnvelope CurrentEnvelope
        {
            get => _currentEnvelope;

            set {
                if (_currentEnvelope == value) return;
                _currentEnvelope = value;

                if (value != null) {
                    if (!RecordedEnvelopes.Contains(value)) RecordedEnvelopes.Add(value);
                }
            }
        }
        #endregion

        public MotionData MotionCalc;

        //Todo: call this MotionCalculator. Make motion data a separate class
        public class MotionData
        {
            EnvelopeRecordingData data;
            public MotionData(EnvelopeRecordingData data)
            {
                this.data = data;
            }

            #region data
            public TxVector CurrentTcpfLocation => data.Robot.TCPF.AbsoluteLocation.Translation;
            public TxVector PrevTcpfLocation;
            public TxVector PrevViaLocation;
            TxVector _prevViaLocationBuffer;
            TxVector _prevTcpfLocationBuffer;
            private LocOp PrevFrameVia;
            LocOp _prevFrameViaBuffer;
            private double? prevFrameTime;
            private double? _prevFrameTimeBuffer;

            private double? speed;
            public double? Speed => speed;
            #endregion

            public void UpdateMotionData()
            {
                PrevFrameVia = _prevFrameViaBuffer;
                PrevViaLocation = _prevViaLocationBuffer;
                prevFrameTime = _prevFrameTimeBuffer;
                PrevTcpfLocation = _prevTcpfLocationBuffer;

                if (PrevFrameVia != null && PrevViaLocation != null && prevFrameTime != null) {
                    var speedByTcpf = (CurrentTcpfLocation - PrevTcpfLocation).Magnitude(); 
                    var speedByVia = ((PrevFrameVia as ITxLocatableObject).AbsoluteLocation.Translation - PrevViaLocation).Magnitude();
                    var dist = Math.Max(speedByTcpf, speedByVia); //This should differ based on whether the via is rtcp. Checking the max value saves having to read this out
                    speed = dist / deltaTime;
                }

                if (SimulatedViaIndex == null || SimulatedViaIndex >= data.CurrentVia.Index()) {
                    if (IsCloserToNextVia && IsMovingFromCurrentVia) data.CurrentVia = data.GetNextVia;
                }

                _prevFrameViaBuffer = data.CurrentVia;
                _prevViaLocationBuffer = new TxVector((data.CurrentVia as ITxLocatableObject).AbsoluteLocation.Translation);
                _prevTcpfLocationBuffer = new TxVector(CurrentTcpfLocation);
                _prevFrameTimeBuffer = data.SimPlayer.CurrentTime;
            }


            public void Reset()
            {
                PrevViaLocation = null;
                PrevFrameVia = null;
                speed = null;
            }


            public bool IsCloserToNextVia
            {
                get {
                    if (!(data.CurrentVia != null && CurrentTcpfLocation != null && data.GetNextVia is LocOp nextVia)) return false;

                    var currentRobotPosition = data.CurrentVia?.GetParameter("MOUNTED_WORKPIECE_FRAME_NAME") is TxRoboticTxObjectParam parm && parm.Value is TxFrame frame ? frame.AbsoluteLocation.Translation : data.Robot.TCPF.AbsoluteLocation.Translation;

            return ((nextVia as ITxLocatableObject).AbsoluteLocation.Translation - currentRobotPosition).Magnitude() < ((data.CurrentVia as 
ITxLocatableObject).AbsoluteLocation.Translation - currentRobotPosition).Magnitude();
                }
            }

            public bool IsMovingFromCurrentVia
            {
                get {
                    //Welds can introduce odd movements that interfere with this algorithm
                    if (data.SimulatingOperations.Any(x => x is TxWeldLocationOperation && x == data.CurrentVia)) return false;

                    if (PrevFrameVia == null) return false;


                    if (data.CurrentVia?.GetParameter("MOUNTED_WORKPIECE_FRAME_NAME") is TxRoboticTxObjectParam parm && parm.Value is TxFrame frame) {
                        if (PrevViaLocation == null) return false;
                        //If RTCP, the via changes location each frame
                        var distanceFromViaPrevLocation = (frame.AbsoluteLocation.Translation - PrevViaLocation).Magnitude();
                        double distanceFromViaCurrentLocation = (frame.AbsoluteLocation.Translation - (PrevFrameVia as ITxLocatableObject).AbsoluteLocation.Translation).Magnitude();

                        return distanceFromViaPrevLocation < distanceFromViaCurrentLocation;
                    }

                    else {
                        //Else, use the movement of the TCPF to judge relative motion
                        return PrevTcpfLocation != null && (CurrentTcpfLocation - (data.CurrentVia as ITxLocatableObject).AbsoluteLocation.Translation).Magnitude() > (PrevTcpfLocation - (data.CurrentVia as ITxLocatableObject).AbsoluteLocation.Translation).Magnitude();
                    }
                }
            }

            public int? SimulatedViaIndex => data.SimulatingOperations.OfType<ITxRoboticLocationOperation>().FirstOrDefault(x => (data.Operation as ITxRoboticOrderedCompoundOperation)?.Vias().Contains(x) == true)?.Index();

            private double? deltaTime => prevFrameTime == null ? null : data.SimPlayer.CurrentTime - prevFrameTime;

            public TxVector CurrentEffectiveLocation
            {
                get {
                    if (data.CurrentVia?.GetParameter("MOUNTED_WORKPIECE_FRAME_NAME") is TxRoboticTxObjectParam parm && parm.Value is TxFrame frame) {
                        return new TxVector(frame.AbsoluteLocation.Translation);
                    }
                    else {
                        return new TxVector(data.Robot.TCPF.AbsoluteLocation.Translation);
                    }
                }
            }
        }

        #region Getters
        //public TxVector CurrentTcpfLocation
        //{
        //    get {
        //        if (CurrentVia.GetParameter("MOUNTED_WORKPIECE_FRAME_NAME") is TxRoboticTxObjectParam parm) {
        //            return (parm.Value as TxFrame).AbsoluteLocation.Translation;
        //        }
        //        else return Operation?.SimulatedObjects.OfType<TxRobot>().FirstOrDefault().TCPF.AbsoluteLocation.Translation;
        //    }
        //}

        //public bool IsCloserToNextVia => CurrentVia != null && CurrentTcpfLocation != null && GetNextVia is LocOp nextVia &&
            //((nextVia as ITxLocatableObject).AbsoluteLocation.Translation - CurrentTcpfLocation).Magnitude() < ((CurrentVia as ITxLocatableObject).AbsoluteLocation.Translation - PrevTcpfLocation).Magnitude();

        //public bool IsMovingFromCurrentVia => PrevTcpfLocation != null && (CurrentTcpfLocation - (CurrentVia as ITxLocatableObject).AbsoluteLocation.Translation).Magnitude() > (PrevTcpfLocation - (CurrentVia as ITxLocatableObject).AbsoluteLocation.Translation).Magnitude();

        public LocOp GetNextVia
        {
            get {
                if (CurrentVia == null) return null;
                if (CurrentVia.Index() + 1 >= (Operation as ITxOrderedCompoundOperation).Count) return null;

                return (CurrentVia.Collection as ITxOrderedCompoundOperation).GetChildAt(CurrentVia.Index() + 1) as LocOp;
            }
        }

        public LocOp GetPrevVia
        {
            get {
                if (CurrentVia == null) return null;
                if (CurrentVia.Index() - 1 < 0) return null;

                return (CurrentVia.Collection as ITxOrderedCompoundOperation).GetChildAt(CurrentVia.Index() - 1) as LocOp;
            }
        }

        public void GetPathVias(out LocOp MovingFrom, out LocOp MovingTo)
        {
            if (!MotionCalc.IsMovingFromCurrentVia) {
                MovingTo = CurrentVia; MovingFrom = GetPrevVia; return;
            }
            else {
                MovingTo = GetNextVia; MovingFrom = CurrentVia; return;
            }
        }



        public double GetCurrentPermissibleClearance()
        {

            var parms = solverParams;
            var defaultRadius = parms.NearMissDistance;

            GetPathVias(out var movingFrom, out var movingTo);
            if (movingFrom == null) return defaultRadius;

            if (parms.acceptedClearances.TryGetValue(movingFrom, out var val)) {

                var acceptedClearance = val.AcceptableClearance;
                if (acceptedClearance > defaultRadius) return defaultRadius;
                if (!parms.IgnoreExistingCollisions) acceptedClearance = Math.Max(0.0001, acceptedClearance);
                if (!parms.IgnoreExistingNearMisses && acceptedClearance != 0) return defaultRadius;

                acceptedClearance = acceptedClearance * 0.9; //Add a small buffer
                return acceptedClearance;
            }

            return defaultRadius;

        }

        #endregion
    }
}

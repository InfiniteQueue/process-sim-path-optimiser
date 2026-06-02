using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PathOptimiser.OptimisationSystem;
using Tecnomatix.Engineering;
using static PathOptimiser.CollisionEnvelope.FrameData;

namespace PathOptimiser
{

    public class CollisionEnvelope
    {
        public void RecordCollisionFrame(EnvelopeRecordingData recordingData, double clearance)
        {
            var newFrame = new FrameData(recordingData, clearance);

            FrameDataList.Add(newFrame);
            AddScorePenalty(newFrame);
        }

        void AddScorePenalty(FrameData data)
        {

            var ignoreRadiusIntersection = data.MinIgnoredClearance - data.Clearance;
            var fractionalIntersection = ignoreRadiusIntersection / data.MinIgnoredClearance;

            var interval = TxApplication.ActiveDocument.SimulationPlayer.TimeInterval;

            if (data.Speed is double speed) { //Make sure the envelope is playing forward

                this.SpeedXFramesScore += (Math.Pow(speed + 1, 2.5) - 1) * interval * fractionalIntersection; //The plus and minus one here avoids increasing scores for speeds lower than one
            }
        }


        /// <summary> Returns the vias in this collision, plus trailing and preceding</summary>
        public IEnumerable<ITxRoboticLocationOperation> ExtendBy(int behind, int ahead)
        {
            if (CollidingVias.Count() == 0) yield break;

            var firstVia = CollidingVias.FirstOrDefault();
            var lastVia = CollidingVias.LastOrDefault();
            var path = firstVia.Collection as ITxOrderedCompoundOperation;

            for (var i = firstVia.Index() - behind; i <= lastVia.Index() + ahead; i++) {

                if (i < 0 || i >= path.Count) continue;

                if ((path).GetChildAt(i) is ITxRoboticLocationOperation YieldVia)
                    yield return YieldVia;
            }
        }

        [Obsolete]
        private double GetSpeedCNTScore()
        {
            var score = 0.0;
            foreach (var via in CollidingVias) {
                if (via.GetCNT() is double cnt) score += cnt;
                if (via.IsLinear()) score += (via.GetSpeed() / 3000.0) * 100.0;
                else score += via.GetSpeed();
            }

            return 1000 * score;
        }

        #region Data



        public List<FrameData> FrameDataList = new List<FrameData>();



        public double StartTime => FrameDataList.First().Time;
        public double? EndTime => FrameDataList.Last().Time + FrameDataList.Last().Duration;

        private double SpeedXFramesScore;


        #region Getters
        private double TopSpeed => FrameDataList.Max(x => x.Speed);
        public double CollisionScore =>
            SpeedXFramesScore
            /*+ Math.Pow(TopSpeed, 2.2)
            + GetSpeedCNTScore()*/; //Small adjustment to ensure steps down are considered slightly better when barely changing the collision
        public IEnumerable<ITxRoboticLocationOperation> CollidingVias => FrameDataList.Select(x => x.CurrentVia).OfType<ITxRoboticLocationOperation>().Distinct();

        public int CollidingFrameCount => FrameDataList.Where(x => x.CollisionState != State.Clear).Count();
        #endregion
        #endregion

        public class FrameData
        {
            public FrameData(EnvelopeRecordingData data, double clearance) : this(data.CurrentVia, data.SimPlayer.CurrentTime, clearance, data.GetCurrentPermissibleClearance(), data.MotionCalc.Speed ?? 0, data.SimPlayer.TimeInterval) { }

            public FrameData(ITxLocationOperation currentVia, double time, double clearance, double minIgnoredClearance, double speed, double duration)
            {
                CurrentVia = currentVia; Time = time; Clearance = clearance; MinIgnoredClearance = minIgnoredClearance; Speed = speed; Duration = duration; ViaName = currentVia?.Name;

                collisionState =
                    (clearance >= minIgnoredClearance) ? State.Clear :
                    (clearance <= 0) ? State.Intersects :
                    State.NearMiss;

                //Note that Clear state should never be recorded, and exists as a fallback
                if (collisionState == State.Clear) Debugger.Break();
            }

            public ITxLocationOperation CurrentVia { get; }

            #region Data

            public string ViaName { get; set; } //Use this instead of Via.Name to avoid potential crashes
            public double Time { get; set; }
            public double Clearance { get; set; }
            public double MinIgnoredClearance { get; set; }
            public double Speed { get; set; }
            public double Duration { get; set; }

            private State collisionState;
            public State CollisionState => collisionState;

            #endregion
            public enum State
            {
                Intersects,
                NearMiss,
                Clear
            }
        }

    }
}

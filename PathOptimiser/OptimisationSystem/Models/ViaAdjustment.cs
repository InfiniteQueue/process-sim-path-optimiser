using System;
using Tecnomatix.Engineering;

namespace PathOptimiser
{
    public struct ViaAdjustment : IDisposable
    {
        public ViaAdjustment(ITxRoboticLocationOperation Via, ViaParameters parms, bool applyNow)
        {
            this.newParams = parms;
            this.Via = Via;
            original = new ViaParameters(this.Via);
            if (applyNow) Apply();
        }

        public override string ToString()
        {
            return $"{Via.Name}: Speed{newParams.Speed}, CNT{newParams.CNT}";
        }
        
        public ITxRoboticLocationOperation Via;
        public ViaParameters original;
        public ViaParameters newParams;

        public AdjustmentTypes AdjustmentType =>
            (original.Speed != newParams.Speed ? AdjustmentTypes.Speed : AdjustmentTypes.None)
            | (original.CNT != newParams.CNT ? AdjustmentTypes.Cnt : AdjustmentTypes.None);

        public enum AdjustmentTypes
        {
            None = 0,
            Speed = 1,
            Cnt = 2
        }

        public void Apply() => newParams.Apply(Via);

        public void Reset() => Dispose();

        public void Dispose() => original.Apply(Via);

        public override bool Equals(object obj)
        {
            if (obj is ViaAdjustment other == false) return false;
            return Via == other.Via && newParams.Speed == other.newParams.Speed && newParams.CNT == other.newParams.CNT;
        }

        public override int GetHashCode()
        {
            return Via.GetHashCode() ^ newParams.Speed.GetHashCode() ^ newParams.CNT.GetHashCode();
        }

    }


    public class ViaParameters
    {
        public ViaParameters() { }
        public ViaParameters(ITxRoboticLocationOperation locOp)
        {
            Speed = locOp.GetSpeed();
            CNT = locOp.GetCNT();
        }

        public bool Equal(ViaParameters viaParameters) => Speed == viaParameters.Speed && CNT == viaParameters.CNT;
        public void Apply(ITxRoboticLocationOperation locOp)
        {
            locOp.SetCNT(CNT);
            locOp.SetSpeed(Speed);
            //return new ViaAdjustment(locOp, this, true);
        }

        public static implicit operator ViaParameters((double, double) tuple) => new ViaParameters() { Speed = tuple.Item1, CNT = tuple.Item2 };

        public double Speed;
        public double? CNT;
    }
}

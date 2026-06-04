using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Markup;
using PathOptimiser.OptimisationSystem;
using PathOptimiser.OptimisationSystem.Services.PathSolver;
using Tecnomatix.Engineering;
using static Tecnomatix.Engineering.TxApplication;

namespace PathOptimiser
{
    public static class TecnomatixStatics
    {
        public static bool InvolvesObject(this TxCollisionState collisionState, ITxObject obj) => collisionState.FirstObject == obj || collisionState.SecondObject == obj;

        #region Vias
        public static void SetCNT(this ITxLocationOperation locOp, double? value)
        {
            //if (locOp.GetCNT() == null && value != null) Debugger.Break();
            (locOp as ITxRoboticOperation)?.SetParameter(new TxRoboticStringParam("RRS_ZONE_NAME", value != null ? $"CNT{value}" : "FINE"));
        }

        public static double? GetCNT(this ITxLocationOperation locOp)
        {
            var value = ((locOp as ITxRoboticOperation)?.GetParameter("RRS_ZONE_NAME") as TxRoboticStringParam).Value;
            if (value.ToLower() == "fine") return null;
            else {
                var trimmed = ((locOp as ITxRoboticOperation)?.GetParameter("RRS_ZONE_NAME") as TxRoboticStringParam)?.Value.Substring(3);
                return double.Parse((trimmed ?? "0"));
            }
        }

        public static bool IsFine(this ITxLocationOperation locOp)
        {
            var value = ((locOp as ITxRoboticOperation)?.GetParameter("RRS_ZONE_NAME") as TxRoboticStringParam).Value;
            return (value.ToLower() == "fine");
        } 


        public static void SetSpeed(this ITxLocationOperation locOp, double value)
        {
            var robOp = locOp as ITxRoboticOperation;
            var parms = robOp.Parameters;


            if (IsLinear(locOp)) {
                if (robOp.GetParameter("RRS_CARTESIAN_POSITION_SPEED") is TxRoboticDoubleParam linearSpeedParam) {
                    linearSpeedParam.Value = value;
                    robOp.SetParameter(linearSpeedParam);
                }
            }
            else if (robOp.GetParameter("RRS_JOINT_SPEED") is TxRoboticDoubleParam jointSpeedParam) {
                jointSpeedParam.Value = value;
                robOp.SetParameter(jointSpeedParam);
            }
        }

        public static double GetSpeed(this ITxLocationOperation locOp)
        {
            var robOp = locOp as ITxRoboticOperation;
            var parms = robOp.Parameters;

            if (locOp.IsLinear() && robOp.GetParameter("RRS_CARTESIAN_POSITION_SPEED") is TxRoboticDoubleParam linearSpeedParam) {

                    return linearSpeedParam.Value;
                }
            
            else if (robOp.GetParameter("RRS_JOINT_SPEED") is TxRoboticDoubleParam jointSpeedParam){
                return jointSpeedParam.Value;
            }

            else return 0;

        }

        public static bool IsLinear(this ITxLocationOperation locOp)
        {
            var robOp = (locOp as ITxRoboticOperation);
            if (robOp.GetParameter("RRS_MOTION_TYPE") is TxRoboticIntParam motionType) return motionType.Value == 2;
            else return false;
                //return (robOp.GetParameter("RRS_CARTESIAN_POSITION_SPEED") is TxRoboticDoubleParam);

        }

        public static bool CanStandardSpeedStepDown(this ITxLocationOperation locOp, int multiplier = 1)
        {
            if (locOp.IsLinear()) return locOp.GetSpeed() > StandardLinearStepDown * multiplier ;
            else return locOp.GetSpeed() > StandardJointStepDown * multiplier;
        }

        public static ViaAdjustment? StandardSpeedStepDown(this ITxLocationOperation locOp, int multiplier = 1)
        {
            if (!CanStandardSpeedStepDown(locOp, multiplier)) return null;
            else {
                var robOp = (locOp as ITxRoboticLocationOperation);
                return new ViaAdjustment() { Via = (ITxRoboticLocationOperation)locOp, original = new ViaParameters(robOp), newParams = new ViaParameters() { CNT = robOp.GetCNT(), Speed = robOp.GetSpeed() - (robOp.IsLinear() ? StandardLinearStepDown : StandardJointStepDown) * multiplier } };
            }
        }

        /// <summary> Sets both speed and CNT to maximum </summary>
        public static ViaAdjustment MaxSpeedAndCnt(this ITxLocationOperation locOp)
        {
            //if (locOp.Name == "via132") Debugger.Break();

            double maxSpeed;
            if (locOp.IsLinear()) {
                var robot = (locOp.Collection as ITxOrderedCompoundOperation).SimulatedObjects.OfType<TxRobot>().FirstOrDefault();
                var maxSpeedParam = robot?.GetParameter("RJ_MAX_CARTESIAN_SPEED");
                maxSpeed = (maxSpeedParam as TxRoboticDoubleParam)?.Value ?? 3000;
            }

            else maxSpeed = 100;

            return new ViaAdjustment((ITxRoboticLocationOperation)locOp, (maxSpeed, 100), false);
        }

        static double StandardJointStepDown => 10;
        static double StandardLinearStepDown => 200;

        public static int Index(this ITxLocationOperation locOP)
        {
            return (locOP.Collection as ITxOrderedCompoundOperation).GetIndexOfChild(locOP);
        }
        #endregion


        public static void Reset(this TxSimulationPlayer simPlayer)
        {
            simPlayer.Pause();
            simPlayer.Stop();
            simPlayer.Rewind();
        }

        public static double Distance(this TxVector startVector, TxVector endVector) => (endVector - startVector).Magnitude();

        public static double Magnitude(this TxVector txVector) => Math.Sqrt(
            txVector.X * txVector.X
+ txVector.Y * txVector.Y
+ txVector.Z * txVector.Z
             
            )
            ;

        public static bool CollisionDetected(this TxCollisionQueryResults results) => results.States.Count > 0;

        public static T Paste<T>(this ITxObjectCollection collection, T original) where T : class, ITxObject
        {
            var copyList = new TxObjectList();
            copyList.Add(original);
            var result = collection.Paste(copyList);
            return result.First() as T;
        }

        public static IEnumerable<T> OfType<T>(this ITxObjectCollection list)
        {
            return list.GetDirectDescendants(new TxTypeFilter(typeof(T))).OfType<T>();
        }

        #region Operations
        public static ITxLocationOperation AtIndex(this ITxOrderedCompoundOperation operation, int index)
        {
            if (index < 0 || index >= operation.Count) return null;
            return operation.GetChildAt(index) as ITxLocationOperation;
        }
        public static IEnumerable<ITxLocationOperation> Vias(this ITxCompoundOperation operation) => operation.GetDirectDescendants(new TxTypeFilter(typeof(ITxLocationOperation))).OfType<ITxLocationOperation>();
        public static IEnumerable<ITxLocationOperation> Vias(this ITxRoboticOrderedCompoundOperation operation) => operation.GetDirectDescendants(new TxTypeFilter(typeof(ITxLocationOperation))).OfType<ITxLocationOperation>();
        #endregion

        public static double MinDistance(this TxCollisionQueryResults collisionQueryResults) => collisionQueryResults.States.Count == 0 ? double.MaxValue : collisionQueryResults.States.OfType<TxCollisionState>().Min(state => state.Distance);



        #region SimPlayer
        public static void SafePlaySim(TxSimulationPlayer SimPlayer, ITxOperation operation, bool rewindWhenDone,  EnvelopeRecordingData Data = null)
        {
            if (ActiveDocument.CurrentOperation != operation) {
                SimPlayer.AskUserForReset(false);
                ActiveDocument.CurrentOperation = operation;
            }

            //A lot of askuserforreset(false) here, just to be safe. Reset prompts would halt the entire program

            SimPlayer.AskUserForReset(false);
            SimPlayer.SetOperation(operation);
            SimPlayer.Pause();
            SimPlayer.Stop();
            SimPlayer.AskUserForReset(false);
            SimPlayer.Rewind();

            Data?.MotionCalc.Reset();

            var debugTime = DateTime.Now;
            SimPlayer.AskUserForReset(false);
            SimPlayer.PlayWithoutRefresh();

            if (Data != null) Data.RecordedEnvelopes.FinalTime = SimPlayer.CurrentTime;


            if (rewindWhenDone) {
                SimPlayer.AskUserForReset(false);
                SimPlayer.Rewind();
            }


            var errors = SimPlayer.GetErrorsAndTraces().Where(x => x.Contains("[Error]"));
            if (errors.Count() > 0) {
                Debug.WriteLine( $"\n--\n--\n--\nSIM PLAYER ERROR: {string.Join("\n", errors)}\n--\n--\n--\n");

                SimulationError?.Invoke(operation);
                //PathSolver.IsCancelRequested = true;
            }

        }

        public static event Action<ITxOperation> SimulationError;
        #endregion
        //RRS_JOINT_SPEED
    }
}

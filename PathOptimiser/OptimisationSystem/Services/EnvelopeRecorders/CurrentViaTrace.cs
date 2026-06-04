using System;
using System.Collections.Generic;
using System.Diagnostics;
using PathOptimiser.OptimisationSystem.Models;
using Tecnomatix.Engineering;

namespace PathOptimiser.OptimisationSystem.Services.EnvelopeRecorders
{
    internal class CurrentViaTrace : EnvelopeRecorder
    {
        SolverParams solverParams;

        public CurrentViaTrace(SimPlayerTracker tracker, ITxOperation operation, SolverParams solverParams) : base(tracker, operation, solverParams)
        {
            this.solverParams = solverParams;
            Data.CurrentVia = (operation as ITxOrderedCompoundOperation).GetChildAt(0) as ITxRoboticLocationOperation;
        }

        protected override void TimeIntervalReached(object sender, TxSimulationPlayer_TimeIntervalReachedEventArgs args)
        {

            if (paused) return;

            Data.MotionCalc.UpdateMotionData();


            Data.GetPathVias(out var from, out var to);

            if (from != null)
            ViasAndLocations.Add((from, Data.MotionCalc.CurrentEffectiveLocation));
            //if (Data.GetPrevVia is ITxRoboticLocationOperation prevVia)

        }


        public void BuildTraceFrames()
        {
            var dictionary = new Dictionary<ITxRoboticLocationOperation, TxColor>();
            var random = new Random();

            foreach (var pair in ViasAndLocations) {

                if (!dictionary.ContainsKey(pair.Item1)) dictionary[pair.Item1] = new TxColor((byte)random.Next(0, 256), (byte)random.Next(0, 256), (byte)random.Next(0, 256));


                var name = pair.Item1.Name;
                var transform = new TxTransformation() { Translation = new TxVector(pair.Item2) };

                var Group = TraceGroup.CreateGroup(new TxGroupCreationData(name));
                var manip = Group.CreateManipulator(new TxManipulatorCreationData(name, transform));
                manip.AddElement(new TxManipulatorSphereElementData(new TxTransformation(), 5) { Color = dictionary[pair.Item1], Emphasizable = true });

            }
        }


        List<(ITxRoboticLocationOperation, TxVector)> ViasAndLocations = new List<(ITxRoboticLocationOperation, TxVector)>();

        TxComponent _traceComponent;
        TxComponent TraceComponent
        {
            get {
                if (_traceComponent == null) {
                    _traceComponent = TxApplication.ActiveDocument.PhysicalRoot.CreateLocalComponent(new TxLocalComponentCreationData("Current Via Trace Component"));
                }

                return _traceComponent;
            }
        }

        TxGroup _traceGroup;
        TxGroup TraceGroup => _traceGroup ?? (_traceGroup = TxApplication.ActiveDocument.PhysicalRoot.CreateGroup(new TxGroupCreationData("Current Via Trace")));

        public override EnvelopeCollection Run(SimPlayerTracker tracker)
        {
            Debug.WriteLine("Get Acceptable Clearances");

            var result = base.Run(tracker);
            Debug.WriteLine($"{solverParams.acceptedClearances.Count} clearances found");

            return result;
        }
    }
}

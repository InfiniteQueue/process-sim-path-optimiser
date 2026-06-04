using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using PathOptimiser.OptimisationSystem.Services;
using OptimisationOperation = PathOptimiser.OptimisationSystem.Services.PathSolver.OptimisationOperation;
using static PathOptimiser.OptimisationSystem.Services.PathSolver.PathSolverStatics;
using Tecnomatix.Engineering;
using static Tecnomatix.Engineering.TxApplication;
using static PathOptimiser.MyDispatchers;
using System.Threading;
using PathOptimiser.OptimisationSystem.Services.EnvelopeRecorders;

namespace PathOptimiser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : SMC_Form_Library.SMC_Form_WPF
    {
        static CancellationTokenSource allOperationsCancel;
        public MainWindow()
        {
            InitializeComponent();

            if (Debugger.IsAttached) HandleDebug();

            this.Closing += MainWindow_Closing;
            ActiveDocument.Unloading += (s, e) => { this.Dispatcher.Invoke(() => this.Close()); };
            this.GotFocus += (s, e) => TaskBarClear();
            this.Activated += (s, e) => TaskBarClear();
            this.MouseDown += (s, e) => TaskBarClear();

            CancelRequestedChanged += PathSolver_CancelRequestedChanged;
            OriginalClearancesFound += (s) => this.Dispatcher.Invoke(() =>  ctrlAllOperations.SetAcceptedClearances(s), System.Windows.Threading.DispatcherPriority.Render);
            AdjustmentApplied += (s) => this.Dispatcher.Invoke(() => PathSolver_AdjustmentApplied(s), System.Windows.Threading.DispatcherPriority.Render);
            OperationFinished += (s) => this.Dispatcher.Invoke(() => ctrlAllOperations.SetOperationClearStatus(s), System.Windows.Threading.DispatcherPriority.Render);
        }


        private void PathSolver_AdjustmentApplied(ViaAdjustment adjustment)
        {
            ctrlAllOperations.AllRows.FirstOrDefault(x => x.LocOp == adjustment.Via)?.Update(adjustment);
        }

        #region Main
        private void Run(object sender, RoutedEventArgs e)
        {
            if (ReadyToRunChecks.FindIssues() is ReadyToRunChecks.Failure failurePoint) {
                MessageBox.Show(ReadyToRunChecks.FailureStrings[failurePoint]);
                return;
            }

            Debug.WriteLine("\n\nPathSolver Start");

            try {
                ctrlAllOperations.RecordStartingValues();

                StatusLog.Log("Running Path Optimisation...", SMC_Form_Library.FormStatusLog.LogState.Warning);

                BlankCables();


                //PathSolver.IsCancelRequested = false;
                allOperationsCancel = new CancellationTokenSource();
                var solver = new OptimisationOperation(new SimPlayerTracker() { SimPlayer = ActiveDocument.SimulationPlayer});
                solver.Done += Solver_Done;
                solver.solverParams.IgnoreExistingNearMisses = chkIgnoreNearMisses.IsChecked == true;

                InRunMode = true;

                var getTrackedVias = ctrlAllOperations.ActiveVias;

                DebugStartTime = DateTime.Now;
                TxDispatcher.InvokeAsync(() => { solver.RunMain(getTrackedVias, ctrlAllOperations.OperationsToData); });
            }
            catch { }
        }
        #endregion

        private static void BlankCables()
        {
            foreach (TxCable item in ActiveDocument.PhysicalRoot.GetAllDescendants(new TxTypeFilter(typeof(TxCable)))) {
                item.Blank();
            }
            Debug.WriteLine("Cables blanked");
        }


        #region Events
        private void Solver_Done(OptimisationOperation.Result result)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (result == OptimisationOperation.Result.Success) Log("Path Optimised", SMC_Form_Library.FormStatusLog.LogState.Success);
                if (result == OptimisationOperation.Result.CouldNotComplete) Log("Path Could Not Be Optimised", SMC_Form_Library.FormStatusLog.LogState.Error);
                if (result == OptimisationOperation.Result.Cancelled) Log("Path Cancelled", SMC_Form_Library.FormStatusLog.LogState.Warning);

                InRunMode = false;
                this.TaskbarItemInfo = new TaskbarItemInfo();
                this.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;

                ctrlEnvelopeDisplay.Visibility = Visibility.Collapsed;

                Debug.WriteLine($"{(DateTime.Now - DebugStartTime).TotalSeconds / 60.0:0.##} minutes elapsed");
            }
                );
        }

        private void PathSolver_CancelRequestedChanged(object sender, bool e)
        {
            this.Dispatcher.Invoke(() =>
            btnCancel.Content = e ? "Cancelling..." : "Cancel");
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            InRunMode = false;
            ctrlAllOperations.RepopulateCategorised(new ITxRoboticLocationOperation[0]);
            StatusLog.Log("Ready", SMC_Form_Library.FormStatusLog.LogState.Neutral);
        }

        //private void Solver_ValuesUpdated() => this.Dispatcher.Invoke(() => { ctrlAllOperations.UpdateViaControls(); }, System.Windows.Threading.DispatcherPriority.Input);

        private void btnAddSelected_Click(object sender, RoutedEventArgs e) => ctrlAllOperations.AssignSelectedVias();

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            allOperationsCancel?.Cancel();
            InRunMode = false;
        }
        #endregion

        #region UI

        void TaskBarClear()
        {
            if (this.TaskbarItemInfo is TaskbarItemInfo itemInfo) itemInfo.ProgressState = TaskbarItemProgressState.None;
        }

        bool _inRunMode;
        bool InRunMode
        {
            get => _inRunMode;
            set {
                _inRunMode = value;
                foreach (var ctrl in DisabledInRunMode) ctrl.IsEnabled = !value;
                btnCancel.IsEnabled = value;
                RunModeChanged?.Invoke(this, value);
            }
        }

        public static event EventHandler<bool> RunModeChanged;

        Control[] DisabledInRunMode => new Control[] { btnAddSelected, btnDebug, btnRun };
        #endregion

        #region Debug
        private void DrawCollisionCurves()
        {
            var collisions = ActiveDocument.CollisionRoot.GetCollidingObjectsAndDistances(new TxCollisionAndDistancesQueryParams() { Mode = TxCollisionQueryParams.TxCollisionQueryMode.DefinedPairs, ReportLevel = TxCollisionQueryParams.TxCollisionReportLevel.ComponentLevel, FindPenetrationRegions = true });

            var states = collisions.States.OfType<TxCollisionState>().FirstOrDefault(x => x.PenetrationRegions.Length > 0);


            var rand = new Random();
            var bytes = new byte[3];
            var group = ActiveDocument.PhysicalRoot.CreateGroup(new TxGroupCreationData("Test"));
            var manip = group.CreateManipulator(new TxManipulatorCreationData("TestManip", new TxTransformation()));

            foreach (var curve in states.PenetrationRegions) {
                rand.NextBytes(bytes);
                var color = new TxColor(bytes[0], bytes[1], bytes[2]);
                foreach (var item in curve.IntersectionCurve) {
                    manip.AddElement(new TxManipulatorSphereElementData(new TxTransformation() { Translation = item }, 1) { Color = color });
                }
            }

            foreach (var item in states.PenetrationRegions.SelectMany(x => x.IntersectionCurve)) { }
        }

        void HandleDebug()
        {
            btnDebug.Visibility = Visibility.Visible;
        }

        DateTime DebugStartTime;

        #endregion

        private void btnDebug_Click(object sender, RoutedEventArgs e)
        {

            TxDispatcher.InvokeAsync(() =>
            {
                if (ActiveDocument.CurrentOperation == null) { Debugger.Break(); return; }

                var simPlayer = new TxSimulationPlayer(false, false);

                simPlayer.SetOperation(ActiveDocument.CurrentOperation);

                var Tracer = new CurrentViaTrace(new SimPlayerTracker() { SimPlayer = ActiveDocument.SimulationPlayer }, ActiveDocument.CurrentOperation, new OptimisationSystem.Models.SolverParams());

                TecnomatixStatics.SafePlaySim(ActiveDocument.SimulationPlayer, ActiveDocument.CurrentOperation, false);

                Tracer.BuildTraceFrames();

                Tracer.Dispose();
            });

            //var via = TxApplication.ActiveSelection.GetAllItems().OfType<ITxRoboticLocationOperation>().FirstOrDefault();

            //if (via.GetParameter("MOUNTED_WORKPIECE_FRAME_NAME") is TxRoboticTxObjectParam param) {
            //    ;
            //}
            //DeviceResetter.Reload();
            ////RobotResetter.Store();
            //TxApplication.RefreshDisplay();
        }
    }
}

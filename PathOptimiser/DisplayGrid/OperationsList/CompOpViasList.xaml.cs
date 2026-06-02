using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PathOptimiser.Models;
using Tecnomatix.Engineering;

namespace PathOptimiser.DisplayGrid.OperationsList
{
    /// <summary>
    /// Interaction logic for CollectionOperationsDisplay.xaml
    /// </summary>
    public partial class CollectionOperationsDisplay : UserControl
    {
        public CollectionOperationsDisplay()
        {
            InitializeComponent();

            MainWindow.RunModeChanged += MainWindow_RunModeChanged;
        }


        public void SetCompletionState(PathSolver.OperationOptimisedReport report)
        {
            borderReset.Visibility = System.Windows.Visibility.Visible;
            ctrlStatusBorder.Visibility = System.Windows.Visibility.Visible;
            if (report.Success == PathSolver.Result.Success) {
                lblStatus.Content = "Success";
                ctrlStatusBorder.Background = Brushes.LightGreen;
            }
            else if (report.Success == PathSolver.Result.CouldNotComplete) {
                lblStatus.Content = "Failed";
                ctrlStatusBorder.Background = Brushes.PaleVioletRed;
            }
            else if (report.Success == PathSolver.Result.Cancelled) {
                lblStatus.Content = "Aborted";
                ctrlStatusBorder.Background = Brushes.PaleVioletRed;
            }
            lblTime.Content = $"{report.originalTime:0.##} -> {report.optimisedTime:0.##} seconds";
        }
        public void SetGroup(IGrouping<ITxCompoundOperation, ITxRoboticLocationOperation> collectionGroup)
        {
            SetGroup(collectionGroup.Key, collectionGroup);
        }

        public void SetGroup(ITxCompoundOperation compOp, IEnumerable<ITxRoboticLocationOperation> Vias)
        {
            ResetControls();

            lblCollectionName.Content = compOp.Name;
            stkRows.Children.Clear();
            Vias = Vias.ToList();
            this.compoundOp = compOp;
            Data.compoundOp = compOp;

            var allVias = compOp.Vias().OfType<ITxRoboticLocationOperation>();

            OriginalViaSpeeds.Clear();
            foreach (var via in allVias) {
                stkRows.Children.Add(new DisplayGridRow(Data) { LocOp = via, Included = compOp.Contains(via) });
                OriginalViaSpeeds.Add(new ViaAdjustment(via, new ViaParameters(via), false));
            }
        }

        void ResetControls()
        {
            ctrlStatusBorder.Visibility = Visibility.Collapsed;
            borderReset.Visibility = Visibility.Collapsed;
            lblTime.Content = "";
        }

        private IEnumerable<Control> RunModeControls => stkRows.Children.OfType<Control>().Concat(new Control[] { btnUndo, chkIgnoreCollisions, chkIgnoreNearMiss, chkSetMaxSpeeds, txtClearance });
        private void MainWindow_RunModeChanged(object sender, bool e)
        {
            foreach (var item in RunModeControls) item.IsEnabled = !e;
        }

        #region Data

        public List<ITxRoboticLocationOperation> Vias = new List<ITxRoboticLocationOperation>();


        ITxCompoundOperation compoundOp;
        public ITxCompoundOperation CompoundOp => compoundOp;

        public List<ViaAdjustment> OriginalViaSpeeds = new List<ViaAdjustment>();


        //private double requiredClearance = 5;
        //public double RequiredClearance
        //{
        //    get => requiredClearance;
        //    set {
        //        requiredClearance = value;
        //        OperationToNearMissRegistry.SetValue(compoundOp, value);
        //    }
        //}


        public OperationData Data { get; set; } = new OperationData();
        public class OperationData
        {
            public ITxCompoundOperation compoundOp;

            private string clearance = "5";
            public string Clearance
            {
                get => clearance;
                set {

                    if (double.TryParse(value, out var result)) {
                        clearance = value;
                        OperationToNearMissRegistry.SetValue(compoundOp, result);
                    }
                }
            }

            bool ignoreNearMisses = true;
            public bool IgnoreNearMisses { get => ignoreNearMisses; set => ignoreNearMisses = value; }
            bool ignoreCollisions = true;
            public bool IgnoreCollisions { get => ignoreCollisions; set => ignoreCollisions = value; }

            bool setToMax = true;
            public bool SetToMax { get => setToMax; set => setToMax = value; }
        }
        #endregion

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show($"Reset all optimisation on {CompoundOp.Name}?", "Confirm", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                foreach (var adjust in OriginalViaSpeeds.ToList()) {
                    OptCommand.DummyWindow.Dispatcher.InvokeAsync(() =>
                    {
                        adjust.Apply();
                    });
                    SetGroup(CompoundOp, CompoundOp.Vias().OfType<ITxRoboticLocationOperation>());
                }
            }
        }

        private void RemoveButtonClick(object sender, RoutedEventArgs e)
        {
            (this.Parent as Panel).Children.Remove(this);
        }
    }
}

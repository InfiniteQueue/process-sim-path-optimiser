using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PathOptimiser.DisplayGrid.OperationsList;
using PathOptimiser.OptimisationSystem.Models;
using Tecnomatix.Engineering;

namespace PathOptimiser.DisplayGrid
{
    /// <summary>
    /// Interaction logic for AllOperationsDisplay.xaml
    /// </summary>
    public partial class AllOperationsDisplay : UserControl
    {
        public AllOperationsDisplay()
        {
            InitializeComponent();
        }

        /// <summary> Call this before the solver starts to account for changes in via values </summary>
        public void RecordStartingValues()
        {
            foreach (var row in AllRows) {
                row.DisplayOriginalValues();
            }
        }

        public void SetOperationClearStatus(PathSolver.OperationOptimisedReport report)
        {
            stkOperations.Children.OfType<RobotOperationsListControl>().SelectMany(o => o.stkCollections.Children.OfType<CollectionOperationsDisplay>()).FirstOrDefault(x => x.CompoundOp == report.Operation)?.SetCompletionState(report);
        }

        public Dictionary<ITxRoboticLocationOperation, DisplayGridRow> GridRows => AllRows.ToDictionary(x => x.LocOp);

        public List<ITxRoboticLocationOperation> AllVias => AllRows.Select(x => x.LocOp).ToList();
        public List<ITxRoboticLocationOperation> ActiveVias => AllRows.Where(x => x.Included).Select(x => x.LocOp).ToList();

        public IEnumerable<DisplayGridRow> AllRows => stkOperations.Children.OfType<RobotOperationsListControl>().SelectMany(o => o.stkCollections.Children.OfType<CollectionOperationsDisplay>()).SelectMany(c => c.stkRows.Children.OfType<DisplayGridRow>());

        public void AssignSelectedVias()
        {

            var selectedVias = TxApplication.ActiveSelection.GetAllItems().OfType<ITxRoboticLocationOperation>().ToList();
            var selectedOperations = TxApplication.ActiveSelection.GetAllItems().OfType<ITxCompoundOperation>().ToList();

            var operationSubVias = selectedOperations.SelectMany(x => x.GetAllDescendants(new TxNoTypeFilter()).OfType<ITxRoboticLocationOperation>());

            var allSelectedVias = selectedVias.Concat(operationSubVias).Distinct().ToList();

            RepopulateCategorised(allSelectedVias);

        }

        public void SetAcceptedClearances(SolverParams.AcceptedClearancesCollection clearances)
        {
            foreach(var item in stkOperations.Children.OfType<RobotOperationsListControl>().SelectMany(o => o.stkCollections.Children.OfType<CollectionOperationsDisplay>())) {
                foreach(var row in item.stkRows.Children.OfType<DisplayGridRow>()) {
                    if (clearances.ContainsKey(row.LocOp)) {
                        double? effectiveAcceptableClearance = clearances[row.LocOp].AcceptableClearance;

                        if (!item.Data.IgnoreNearMisses) {
                            if (item.Data.IgnoreCollisions) {
                                if (effectiveAcceptableClearance != 0) effectiveAcceptableClearance = null;
                            }
                            else {
                                effectiveAcceptableClearance = null;
                            }
                        }

                        if (effectiveAcceptableClearance != null) 
                            row.SetAcceptedClearance(effectiveAcceptableClearance.Value);
                    }
                }
            }

            var gridRows = this.GridRows;

            foreach (var item in clearances) {
                var locOp = item.Key as ITxRoboticLocationOperation;
                if (gridRows.ContainsKey(locOp)) {
                    gridRows[locOp].SetAcceptedClearance(item.Value.AcceptableClearance);
                }
            }

        }


        public void RepopulateCategorised(IEnumerable<ITxRoboticLocationOperation> selectedVias)
        {
            stkOperations.Children.Clear();

            var groupsByRobot = selectedVias
                .GroupBy(v => (v.Collection as ITxOrderedCompoundOperation).SimulatedObjects.OfType<TxRobot>().FirstOrDefault())
                .Where(g => g.Key != null);

            foreach (var robotGroup in groupsByRobot) {
                var list = new OperationsList.RobotOperationsListControl();
                list.SetGroup(robotGroup);
                stkOperations.Children.Add(list);
            }

            foreach(var row in AllRows) {
                row.Included = selectedVias.Contains(row.LocOp);
            }
        }

        public Dictionary<ITxCompoundOperation, CollectionOperationsDisplay.OperationData> OperationsToData {
            get {
                Dictionary < ITxCompoundOperation, CollectionOperationsDisplay.OperationData > result = null;
                this.Dispatcher.Invoke(() =>
                {
                    result = 
                stkOperations.Children.OfType<RobotOperationsListControl>().SelectMany(o => o.stkCollections.Children.OfType<CollectionOperationsDisplay>()).ToDictionary(x => x.CompoundOp, x => x.Data);
                }, System.Windows.Threading.DispatcherPriority.Normal
                );
                return result;
            }
    }
    }
}

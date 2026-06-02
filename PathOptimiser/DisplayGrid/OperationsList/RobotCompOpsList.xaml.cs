using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Tecnomatix.Engineering;

namespace PathOptimiser.DisplayGrid.OperationsList
{
    /// <summary>
    /// Interaction logic for OperationsList.xaml
    /// </summary>
    public partial class RobotOperationsListControl : UserControl
    {
        public RobotOperationsListControl()
        {
            InitializeComponent();
        }

        public List<ITxRoboticLocationOperation> Vias = new List<ITxRoboticLocationOperation>();

        public void SetGroup(IGrouping<TxRobot, ITxRoboticLocationOperation> robotGroup)
        {
            lblRobotName.Content = robotGroup.Key?.Name;
            stkCollections.Children.Clear();
            Vias = robotGroup.ToList();

            var byCollection = robotGroup.GroupBy(v => v.Collection as ITxCompoundOperation);
            foreach (var collectionGroup in byCollection) {
                var display = new CollectionOperationsDisplay();
                display.SetGroup(collectionGroup);
                stkCollections.Children.Add(display);
            }
        }
    }
}

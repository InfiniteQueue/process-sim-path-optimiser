using System;
using System.Collections.Generic;
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
using Tecnomatix.Engineering;


//Fairly certain this is deprecated



//namespace PathOptimiser.DisplayGrid
//{
//    /// <summary>
//    /// Interaction logic for ViaDisplayGrid.xaml
//    /// </summary>
//    public partial class ViaDisplayGrid : UserControl
//    {
//        public ViaDisplayGrid()
//        {
//            InitializeComponent();
//        }


//        private ITxCompoundOperation _operation;
//        public ITxCompoundOperation Operation
//        {
//            get => _operation;
//            set {
//                _operation = value;
//                SetOperation(value);
//            }
//        }

//        public IEnumerable<DisplayGridRow> DisplayGridRows => stkViasData.Children.OfType<DisplayGridRow>();

//        public IEnumerable<ITxRoboticLocationOperation> AllVias => stkViasData.Children.OfType<DisplayGridRow>().Select(x => x.LocOp);
//        public IEnumerable<ITxRoboticLocationOperation> ActiveVias => stkViasData.Children.OfType<DisplayGridRow>().Where(x => x.Included).Select(x => x.LocOp);

//        public void SetOperation(ITxCompoundOperation operation) => SetVias(operation.GetDirectDescendants(new TxTypeFilter(typeof(ITxRoboticLocationOperation))).OfType<ITxRoboticLocationOperation>().ToList());

//        public void SetVias(List<ITxRoboticLocationOperation> vias)
//        {
//            stkViasData.Children.Clear();
//            foreach(var via in vias) {
//                stkViasData.Children.Add(new DisplayGridRow() { LocOp = via});
//            }
//        }

//        public void Update()
//        {
//            foreach(var item in stkViasData.Children.OfType<DisplayGridRow>()) {
//                item.Update();
//            }
//            Dispatcher.Invoke(() => UpdateLayout(), System.Windows.Threading.DispatcherPriority.Render);
//        }
//    }
//}

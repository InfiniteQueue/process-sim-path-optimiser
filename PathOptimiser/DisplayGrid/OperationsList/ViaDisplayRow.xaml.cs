using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using PathOptimiser.DisplayGrid.OperationsList;
using Tecnomatix.Engineering;

namespace PathOptimiser
{
    /// <summary>
    /// Interaction logic for DisplayGridRow.xaml
    /// </summary>
    public partial class DisplayGridRow : UserControl, INotifyPropertyChanged
    {
        CollectionOperationsDisplay.OperationData data;

        public DisplayGridRow(CollectionOperationsDisplay.OperationData data)
        {
            InitializeComponent();
            this.DataContext = this;
            this.data = data;
        }

        private ViaParameters OriginalValues;

        private ITxRoboticLocationOperation locOp;

        public void DisplayOriginalValues()
        {
            this.LocOp = LocOp;
        }

        public ITxRoboticLocationOperation LocOp
        {
            get => locOp;
            set {
                locOp = value;
                OriginalValues = new ViaParameters(LocOp);
                Update();
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocOp)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanBeActive)));
            }
        }


        public void Update(ViaAdjustment? adjustment)
        {
            chkActivated.IsEnabled = CanBeActive;

            //Set up visuals:
            //Adjustment of null represents an unchanged via
            var maxSpeed = locOp.MaxSpeedAndCnt();
            if (adjustment?.newParams.Equal(OriginalValues) == true && adjustment?.newParams.Equal(locOp.MaxSpeedAndCnt().newParams) == true) adjustment = null;

            var cntIsDefault = adjustment?.newParams.CNT == null || adjustment?.newParams.CNT == 100 || !Included;
            var speedIsDefault = adjustment?.newParams.Speed == LocOp.MaxSpeedAndCnt().newParams.Speed || !Included;
            var isDefault = adjustment == null || (speedIsDefault && cntIsDefault);

            lblSpeed.Foreground = (speedIsDefault) ? Brushes.Gray : Brushes.Black;
            lblCnt.Foreground = (cntIsDefault) ? Brushes.Gray : Brushes.Black;

            //Set speed and cnt labels
            lblName.Content = locOp.Name;
            lblCnt.Content = (OriginalValues?.CNT?.ToString() ?? "FINE");
            lblCnt.Content += (adjustment != null) ? " -> " + (adjustment?.newParams.CNT?.ToString() ?? "FINE") : "";

            lblSpeed.Content = $"{OriginalValues?.Speed}";
            lblSpeed.Content += (adjustment != null ) ? $" -> {adjustment?.newParams.Speed}" : ""; 
            lblSpeed.Content += (locOp.IsLinear() ? "mm/s" : "%");
        }

        public void Update()
        {
            Update(null);
            lblCnt.Foreground = Brushes.Gray;
            lblSpeed.Foreground = Brushes.Gray;
        }

        public void SetAcceptedClearance(double clearance)
        {
            stkAcceptedClearance.Visibility = Visibility.Visible;
            lblAcceptedClearance.Content = $"{clearance:0.##}mm";
        }

        #region properties

        private bool _included;
        public bool Included
        {
            get {
                if (!CanBeActive) return false;
                else return _included;
            }
            set {
                _included = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Included)));
            }
        }

        public bool CanBeActive
        {
            get => LocOp is TxWeldLocationOperation == false;
        }
        #endregion
        public event PropertyChangedEventHandler PropertyChanged;


        #region events
        private void lblName_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Included = !Included;
        }

        private void chkActivated_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) {
                chkActivated.IsChecked = chkActivated.IsChecked != true;
            }
        }
        #endregion

    }

    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 'value' is the item itself (bound via RelativeSource Self)
            // 'parameter' is the parent panel passed via ConverterParameter
            if (value is DependencyObject item) {
                var parent = VisualTreeHelper.GetParent(item) as Panel;
                if (parent != null)
                    return parent.Children.IndexOf((UIElement)item);
            }
            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PathOptimiser.OptimisationSystem;
using PathOptimiser.OptimisationSystem.Services.EnvelopeRecorders;

namespace PathOptimiser.DisplayGrid.DebugEnvelopeDisplay
{
    /// <summary>
    /// Interaction logic for EnvelopeDisplayGrid.xaml
    /// </summary>
    public partial class EnvelopeDisplayGrid : UserControl
    {
        public EnvelopeDisplayGrid()
        {
            InitializeComponent();
            EnvelopeRecorder.CollectionGot += SetCollection;
        }

        EnvelopeCollection _boundEnvelope;
        public EnvelopeCollection BoundEnvelope
        {
            get => _boundEnvelope;
            set {
                _boundEnvelope = value;
            }
        }

        public void SetCollection(object sender, EnvelopeCollection collection)
        {
            this.Dispatcher.Invoke(() =>
            {
                lblOperationName.Content = collection.Operation.Name;

                this.Visibility = collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                stkRows.Children.Clear();

                foreach (var item in collection.Envelopes) {
                    var row = new EnvelopeDisplayRow();
                    row.SetEnvelope(item);
                    stkRows.Children.Add(row);
                }

                ctrlTimeLine.Update(collection);
            },
            System.Windows.Threading.DispatcherPriority.Render
            );
        }

        public void SetEnvelope(object sender, CollisionEnvelope env)
        {
            this.Dispatcher.Invoke(() =>
            {
                stkRows.Children.Clear();
                var row = new EnvelopeDisplayRow();
                row.SetEnvelope(env);
                stkRows.Children.Add(row);
            });
        }
    }
}

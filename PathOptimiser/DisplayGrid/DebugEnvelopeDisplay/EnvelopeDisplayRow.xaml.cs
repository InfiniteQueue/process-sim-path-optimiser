using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
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

namespace PathOptimiser.DisplayGrid.DebugEnvelopeDisplay
{
    /// <summary>
    /// Interaction logic for EnvelopeDisplayRow.xaml
    /// </summary>
    public partial class EnvelopeDisplayRow : UserControl
    {
        public EnvelopeDisplayRow()
        {
            InitializeComponent();
            lblStartVia = new Label() { HorizontalAlignment = HorizontalAlignment.Left };
            lblEndVia = new Label() { HorizontalAlignment = HorizontalAlignment.Left };
            lblTotalFrames = new Label() { HorizontalAlignment = HorizontalAlignment.Left };
            lblScore = new Label() { HorizontalAlignment = HorizontalAlignment.Left };
            stkPanel.Children.Add(lblStartVia);
            stkPanel.Children.Add(lblEndVia);
            stkPanel.Children.Add(lblTotalFrames);
            stkPanel.Children.Add(lblScore);
        }

        Label lblStartVia;
        Label lblEndVia;
        Label lblTotalFrames;
        Label lblScore;

        public void SetEnvelope(CollisionEnvelope envelope)
        {
            lblStartVia.Content = $"First Via: {envelope.CollidingVias.First().Name}";
            lblEndVia.Content = $"Last Via: {envelope.CollidingVias.Last().Name}";
            lblTotalFrames.Content = $"Colliding Frames: {envelope.CollidingFrameCount}";
            lblScore.Content = $"Collision Score: {Math.Pow( envelope.CollisionScore, 1.0/3):0.###}";
        }

        StackPanel stkPanel => Content as StackPanel;
    }
}

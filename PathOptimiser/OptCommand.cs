using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Tecnomatix.Engineering;

namespace PathOptimiser
{
    public class OptCommand : TxButtonCommand
    {
        public override string Category => "Robot";
        public override string Name => "Path Optimiser";

        MainWindow mainWindow;

        public static Thread UIThread;

        static Window DummyWindow;

        public static bool IsRunning;

        public static Dispatcher TxDispatcher => DummyWindow.Dispatcher;

        public static Dispatcher UiDispatcher;

        static bool Setup = false;

        public override void Execute(object cmdParams)
        {

            DummyWindow?.Close();
            DummyWindow = new Window();

            if (!Setup) {
                var ready = new ManualResetEventSlim();

                UIThread = new Thread(() =>
                {
                    // Capture the dispatcher from *inside* the thread,
                    // because Dispatcher.CurrentDispatcher is thread-local
                    UiDispatcher = Dispatcher.CurrentDispatcher;

                    // Signal the calling thread that _uiDispatcher is now set
                    ready.Set();

                    // Pump the message loop — this keeps the thread alive indefinitely
                    Dispatcher.Run();
                });

                UIThread.SetApartmentState(ApartmentState.STA);
                UIThread.IsBackground = true; // Won't block app shutdown
                UIThread.Start();

                ready.Wait();

                Setup = true;
            }

            if (mainWindow != null) {
                UiDispatcher.Invoke(() => mainWindow.Show());
                return;
            }
            else {


                UiDispatcher.Invoke(() =>
                {
                    mainWindow = new MainWindow();
                    mainWindow.Closing += (s, e) => { e.Cancel = true; mainWindow.Hide(); };
                    mainWindow.Show();
                });
            }
        }
    }
}

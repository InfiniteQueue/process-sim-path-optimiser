using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace PathOptimiser
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }
    }

//public static class TaskbarFlash
//    {
//        [DllImport("user32.dll")]
//        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

//        [StructLayout(LayoutKind.Sequential)]
//        private struct FLASHWINFO
//        {
//            public uint cbSize;
//            public IntPtr hwnd;
//            public uint dwFlags;
//            public uint uCount;
//            public uint dwTimeout;
//        }

//        private const uint FLASHW_STOP = 0;
//        private const uint FLASHW_CAPTION = 1;
//        private const uint FLASHW_TRAY = 2;
//        private const uint FLASHW_ALL = 3; // Caption + Tray
//        private const uint FLASHW_TIMER = 4; // Flash continuously
//        private const uint FLASHW_TIMERNOFG = 12; // Flash until window comes to foreground

//        public static void Flash(Window window, uint count = 5)
//        {
//            var helper = new WindowInteropHelper(window);
//            var info = new FLASHWINFO
//            {
//                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
//                hwnd = helper.Handle,
//                dwFlags = FLASHW_ALL,
//                uCount = count,
//                dwTimeout = 0 // Use default cursor blink rate
//            };
//            FlashWindowEx(ref info);
//        }

//        public static void FlashUntilFocused(Window window)
//        {
//            var helper = new WindowInteropHelper(window);
//            var info = new FLASHWINFO
//            {
//                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
//                hwnd = helper.Handle,
//                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
//                uCount = uint.MaxValue,
//                dwTimeout = 0
//            };
//            FlashWindowEx(ref info);
//        }

//        public static void StopFlash(Window window)
//        {
//            var helper = new WindowInteropHelper(window);
//            var info = new FLASHWINFO
//            {
//                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
//                hwnd = helper.Handle,
//                dwFlags = FLASHW_STOP,
//                uCount = 0,
//                dwTimeout = 0
//            };
//            FlashWindowEx(ref info);
//        }
//    }
}

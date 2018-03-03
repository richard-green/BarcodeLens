using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ZXing;

namespace BarcodeLens.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BarcodeReader reader;

        public MainWindow()
        {
            InitializeComponent();

            reader = new BarcodeReader();

            var writer = new ZXing.BarcodeWriter();
            writer.Format = BarcodeFormat.QR_CODE;
            var bmp = writer.Write("BarcodeLens");
            bmp.Save(@"D:\BarcodeLens.bmp");
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var cappedDelta = MathExt.Constrain(e.Delta, -30, 30);
            this.Top -= cappedDelta / 2;
            this.Left -= cappedDelta / 2;
            this.Width = MathExt.Constrain(this.Width + cappedDelta, 200, 800);
            this.Height = MathExt.Constrain(this.Height + cappedDelta, 200, 800);
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ScanNow();
        }

        private async void Scan_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            ScanNow();
        }

        private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void ScanNow()
        {
            Stopwatch timer = Stopwatch.StartNew();
            try
            {
                var capture = CaptureWindowPicture();

                if (capture != null)
                {
                    var decoded = reader.Decode(capture);

                    if (decoded != null)
                    {
                        timer.Stop();
                        var text = decoded.Text;
                        Clipboard.SetText(text);

                        if (Regex.IsMatch(text, @"^(([a-zA-Z]{1})|([a-zA-Z]{1}[a-zA-Z]{1})|([a-zA-Z]{1}[0-9]{1})|([0-9]{1}[a-zA-Z]{1})|([a-zA-Z0-9][a-zA-Z0-9-_]{1,61}[a-zA-Z0-9]))\.([a-zA-Z]{2,6}|[a-zA-Z0-9-]{2,30}\.[a-zA-Z]{2,3})"))
                        {
                            text = "http://" + text;
                        }

                        if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && uri.Scheme.AnyOf(StringComparison.CurrentCultureIgnoreCase, "http", "https"))
                        {
                            var response = MessageBox.Show($"Decoded the following URL:\r\n\r\n{text}\r\n\r\nVisit site?", "Decoded Data", MessageBoxButton.YesNo);

                            if (response == MessageBoxResult.Yes)
                            {
                                Process.Start(text);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Decoded the following data:\r\n\r\n{text}", "Decoded Data");
                        }

                        return;
                    }
                    else
                    {
                        MessageBox.Show("Could not decode barcode", "Error");
                    }
                }
                else
                {
                    MessageBox.Show("Could not capture image", "Error");
                }
            }
            catch
            {
                MessageBox.Show("Could not decode barcode", "Error");
            }
        }

        private Bitmap CaptureWindowPicture()
        {
            try
            {
                var x = (int)this.Left;
                var y = (int)this.Top;
                var w = (int)this.Width;
                var h = (int)this.Height;
                Bitmap capture = new Bitmap(w, h);
                Graphics graphics = Graphics.FromImage(capture);
                IntPtr dc1 = graphics.GetHdc();
                IntPtr dc2 = NativeMethods.GetWindowDC(NativeMethods.GetDesktopWindow());
                NativeMethods.BitBlt(dc1, 0, 0, w, h, dc2, x, y, 13369376);
                graphics.ReleaseHdc(dc1);
                return capture;
            }
            catch
            {
                return null;
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
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
            reader.Options.TryHarder = true;
            reader.AutoRotate = true;
        }

        public bool CtrlDown => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        public bool ShiftDown => Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var cappedDelta = MathExt.Constrain(e.Delta, ShiftDown ? -5 : -30, ShiftDown ? 5 : 30);
            var newWidth = CtrlDown ? this.Width : MathExt.Constrain(this.Width + cappedDelta, 100, 800);
            var newHeight = MathExt.Constrain(this.Height + cappedDelta, 100, 800);
            if (newWidth != this.Width || newHeight != this.Height)
            {
                this.Left -= (newWidth - this.Width) / 2;
                this.Top -= (newHeight - this.Height) / 2;
                this.Width = newWidth;
                this.Height = newHeight;
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ScanNow();
        }

        private async void Scan_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(200);
            ScanNow();
        }

        private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void ScanNow()
        {
            try
            {
                var capture = CaptureWindowPicture();

                if (capture != null)
                {
                    var decoded = TryDecode(capture);

                    if (decoded != null)
                    {
                        var text = decoded.Text;
                        Clipboard.SetText(text);

                        if (TryGetUri(text, out var uri) && uri.Scheme.AnyOf(StringComparison.CurrentCultureIgnoreCase, "http", "https"))
                        {
                            var response = MessageBox.Show($"Decoded the following data:\r\n\r\n{text}\r\n\r\nVisit this site?\r\n\r\n{uri.ToString()}", "Decoded Data", MessageBoxButton.YesNo);

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

        private Result TryDecode(Bitmap capture)
        {
            var result = reader.Decode(capture);

            if (result == null)
            {
                if (Debugger.IsAttached)
                {
                    DumpScan(capture, "FailedOnce");
                }

                capture = ResizeBitmap(capture, 800, 800, InterpolationMode.HighQualityBicubic, Color.White);
                capture = AddBorder(capture, Color.White, 100);
                capture = AdjustContrast(capture, 30);
                result = reader.Decode(capture);

                if (Debugger.IsAttached)
                {
                    DumpScan(capture, result == null ? "FailedTwice" : "ResizeSuccessful");
                }
            }

            return result;
        }

        private Bitmap ResizeBitmap(Bitmap image, int width, int height, InterpolationMode mode, Color backgroundColor)
        {
            // Attribution: https://stackoverflow.com/questions/10442269/scaling-a-system-drawing-bitmap-to-a-given-size-while-maintaining-aspect-ratio

            var brush = new SolidBrush(backgroundColor);
            var scale = Math.Min((float)width / (float)image.Width, (float)height / (float)image.Height);
            var scaleWidth = (int)(image.Width * scale);
            var scaleHeight = (int)(image.Height * scale);
            var newRect = new Rectangle(((int)width - scaleWidth) / 2, ((int)height - scaleHeight) / 2, scaleWidth, scaleHeight);
            var newImage = new Bitmap((int)width, (int)height);
            var graphics = Graphics.FromImage(newImage);
            graphics.FillRectangle(brush, new RectangleF(0, 0, width, height));
            graphics.InterpolationMode = mode;
            graphics.DrawImage(image, newRect);
            return newImage;
        }

        private Bitmap AddBorder(Bitmap image, Color color, int width)
        {
            var newImage = new Bitmap(image.Width + width * 2, image.Height + width * 2);
            var graphics = Graphics.FromImage(newImage);
            var newRect = new Rectangle(width, width, image.Width, image.Height);
            var brush = new SolidBrush(color);
            graphics.FillRectangle(brush, new RectangleF(0, 0, newImage.Width, newImage.Height));
            graphics.DrawImage(image, newRect);
            return newImage;
        }

        public static Bitmap AdjustContrast(Bitmap image, float value)
        {
            // Attribution: https://stackoverflow.com/questions/3115076/adjust-the-contrast-of-an-image-in-c-sharp-efficiently

            value = (100.0f + value) / 100.0f;
            value *= value;
            var newImage = (Bitmap)image.Clone();
            var dimensions = new Rectangle(0, 0, newImage.Width, newImage.Height);
            var data = newImage.LockBits(dimensions, ImageLockMode.ReadWrite, newImage.PixelFormat);
            int Height = newImage.Height;
            int Width = newImage.Width;

            unsafe
            {
                for (int y = 0; y < Height; ++y)
                {
                    byte* row = (byte*)data.Scan0 + (y * data.Stride);
                    int columnOffset = 0;
                    for (int x = 0; x < Width; ++x)
                    {
                        byte B = row[columnOffset];
                        byte G = row[columnOffset + 1];
                        byte R = row[columnOffset + 2];

                        float red = R / 255.0f;
                        float green = G / 255.0f;
                        float blue = B / 255.0f;
                        red = (((red - 0.5f) * value) + 0.5f) * 255.0f;
                        green = (((green - 0.5f) * value) + 0.5f) * 255.0f;
                        blue = (((blue - 0.5f) * value) + 0.5f) * 255.0f;

                        int iR = (int)red;
                        iR = iR > 255 ? 255 : iR;
                        iR = iR < 0 ? 0 : iR;
                        int iG = (int)green;
                        iG = iG > 255 ? 255 : iG;
                        iG = iG < 0 ? 0 : iG;
                        int iB = (int)blue;
                        iB = iB > 255 ? 255 : iB;
                        iB = iB < 0 ? 0 : iB;

                        row[columnOffset] = (byte)iB;
                        row[columnOffset + 1] = (byte)iG;
                        row[columnOffset + 2] = (byte)iR;

                        columnOffset += 4;
                    }
                }
            }

            newImage.UnlockBits(data);

            return newImage;
        }

        private void DumpScan(Bitmap capture, string postfix)
        {
            var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "BarcodeLensScans");
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            capture.Save(Path.Combine(outputPath, $"{DateTime.UtcNow.Ticks}-{postfix}.bmp"));
        }

        Regex ValidateUriScheme = new Regex(@"^[a-z][a-z0-9]{0,20}([\+\-\.][a-z0-9]{1,20}){0,4}://", RegexOptions.IgnoreCase);

        bool TryGetUri(string text, out Uri uri)
        {
            uri = null;
            text = text.Trim();
            text = text.Split(' ', '\t')[0];
            return ValidateUriScheme.IsMatch(text) && Uri.TryCreate(text, UriKind.Absolute, out uri);
        }

        private Bitmap CaptureWindowPicture()
        {
            // Attribution: https://www.codeproject.com/Articles/91487/Screen-Capture-in-WPF-WinForms-Application

            try
            {
                var borderWidth = OuterBorder.BorderThickness.Left + InnerBorder.BorderThickness.Left + 1;
                var x = (int)(this.Left + borderWidth);
                var y = (int)(this.Top + borderWidth);
                var w = (int)(this.Width - borderWidth * 2);
                var h = (int)(this.Height - borderWidth * 2);
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

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiveSPICE
{
    public class ImageButton : Button, INotifyPropertyChanged
    {
        static ImageButton() { DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageButton), new FrameworkPropertyMetadata(typeof(ImageButton))); }
        private Image enabled, disabled;

        private double width = double.NaN, height = double.NaN;
        [LocalizabilityAttribute(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverterAttribute(typeof(LengthConverter))]
        public double ImageWidth { get { return width; } set { width = value; NotifyChanged(nameof(ImageWidth)); } }
        [LocalizabilityAttribute(LocalizationCategory.None, Readability = Readability.Unreadable)]
        [TypeConverterAttribute(typeof(LengthConverter))]
        public double ImageHeight { get { return height; } set { height = value; NotifyChanged(nameof(ImageHeight)); } }

        public ImageSource Source
        {
            get { return enabled?.Source; }
            set
            {
                if (value != null)
                {
                    enabled = new Image() { Source = value };
                    if (enabled != null)
                    {
                        disabled = MakeDisabledImage(enabled);
                        enabled.SetBinding(Image.WidthProperty, new Binding("ImageWidth") { Source = this });
                        enabled.SetBinding(Image.HeightProperty, new Binding("ImageHeight") { Source = this });
                        disabled.SetBinding(Image.WidthProperty, new Binding("ImageWidth") { Source = this });
                        disabled.SetBinding(Image.HeightProperty, new Binding("ImageHeight") { Source = this });
                    }
                }
                else
                {
                    enabled = null;
                    disabled = null;
                }
                Update();
                NotifyChanged(nameof(Source));
            }
        }

        public ICommand CommandImage
        {
            get { return base.Command; }
            set
            {
                base.Command = value;
                Source = Images.ForCommand(value);
            }
        }

        public ImageButton() { IsEnabledChanged += OnEnabledChanged; }

        private void Update() { Content = IsEnabled ? enabled : disabled; }

        private void OnEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) { Update(); }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public static Image MakeDisabledImage(Image Enabled)
        {
            WriteableBitmap bitmap = new WriteableBitmap(new FormatConvertedBitmap((BitmapSource)Enabled.Source, PixelFormats.Bgra32, null, 0.0));
            bitmap.Lock();

            byte[] row = new byte[bitmap.PixelWidth * 4];
            for (int i = 0; i < bitmap.PixelHeight; ++i)
            {
                Marshal.Copy(bitmap.BackBuffer + bitmap.BackBufferStride * i, row, 0, row.Length);

                for (int j = 0; j < bitmap.PixelWidth * 4; j += 4)
                {
                    double g =
                        0.114 * row[j + 0] +
                        0.587 * row[j + 1] +
                        0.299 * row[j + 2];
                    row[j + 0] = row[j + 1] = row[j + 2] = (byte)g;
                    row[j + 3] /= 2;
                }
                Marshal.Copy(row, 0, bitmap.BackBuffer + bitmap.BackBufferStride * i, row.Length);
            }

            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));

            bitmap.Unlock();
            return new Image() { Source = bitmap };
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LoadingDemo.Controls
{
    /// <summary>
    /// CustomLoading.xaml 的交互逻辑
    /// </summary>
    public partial class CustomLoading : UserControl
    {
        public CustomLoading()
        {
            InitializeComponent();
        }

        public Color FillColor
        {
            get { return (Color)GetValue(FillColorProperty); }
            set { SetValue(FillColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FillColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register("FillColor", typeof(Color), typeof(CustomLoading), new PropertyMetadata(Colors.Black));
    }
}

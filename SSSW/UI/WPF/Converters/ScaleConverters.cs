// ============================================================================
//  ScaleConverters.cs
//  Value converters dùng trong ShotWeightWindow (MVVM)
//  Namespace : SSSW.UI.WPF.Converters
// ============================================================================
using SSSW.UI.WPF.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color   = System.Windows.Media.Color;

namespace SSSW.UI.WPF.Converters
{
    /// <summary>Maps ToleranceCategory → background Brush cho DataGridRow.</summary>
    public class ToleranceToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToleranceCategory t)
                return t switch
                {
                    ToleranceCategory.Ok   => new SolidColorBrush(Color.FromRgb(212, 247, 220)),
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(255, 243, 205)),
                    ToleranceCategory.Err  => new SolidColorBrush(Color.FromRgb(253, 232, 232)),
                    _                      => new SolidColorBrush(Color.FromRgb(245, 245, 245))
                };
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>Maps ToleranceCategory → foreground Brush cho DataGridRow.</summary>
    public class ToleranceToForeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToleranceCategory t)
                return t switch
                {
                    ToleranceCategory.Ok   => new SolidColorBrush(Color.FromRgb(0,   100, 0)),
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(130,  80, 0)),
                    ToleranceCategory.Err  => new SolidColorBrush(Color.FromRgb(160,   0, 0)),
                    _                      => new SolidColorBrush(Color.FromRgb( 80,  80, 80))
                };
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    /// <summary>bool → Visibility (True = Visible, False = Collapsed).</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b)
               ? System.Windows.Visibility.Visible
               : System.Windows.Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is System.Windows.Visibility v && v == System.Windows.Visibility.Visible;
    }
}

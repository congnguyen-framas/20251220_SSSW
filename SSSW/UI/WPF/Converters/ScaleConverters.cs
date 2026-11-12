// ============================================================================
//  ScaleConverters.cs
//  Value converters dùng trong ShotWeightWindow (MVVM)
//  Namespace : SSSW.UI.WPF.Converters
// ============================================================================
using ScanAndScale.Core.Models;
using SSSW.UI.WPF.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Windows.Media.Brushes;
using Color   = System.Windows.Media.Color;

namespace SSSW.UI.WPF.Converters
{

    /// <summary>
    /// Giá trị + đơn vị theo dòng, đơn vị lấy từ cột Unit (FT600.C037: PRS / PCS / PCS-L / PCS-R).
    /// values[0] = số (double?), values[1] = C037, values[2] = C003 (tên step, chỉ dùng cho "mold").
    /// parameter "kiểu:format": "g" → g/prs | g/pcs, "count" → prs | pcs, "mold" → như count nhưng step Logo / Cleat_Ring luôn pcs
    /// (Mold Pairs Shot của 2 loại này là số chiếc, không theo Unit), "pcs" → pcs. Unit PU giữ hậu tố phía (pcs-L / pcs-R).
    /// Giá trị null → rỗng; Unit trống → prs.
    /// </summary>
    public class ValueUnitConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 1 || values[0] is not IConvertible raw || values[0] == DependencyProperty.UnsetValue) return string.Empty;
            var num = raw.ToDouble(CultureInfo.InvariantCulture);
            var spec = (parameter as string ?? "g").Split(':');          // "g:F3" → đơn vị g, format F3
            var fmt = spec.Length > 1 ? spec[1] : "F2";

            var count = spec[0] == "pcs" ? "pcs" : CountFromUnit(values.Length > 1 ? values[1] as string : null);
            if (spec[0] == "mold" && IsPieceMold(values)) count = "pcs";
            var unit = spec[0] == "g" ? "g/" + count : count;
            return $"{num.ToString(fmt, culture)} {unit}";
        }

        /// <summary>PRS → "prs"; PCS / PCS-L / PCS-R → "pcs" (giữ "-L"/"-R"); trống / khác → "prs".</summary>
        private static string CountFromUnit(string? unit)
        {
            var u = unit?.Trim();
            if (string.IsNullOrEmpty(u)) return "prs";
            if (u.StartsWith("PCS", StringComparison.OrdinalIgnoreCase)) return "pcs" + u[3..];
            return "prs";
        }

        private static bool IsPieceMold(object[] values)
        {
            var name = values.Length > 2 ? (values[2] as string)?.Trim() : null;
            return name != null && (name.StartsWith("Logo", StringComparison.OrdinalIgnoreCase)
                                    || name.StartsWith("Cleat_Ring", StringComparison.OrdinalIgnoreCase));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }


    /// <summary>null / chuỗi rỗng → Collapsed, ngược lại Visible.</summary>
    public class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

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
            => System.Windows.Data.Binding.DoNothing;
    }

    /// <summary>Maps ToleranceCategory → border Brush cho value cells (STD / Δ / ACTUAL).</summary>
    public class ToleranceToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToleranceCategory t)
                return t switch
                {
                    ToleranceCategory.Ok   => new SolidColorBrush(Color.FromRgb(0x43, 0xA0, 0x47)), // green
                    ToleranceCategory.Warn => new SolidColorBrush(Color.FromRgb(0xFB, 0x8C, 0x00)), // amber
                    ToleranceCategory.Err  => new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35)), // red
                    _                      => new SolidColorBrush(Color.FromRgb(0xB0, 0xBE, 0xC5))  // gray
                };
            return Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
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
            => System.Windows.Data.Binding.DoNothing;
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

    // ── ScanAndScale.Core – Device Status Converters ──────────────────────────

    /// <summary>
    /// DriverStatus → mau LED trang thai ket noi thiet bi.
    /// Connected = xanh la  Disconnected = do  Reconnecting = vang  Unknown = xam
    /// </summary>
    [ValueConversion(typeof(DriverStatus), typeof(System.Drawing.Brush))]
    public class DriverStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DriverStatus status)
                return status switch
                {
                    DriverStatus.Connected    => new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45)),
                    DriverStatus.Disconnected => new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45)),
                    DriverStatus.Reconnecting => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),
                    _                         => new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D))
                };
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

    /// <summary>
    /// bool (ScaleStable) → mau chu so can.
    /// Stable = xanh dam  Unstable = do dam
    /// </summary>
    [ValueConversion(typeof(bool), typeof(System.Drawing.Brush))]
    public class StableToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool stable)
                return stable
                    ? new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20))
                    : new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

    /// <summary>
    /// bool (ScaleTare) → mau nen vung hien thi can.
    /// Tare = nen hong nhat  Normal = trang
    /// </summary>
    [ValueConversion(typeof(bool), typeof(System.Drawing.Brush))]
    public class TareToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool tare)
                return tare
                    ? new SolidColorBrush(Color.FromRgb(0xFF, 0xD0, 0xD0))
                    : Brushes.White;
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => System.Windows.Data.Binding.DoNothing;
    }

    /// <summary>
    /// Provides a value converter that maps a Boolean value to a System.Windows.Visibility value, returning Visible
    /// when the input is false and Collapsed when the input is true.
    /// </summary>
    /// <remarks>This converter is typically used in WPF data binding scenarios to invert the standard
    /// Boolean-to-Visibility mapping. It is useful when you want a UI element to be visible when a bound Boolean
    /// property is false, and collapsed when it is true. The converter does not support conversion to Hidden; only
    /// Visible and Collapsed are used.</remarks>
    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = value is bool b && b;
            return val ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is System.Windows.Visibility v && v == System.Windows.Visibility.Visible;
    }
}

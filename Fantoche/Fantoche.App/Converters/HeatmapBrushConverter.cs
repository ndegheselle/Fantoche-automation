using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Fantoche.App.Converters
{
    /// <summary>
    /// The color of an intensity (from 0 to 1) on a heatmap, along the "rocket" palette of seaborn :
    /// from a near black purple to a light cream, the lighter the more intense. 0 is left transparent,
    /// nothing being there to show.
    /// </summary>
    public class HeatmapBrushConverter : IValueConverter
    {
        /// <summary>The cell itself.</summary>
        public static readonly HeatmapBrushConverter Default = new(false);

        /// <summary>A text over the cell, kept readable on the light end of the palette.</summary>
        public static readonly HeatmapBrushConverter Text = new(true);

        private static readonly (double At, Color Color)[] _stops =
        [
            (0.0, Color.FromRgb(0x03, 0x05, 0x1A)),
            (0.2, Color.FromRgb(0x4C, 0x1D, 0x4B)),
            (0.4, Color.FromRgb(0xA1, 0x1A, 0x5B)),
            (0.6, Color.FromRgb(0xE8, 0x3F, 0x3F)),
            (0.8, Color.FromRgb(0xF6, 0x9C, 0x73)),
            (1.0, Color.FromRgb(0xFA, 0xEB, 0xDD)),
        ];

        /// <summary>Past it the cells are light enough for a dark text.</summary>
        private const double LightFrom = 0.7;

        private readonly bool _isText;

        private HeatmapBrushConverter(bool isText)
        {
            _isText = isText;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double intensity = Math.Clamp(value is double x ? x : 0, 0, 1);
            if (_isText)
                return intensity >= LightFrom ? Brushes.Black : Brushes.White;
            if (intensity == 0)
                return Brushes.Transparent;

            var brush = new SolidColorBrush(ColorAt(intensity));
            brush.Freeze();
            return brush;
        }

        private static Color ColorAt(double intensity)
        {
            int upper = Array.FindIndex(_stops, x => x.At >= intensity);
            if (upper <= 0)
                return _stops[0].Color;

            (double fromAt, Color from) = _stops[upper - 1];
            (double toAt, Color to) = _stops[upper];
            double t = (intensity - fromAt) / (toAt - fromAt);
            return Color.FromRgb(Lerp(from.R, to.R, t), Lerp(from.G, to.G, t), Lerp(from.B, to.B, t));
        }

        private static byte Lerp(byte from, byte to, double t) => (byte)Math.Round(from + (to - from) * t);

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

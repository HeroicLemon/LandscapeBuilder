using System;
using System.Globalization;
using System.Windows.Data;
using LandscapeBuilderLib;

namespace LandscapeBuilderGUI.Converters
{
    class DrawingColorToThermalColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Drawing.Color color = (System.Drawing.Color)value;
            uint uintColor = (uint)((color.A << 24) | (color.R << 16) | (color.G << 8) | (color.B << 0));

            ThermalColor thermalColor = (ThermalColor)uintColor;

            //if(!Enum.IsDefined(typeof(ThermalColor), thermalColor))
            //{
            //    thermalColor = ThermalColor.Custom;
            //}

            return thermalColor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            uint thermalColor = (uint)value;

            byte a = (byte)(thermalColor >> 24);
            byte r = (byte)(thermalColor >> 16);
            byte g = (byte)(thermalColor >> 8);
            byte b = (byte)(thermalColor >> 0);

            return System.Drawing.Color.FromArgb(a, r, g, b);
        }
    }
}

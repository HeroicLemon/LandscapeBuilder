using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using LandscapeBuilderLib;

namespace LandscapeBuilderGUI.Converters
{
    // If the value is a ColoredLandData, return Visible. Otherwise return Collapsed.
    class LandDataToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility vis = Visibility.Collapsed;

            if(parameter.ToString() == "ColoredLandData" && value is ColoredLandData)
            {
                vis = Visibility.Visible;
            }
            else if(parameter.ToString() == "TexturedLandData" && value is TexturedLandData)
            {
                vis = Visibility.Visible;
            }

            return vis;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

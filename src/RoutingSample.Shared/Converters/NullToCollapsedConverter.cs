using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
#elif WINDOWS_PHONE
using System.Windows;
using System.Windows.Data;
#endif
namespace RoutingSample.Converters
{
	public class NullToCollapsedConverter : IValueConverter
	{
#if WINDOWS_PHONE
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#elif NETFX_CORE
		public object Convert(object value, Type targetType, object parameter, string language)
#endif
		{
			return value == null ? Visibility.Collapsed : Visibility.Visible;
		}

#if WINDOWS_PHONE
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#elif NETFX_CORE
		public object ConvertBack(object value, Type targetType, object parameter, string language)
#endif
		{
			throw new NotImplementedException();
		}
	}
}

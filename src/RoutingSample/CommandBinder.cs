using ESRI.ArcGIS.Runtime;
using ESRI.ArcGIS.Runtime.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Xaml;
#elif WINDOWS_PHONE
using System.Windows;
#endif

namespace RoutingSample
{
	/// <summary>
	/// Binding helpers
	/// </summary>
	public class CommandBinder
	{
		/// <summary>
		/// This command binding allows you to set the extent on a map from your view-model through binding
		/// </summary>
		public static Envelope GetZoomTo(DependencyObject obj)
		{
			return (Envelope)obj.GetValue(ZoomToProperty);
		}

		/// <summary>
		/// This command binding allows you to set the extent on a map from your view-model through binding
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="extent"></param>
		public static void SetZoomTo(DependencyObject obj, Envelope extent)
		{
			obj.SetValue(ZoomToProperty, extent);
		}

		/// <summary>
		/// Identifies the ZoomTo Attached Property.
		/// </summary>
		public static readonly DependencyProperty ZoomToProperty =
			DependencyProperty.RegisterAttached("ZoomTo", typeof(Envelope), typeof(CommandBinder), new PropertyMetadata(null, OnZoomToPropertyChanged));

		private static void OnZoomToPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is Map)
			{
				Map map = d as Map;
				if (e.NewValue is Envelope)
				{
					var extent = (e.NewValue as Envelope);
					if (map.Extent != null)
					{
						//If requested extent is a different spatial reference, project it first
						if (extent.SpatialReference != null && map.SpatialReference != extent.SpatialReference)
							extent = GeometryEngine.Project(extent, map.SpatialReference) as Envelope;
						map.ZoomTo(extent);
					}
					else //Map not ready, wait for the map to set its extent
					{
						EventHandler handler = null;
						handler = (sender, eventargs) =>
						{
							if (map.Extent != null)
							{
								map.ExtentChanged -= handler;
								if (map.GetValue(ZoomToProperty) == extent) //Ensure another extent hasn't been requested in the meantime
								{
									OnZoomToPropertyChanged(d, e);
								}
							}
						};
						map.ExtentChanged += handler;
					}
				}
			}
		}
	}
}

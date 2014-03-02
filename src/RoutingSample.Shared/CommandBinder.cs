using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI.Xaml;
#else
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
		/// This command binding allows you to set the extent on a mapView from your view-model through binding
		/// </summary>
		public static Envelope GetZoomTo(DependencyObject obj)
		{
			return (Envelope)obj.GetValue(ZoomToProperty);
		}

		/// <summary>
		/// This command binding allows you to set the extent on a mapView from your view-model through binding
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
			if (d is MapView)
			{
				MapView mapView = d as MapView;
				if (e.NewValue is Geometry)
				{
					if (mapView.Extent != null)
					{
						var _ = mapView.SetViewAsync((Geometry)e.NewValue);
					}
					else //Map not ready, try again later (yeah... I know... not pretty... I could do better by listening for property changed but it's late and I'm tired and lazy :)
					{
						Task.Delay(100).ContinueWith(_ => OnZoomToPropertyChanged(d, e), TaskScheduler.FromCurrentSynchronizationContext());
					}
				}
			}
		}
	}
}

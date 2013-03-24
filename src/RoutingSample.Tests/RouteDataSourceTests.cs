using ESRI.ArcGIS.Runtime;
using ESRI.ArcGIS.Runtime.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using RoutingSample.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RoutingSample.Tests
{
	[TestClass]
	public class RouteDataSourceTests
	{
		private static RouteResult route;
		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			var testHandler = TestMessageHandler.FromResourceResponse("RouteFrom1172_34ToRedlandsCA");
			var task = new RouteService(testHandler).GetRoute(
				new MapPoint(-117.2, 34) { SpatialReference = SpatialReference.Wgs84 },
				new MapPoint(-117.18253721699972, 34.055566969000438) { SpatialReference = SpatialReference.Wgs84 },
				CancellationToken.None);
			task.Wait();
			route = task.Result;
		}
		[TestMethod]
		public void RouteDataSource_SetCurrentLocation()
		{
			//Generate route from mock up response
			RouteDataSource ds = new RouteDataSource(route);
			//Before setting location these values are not initialize
			Assert.AreEqual(0, ds.DistanceToDestination);
			Assert.IsNull(ds.NextManeuver);
			//Push location to data source
			ds.SetCurrentLocation(new MapPoint(-117.16537, 34.122730, SpatialReference.Wgs84));

			Assert.AreEqual(21166.0, ds.DistanceToDestination);
			Assert.AreEqual(new TimeSpan(0, 18, 36), ds.TimeToDestination);
			Assert.AreEqual(124, ds.DistanceToWaypoint);
			Assert.AreEqual("13 mi", ds.MilesToDestination);
			Assert.AreEqual("Turn left on Club View Dr", ds.NextManeuver);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using RoutingSample.Models;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.Runtime;

namespace RoutingSample.Tests
{
    [TestClass]
    public class RouteServiceTests
    {
        [TestMethod]
        public async Task GetLocationTest()
        {
			var testHandler = TestMessageHandler.FromResourceResponse("GetLocationRedlands");
			var result = await new RouteService(testHandler).Geocode("Redlands, CA", CancellationToken.None);
			Assert.IsNotNull(result);	
			Assert.AreEqual(-117.18253721699972, result.X);
			Assert.AreEqual(34.055566969000438, result.Y);
			Assert.IsNotNull(result.SpatialReference);
			Assert.AreEqual(4326, result.SpatialReference.Wkid);
        }

		[TestMethod]
		public async Task GetRouteTest()
		{
			var testHandler = TestMessageHandler.FromResourceResponse("RouteFrom1172_34ToRedlandsCA");
			var result = await new RouteService(testHandler).GetRoute(
				new MapPoint(-117.2, 34) { SpatialReference = SpatialReference.Wgs84 },
				new MapPoint(-117.18253721699972, 34.055566969000438) { SpatialReference = SpatialReference.Wgs84 },
				CancellationToken.None);
			Assert.IsNotNull(result);
			
		}
    }
}

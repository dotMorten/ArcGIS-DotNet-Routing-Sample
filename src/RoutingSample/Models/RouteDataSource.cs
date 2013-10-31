using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalyst;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if NETFX_CORE
using Windows.UI;
#else
using System.Windows.Media;
#endif

namespace RoutingSample
{
	/// <summary>
	/// Data source that wraps a route result and based on a location exposes properties
	/// like next turn, distance, etc
	/// </summary>
	public class RouteDataSource : ModelBase
	{
		private readonly RouteResult m_route;

		/// <summary>
		/// Initializes a new instance of the <see cref="RouteDataSource"/> class.
		/// </summary>
		/// <param name="route">The route.</param>
		public RouteDataSource(RouteResult route)
		{
			m_route = route;
			if (IsDesignMode) //Design time data
			{
				DistanceToDestination = 1000;
				DistanceToWaypoint = 500;
				TimeToWaypoint = new TimeSpan(1, 2, 3);
				TimeToDestination = new TimeSpan(2, 3, 4);
				NextManeuver = "Turn right onto Main St.";
			}
			else
			{
				InitializeRoute();
			}
		}

		public Uri ManueverImage { get; private set; }

		public IList<Graphic> Maneuvers { get; private set; }

		public IList<Graphic> RouteLines { get; private set; }

		public string NextManeuver { get; private set; }

		public double DistanceToDestination { get; private set; }

		public string MilesToDestination
		{
			get { return MetersToMilesFeet(DistanceToDestination); }
		}

		public TimeSpan TimeToDestination { get; private set; }

		public TimeSpan TimeToWaypoint { get; private set; }

		public double DistanceToWaypoint { get; private set; }

		public string MilesToWaypoint
		{
			get { return MetersToMilesFeet(DistanceToWaypoint); }
		}

		public MapPoint SnappedLocation { get; private set; }

		public Uri ManeuverImage { get; private set; }

		private string MetersToMilesFeet(double distance)
		{

			var miles = Units.Miles.ConvertFromMeters(distance);
			if (miles >= 10)
				return string.Format("{0:0} mi", miles);
			if (miles >= 1)
				return string.Format("{0:0.0} mi", miles);
			else if (miles >= .25)
				return string.Format("{0:0.00} mi", miles);
			else //less than .25mi
				return string.Format("{0:0} ft", Units.Feet.ConvertFromMeters(distance));
		}

		private void InitializeRoute()
		{
			var routeLines = new ObservableCollection<Graphic>();
			var maneuvers = new ObservableCollection<Graphic>();
			var lineSymbol = new SimpleLineSymbol() { Width = 10, Color = Color.FromArgb(190, 50, 50, 255) };
			var turnSymbol = new SimpleMarkerSymbol() { Size = 20, Outline = new SimpleLineSymbol() { Color = Color.FromArgb(255, 0, 255, 0), Width = 5 }, Color = Color.FromArgb(180, 255, 255, 255) };
			foreach (var directions in m_route.Routes)
			{
				routeLines.Add(new Graphic() { Geometry = CombineParts(directions.RouteGraphic.Geometry as Polyline), Symbol = lineSymbol });
				var turns = (from a in directions.RouteDirections select a.Geometry).OfType<Polyline>().Select(line => line.Paths[0][0]);
				foreach (var m in turns)
				{
					maneuvers.Add(new Graphic() { Geometry = new MapPoint(m, SpatialReferences.Wgs84), Symbol = turnSymbol });
				}
			}
			RouteLines = routeLines;
			Maneuvers = maneuvers;
		}

		/// <summary>
		/// Call this to set your current location and update directions based on that.
		/// </summary>
		/// <param name="location"></param>
		public void SetCurrentLocation(MapPoint location)
		{
			RouteDirection closest = null;
			double distance = double.NaN;
			MapPoint snappedLocation = null;
			Route direction = null;
			// Find the route part that we are currently on by snapping to each segment and see which one is the closest
			foreach (var dir in m_route.Routes)
			{
				var closestCandidate = (from a in dir.RouteDirections
										where a.Geometry is Polyline
										select new { Direction = a, Proximity = GeometryEngine.NearestCoordinateInGeometry(a.Geometry, location) }).OrderBy(b => b.Proximity.Distance).FirstOrDefault();
				if (double.IsNaN(distance) || distance < closestCandidate.Proximity.Distance)
				{
					distance = closestCandidate.Proximity.Distance;
					closest = closestCandidate.Direction;
					snappedLocation = closestCandidate.Proximity.Point;
					direction = dir;
				}
			}
			if (closest != null)
			{
				var directions = direction.RouteDirections.ToList();
				var idx = directions.IndexOf(closest);
				if (idx < directions.Count)
				{
					RouteDirection next = directions[idx + 1];

					//calculate how much is left of current route segment
					var segment = closest.Geometry as Polyline;
					var proximity = GeometryEngine.NearestVertexInGeometry(segment, snappedLocation);
					double frac = 1 - GetFractionAlongLine(segment, proximity, snappedLocation);
					TimeSpan timeLeft = new TimeSpan((long)(closest.Time.Ticks * frac));
					double segmentLengthLeft = (Convert.ToDouble(closest.GetLength(Units.Meters))) * frac;
					//Sum up the time and lengths for the remaining route segments
					TimeSpan totalTimeLeft = timeLeft;
					double totallength = segmentLengthLeft;
					for (int i = idx + 1; i < directions.Count; i++)
					{
						totalTimeLeft += directions[i].Time;
						totallength += directions[i].GetLength(Units.Meters);
					}

					//Update properties
					TimeToWaypoint = timeLeft;
					TimeToDestination = totalTimeLeft;
					DistanceToWaypoint = Math.Round(segmentLengthLeft);
					DistanceToDestination = Math.Round(totallength);
					SnappedLocation = snappedLocation;
					var maneuverType = next.ManeuverType;
#if NETFX_CORE || WINDOWS_PHONE
					ManeuverImage = new Uri(string.Format("ms-appx:///Assets/Maneuvers/{0}.png", maneuverType));
#else
					ManeuverImage = new Uri(string.Format("pack://application:,,,/Assets/Maneuvers/{0}.png", maneuverType));
#endif
					NextManeuver = next.Text;

					RaisePropertiesChanged(new string[] {
						"NextManeuver","SnappedLocation", "CurrentDirection", "TimeToWaypoint", 
						"DistanceToDestination", "DistanceToWaypoint", "TimeToDestination",
						"MilesToDestination", "MilesToWaypoint", "ManeuverImage"
					});
				}
			}
		}

		private static Polyline CombineParts(Polyline line)
		{
			List<Coordinate> vertices = new List<Coordinate>();
			Coordinate lastvertex = line.Paths[0][0];
			vertices.Add(lastvertex);
			for (int i = 0; i < line.Paths.Count; i++)
			{
				for (int j = 1; j < line.Paths[i].Count; j++)
				{
					vertices.Add(line.Paths[i][j]);
				}
			}
			Polyline newline= new Polyline() { SpatialReference = line.SpatialReference };
			newline.Paths.AddPart(vertices);
			return newline;
		}
		
		// calculates how far down a line a certain point on the line is located as a value from 0..1
		private double GetFractionAlongLine(Polyline segment, ProximityResult proximity, MapPoint location)
		{
			double distance1 = 0;
			double distance2 = 0;
			int pointIndex = proximity.PointIndex;
			int vertexCount = segment.Paths[0].Count;
			var vertexPoint = segment.Paths[proximity.PartIndex][pointIndex];
			Coordinate previousPoint;
			int onSegmentIndex = 0;
			//Detect which line segment we currently are on
			if (pointIndex == 0) //Snapped to first vertex
				onSegmentIndex = 0;
			else if (pointIndex == vertexCount - 1) //Snapped to last vertex
				onSegmentIndex = segment.Paths[0].Count - 2;
			else
			{
				Coordinate nextPoint = segment.Paths[0][pointIndex + 1];
				var d1 = GeometryEngine.DistanceFromGeometry(new MapPoint(vertexPoint, segment.SpatialReference), new MapPoint(nextPoint, segment.SpatialReference));
				var d2 = GeometryEngine.DistanceFromGeometry(location, new MapPoint( nextPoint, segment.SpatialReference));
				if (d1 < d2)
					onSegmentIndex = pointIndex - 1;
				else
					onSegmentIndex = pointIndex;
			}
			previousPoint = segment.Paths[0][0];
			for (int j = 1; j < onSegmentIndex + 1; j++)
			{
				Coordinate point = segment.Paths[0][j];
				distance1 += GeometryEngine.DistanceFromGeometry(new MapPoint(previousPoint, segment.SpatialReference), new MapPoint(point, segment.SpatialReference));
				previousPoint = point;
			}
			distance1 += GeometryEngine.DistanceFromGeometry(new MapPoint(previousPoint, segment.SpatialReference), location);
			previousPoint = segment.Paths[0][onSegmentIndex + 1];
			distance2 = GeometryEngine.DistanceFromGeometry(location, new MapPoint(previousPoint, segment.SpatialReference));
			previousPoint = vertexPoint;
			for (int j = onSegmentIndex + 2; j < segment.Paths[0].Count; j++)
			{
				Coordinate point = segment.Paths[0][j];
				distance2 += GeometryEngine.DistanceFromGeometry(new MapPoint(previousPoint, segment.SpatialReference), new MapPoint(point, segment.SpatialReference));
				previousPoint = point;
			}


			//var previousPoint = proximity.PointIndex ? segment.GetPoint(proximity.PartIndex + 1, 0) : segment.GetPoint(proximity.PartIndex, proximity.PointIndex + 1);
			if (distance1 + distance2 == 0)
				return 1;
			return distance1 / (distance1 + distance2);
		}
	}
}

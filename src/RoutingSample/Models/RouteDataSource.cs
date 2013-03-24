using ESRI.ArcGIS.Runtime;
using ESRI.ArcGIS.Runtime.Symbology;
using ESRI.ArcGIS.Runtime.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

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

			var miles = LinearUnit.Mile.ConvertFromMeters(distance);
			if (miles >= 10)
				return string.Format("{0:0} mi", miles);
			if (miles >= 1)
				return string.Format("{0:0.0} mi", miles);
			else if (miles >= .25)
				return string.Format("{0:0.00} mi", miles);
			else //less than .25mi
				return string.Format("{0:0} ft", LinearUnit.Foot.ConvertFromMeters(distance));
		}

		private void InitializeRoute()
		{
			var routeLines = new ObservableCollection<Graphic>();
			var maneuvers = new ObservableCollection<Graphic>();
			var lineSymbol = new SimpleLineSymbol() { Width = 10, Color = Color.FromArgb(190, 50, 50, 255) };
			var turnSymbol = new SimpleMarkerSymbol() { Size = 20, Outline = new SimpleLineSymbol() { Color = Color.FromArgb(255, 0, 255, 0), Width = 5 }, Color = Color.FromArgb(180, 255, 255, 255) };
			foreach (var directions in m_route.Directions)
			{
				routeLines.Add(new Graphic() { Geometry = CombineParts(directions.MergedGeometry), Symbol = lineSymbol });
				var turns = (from a in directions.Graphics select a.Geometry).OfType<Polyline>().Select(line => line.GetPoint(0, 0));
				foreach (var m in turns)
				{
					maneuvers.Add(new Graphic() { Geometry = m, Symbol = turnSymbol });
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
			Graphic closest = null;
			double distance = double.NaN;
			MapPoint snappedLocation = null;
			Direction direction = null;
			// Find the route part that we are currently on by snapping to each segment and see which one is the closest
			foreach (var dir in m_route.Directions)
			{
				var closestCandidate = (from a in dir.Graphics
										select new { Graphic = a, Proximity = GeometryEngine.NearestCoordinateInGeometry(a.Geometry, location) }).OrderBy(b => b.Proximity.Distance).FirstOrDefault();
				if (double.IsNaN(distance) || distance < closestCandidate.Proximity.Distance)
				{
					distance = closestCandidate.Proximity.Distance;
					closest = closestCandidate.Graphic;
					snappedLocation = closestCandidate.Proximity.Point;
					direction = dir;
				}
			}
			if (closest != null)
			{
				var graphics = direction.Graphics.ToList();
				var idx = graphics.IndexOf(closest);
				if (idx < graphics.Count)
				{
					Graphic next = graphics[idx + 1];

					//calculate how much is left of current route segment
					var segment = closest.Geometry as Polyline;
					var proximity = GeometryEngine.NearestVertexInGeometry(segment, snappedLocation);
					double frac = 1 - GetFractionAlongLine(segment, proximity, snappedLocation);
					double timeLeft = (Convert.ToDouble(closest.Attributes["time"])) * frac;
					double segmentLengthLeft = (Convert.ToDouble(closest.Attributes["length"])) * frac;
					//Sum up the time and lengths for the remaining route segments
					double totalTimeLeft = timeLeft;
					double totallength = segmentLengthLeft;
					for (int i = idx + 1; i < graphics.Count; i++)
					{
						totalTimeLeft += Convert.ToDouble(graphics[i].Attributes["time"]);
						totallength += Convert.ToDouble(graphics[i].Attributes["length"]);
					}

					//Update properties
					TimeToWaypoint = TimeSpan.FromSeconds(Math.Round(timeLeft * 60));
					TimeToDestination = TimeSpan.FromSeconds(Math.Round(totalTimeLeft * 60));
					DistanceToWaypoint = Math.Round(segmentLengthLeft);
					if(DistanceToDestination < Math.Round(totallength))
					{
						System.Diagnostics.Debug.Assert(false);
					}
					DistanceToDestination = Math.Round(totallength);
					SnappedLocation = snappedLocation;
					var maneuverType = next.Attributes["maneuverType"];
					ManeuverImage = new Uri(string.Format("ms-appx:///Assets/Maneuvers/{0}.png", maneuverType));

					NextManeuver = next.Attributes["text"] as string;

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
			List<MapPoint> vertices = new List<MapPoint>();
			MapPoint lastvertex = line.GetPoint(0,0);
			vertices.Add(lastvertex);
			for (int i = 0; i < line.PartCount; i++)
			{
				for (int j = 1; j < line.GetPointCount(i); j++)
				{
					vertices.Add(line.GetPoint(i, j));
				}
			}
			Polyline newline= new Polyline() { SpatialReference = line.SpatialReference };
			newline.AddPart(vertices);
			return newline;
		}
		
		// calculates how far down a line a certain point on the line is located as a value from 0..1
		private double GetFractionAlongLine(Polyline segment, ProximityResult proximity, MapPoint location)
		{
			double distance1 = 0;
			double distance2 = 0;
			int pointIndex = proximity.PointIndex;
			int vertexCount = segment.GetPointCount(0);
			var vertexPoint = segment.GetPoint(proximity.PartIndex, pointIndex);
			MapPoint previousPoint;
			int onSegmentIndex = 0;
			//Detect which line segment we currently are on
			if (pointIndex == 0) //Snapped to first vertex
				onSegmentIndex = 0;
			else if (pointIndex == vertexCount - 1) //Snapped to last vertex
				onSegmentIndex = segment.GetPointCount(0) - 2;
			else
			{
				MapPoint nextPoint = segment.GetPoint(0, pointIndex + 1);
				var d1 = GeometryEngine.DistanceFromGeometry(vertexPoint, nextPoint);
				var d2 = GeometryEngine.DistanceFromGeometry(location, nextPoint);
				if (d1 < d2)
					onSegmentIndex = pointIndex - 1;
				else
					onSegmentIndex = pointIndex;
			}
			previousPoint = segment.GetPoint(0, 0);
			for (int j = 1; j < onSegmentIndex + 1; j++)
			{
				MapPoint point = segment.GetPoint(0, j);
				distance1 += GeometryEngine.DistanceFromGeometry(previousPoint, point);
				previousPoint = point;
			}
			distance1 += GeometryEngine.DistanceFromGeometry(previousPoint, location);
			previousPoint = segment.GetPoint(0, onSegmentIndex + 1);
			distance2 = GeometryEngine.DistanceFromGeometry(location, previousPoint);
			previousPoint = vertexPoint;
			for (int j = onSegmentIndex + 2; j < segment.GetPointCount(0); j++)
			{
				MapPoint point = segment.GetPoint(0, j);
				distance2 += GeometryEngine.DistanceFromGeometry(previousPoint, point);
				previousPoint = point;
			}


			//var previousPoint = proximity.PointIndex ? segment.GetPoint(proximity.PartIndex + 1, 0) : segment.GetPoint(proximity.PartIndex, proximity.PointIndex + 1);
			if (distance1 + distance2 == 0)
				return 1;
			return distance1 / (distance1 + distance2);
		}
	}
}

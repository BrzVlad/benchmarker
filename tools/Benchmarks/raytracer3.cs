/* The Great Computer Language Shootout 
   http://shootout.alioth.debian.org/
   
   contributed by Isaac Gouy 
*/

using System;
using System.Collections.Generic;
using System.IO;
using Common.Logging;

namespace Benchmarks.Raytracer3
{
	public class RayTracer3
	{
		const int levels = 6, ss = 4;
		const double Epsilon = 1.49012e-08;
		// Normally we'd use double.Epsilon

		public static void Main (String[] args, ILog ilog)
		{        
			int n = 0;
			if (args.Length > 0)
				n = Int32.Parse (args [0]);

			Scene scene = Scene.SphereScene (levels, new Vector (0.0, -1.0, 0.0), 1.0);

			ilog.InfoFormat ("P5");
			ilog.InfoFormat ("{0} {1}", n, n);
			ilog.InfoFormat ("255");

			Stream stream = Stream.Null;
			byte[] temp = new byte[1];

			for (int y = n - 1; y >= 0; --y) {
				for (int x = 0; x < n; ++x) {

					double greyscale = 0.0;
					for (int dx = 0; dx < ss; ++dx) {
						for (int dy = 0; dy < ss; ++dy) {

							Vector v = new Vector (
								                      x + dx / (double)ss - n / 2.0
                     , y + dy / (double)ss - n / 2.0
                     , n);

							Ray ray = new Ray (new Vector (0.0, 0.0, -4.0), v.Normalized ());

							greyscale += scene.TraceRay (ray, 
								new Vector (-1.0, -3.0, 2.0).Normalized ());
						}
					}

					temp [0] = (byte)(0.5 + 255.0 * greyscale / (ss * ss));
					stream.Write (temp, 0, 1);
				}
			}
		}


		abstract class Scene
		{
			abstract internal IntersectionPoint Intersect (Ray ray, IntersectionPoint p);

			internal static Scene SphereScene (int level, Vector center, double radius)
			{
				Sphere sphere = new Sphere (center, radius);
				if (level == 1) { 
					return sphere;
				} else {
					Group scene = new Group (new Sphere (center, 3.0 * radius));
					scene.Add (sphere);
					double rn = 3.0 * radius / Math.Sqrt (12.0);

					for (int dz = -1; dz <= 1; dz += 2) {
						for (int dx = -1; dx <= 1; dx += 2) {

							Vector c2 = new Vector (
								                       center.x - dx * rn
                     , center.y + rn
                     , center.z - dz * rn
							                       );

							scene.Add (SphereScene (level - 1, c2, radius / 2.0));
						}
					}
					return scene;
				}
			}


			internal double TraceRay (Ray ray, Vector light)
			{
				IntersectionPoint p = Intersect (ray,
					                           new IntersectionPoint (
						                           double.PositiveInfinity, new Vector (0.0, 0.0, 0.0)));
                 
				if (double.IsInfinity (p.distance))
					return 0.0;

				double greyscale = -(p.normal * light);
				if (greyscale <= 0.0)
					return 0.0;

				Vector o = ray.origin +
				                (p.distance * ray.direction) + (Epsilon * p.normal);
            
				Ray shadowRay = new Ray (o, new Vector (0.0, 0.0, 0.0) - light);
				IntersectionPoint shadowp = Intersect (shadowRay,
					                                 new IntersectionPoint (double.PositiveInfinity, p.normal));

				return double.IsInfinity (shadowp.distance) ? greyscale : 0.0;
			}
		}


		// a leaf node in the scene tree
		class Sphere : Scene
		{
			private Vector center;
			private double radius;

			internal Sphere (Vector center, double radius)
			{ 
				this.center = center;
				this.radius = radius; 
			}

			internal double Distance (Ray ray)
			{
				Vector v = center - ray.origin;
				double b = v * ray.direction;
				double disc = b * b - v * v + radius * radius;         
				if (disc < 0)
					return double.PositiveInfinity; // No intersection

				double d = Math.Sqrt (disc);
				double t1 = b + d;
				if (t1 < 0)
					return double.PositiveInfinity;

				double t2 = b - d;
				return t2 > 0 ? t2 : t1;
			}

			override internal IntersectionPoint Intersect (Ray r, IntersectionPoint p)
			{
				double d = Distance (r);
				if (d < p.distance) { 
					Vector v = r.origin + ((d * r.direction) - center);
					p = new IntersectionPoint (d, v.Normalized ());
				}
				return p;
			}
		}

		// non-leaf node in the scene tree
		class Group : Scene
		{
			private Sphere bound;
			private List<Scene> scenes = new List<Scene> ();

			internal Group (Sphere bound)
			{
				this.bound = bound;
			}

			override internal IntersectionPoint Intersect (Ray r, IntersectionPoint p)
			{
				if (bound.Distance (r) < p.distance) { 
					foreach (Scene each in scenes)
						p = each.Intersect (r, p);
				}
				return p;
			}

			internal void Add (Scene s)
			{
				scenes.Insert (0, s);
			}
		}
	}


	class Vector
	{
		private double _x, _y, _z;

		internal Vector (double x, double y, double z)
		{
			_x = x;
			_y = y;
			_z = z;
		}

		public static Vector operator + (Vector a, Vector b)
		{
			return new Vector (a._x + b._x, a._y + b._y, a._z + b._z);
		}

		public static Vector operator - (Vector a, Vector b)
		{
			return new Vector (a._x - b._x, a._y - b._y, a._z - b._z);
		}

		public static double operator * (Vector a, Vector b)
		{
			return (a._x * b._x) + (a._y * b._y) + (a._z * b._z);
		}

		public static Vector operator * (double s, Vector b)
		{
			return new Vector (s * b._x, s * b._y, s * b._z);
		}

		public static Vector operator * (Vector a, double s)
		{
			return new Vector (a._x * s, a._y * s, a._z * s);
		}

		internal Vector Normalized ()
		{
			return (1.0 / Math.Sqrt (this * this)) * this;
		}

		internal double x {
			get { return _x; }
		}

		internal double y {
			get { return _y; }
		}

		internal double z {
			get { return _z; }
		}
	}


	class Ray
	{
		private Vector _origin, _direction;

		internal Ray (Vector origin, Vector direction)
		{
			_origin = origin;
			_direction = direction;
		}

		internal Vector origin {
			get { return _origin; }
		}

		internal Vector direction {
			get { return _direction; }
		}
	}


	class IntersectionPoint
	{
		private double _distance;
		private Vector _normal;

		internal IntersectionPoint (double distance, Vector normal)
		{
			_distance = distance;
			_normal = normal;
		}

		internal double distance {
			get { return _distance; }
		}

		internal Vector normal {
			get { return _normal; }
		}
	}
}
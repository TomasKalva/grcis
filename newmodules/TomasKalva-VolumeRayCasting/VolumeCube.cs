using MathSupport;
using OpenTK;
using Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using static MathSupport.RandomJames;

namespace TomasKalva
{

  /// <summary>
  /// Normalized (unit) cube as a simple solid able to compute ray-intersection, normal vector
  /// and 2D texture coordinates. [0,1]^3
  /// </summary>
  [Serializable]
  public class VolumeCube : Cube, ISolid, ITimeDependent
  {
    /// <summary>
    /// RGB color at the given point.
    /// </summary>
    Func<Vector3d, double, Vector3d> Color;

    public VolumeCube (Func<Vector3d, double, Vector3d> color, double step = 0.01)
    {
      Color = color;
      var random = new WhiteNoise();
      // Function calculating color of cube bounded volume objects
      RecursionFunction del = (Intersection i, Vector3d dir, double importance, out RayRecursion rr) =>
      {
        var colorVec = Vector3d.Zero;
        var localP0 = i.CoordLocal;
        var localP1 = Vector3d.TransformVector(dir, i.WorldToLocal).Normalized();

        // iterate over the ray
        for (double t = 0; t < 1.8; t += step)
        {
          // calculate position in the local cube space
          var p = localP0 + t * localP1;
          // jittering
          p += 0.001 * random.GetValue(p.X, p.Y, p.Z) * localP1;
          // ignore positions out of bounds
          if ((p.X >= 1 || p.X < 0) ||
              (p.Y >= 1 || p.Y < 0) ||
              (p.Z >= 1 || p.Z < 0))
            continue;

          // sum color values
          colorVec += MyMath.Max(color(p, Time), 0) * step;
        }

        // calculate intersection where the ray leaves the solid
        var outIntersection = OutIntersection(localP0, localP1);
        outIntersection.Complete();

        double[] colorVal = new double[3] { colorVec.X, colorVec.Y, colorVec.Z };
        rr = new RayRecursion(
          colorVal,
          new RayRecursion.RayContribution(outIntersection, dir, importance));

        return 144L;
      };

      SetAttribute(PropertyName.RECURSION, del);
      SetAttribute(PropertyName.NO_SHADOW, true);
    }

    public double Start { get; set; }
    public double End { get; set; }
    public double Time { get; set; }

    public object Clone ()
    {
      return new VolumeCube(Color)
      {
        Start = Start,
        End = End,
        Time = Time,
      };
    }

    private Intersection OutIntersection (Vector3d p0, Vector3d p1)
    {
      var intersections = Intersect(p0, p1);
      return intersections.Count == 0 ? null : intersections.Last.Value;
    }

    public static Func<Vector3d, double> ParaboloidShape (double r, double top) 
    {
      return v =>
      {
        Vector3d origin = new Vector3d(0.5, 0, 0.5);
        Vector3d relV = new Vector3d(v.X, 1 - v.Y, v.Z) - origin;
        var pY = 1 - top * (relV.X * relV.X + relV.Z * relV.Z) / (r * r);
        var dy = Math.Max(0, pY - v.Y);
        return v.Y < pY ? (1 - Math.Max(0, v.Y) / pY) : 0;
      };
    }

    public static Func<Vector3d, double> BallShape (Vector3d origin, Vector3d scale, double r)
    {
      return v =>
      {
        Vector3d relV = Vector3d.Divide(v - origin, scale);
        var c = relV.X * relV.X + relV.Y * relV.Y + relV.Z * relV.Z;
        return c < r * r ? 1 : 0;
      };
    }

    public static Func<Vector3d, double> FadeBottom(double bottomY)
    {
      return v =>
      {
        return v.Y < bottomY ? v.Y / bottomY : 1;
      };
    }

    public static Func<Vector3d, double, Vector3d> Fire (Func<Vector3d, double> shape, Func<double, Vector3d> color)
    {
      var noise = new PerlinNoise3d();
      return (Vector3d v, double t) =>
      {
        Vector3d scaledV = v * new Vector3d(15, 1, 15);
        Vector3d displ = new Vector3d(noise[v * 3 - t * Vector3d.UnitY], noise[v + Vector3d.UnitX * 10 - t * Vector3d.UnitY] , noise[v * 3 + Vector3d.UnitX * 20 - t * Vector3d.UnitY]);
        var intensity = noise[scaledV - 4 * t * Vector3d.UnitY + 3 * displ] * shape(v + 0.21532446 * displ);

        return 70 * intensity * color(intensity) /** (1 - v.Y) * (1 - v.Y)*/;
      };
    }
  }
}

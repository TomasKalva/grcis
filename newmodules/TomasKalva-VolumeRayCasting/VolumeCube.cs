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

    public static Func<Vector3d, double> ParaboloidShape (double r, double top = 1.0, double steepness = 1.0) 
    {
      return v =>
      {
        Vector3d origin = new Vector3d(0.5, 0, 0.5);
        Vector3d relV = new Vector3d(v.X, 1 - v.Y, v.Z) - origin;
        var pY = top - steepness * (relV.X * relV.X + relV.Z * relV.Z) / (r * r);
        var dy = Math.Max(0, pY - v.Y);
        return v.Y < pY ? Math.Min(1.0, 1.0 - v.Y / pY) : 0;
      };
    }

    public static Func<Vector3d, double> ParaboloidFireShape (double r, double top = 1.0, double steepness = 1.0)
    {
      Func<Vector3d, double> paraboloid = ParaboloidShape(r, top, steepness);
      Func<Vector3d, double> fadeBottom = FadeBottom(0.2);
      return v =>
      {
        return paraboloid(v) * fadeBottom(v) * fadeBottom(v) * (1 - v.Y) * (1 - v.Y);
      };
    }

    public static Func<Vector3d, double> BallShape (Vector3d origin, Vector3d scale)
    {
      return v =>
      {
        Vector3d relV = Vector3d.Divide(v - origin, scale);
        var c = relV.X * relV.X + relV.Y * relV.Y + relV.Z * relV.Z;
        return Math.Max(0.0, 1.0 - c);
      };
    }

    public static Func<Vector3d, double> FadeBottom(double bottomY)
    {
      return v =>
      {
        return v.Y < bottomY ? v.Y / bottomY : 1;
      };
    }

    public static Func<Vector3d, double, Vector3d> Fire (Func<Vector3d, double> shape, Func<Vector3d, double> textureNoise, Func<Vector3d, double> displNoise, Func<double, Vector3d> color, double speed = 1.0)
    {
      var noise = new PerlinNoise3d();
      var turbulence = new Turbulence3d(noise, 4);
      return (Vector3d v, double t) =>
      {
        Vector3d scaledV = v * new Vector3d(15, 1, 15);
        var offset =  - speed * 4 * t * Vector3d.UnitY;
        Vector3d displ = new Vector3d(displNoise(v * 3 + offset), displNoise(v + Vector3d.UnitX * 10 + offset) , displNoise(v * 3 + Vector3d.UnitX * 20 + offset));
        var intensity = textureNoise(scaledV + offset + displ) * shape(v + 0.21532446 * displ) * ( v.Y < 0.1 ? 0 : 1);

        return 70 * intensity * color(intensity) ;
      };
    }

    public static Func<Vector3d, double> Noise ()
    {
      var noise = new PerlinNoise3d();
      return v =>
      {
        return noise[v];
      };
    }

    public static Func<Vector3d, double> Turbulence (int octaves, double lacunarity = 2.0, double gain = 0.5)
    {
      var noise = new PerlinNoise3d();
      var turbulence = new Turbulence3d(noise, octaves, lacunarity, gain);
      return v =>
      {
        return turbulence[v];
      };
    }
  }
}

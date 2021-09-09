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
  /// Returns intensity for the position.
  /// </summary>
  public delegate double Intensity (Vector3d v);
  /// <summary>
  /// Returns intensity for the position and time.
  /// </summary>
  public delegate double AnimatedIntensity (Vector3d v, double t);
  /// <summary>
  /// Returns rgb color for the position.
  /// </summary>
  public delegate Vector3d Color (Vector3d v);
  /// <summary>
  /// Returns rgb color for the position and time.
  /// </summary>
  public delegate Vector3d AnimatedColor (Vector3d v, double t);

  /// <summary>
  /// Normalized (unit) cube that serves as bounding shape for volumetrically defined objects.
  /// </summary>
  [Serializable]
  public class VolumeCube : Cube, ISolid, ITimeDependent
  {
    /// <summary>
    /// RGB color at the given point.
    /// </summary>
    AnimatedColor Color;

    public VolumeCube (AnimatedColor color, double step = 0.01)
    {
      Color = color;
      var random = new WhiteNoise();

      // Function calculating color of cube bounded volume objects
      RecursionFunction del = (Intersection i, Vector3d dir, double importance, out RayRecursion rr) =>
      {
        var colorVec = Vector3d.Zero;
        var localP0 = i.CoordLocal;
        var localP1 = Vector3d.TransformVector(dir, i.WorldToLocal).Normalized();
        
        // calculate intersection where the ray leaves the solid
        var outIntersection = ExitIntersection(localP0, localP1);
        outIntersection.Complete();

        // iterate over the ray
        for (double t = 0; t <= outIntersection.T; t += step)
        {
          // calculate position in the local cube space
          var p = localP0 + t * localP1;
          // jittering
          p += 0.001 * random[p.X, p.Y, p.Z] * localP1;
          // ignore positions out of bounds
          if ((p.X >= 1 || p.X < 0) ||
              (p.Y >= 1 || p.Y < 0) ||
              (p.Z >= 1 || p.Z < 0))
            continue;

          // sum color values
          colorVec += MyMath.Max(color(p, Time), 0) * step;
        }


        double[] colorVal = new double[3] { colorVec.X, colorVec.Y, colorVec.Z };
        rr = new RayRecursion(
          colorVal,
          new RayRecursion.RayContribution(outIntersection, dir, importance));

        return 144L;
      };

      SetAttribute(PropertyName.RECURSION, del);
      SetAttribute(PropertyName.NO_SHADOW, true);
    }


    #region ITimeDependent

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

    #endregion

    /// <summary>
    /// Returns intersection, where the ray with starting point p0 and direction p1 leaves the solid.
    /// </summary>
    private Intersection ExitIntersection (Vector3d p0, Vector3d p1)
    {
      var intersections = Intersect(p0, p1);
      return intersections.Count == 0 ? null : intersections.Last.Value;
    }

    /// <summary>
    /// Creates rotation parabola around y axis with given vertex and scale. The shape fades smoothly
    /// to the border.
    /// </summary>
    /// <param name="vertex">Peak of the paraboloid</param>
    /// <param name="scale">Scale of the inputs</param>
    public static Intensity ParaboloidShape (Vector3d vertex, Vector3d scale) 
    {
      return v =>
      {
        Vector3d relV = Vector3d.Divide(v - vertex, scale);
        var pY = (relV.X * relV.X + relV.Z * relV.Z);
        var dy = Math.Max(0, relV.Y - pY);
        var smoothDy = 1.0 - 1.0 / (dy + 1.0);
        return dy > 0 ? Math.Min(1.0, smoothDy) : 0;
      };
    }

    /// <summary>
    /// Paraboloid smoothly cut off from bottom and the top.
    /// </summary>
    /// <param name="vertex">Peak of the paraboloid</param>
    /// <param name="scale">Scale of the inputs</param>
    public static Intensity ParaboloidFireShape (Vector3d vertex, Vector3d scale)
    {
      Intensity paraboloid = ParaboloidShape(vertex, scale);
      Intensity fadeBottom = (v) => v.Y < 0.2 ? Math.Max(v.Y, 0d) / 0.2 : 1;
      Intensity fadeTop = (v) => (1 - v.Y);
      return v =>
      {
        var fadeBottomV = fadeBottom(v);
        var fadeTopV = fadeTop(v);

        return paraboloid(v) * fadeBottomV * fadeBottomV * fadeTopV * fadeTopV;
      };
    }

    /// <summary>
    /// Creates a ball with given center and scale. The ball fades smoothly from the center to the border.
    /// </summary>
    /// <param name="center">Center of the ball</param>
    /// <param name="scale">Scale of the inputs</param>
    public static Intensity BallShape(Vector3d center, Vector3d scale)
    {
      return v =>
      {
        Vector3d relV = Vector3d.Divide(v - center, scale);
        var c = relV.X * relV.X + relV.Y * relV.Y + relV.Z * relV.Z;
        return Math.Max(0.0, 1.0 - c);
      };
    }

    /// <summary>
    /// Creates function that returns 3D vector with values generated by noise.
    /// </summary>
    /// <param name="noise">4D noise</param>
    public static Func<Vector4d, Vector3d> Displacement(Func<Vector4d, double> noise)
    {
      return v =>
      {
        return new Vector3d(noise(v), noise(v + Vector4d.UnitX * 10.0) , noise(v + Vector4d.UnitX * 20.0));
      };
    }

    /// <summary>
    /// Creates function that returns 3D vector with values generated by noise.
    /// </summary>
    /// <param name="noise">3D noise</param>
    public static Color Displacement (Intensity noise)
    {
      return v =>
      {
        return new Vector3d(noise(v), noise(v + Vector3d.UnitX * 10.0), noise(v + Vector3d.UnitX * 20.0));
      };
    }

    /// <summary>
    /// Creates function returning color of fire. It is animated.
    /// </summary>
    /// <param name="shape">Shape of fire</param>
    /// <param name="textureNoise">Noise that gives fire its texture</param>
    /// <param name="displNoise">Noise that displaces shape and texture of fire</param>
    /// <param name="color">Converts intensity to color</param>
    /// <param name="speed">How quickly the fire burns</param>
    /// <param name="intensityMult">Multiplies intensity</param>
    public static AnimatedColor Fire (
      Intensity shape,
      Intensity textureNoise,
      Intensity displNoise,
      Func<double, Vector3d> color,
      double speed = 1.0,
      double intensityMult = 1.0)
    {
      return (Vector3d v, double t) =>
      {
        Vector3d scaledV = v * new Vector3d(15, 1, 15);
        var offset =  - speed * 4 * t * Vector3d.UnitY;
        Vector3d displ = new Vector3d(
          displNoise(v * 3 + offset),
          displNoise(v + Vector3d.UnitX * 10 + offset) ,
          displNoise(v * 3 + Vector3d.UnitX * 20 + offset)
          );
        var intensity = textureNoise(scaledV + offset + displ) * shape(v + 0.2 * displ) /** ( v.Y < 0.1 ? 0 : 1)*/;

        return 200 * intensityMult * intensity * color(intensity);
      };
    }

    /// <summary>
    /// Creates function returning color of cloud. It is not animated.
    /// </summary>
    /// <param name="shape">Shape of cloud</param>
    /// <param name="displ">Noise that displaces shape and texture</param>
    /// <param name="texture">Noise that gives cloud its texture</param>
    /// <param name="color">Converts intensity to color</param>
    /// <param name="intensityMult">Multiplies intensity</param>
    /// <returns></returns>
    public static AnimatedColor Cloud (
      Intensity shape,
      Color displ,
      Intensity texture,
      Func<double, Vector3d> color,
      double intensityMult = 1.0)
    {
      return 
        (v, t) =>
        {
          var scaledV = 15 * v + 2 * displ(v * 3 + t * Vector3d.One);
          var intensity = intensityMult * texture(scaledV) * shape(v + 0.5 * displ(v * 2));
          return 3 * intensity * color(intensity);
        };
    }

    /// <summary>
    /// Creates 3D noise.
    /// </summary>
    /// <returns>3D Perlin noise</returns>
    public static Intensity Noise3d ()
    {
      var noise = new PerlinNoise3d();
      return v =>
      {
        return noise[v.X, v.Y, v.Z];
      };
    }

    /// <summary>
    /// Creates 4D noise.
    /// </summary>
    /// <returns>4D Perlin noise</returns>
    public static Func<Vector4d, double> Noise4d ()
    {
      var noise = new PerlinNoise4d();
      return v =>
      {
        return noise[v.X, v.Y, v.Z, v.W];
      };
    }

    /// <summary>
    /// Adds together multiple instances of the given noise.
    /// </summary>
    /// <param name="noise">3D noise</param>
    /// <param name="octaves">Number of added instances</param>
    /// <param name="lacunarity">Increase of scale between consecutive instances</param>
    /// <param name="gain">Increase of value between consecutive instances</param>
    /// <returns>Turbulence for the noise</returns>
    public static Intensity Turbulence (Intensity noise, int octaves, double lacunarity = 2.0, double gain = 0.5)
    {
      return v =>
      {
        double total = 0;
        double a = 1.0;
        double f = 1.0;
        for (int i = 0; i < octaves; i++)
        {
          total += a * noise(f * v);
          a *= gain;
          f *= lacunarity;
        }
        return total;
      };
    }

    /// <summary>
    /// Adds together multiple instances of the given noise.
    /// </summary>
    /// <param name="noise">4D noise</param>
    /// <param name="octaves">Number of added instances</param>
    /// <param name="lacunarity">Increase of scale between consecutive instances</param>
    /// <param name="gain">Increase of value between consecutive instances</param>
    /// <returns>Turbulence for the noise</returns>
    public static Func<Vector4d, double> Turbulence (Func<Vector4d, double> noise, int octaves, double lacunarity = 2.0, double gain = 0.5)
    {
      return v =>
      {
        double total = 0;
        double a = 1.0;
        double f = 1.0;
        for (int i = 0; i < octaves; i++)
        {
          total += a * noise(f * v);
          a *= gain;
          f *= lacunarity;
        }
        return total;
      };
    }
  }
}

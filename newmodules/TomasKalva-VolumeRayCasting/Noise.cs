using MathSupport;
using OpenTK;
using Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities;
using static MathSupport.RandomJames;

namespace TomasKalva
{
  using static MyMath;

  /// <summary>
  /// Pseudo-random white noise generator.
  /// </summary>
  public class WhiteNoise 
  {
    /// <summary>
    /// Table with random values.
    /// </summary>
    double[] RandomTab;
    /// <summary>
    /// Random permutations used for indexing RandomTab.
    /// </summary>
    int[] Indx, Indy, Indz;

    public WhiteNoise()
    {
      var rnd = new RandomJames();
      RandomTab = new double[256];
      for (int i = 0; i < RandomTab.Length; i++)
      {
        RandomTab[i] = rnd.UniformNumber() * 2 - 1;
      }
      Permutation p = null;
      rnd.PermutationFirst(256, ref p);
      Indx = p.perm;
      rnd.PermutationNext(ref p);
      Indy = p.perm;
      rnd.PermutationNext(ref p);
      Indz = p.perm;
    }

    private int LowerBits(double x)
    {
      return (int)(0xff * x) & 0xff;
    }

    /// <summary>
    /// Returns deterministic pseudo-random value.
    /// </summary>
    public double this[double x, double y, double z]
    {
      get
      {
        var lowerBitsY = LowerBits(y);
        int hash = 7907 * Indx[LowerBits(x)] + 3321 * Indy[LowerBits(y)] + 5321 * Indy[LowerBits(z)];
        return RandomTab[hash % RandomTab.Length];
      }
    }
  }

  /// <summary>
  /// 2-dimensional deterministic Perlin noise.
  /// </summary>
  public class PerlinNoise2d
  {
    WhiteNoise whiteNoise;

    public PerlinNoise2d ()
    {
      whiteNoise = new WhiteNoise();
    }

    /// <summary>
    /// Returns a unit vector.
    /// /// </summary>
    Vector2d GetGradient (int x, int y)
    {
      var vec = new Vector2d(whiteNoise[x, y, 0], whiteNoise[x + 0.1, y, 0]).Normalized();
      return vec;
    }

    public double this[double x, double y]
    {
      get
        {
          var ix = (int)Math.Floor(x);
          var iy = (int)Math.Floor(y);

          var bl = GetGradient(ix, iy);
          var tl = GetGradient(ix, iy + 1);
          var br = GetGradient(ix + 1, iy);
          var tr = GetGradient(ix + 1, iy + 1);

          var dx = x - ix;
          var dy = y - iy;
          var d = new Vector2d(dx, dy);
          var t = d * d * (Vector2d.One * 3.0 - 2 * d);

          return Lerp(Lerp(Vector2d.Dot(bl, d), Vector2d.Dot(br, new Vector2d(dx - 1, dy)), t.X),
                      Lerp(Vector2d.Dot(tl, new Vector2d(dx, dy - 1)), Vector2d.Dot(tr, new Vector2d(dx - 1, dy - 1)), t.X), t.Y);
        }
    }
  }

  /// <summary>
  /// 3-dimensional deterministic Perlin noise.
  /// </summary>
  public class PerlinNoise3d
  {
    WhiteNoise whiteNoise;

    public PerlinNoise3d ()
    {
      whiteNoise = new WhiteNoise();
    }

    /// <summary>
    /// Returns a unit vector.
    /// </summary>
    Vector3d GetGradient (int x, int y, int z)
    {
      var vec = new Vector3d(whiteNoise[x, y, z], whiteNoise[x + 0.1, y, z], whiteNoise[x + 0.1, y + 0.1, z]).Normalized();
      return vec;
    }

    public double this[double x, double y, double z]
    {
      get
      {
        var ix = (int)Math.Floor(x);
        var iy = (int)Math.Floor(y);
        var iz = (int)Math.Floor(z);

        var g000 = GetGradient(ix, iy, iz);
        var g010 = GetGradient(ix, iy + 1, iz);
        var g100 = GetGradient(ix + 1, iy, iz);
        var g110 = GetGradient(ix + 1, iy + 1, iz);

        var g001 = GetGradient(ix, iy, iz + 1);
        var g011 = GetGradient(ix, iy + 1, iz + 1);
        var g101 = GetGradient(ix + 1, iy, iz + 1);
        var g111 = GetGradient(ix + 1, iy + 1, iz + 1);

        var dx = x - ix;
        var dy = y - iy;
        var dz = z - iz;
        var d = new Vector3d(dx, dy, dz);
        var t = d * d * (Vector3d.One * 3.0 - 2 * d);

        return Lerp(
                 Lerp(
                   Lerp(Vector3d.Dot(g000, d), Vector3d.Dot(g100, new Vector3d(dx - 1, dy, dz)), t.X),
                   Lerp(Vector3d.Dot(g010, new Vector3d(dx, dy - 1, dz)), Vector3d.Dot(g110, new Vector3d(dx - 1, dy - 1, dz)), t.X),
                   t.Y),
                 Lerp(
                   Lerp(Vector3d.Dot(g001, new Vector3d(dx, dy, dz - 1)), Vector3d.Dot(g101, new Vector3d(dx - 1, dy, dz - 1)), t.X),
                   Lerp(Vector3d.Dot(g011, new Vector3d(dx, dy - 1, dz - 1)), Vector3d.Dot(g111, new Vector3d(dx - 1, dy - 1, dz - 1)), t.X),
                   t.Y),
                 t.Z);
      }
    }
  }

  /// <summary>
  /// 4-dimensional deterministic Perlin noise.
  /// </summary>
  public class PerlinNoise4d
  {
    WhiteNoise whiteNoise;

    public PerlinNoise4d ()
    {
      whiteNoise = new WhiteNoise();
    }

    /// <summary>
    /// Returns a unit vector.
    /// </summary>
    Vector4d GetGradient (int x, int y, int z, int w)
    {
      var vec = new Vector4d(whiteNoise[x, y, z], whiteNoise[w + 0.1, y, z], whiteNoise[x + 0.2, w, z], whiteNoise[x + 0.3, y, w]).Normalized();
      return vec;
    }

    public double this[double x, double y, double z, double w]
    {
      get
      {
        var ix = (int)Math.Floor(x);
        var iy = (int)Math.Floor(y);
        var iz = (int)Math.Floor(z);
        var iw = (int)Math.Floor(w);

        var g0000 = GetGradient(ix, iy, iz, iw);
        var g0100 = GetGradient(ix, iy + 1, iz, iw);
        var g1000 = GetGradient(ix + 1, iy, iz, iw);
        var g1100 = GetGradient(ix + 1, iy + 1, iz, iw);
        var g0010 = GetGradient(ix, iy, iz + 1, iw);
        var g0110 = GetGradient(ix, iy + 1, iz + 1, iw);
        var g1010 = GetGradient(ix + 1, iy, iz + 1, iw);
        var g1110 = GetGradient(ix + 1, iy + 1, iz + 1, iw);

        var g0001 = GetGradient(ix, iy, iz, iw + 1);
        var g0101 = GetGradient(ix, iy + 1, iz, iw + 1);
        var g1001 = GetGradient(ix + 1, iy, iz, iw + 1);
        var g1101 = GetGradient(ix + 1, iy + 1, iz, iw + 1);
        var g0011 = GetGradient(ix, iy, iz + 1, iw + 1);
        var g0111 = GetGradient(ix, iy + 1, iz + 1, iw + 1);
        var g1011 = GetGradient(ix + 1, iy, iz + 1, iw + 1);
        var g1111 = GetGradient(ix + 1, iy + 1, iz + 1, iw + 1);

        var dx = x - ix;
        var dy = y - iy;
        var dz = z - iz;
        var dw = w - iw;
        var d = new Vector4d(dx, dy, dz, dw);
        var t = d * d * (Vector4d.One * 3.0 - 2 * d);

        return Lerp(
                  Lerp(
                    Lerp(
                      Lerp(Vector4d.Dot(g0000, d), Vector4d.Dot(g1000, new Vector4d(dx - 1, dy, dz, dw)), t.X),
                      Lerp(Vector4d.Dot(g0100, new Vector4d(dx, dy - 1, dz, dw)), Vector4d.Dot(g1100, new Vector4d(dx - 1, dy - 1, dz, dw)), t.X),
                      t.Y),
                    Lerp(
                      Lerp(Vector4d.Dot(g0010, new Vector4d(dx, dy, dz - 1, dw)), Vector4d.Dot(g1010, new Vector4d(dx - 1, dy, dz - 1, dw)), t.X),
                      Lerp(Vector4d.Dot(g0110, new Vector4d(dx, dy - 1, dz - 1, dw)), Vector4d.Dot(g1110, new Vector4d(dx - 1, dy - 1, dz - 1, dw)), t.X),
                      t.Y),
                    t.Z),
                   Lerp(
                     Lerp(
                       Lerp(Vector4d.Dot(g0001, new Vector4d(dx, dy, dz, dw - 1)), Vector4d.Dot(g1001, new Vector4d(dx - 1, dy, dz, dw - 1)), t.X),
                       Lerp(Vector4d.Dot(g0101, new Vector4d(dx, dy - 1, dz, dw - 1)), Vector4d.Dot(g1101, new Vector4d(dx - 1, dy - 1, dz, dw - 1)), t.X),
                     t.Y),
                     Lerp(
                       Lerp(Vector4d.Dot(g0011, new Vector4d(dx, dy, dz - 1, dw - 1)), Vector4d.Dot(g1011, new Vector4d(dx - 1, dy, dz - 1, dw - 1)), t.X),
                       Lerp(Vector4d.Dot(g0111, new Vector4d(dx, dy - 1, dz - 1, dw - 1)), Vector4d.Dot(g1111, new Vector4d(dx - 1, dy - 1, dz - 1, dw - 1)), t.X),
                     t.Y),
                   t.Z),
                 t.W);
      }
    }
  }

  public static class MyMath
  {
    /// <summary>
    /// Linear interpolation between points x and y.
    /// </summary>
    public static double Lerp (double x, double y, double a)
    {
      return x * (1.0 - a) + y * a;
    }

    /// <summary>
    /// Makes all components of color at least max. Returns color.
    /// </summary>
    /// <param name="color">Color bands</param>
    public static double[] Max (double[] color, double max)
    {
      Debug.Assert(color != null);

      int bands = color.Length;
      for (int i = 0; i < bands; i++)
        color[i] = Math.Max(color[i], max);
      return color;
    }

    /// <summary>
    /// Makes all components of color at least max. Returns color.
    /// </summary>
    /// <param name="color">Color bands</param>
    public static Vector3d Max (Vector3d color, double max)
    {
      return new Vector3d(Math.Max(color.X, max), Math.Max(color.Y, max), Math.Max(color.Z, max));
    }

    /// <summary>
    /// Smooth interpolation of t between 0 and 1.
    /// </summary>
    public static double Smoothstep (double from, double to, double t)
    {
      if (t < from)
        return 0;
      if (t > to)
        return 1;
      t = (t - from) / (to - from);
      return t * t * (2 - 3 * t);
    }

    /// <summary>
    /// Returns 1 if p lies in [0,1]^3. Returns 0 otherwise.
    /// </summary>
    public static double InZeroOneBounds (Vector3d p)
    {
      if ((p.X >= 1 || p.X < 0) ||
          (p.Y >= 1 || p.Y < 0) ||
          (p.Z >= 1 || p.Z < 0))
        return 0;
      else
        return 1;
    }
  }
}

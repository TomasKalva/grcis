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

  interface IFunction2d<T>
  {
    T GetValue (double x, double y);
  }

  interface IFunction3d<T>
  {
    T GetValue (double x, double y, double z);
  }

  public class WhiteNoise : IFunction3d<double>
  {
    double[] RandomTab;
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
      return (int)(0xff * x) & 0xff;//(BitConverter.DoubleToInt64Bits(x) & 0xff);
    }

    public double GetValue(double x, double y, double z)
    {
      var lowerBitsY = LowerBits(y);
      int hash = 7907 * Indx[LowerBits(x)] + 3321 * Indy[LowerBits(y)] + 5321 * Indy[LowerBits(z)];
      return RandomTab[hash % RandomTab.Length];
    }
  }

  public static class MyMath
  {
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
  }

  public class PerlinNoise2d : IFunction2d<double>
  {
    WhiteNoise whiteNoise;

    public PerlinNoise2d ()
    {
      whiteNoise = new WhiteNoise();
    }

    Vector2d GetGradient(int x, int y)
    {
      var vec = new Vector2d(whiteNoise.GetValue(x, y, 0), whiteNoise.GetValue(x + 0.1, y, 0)).Normalized();
      return vec;
    }

    public double GetValue (double x, double y)
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

  public class PerlinNoise3d : IFunction3d<double>
  {
    WhiteNoise whiteNoise;

    public PerlinNoise3d ()
    {
      whiteNoise = new WhiteNoise();
    }

    Vector3d GetGradient (int x, int y, int z)
    {
      var vec = new Vector3d(whiteNoise.GetValue(x, y, z), whiteNoise.GetValue(x + 0.1, y, z), whiteNoise.GetValue(x + 0.1, y + 0.1, z)).Normalized();
      return vec;
    }

    public double GetValue (double x, double y, double z)
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

      return Lerp(Lerp(Lerp(Vector3d.Dot(g000, d), Vector3d.Dot(g100, new Vector3d(dx - 1, dy, dz)), t.X),
                  Lerp(Vector3d.Dot(g010, new Vector3d(dx, dy - 1, dz)), Vector3d.Dot(g110, new Vector3d(dx - 1, dy - 1, dz)), t.X), t.Y),
                  Lerp(Lerp(Vector3d.Dot(g001, new Vector3d(dx, dy, dz - 1)), Vector3d.Dot(g101, new Vector3d(dx - 1, dy, dz - 1)), t.X),
                  Lerp(Vector3d.Dot(g011, new Vector3d(dx, dy - 1, dz - 1)), Vector3d.Dot(g111, new Vector3d(dx - 1, dy - 1, dz - 1)), t.X), t.Y), t.Z);
    }
  }

  /// <summary>
  /// Simple texture able to modulate surface color.
  /// </summary>
  [Serializable]
  public class NoiseTexture : ITexture
  {
    private IFunction3d<double> noise;

    public NoiseTexture ()
    {
      noise = new PerlinNoise3d();
    }

    /// <summary>
    /// Apply the relevant value-modulation in the given Intersection instance.
    /// Simple variant, w/o an integration support.
    /// </summary>
    /// <param name="inter">Data object to modify.</param>
    /// <returns>Hash value (texture signature) for adaptive subsampling.</returns>
    public virtual long Apply (Intersection inter)
    {
      double u = inter.CoordWorld.X;
      double v = inter.CoordWorld.Y;
      double w = inter.CoordWorld.Z;


      double val = noise.GetValue(u, v, w) / 2.0 + .5;

      long ui = (long)Math.Floor(u);
      long vi = (long)Math.Floor(v);

      inter.SurfaceColor = new double[3] { val, val, val };
      //if (((ui + vi) & 1) != 0)
      //  Util.ColorAdd(new double[3] { 0.5, .5, .5 }, inter.SurfaceColor);

      inter.textureApplied = true; // warning - this changes textureApplied bool even when only one texture was applied - not all of them

      return ui + (long)RandomStatic.numericRecipes((ulong)vi);
    }
  }
}

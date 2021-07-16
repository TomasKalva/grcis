﻿using MathSupport;
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
  public class VolumeCube : Cube, ISolid
  {
    public VolumeCube (Func<Vector3d, Vector3d> color, double step = 0.01)
    {
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
          //p += 0.05 * random.GetValue(p.X, p.Y, p.Z) * localP1;
          // ignore positions out of bounds
          if ((p.X >= 1 || p.X < 0) ||
              (p.Y >= 1 || p.Y < 0) ||
              (p.Z >= 1 || p.Z < 0))
              continue;

          // sum color values
          colorVec += MyMath.Max(color(p), 0) * step;
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

    private Intersection OutIntersection (Vector3d p0, Vector3d p1)
    {
      var intersections = Intersect(p0, p1);
      return intersections.Count == 0 ? null : intersections.Last.Value;
    }
  }
}
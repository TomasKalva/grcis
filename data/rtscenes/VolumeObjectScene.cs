//////////////////////////////////////////////////
// Rendering params.

using _048rtmontecarlo;

Debug.Assert(scene != null);
Debug.Assert(context != null);

//////////////////////////////////////////////////
// Preprocessing stage support.

// Uncomment the block if you need preprocessing.
/*
if (Util.TryParseBool(context, PropertyName.CTX_PREPROCESSING))
{
  // TODO: put your preprocessing code here!
  //
  // It will be run only this time.
  // Store preprocessing results to arbitrary (non-reserved) context item,
  //  subsequent script calls will find it there...

  return;
}
*/

// Tooltip (if script uses values from 'param').
context[PropertyName.CTX_TOOLTIP] = "n=<double> (index of refraction)\rmat={mirror|glass}}";

//////////////////////////////////////////////////
// CSG scene.

CSGInnerNode root = new CSGInnerNode(SetOperation.Union);
root.SetAttribute(PropertyName.REFLECTANCE_MODEL, new PhongModel());
root.SetAttribute(PropertyName.MATERIAL, new PhongMaterial(new double[] {1.0, 0.6, 0.1}, 0.1, 0.8, 0.2, 16));
scene.Intersectable = root;

// Background color.
scene.BackgroundColor = new double[] {0.0, 0.05, 0.07};

// Camera.
scene.Camera = new StaticCamera(new Vector3d(0.7, 3.0, -10.0),
                                new Vector3d(0.0, -0.3, 1.0),
                                50.0);

// Light sources.
scene.Sources = new System.Collections.Generic.LinkedList<ILightSource>();
scene.Sources.Add(new AmbientLightSource(0.8));
scene.Sources.Add(new PointLightSource(new Vector3d(-5.0, 3.0, -3.0), 1.0));

// --- NODE DEFINITIONS ----------------------------------------------------

// Params dictionary.
Dictionary<string, string> p = Util.ParseKeyValueList(param);

// n = <index-of-refraction>
double n = 1.6;
Util.TryParse(p, "n", ref n);

// mat = {mirror|glass|diffuse}
PhongMaterial pm = new PhongMaterial(new double[] {1.0, 0.6, 0.1}, 0.1, 0.8, 0.2, 16);
string mat;
if (p.TryGetValue("mat", out mat))
  switch (mat)
  {
    case "mirror":
      pm = new PhongMaterial(new double[] {1.0, 1.0, 0.8}, 0.0, 0.1, 0.9, 128);
      break;

    case "glass":
      pm = new PhongMaterial(new double[] {0.0, 0.2, 0.1}, 0.05, 0.05, 0.1, 128);
      pm.n = n;
      pm.Kt = 0.9;
      break;
  }




var noise = new PerlinNoise3d();

// Function calculating color of cube bounded volume objects
RecursionFunction del = (Intersection i, Vector3d dir, double importance, out RayRecursion rr) =>
{
  double direct = 1.0 - i.TextureCoord.X;
  direct = Math.Pow(direct * direct, 6.0);

  double colorVal = 0;
  var localP0 = i.CoordLocal;
  var localP1 = Vector3d.TransformVector(dir, i.WorldToLocal).Normalized();

  // iterate over the ray
  for (double t = 0; t < 1.8; t += 0.01)
  {
    // calculate position in the local cube space
    var p = localP0 + t * localP1;
    // ignore positions out of bounds
    if ((p.X >= 1 || p.X < 0) &&
        (p.Y >= 1 || p.Y < 0) &&
        (p.Z >= 1 || p.Z < 0))
      continue;

    // sum color values in ball in middle of the cube
    var toCenter = new Vector3d(.5) - p;
    if (toCenter.X * toCenter.X + toCenter.Z * toCenter.Z + toCenter.Y * toCenter.Y < .5 * .5)
      colorVal += Math.Max(0, noise.GetValue(10 * p.X, 10 * p.Y, 10 * p.Z) / 20);
  }

  rr = new RayRecursion(
    new double[3] { colorVal, colorVal, colorVal },
    new RayRecursion.RayContribution(i, dir, importance));

  return 144L;
};

// Cubes.
ISolid c;

// Volume object
c = new Cube();
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-3.5, -1.9, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);
c.SetAttribute(PropertyName.RECURSION, del);
c.SetAttribute(PropertyName.NO_SHADOW, true);
c.SetAttribute(PropertyName.TEXTURE, new NoiseTexture());

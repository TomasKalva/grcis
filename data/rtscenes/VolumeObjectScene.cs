//////////////////////////////////////////////////
// Rendering params.

using _048rtmontecarlo;
using TomasKalva;

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
scene.BackgroundColor = new double[] {0.0, 0.0, 0.0};

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




// Cubes.
ISolid c;

var noise = new PerlinNoise3d();
Func<Vector3d, double, double, double> parabolaShape = (Vector3d v, double r, double top) =>
{
  Vector3d origin = new Vector3d(0.5, 0, 0.5);
  Vector3d relV = new Vector3d(v.X, 1 - v.Y, v.Z) - origin;
  var pY = 1 - top * (relV.X * relV.X + relV.Z * relV.Z) / (r * r);
  var dy = Math.Max(0, pY - v.Y);
  return v.Y < pY ? ( 1 - Math.Max(0, v.Y) / pY) /** MyMath.InZeroOneBounds(v)*/: 0;//Math.Max(dy * dy, 0);
};

Func<Vector3d, double> ballShape = (Vector3d v) =>
{
  Vector3d scale = new Vector3d(1, 1, 1);
  Vector3d origin = new Vector3d(0.5, 0.5, 0.5);
  Vector3d relV = Vector3d.Divide(v - origin, scale);
  var c = relV.X * relV.X + relV.Y * relV.Y + relV.Z * relV.Z;
  return c < 0.25 ? 1 : 0;
};

Func<Vector3d, Vector3d> color = (Vector3d v) =>
{
  Vector3d scaledV = v * new Vector3d(15, 1, 15);
  Vector3d displ = new Vector3d(noise[v * 3], noise[v + Vector3d.UnitX * 10] , noise[v * 3 + Vector3d.UnitX * 20]);
  var c = noise[scaledV + 3 * displ] * parabolaShape(v + 0.21532446 * displ, 0.5, 1);

  var fireLight = new Vector3d(0.916, 0.930, 0.122);
  var fireDark = new Vector3d(0.920, 0.0, 0.0);

  return 70 * c * Vector3d.Lerp(fireDark, fireLight, c) * (1 - v.Y) * (1 - v.Y) * (v.Y < 0.3 ? v.Y / 0.3 : 1);
};

// Volume object
c = new VolumeCube(color);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-3.5, -1.9, 0.0));

c = new Cube();
root.InsertChild(c, /* Matrix4d.RotateY(0.6) **/ Matrix4d.CreateTranslation(-3.6, -0.8, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);

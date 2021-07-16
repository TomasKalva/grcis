//////////////////////////////////////////////////
// Rendering params.

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

AnimatedCSGInnerNode root = new AnimatedCSGInnerNode(SetOperation.Union);
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

Func<Vector3d, double> fadeBottom = VolumeCube.FadeBottom(0.3);
//Func<Vector3d, double> fireShape = VolumeCube.ParaboloidShape(0.3, 1);
Func<Vector3d, double> fireShape = VolumeCube.BallShape(Vector3d.One * 0.5, Vector3d.One, 0.5);

var fireLight = new Vector3d(0.916, 0.930, 0.122);
var fireDark = new Vector3d(0.920, 0.0, 0.0);
Func<double, Vector3d> fireColor = intensity => Vector3d.Lerp(fireDark, fireLight, intensity);

Func<Vector3d, double, Vector3d> fire = VolumeCube.Fire(v => fireShape(v) * fadeBottom(v), fireColor);


// Volume object
c = new VolumeCube(fire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-3.5, -1.9, 0.0));

c = new Cube();
root.InsertChild(c, Matrix4d.CreateTranslation(-3.6, -0.8, 0.0));
c.SetAttribute(PropertyName.MATERIAL, pm);

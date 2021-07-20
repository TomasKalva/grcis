//////////////////////////////////////////////////
// Rendering params.

using TomasKalva;

Debug.Assert(scene != null);
Debug.Assert(context != null);

//////////////////////////////////////////////////
// CSG scene.

AnimatedCSGInnerNode root = new AnimatedCSGInnerNode(SetOperation.Union);
root.SetAttribute(PropertyName.REFLECTANCE_MODEL, new PhongModel());
root.SetAttribute(PropertyName.MATERIAL, new PhongMaterial(new double[] {1.0, 0.6, 0.1}, 0.1, 0.8, 0.2, 16));
scene.Intersectable = root;

// Background color.
scene.BackgroundColor = new double[] {0.0, 0.0, 0.0};

// Camera.
scene.Camera = new StaticCamera(new Vector3d(0.0, 3.0, -15.0),
                                new Vector3d(0.0, -0.3, 1.0),
                                50.0);

// Light sources.
scene.Sources = new System.Collections.Generic.LinkedList<ILightSource>();
scene.Sources.Add(new AmbientLightSource(0.8));
scene.Sources.Add(new PointLightSource(new Vector3d(-5.0, 3.0, -3.0), 1.0));

// --- NODE DEFINITIONS ----------------------------------------------------

// Params dictionary.
Dictionary<string, string> p = Util.ParseKeyValueList(param);

// Cubes.
ISolid c;

Func<Vector3d, double> fadeBottom = VolumeCube.FadeBottom(0.2);
Func<Vector3d, double> fireShape = VolumeCube.ParaboloidFireShape(0.5, 1.0);
Func<Vector3d, double> ellipsoid = VolumeCube.BallShape(Vector3d.One * 0.5, 0.25 * Vector3d.One);

Func<double, Vector3d> fireColorYellow = intensity => Vector3d.Lerp(new Vector3d(0.920, 0.0, 0.0), new Vector3d(0.916, 0.930, 0.122), intensity);

Func<double, Vector3d> fireColorBlue = intensity => Vector3d.Lerp(new Vector3d(0.339, 0.717, 0.925), new Vector3d(1.000, 1.000, 1.000), intensity);

var noise = VolumeCube.Noise3d();
var turbulence = VolumeCube.Turbulence(noise, 4);
var positiveTurbulence = VolumeCube.Turbulence(v => noise(v) * 0.5 + 0.5, 4);

Func<Vector3d, double, Vector3d> smoothFire = VolumeCube.Fire(fireShape, noise, noise, fireColorYellow, 1.0);
Func<Vector3d, double, Vector3d> fire = VolumeCube.Fire(fireShape, turbulence, noise, fireColorYellow, 1.0);
Func<Vector3d, double, Vector3d> chaoticFire = VolumeCube.Fire(fireShape, turbulence, turbulence, fireColorYellow, 1.0);

Func<Vector3d, double, Vector3d> smoothFireBlue = VolumeCube.Fire(fireShape, noise, noise, fireColorBlue, 1.0);
Func<Vector3d, double, Vector3d> fireBlue = VolumeCube.Fire(fireShape, turbulence, noise, fireColorBlue, 1.0);
Func<Vector3d, double, Vector3d> chaoticFireBlue = VolumeCube.Fire(fireShape, turbulence, turbulence, fireColorBlue, 1.0);

Func<Vector4d, Vector3d> displ = VolumeCube.Displacement(VolumeCube.Turbulence(VolumeCube.Noise4d(), 3));
//Func<Vector3d, Vector3d> displ = VolumeCube.Displacement(VolumeCube.Turbulence(VolumeCube.Noise3d(), 4));
Func<Vector4d, double> noise4d = VolumeCube.Noise4d();

Func<Vector3d, double, Vector3d> cloud = (v, t) =>
{
  var v4d = new Vector4d(v);
  var scaledV = 5 * v + 2 * displ(v4d * 3 + 4 * t * Vector4d.UnitW);
  var turb = positiveTurbulence(scaledV);
  return 3 * turb * Vector3d.One * ellipsoid(v + 0.5 * displ(v4d * 2 + 4 * t * Vector4d.UnitW));
};

// Volume object
c = new VolumeCube(smoothFire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-8.0, -0.9, 0.0));

c = new VolumeCube(fire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-4.0, -0.9, 0.0));

c = new VolumeCube(chaoticFire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(0.0, -0.9, 0.0));

c = new VolumeCube(smoothFireBlue);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-8.0, -6.0, 0.0));

c = new VolumeCube(fireBlue);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-4.0, -6.0, 0.0));

c = new VolumeCube(chaoticFireBlue);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(0.0, -6.0, 0.0));


//c = new VolumeCube(cloud);
//root.InsertChild(c, Matrix4d.Scale(15) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(-12.0, -12.0, 10.0));

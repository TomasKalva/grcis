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
var lookAt = new Vector3d(12.5, 2.0, 7.5);
var pos = new Vector3d(-15.0, 8.0, 1.0);
scene.Camera = new StaticCamera(pos,
                                lookAt - pos,
                                50.0);

// Light sources.
scene.Sources = new System.Collections.Generic.LinkedList<ILightSource>();
scene.Sources.Add(new AmbientLightSource(0.8));
scene.Sources.Add(new PointLightSource(new Vector3d(-5.0, 3.0, -3.0), 1.0));

// --- NODE DEFINITIONS ----------------------------------------------------

// Params dictionary.
Dictionary<string, string> p = Util.ParseKeyValueList(param);

// Base plane.
Plane pl = new Plane();
pl.SetAttribute(PropertyName.COLOR, new double[] { 0.1, 0.0, 0.5 });
root.InsertChild(pl, Matrix4d.RotateX(-MathHelper.PiOver2) * Matrix4d.CreateTranslation(0.0, -0.01, 0.0));

// Cubes.
ISolid c;

// Noises
var noise = VolumeCube.Noise3d();
var turbulence = VolumeCube.Turbulence(noise, 4);
var positiveTurbulence = VolumeCube.Turbulence(v => noise(v) * 0.5 + 0.5, 4);

// Shapes
Intensity fireShape = VolumeCube.ParaboloidFireShape(new Vector3d(0.5, 1.0, 0.5), new Vector3d(0.5, -1.0, 0.5));
Intensity smallFireShape = VolumeCube.ParaboloidFireShape(new Vector3d(0.5, 1.0, 0.5), new Vector3d(0.3, -1.0, 0.3));
Intensity ellipsoid = VolumeCube.BallShape(Vector3d.One * 0.5, 0.25 * Vector3d.One);

// Intensity to color
Func<double, Vector3d> fireColorYellow = intensity => Vector3d.Lerp(new Vector3d(0.920, 0.0, 0.0), new Vector3d(0.916, 0.930, 0.122), intensity);
Func<double, Vector3d> fireColorBlue = intensity => Vector3d.Lerp(new Vector3d(0.339, 0.717, 0.925), new Vector3d(1.000, 1.000, 1.000), intensity);

// Fires
AnimatedColor smoothFire = VolumeCube.Fire(fireShape, noise, noise, fireColorYellow, 1.0, 10.0);
AnimatedColor fire = VolumeCube.Fire(smallFireShape, turbulence, noise, fireColorYellow, 1.0, 10.0);
AnimatedColor chaoticFire = VolumeCube.Fire(fireShape, turbulence, turbulence, fireColorYellow, 1.0, 10.0);

AnimatedColor smoothFireBlue = VolumeCube.Fire(smallFireShape, noise, noise, fireColorBlue, 1.0);
AnimatedColor fireBlue = VolumeCube.Fire(fireShape, turbulence, noise, fireColorBlue, 1.0);
AnimatedColor chaoticFireBlue = VolumeCube.Fire(fireShape, turbulence, turbulence, fireColorBlue, 1.0);

// Clouds
Color displ = VolumeCube.Displacement(VolumeCube.Turbulence(VolumeCube.Noise3d(), 3));
AnimatedColor cloud = VolumeCube.Cloud(ellipsoid, displ, turbulence, fireColorBlue, 3.0);

// Volume objects

// Fire objects
c = new VolumeCube(smoothFire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(7.0, 0.0, 0.0));

c = new VolumeCube(fire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(6.0, 0.0, 8.0));

c = new VolumeCube(chaoticFire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(12.0, 0.0, 3.0));

c = new VolumeCube(smoothFireBlue);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(1.0, 0.0, 9.0));

c = new VolumeCube(fireBlue);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(2.0, 0.0, 3.0));

c = new VolumeCube(chaoticFireBlue);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(13.0, 0.0, 13.0));

//Cloud objects
c = new VolumeCube(cloud);
root.InsertChild(c, Matrix4d.Scale(40.0, 10.0, 40.0) * Matrix4d.RotateY(0.6) * Matrix4d.CreateTranslation(15.0, 1.0, 7.0));


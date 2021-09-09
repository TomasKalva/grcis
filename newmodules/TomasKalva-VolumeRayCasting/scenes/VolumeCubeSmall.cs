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
var lookAt = new Vector3d(2.0, 2.0, 2.0);
var pos = new Vector3d(-6.0, 4.0, -6.0);
scene.Camera = new StaticCamera(pos,
                                lookAt - pos,
                                40.0);

// Light sources.
scene.Sources = new System.Collections.Generic.LinkedList<ILightSource>();
scene.Sources.Add(new AmbientLightSource(0.8));
scene.Sources.Add(new PointLightSource(new Vector3d(-5.0, 3.0, -3.0), 1.0));

// --- NODE DEFINITIONS ----------------------------------------------------

// Base plane.
Plane pl = new Plane();
pl.SetAttribute(PropertyName.COLOR, new double[] { 0.0, 0.0, 0.0 });
root.InsertChild(pl, Matrix4d.RotateX(-MathHelper.PiOver2) * Matrix4d.CreateTranslation(0.0, -0.01, 0.0));

// Create noises that are used as parameters for fire
Intensity noise = VolumeCube.Noise3d();
Intensity turbulence = VolumeCube.Turbulence(noise, 4);

// Define the shape of the entire fire
Intensity fireShape = VolumeCube.ParaboloidFireShape(new Vector3d(0.5, 1.0, 0.5), new Vector3d(0.5, -1.0, 0.5));

// Define the color for a point with the given intensity, colors are vectors with rgb components
Func<double, Vector3d> fireColorYellow = intensity => Vector3d.Lerp(new Vector3d(0.920, 0.0, 0.0), new Vector3d(0.916, 0.930, 0.122), intensity);

// Create fire by defining its shape, texture, color, intensity and speed of burning 
AnimatedColor fire = VolumeCube.Fire(fireShape, turbulence, noise, fireColorYellow, 1.0, 1.0);

// Create a new VolumeCube with the fire object as its color density
ISolid c = new VolumeCube(fire);
root.InsertChild(c, Matrix4d.Scale(4) * Matrix4d.CreateTranslation(0.0, 0.2, 0.0));

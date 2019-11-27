using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public class SavedView
	{
		public SavedView(string name, View3d.Camera camera)
		{
			Name = name;
			C2W = camera.O2W;
			FocusDist = camera.FocusDist;
			AlignAxis = camera.AlignAxis;
			Aspect = camera.Aspect;
			FovX = camera.FovX;
			FovY = camera.FovY;
			Orthographic = camera.Orthographic;
		}

		public string Name { get; set; }
		public m4x4 C2W { get; private set; }
		public float FocusDist { get; private set; }
		public v4 AlignAxis { get; private set; }
		public float Aspect { get; private set; }
		public float FovX { get; private set; }
		public float FovY { get; private set; }
		public bool Orthographic { get; private set; }

		public void Apply(View3d.Camera camera)
		{
			camera.FocusDist = FocusDist;
			camera.AlignAxis = AlignAxis;
			camera.Aspect = Aspect;
			camera.FovX = FovX;
			camera.FovY = FovY;
			camera.Orthographic = Orthographic;
			camera.O2W = C2W;
			camera.Commit();
		}
	}
}

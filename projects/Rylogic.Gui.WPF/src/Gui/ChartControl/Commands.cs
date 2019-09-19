using System.Windows;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		// Notes:
		//  - Many commands are available on View3dControl but I want to make
		//    the Options the source of truth for the scene settings, so the
		//    scene commands are implemented again here, usually setting the
		//    Options value first then forwarding to the View3dControl command.

		/// <summary>Initialise the commands</summary>
		private void InitCommands()
		{
			// Objects Menu
			ToggleGridLines = Command.Create(this, () =>
			{
				Options.ShowGridLines = !Options.ShowGridLines;
				Invalidate();
			});
			ToggleAxes = Command.Create(this, () =>
			{
				Options.ShowAxes = !Options.ShowAxes;
				Invalidate();
			});

			// Tools Menu
			ToggleShowValue = Command.Create(this, () =>
			{
				ShowValueAtPointer = !ShowValueAtPointer;
			});
			ToggleShowCrossHair = Command.Create(this, () =>
			{
				ShowCrossHair = !ShowCrossHair;
			});

			// Zoom Menu=
			DoAutoRange = Command.Create(this, () =>
			{
				AutoRange();
			});
			DoAspect11 = Command.Create(this, () =>
			{
				Range.Aspect = Scene.Window.Viewport.Width / Scene.Window.Viewport.Height;
				SetCameraFromRange();
				Invalidate();
			});
			ToggleLockAspect = Command.Create(this, () =>
			{
				LockAspect = !LockAspect;
			});
			ToggleMouseCentredZoom = Command.Create(this, () =>
			{
				Options.MouseCentredZoom = !Options.MouseCentredZoom;
			});

			// Rendering
			SetBackgroundColour = Command.Create(this, () =>
			{
				Scene.SetBackgroundColour.Execute(null);
				Options.BackgroundColour = Window.BackgroundColour;
			});
			ToggleOrthographic = Command.Create(this, () =>
			{
				Options.Orthographic = !Options.Orthographic;
			});
			ToggleAntiAliasing = Command.Create(this, () =>
			{
				Options.AntiAliasing = !Options.AntiAliasing;
			});
		}

		/// <summary>Toggle visibility of the origin point</summary>
		public Command ToggleOriginPoint => Scene.ToggleOriginPoint;

		/// <summary>Toggle visibility of the focus point</summary>
		public Command ToggleFocusPoint => Scene.ToggleFocusPoint;

		/// <summary>Toggle visibility of the grid lines</summary>
		public Command ToggleGridLines { get; private set; }

		/// <summary>Toggle visibility of the axes</summary>
		public Command ToggleAxes { get; private set; }

		/// <summary>Toggle the visibility of the value at the pointer</summary>
		public Command ToggleShowValue { get; private set; }

		/// <summary>Toggle the visibility of the cross hair</summary>
		public Command ToggleShowCrossHair { get; private set; }

		/// <summary>Display the measurement tool</summary>
		public Command ShowMeasureTool => Scene.ShowMeasureTool;

		/// <summary>Auto range the chart</summary>
		public Command DoAutoRange { get; private set; }

		/// <summary>Set the pixel aspect ratio to 1:1</summary>
		public Command DoAspect11 { get; private set; }

		/// <summary>Toggle the state of 'LockAspect'</summary>
		public Command ToggleLockAspect { get; private set; }

		/// <summary>Toggle the state of zooming at the mouse pointer location</summary>
		public Command ToggleMouseCentredZoom { get; private set; }

		/// <summary>Set the background colour</summary>
		public Command SetBackgroundColour { get; private set; }

		/// <summary>Toggle between orthographic and perspective projection</summary>
		public Command ToggleOrthographic { get; private set; }

		/// <summary>Toggle antialiasing on/off</summary>
		public Command ToggleAntiAliasing { get; private set; }
	}
}

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
			ToggleGridLines = Command.Create(this, ToggleGridLinesInternal);
			ToggleAxes = Command.Create(this, ToggleAxesInternal);

			// Tools Menu
			ToggleShowValue = Command.Create(this, ToggleShowValueInternal);
			ToggleShowCrossHair = Command.Create(this, ToggleShowCrossHairInternal);
			ToggleShowTapeMeasure = Command.Create(this, ToggleShowTapeMeasureInternal);

			// Zoom Menu
			DoAutoRange = Command.Create(this, DoAutoRangeInternal);
			DoAspect11 = Command.Create(this, DoAspect11Internal);
			ToggleLockAspect = Command.Create(this, ToggleLockAspectInternal);
			ToggleMouseCentredZoom = Command.Create(this, ToggleMouseCentredZoomInternal);

			// Rendering
			SetBackgroundColour = Command.Create(this, SetBackgroundColourInternal);
			ToggleOrthographic = Command.Create(this, ToggleOrthographicInternal);
			ToggleAntiAliasing = Command.Create(this, ToggleAntiAliasingInternal);
		}

		/// <summary>Toggle visibility of the origin point</summary>
		public Command ToggleOriginPoint => Scene.ToggleOriginPoint;

		/// <summary>Toggle visibility of the focus point</summary>
		public Command ToggleFocusPoint => Scene.ToggleFocusPoint;

		/// <summary>Toggle visibility of the grid lines</summary>
		public Command ToggleGridLines { get; private set; } = null!;
		private void ToggleGridLinesInternal()
		{
			Options.ShowGridLines = !Options.ShowGridLines;
			Invalidate();
		}

		/// <summary>Toggle visibility of the axes</summary>
		public Command ToggleAxes { get; private set; } = null!;
		private void ToggleAxesInternal()
		{
			Options.ShowAxes = !Options.ShowAxes;
			Invalidate();
		}

		/// <summary>Toggle the visibility of the value at the pointer</summary>
		public Command ToggleShowValue { get; private set; } = null!;
		private void ToggleShowValueInternal()
		{
			ShowValueAtPointer = !ShowValueAtPointer;
		}

		/// <summary>Toggle the visibility of the cross hair</summary>
		public Command ToggleShowCrossHair { get; private set; } = null!;
		private void ToggleShowCrossHairInternal()
		{
			ShowCrossHair = !ShowCrossHair;
		}

		/// <summary>Toggle the tape measure tool</summary>
		public Command ToggleShowTapeMeasure { get; private set; } = null!;
		private void ToggleShowTapeMeasureInternal()
		{
			ShowTapeMeasure = !ShowTapeMeasure;
		}

		/// <summary>Display the measurement tool</summary>
		public Command ShowMeasureTool => Scene.ShowMeasureTool;

		/// <summary>Auto range the chart</summary>
		public Command DoAutoRange { get; private set; } = null!;
		private void DoAutoRangeInternal()
		{
			AutoRange();
		}

		/// <summary>Set the pixel aspect ratio to 1:1</summary>
		public Command DoAspect11 { get; private set; } = null!;
		private void DoAspect11Internal()
		{
			Range.Aspect = Scene.Window.Viewport.Width / Scene.Window.Viewport.Height;
			SetCameraFromRange();
			Invalidate();
		}

		/// <summary>Toggle the state of 'LockAspect'</summary>
		public Command ToggleLockAspect { get; private set; } = null!;
		private void ToggleLockAspectInternal()
		{
			LockAspect = !LockAspect;
		}

		/// <summary>Toggle the state of zooming at the mouse pointer location</summary>
		public Command ToggleMouseCentredZoom { get; private set; } = null!;
		private void ToggleMouseCentredZoomInternal()
		{
			Options.MouseCentredZoom = !Options.MouseCentredZoom;
		}

		/// <summary>Set the background colour</summary>
		public Command SetBackgroundColour { get; private set; } = null!;
		private void SetBackgroundColourInternal()
		{
			Scene.SetBackgroundColour.Execute(null);
			Options.BackgroundColour = Window.BackgroundColour;
		}

		/// <summary>Toggle between orthographic and perspective projection</summary>
		public Command ToggleOrthographic { get; private set; } = null!;
		private void ToggleOrthographicInternal()
		{
			Options.Orthographic = !Options.Orthographic;
		}

		/// <summary>Toggle antialiasing on/off</summary>
		public Command ToggleAntiAliasing { get; private set; } = null!;
		private void ToggleAntiAliasingInternal()
		{
			Options.AntiAliasing = !Options.AntiAliasing;
		}
	}
}

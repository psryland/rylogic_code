using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Initialise the commands</summary>
		private void InitCommands()
		{
			// Objects Menu
			ToggleGridLines = Command.Create(this, () =>
			{
				Options.ShowGridLines = !Options.ShowGridLines;
				Scene.Invalidate();
			});
			ToggleAxes = Command.Create(this, () =>
			{
				Options.ShowAxes = !Options.ShowAxes;
				Scene.Invalidate();
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

			// Zoom Menu
			DoAutoRange = Command.Create(this, () =>
			{
				AutoRange();
			});
			DoAspect11 = Command.Create(this, () =>
			{
				Range.Aspect = Scene.Window.Viewport.Width / Scene.Window.Viewport.Height;
				SetCameraFromRange();
				Scene.Invalidate();
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
			NavigationModes = new ListCollectionView(Enum<ENavMode>.ValuesArray);
			NavigationModes.CurrentChanged += (s, a) =>
			{
				Options.NavigationMode = (ENavMode)NavigationModes.CurrentItem;
				Scene.Invalidate();
			};
			RenderingFillModes.MoveCurrentTo(Options.FillMode);
			RenderingFillModes.CurrentChanged += (s, a) =>
			{
				Options.FillMode = (View3d.EFillMode)RenderingFillModes.CurrentItem;
			};
			ToggleAntiAliasing = Command.Create(this, () =>
			{
				Options.AntiAliasing = !Options.AntiAliasing;
				Scene.MultiSampling = Options.AntiAliasing ? 4U : 1U;
				Scene.Invalidate();
			});

			// Properties
			ShowOptionsUI = Command.Create(this, () =>
			{
				new ChartDetail.OptionsUI(this, Options).ShowDialog();
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

		/// <summary>Available navigation modes for the chart</summary>
		public ICollectionView NavigationModes { get; private set; }

		/// <summary>Toggle between orthographic and perspective projection</summary>
		public Command ToggleOrthographic => Scene.ToggleOrthographic;

		/// <summary>Available rendering fill modes</summary>
		public ICollectionView RenderingFillModes => Scene.RenderingFillModes;

		/// <summary>Toggle antialiasing on/off</summary>
		public Command ToggleAntiAliasing { get; private set; }

		/// <summary>Show the chart options UI</summary>
		public Command ShowOptionsUI { get; private set; }

		/// <summary>Create and display a context menu</summary>
		public void ShowContextMenu(Point location, HitTestResult hit_result)
		{
			if (!(FindResource("ChartCMenu") is ContextMenu cmenu))
				return;

			// Refresh the state
			cmenu.DataContext = null;
			cmenu.DataContext = this;

			// Allow users to add/remove menu options
			// Do this last so that users have the option of removing options they don't want displayed
			OnCustomiseContextMenu(new AddUserMenuOptionsEventArgs(cmenu, hit_result));

			// Show the context menu
			cmenu.Items.TidySeparators();
			cmenu.PlacementTarget = this;
			cmenu.IsOpen = true;
		}

		/// <summary>Event allowing callers to add options to the context menu</summary>
		public event EventHandler<AddUserMenuOptionsEventArgs> AddUserMenuOptions;
		protected virtual void OnCustomiseContextMenu(AddUserMenuOptionsEventArgs args)
		{
			AddUserMenuOptions?.Invoke(this, args);
		}
	}
}

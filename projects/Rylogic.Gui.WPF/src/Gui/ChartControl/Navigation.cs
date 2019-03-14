using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Enable/Disable mouse navigation</summary>
		public bool DefaultMouseControl
		{
			get { return m_default_mouse_control; }
			set
			{
				if (m_default_mouse_control == value) return;
				m_default_mouse_control = value;
				NotifyPropertyChanged(nameof(DefaultMouseControl));
			}
		}
		private bool m_default_mouse_control;

		/// <summary>Enable/Disable keyboard shortcuts for navigation</summary>
		public bool DefaultKeyboardShortcuts
		{
			get { return m_default_keyshortcuts; }
			set
			{
				if (m_default_keyshortcuts == value) return;
				m_default_keyshortcuts = value;
				NotifyPropertyChanged(nameof(DefaultKeyboardShortcuts));
			}
		}
		private bool m_default_keyshortcuts;

		/// <summary>The default behaviour of area select mode</summary>
		public EAreaSelectMode AreaSelectMode { get; set; }
		public enum EAreaSelectMode { Disabled, SelectElements, Zoom }

		/// <summary>Mouse events on the chart</summary>
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			var location = e.GetPosition(this);

			// If a mouse op is already active, ignore mouse down
			if (MouseOperations.Active != null)
				return;

			// Look for the mouse op to perform
			if (MouseOperations.Pending(e.ChangedButton) == null && DefaultMouseControl)
			{
				switch (e.ChangedButton)
				{
				default: return;
				case MouseButton.Left: MouseOperations.SetPending(e.ChangedButton, new MouseOpDefaultLButton(this)); break;
				case MouseButton.Middle: MouseOperations.SetPending(e.ChangedButton, new MouseOpDefaultMButton(this)); break;
				case MouseButton.Right: MouseOperations.SetPending(e.ChangedButton, new MouseOpDefaultRButton(this)); break;
				}
			}

			// Start the next mouse op
			MouseOperations.BeginOp(e.ChangedButton);

			// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
			{
				op.m_btn_down = true;
				op.m_grab_client = location; // Note: in ChartControl space, not ChartPanel space
				op.m_grab_chart = ClientToChart(op.m_grab_client);
				op.m_hit_result = HitTestCS(op.m_grab_client, Keyboard.Modifiers, null);
				op.MouseDown(e);
				CaptureMouse();
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			var location = e.GetPosition(this);

			// Look for the mouse op to perform
			var op = MouseOperations.Active;
			if (op != null)
			{
				if (!op.Cancelled)
					op.MouseMove(e);
			}
			// Otherwise, provide mouse hover detection
			else
			{
				var hit = HitTestCS(location, Keyboard.Modifiers, null);
				var hovered = hit.Hits.Select(x => x.Element).ToHashSet();

				// Remove elements that are no longer hovered
				// and remove existing hovered's from the set.
				for (int i = Hovered.Count; i-- != 0;)
				{
					if (hovered.Contains(Hovered[i]))
						hovered.Remove(Hovered[i]);
					else
						Hovered.RemoveAt(i);
				}

				// Add elements that are now hovered
				Hovered.AddRange(hovered);

				// Notify that the chart coordinate at the mouse pointer has changed
				if (hit.Zone == EZone.Chart)
					NotifyPropertyChanged(nameof(ValueAtPointer));
			}
		}
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);

			// Only release the mouse when all buttons are up
			if (e.ToMouseBtns() == EMouseBtns.None)
				ReleaseMouseCapture();

			// Look for the mouse op to perform
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
				op.MouseUp(e);

			MouseOperations.EndOp(e.ChangedButton);
		}
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);
			var location = e.GetPosition(this);

			var along_ray = Options.MouseCentredZoom || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
			var chart_pt = ClientToChart(location);

			if (SceneBounds.Contains(location))
			{
				// If there is a mouse op in progress, ignore the wheel
				var op = MouseOperations.Active;
				if (op == null || op.Cancelled)
				{
					// Translate the camera along a ray through 'point'
					var loc = Gui_.MapPoint(this, Scene, location);
					Scene.Window.MouseNavigateZ(loc.ToPointF(), e.ToMouseBtns(Keyboard.Modifiers), e.Delta, along_ray);

					// Update the axes from the camera position
					SetRangeFromCamera();
					Scene.Invalidate();

					// Notify that the chart coordinate at the mouse pointer has changed
					NotifyPropertyChanged(nameof(ValueAtPointer));
				}
			}
			else if (Options.ShowAxes)
			{
				var scale = 0.001f;
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) scale *= 0.1f;
				if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) scale *= 0.01f;
				var delta = Math_.Clamp(e.Delta * scale, -0.999f, 0.999f);

				var chg = false;

				// Change the aspect ratio by zooming on the XAxis
				if (XAxisBounds.Contains(location) && !XAxis.LockRange)
				{
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && XAxis.AllowScroll)
					{
						XAxis.Shift(XAxis.Span * delta);
						chg = true;
					}
					if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && XAxis.AllowZoom)
					{
						var x = along_ray ? chart_pt.X : XAxis.Centre;
						var left = (XAxis.Min - x) * (1f - delta);
						var rite = (XAxis.Max - x) * (1f - delta);
						XAxis.Set(x + left, x + rite);
						if (Options.LockAspect != null)
							YAxis.Span *= (1f - delta);

						chg = true;
					}
				}

				// Check the aspect ratio by zooming on the YAxis
				if (YAxisBounds.Contains(location) && !YAxis.LockRange)
				{
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && YAxis.AllowScroll)
					{
						YAxis.Shift(YAxis.Span * delta);
						chg = true;
					}
					if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && YAxis.AllowZoom)
					{
						var y = along_ray ? chart_pt.Y : YAxis.Centre;
						var left = (YAxis.Min - y) * (1f - delta);
						var rite = (YAxis.Max - y) * (1f - delta);
						YAxis.Set(y + left, y + rite);
						if (Options.LockAspect != null)
							XAxis.Span *= (1f - delta);

						chg = true;
					}
				}

				// Set the camera position from the new axis ranges
				if (chg)
				{
					SetCameraFromRange();
					Scene.Invalidate();
				}
			}
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			SetCursor();

			var op = MouseOperations.Active;
			if (op != null && !e.Handled)
				op.OnKeyDown(e);

			// Allow derived classes to handle the key
			base.OnKeyDown(e);

			// If the current mouse operation doesn't use the key,
			// see if it's a default keyboard shortcut.
			if (!e.Handled && DefaultKeyboardShortcuts)
				TranslateKey(e);
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			SetCursor();

			var op = MouseOperations.Active;
			if (op != null && !e.Handled)
				op.OnKeyUp(e);

			base.OnKeyUp(e);
		}

		/// <summary>Set the mouse cursor based on key state</summary>
		protected virtual void SetCursor()
		{
			// Sub class to do something like this:
			// if (ModifierKeys.HasFlag(Keys.Shift))   Cursor = Cursors.ArrowPlus;
			// if (ModifierKeys.HasFlag(Keys.Control)) Cursor = Cursors.ArrowMinus;
			Cursor = Cursors.Arrow;
		}

		/// <summary>Raised when the chart is clicked with the mouse</summary>
		public event EventHandler<ChartClickedEventArgs> ChartClicked;
		protected virtual void OnChartClicked(ChartClickedEventArgs args)
		{
			ChartClicked?.Invoke(this, args);
		}

		/// <summary>Raised when the chart is area selected</summary>
		public event EventHandler<ChartAreaSelectEventArgs> ChartAreaSelect;
		protected virtual void OnChartAreaSelect(ChartAreaSelectEventArgs args)
		{
			ChartAreaSelect?.Invoke(this, args);

			// Select chart elements by default
			if (!args.Handled)
			{
				switch (AreaSelectMode)
				{
				default: throw new Exception("Unknown area select mode");
				case EAreaSelectMode.Disabled:
					{
						break;
					}
				case EAreaSelectMode.SelectElements:
					{
						var rect = new Rect(args.SelectionArea.MinX, args.SelectionArea.MinY, args.SelectionArea.SizeX, args.SelectionArea.SizeY);
						SelectElements(rect, Keyboard.Modifiers);
						break;
					}
				case EAreaSelectMode.Zoom:
					{
						PositionChart(args.SelectionArea);
						break;
					}
				}
			}
		}

		/// <summary>Adjust the X/Y axis of the chart to cover the given area</summary>
		public void PositionChart(BBox area)
		{
			// Ensure the selection area is >= 1 pixel for width/height
			var sz = ClientToChart(new Size(1,1));
			XAxis.Set(area.MinX, Math.Max(area.MaxX, area.MinX + sz.Width));
			YAxis.Set(area.MinY, Math.Max(area.MaxY, area.MinY + sz.Height));
			SetCameraFromRange();
		}

		/// <summary>Shifts the X and Y range of the chart so that chart space position 'gs_point' is at client space position 'cs_point'</summary>
		public void PositionChart(Point cs_point, Point gs_point)
		{
			var dst = ClientToChart(cs_point);
			var c2w = Scene.Camera.O2W;
			c2w.pos += c2w.x * (float)(gs_point.X - dst.X) + c2w.y * (float)(gs_point.Y - dst.Y);
			Scene.Camera.O2W = c2w;
			Scene.Invalidate();
		}

		/// <summary>Handle navigation keyboard shortcuts</summary>
		public virtual void TranslateKey(KeyEventArgs e)
		{
			if (e.Handled) return;
			switch (e.Key)
			{
			case Key.Escape:
				{
					if (AllowSelection)
					{
						Selected.Clear();
						Scene.Invalidate();
					}
					break;
				}
			case Key.Delete:
				{
					if (AllowEditing)
					{
						//// Allow the caller to cancel the deletion or change the selection
						//var res = new DiagramChangedRemoveElementsEventArgs(Selected.ToArray());
						//if (!RaiseDiagramChanged(res).Cancel)
						//{
						//	foreach (var elem in res.Elements)
						//	{
						//		var node = elem as Node;
						//		if (node != null)
						//			node.DetachConnectors();

						//		var conn = elem as Connector;
						//		if (conn != null)
						//			conn.DetachNodes();

						//		Elements.Remove(elem);
						//	}
						//	Invalidate();
						//}
					}
					break;
				}
			case Key.F5:
				{
					View3d.ReloadScriptSources();
					Scene.Invalidate();
					break;
				}
			case Key.F7:
				{
					AutoRange();
					Scene.Invalidate();
					break;
				}
			case Key.A:
				{
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					{
						Selected.Clear();
						Selected.AddRange(Elements);
						Scene.Invalidate();
						Debug.Assert(CheckConsistency());
					}
					break;
				}
			}
		}
	}
}

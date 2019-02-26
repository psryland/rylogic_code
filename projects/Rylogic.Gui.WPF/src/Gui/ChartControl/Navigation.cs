using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	using ChartDetail;

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
			}
		}
		private bool m_default_mouse_control;

		/// <summary>Enable/Disable keyboard shortcuts for navigation</summary>
		public bool DefaultKeyboardShortcuts
		{
			get { return m_default_keyboard_shortcuts; }
			set
			{
				if (m_default_keyboard_shortcuts == value) return;
				m_default_keyboard_shortcuts = value;
			}
		}
		private bool m_default_keyboard_shortcuts;

		/// <summary>The default behaviour of area select mode</summary>
		public EAreaSelectMode AreaSelectMode { get; set; }
		public enum EAreaSelectMode { Disabled, SelectElements, Zoom }

		/// <summary>Mouse events on the chart</summary>
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			var location = e.GetPosition(this).ToPointF();

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
				Mouse.Capture(this);
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			var location = e.GetPosition(this).ToPointF();

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
			}
		}
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			base.OnMouseUp(e);

			// Only release the mouse when all buttons are up
			if (e.ToMouseBtns() == EMouseBtns.None)
				Mouse.Capture(this, CaptureMode.None);

			// Look for the mouse op to perform
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
				op.MouseUp(e);

			MouseOperations.EndOp(e.ChangedButton);
		}
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			var location = e.GetPosition(this).ToPointF();
			var scene_bounds = SceneBounds;
			var perp_z = Options.PerpendicularZTranslation != Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
			var chart_pt = ClientToChart(location);

			if (scene_bounds.Contains(location))
			{
				// If there is a mouse op in progress, ignore the wheel
				var op = MouseOperations.Active;
				if (op == null || op.Cancelled)
				{
					// Translate the camera along a ray through 'point'
					var loc = Gui_.MapPoint(this, Scene, location);
					Scene.Window.MouseNavigateZ(loc, e.ToMouseBtns(Keyboard.Modifiers), e.Delta, !perp_z);
					//Invalidate();
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
				var xaxis_bounds = RectangleF.FromLTRB(scene_bounds.Left, scene_bounds.Bottom, scene_bounds.Right, (float)Height);
				if (xaxis_bounds.Contains(location) && !XAxis.LockRange)
				{
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && XAxis.AllowScroll)
					{
						XAxis.Shift(XAxis.Span * delta);
						chg = true;
					}
					if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && XAxis.AllowZoom)
					{
						var x = perp_z ? XAxis.Centre : chart_pt.X;
						var left = (XAxis.Min - x) * (1f - delta);
						var rite = (XAxis.Max - x) * (1f - delta);
						XAxis.Set(chart_pt.X + left, chart_pt.X + rite);
						if (Options.LockAspect != null)
							YAxis.Span *= (1f - delta);

						chg = true;
					}
				}

				// Check the aspect ratio by zooming on the YAxis
				var yaxis_bounds = RectangleF.FromLTRB(0, scene_bounds.Top, scene_bounds.Left, scene_bounds.Bottom);
				if (yaxis_bounds.Contains(location) && !YAxis.LockRange)
				{
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && YAxis.AllowScroll)
					{
						YAxis.Shift(YAxis.Span * delta);
						chg = true; ;
					}
					if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && YAxis.AllowZoom)
					{
						var y = perp_z ? YAxis.Centre : chart_pt.Y;
						var left = (YAxis.Min - y) * (1f - delta);
						var rite = (YAxis.Max - y) * (1f - delta);
						YAxis.Set(chart_pt.Y + left, chart_pt.Y + rite);
						if (Options.LockAspect != null)
							XAxis.Span *= (1f - delta);

						chg = true;
					}
				}

				// Set the camera position from the Axis ranges
				if (chg)
				{
					SetCameraFromRange();
					//Invalidate();
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
						var rect = new RectangleF(args.SelectionArea.MinX, args.SelectionArea.MinY, args.SelectionArea.SizeX, args.SelectionArea.SizeY);
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
			// Ignore if the given area is smaller than the minimum selection distance (in screen space)
			var bl = ChartToClient(new PointF(area.MinX, area.MinY));
			var tr = ChartToClient(new PointF(area.MaxX, area.MaxY));
			if (Math.Abs(bl.X - tr.X) < Options.MinSelectionDistance ||
				Math.Abs(bl.Y - tr.Y) < Options.MinSelectionDistance)
				return;

			XAxis.Set(area.MinX, area.MaxX);
			YAxis.Set(area.MinY, area.MaxY);
			SetCameraFromRange();
		}

		/// <summary>Shifts the X and Y range of the chart so that chart space position 'gs_point' is at client space position 'cs_point'</summary>
		public void PositionChart(Point cs_point, PointF gs_point)
		{
			var dst = ClientToChart(cs_point);
			var c2w = Scene.Camera.O2W;
			c2w.pos += c2w.x * (gs_point.X - dst.X) + c2w.y * (gs_point.Y - dst.Y);
			Scene.Camera.O2W = c2w;
			//Invalidate();
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
						//Invalidate();
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
					//Invalidate();
					break;
				}
			case Key.F7:
				{
					AutoRange();
					break;
				}
			case Key.A:
				{
					if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
					{
						Selected.Clear();
						Selected.AddRange(Elements);
						//Invalidate();
						Debug.Assert(CheckConsistency());
					}
					break;
				}
			}
		}
	}
}

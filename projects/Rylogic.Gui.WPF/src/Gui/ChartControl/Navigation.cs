using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		private UndoHistory<NavHistoryRecord> m_nav_history;

		/// <summary>Set up the chart for MouseOps</summary>
		private void InitNavigation()
		{
			m_nav_history = new UndoHistory<NavHistoryRecord>
			{
				IsDuplicate = NavHistoryRecord.ApproxEqual,
				ApplySnapshot = snapshot =>
				{
					XAxis.Range = snapshot.XRange;
					YAxis.Range = snapshot.YRange;
					SetCameraFromRange();
					Scene.Invalidate();
				},
			};
		}

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

		/// <summary>Mouse/key events on the chart</summary>
		protected override void OnMouseDown(MouseButtonEventArgs args)
		{
			//
			//  *** Use PreviewMouseDown to set pending MouseOps ***
			//  *** Don't set e.Handled = true, SetPending() is enough ***
			//
			// Notes:
			//  - MouseButton events in WPF are routed events that behave differently to WinForms.
			//  - The order of handlers for MouseDown is:
			//     1) Registered class handlers
			//     2) OnMouseDown - base.OnMouseDown() does *not* raise the MouseDown event
			//     3) Handers attached to event MouseDown
			//  - MouseDown is a 'bubbling' event, it starts at the visual tree leaf element and
			//    bubble up the tree, stopped when a handle sets 'e.Handled = true'
			//  - PreviewMouseDown is a tunnelling 'event', it starts at the window and drills
			//    down the tree to the leaves. If 'e.Handled = true' in a PreviewMouseDown handler
			//    then MouseDown is never raised, and override OnMouseDown isn't called.

			var location = args.GetPosition(this);

			// If a mouse op is already active, ignore mouse down
			if (MouseOperations.Active != null)
				return;

			// Look for the mouse op to perform
			if (MouseOperations.Pending(args.ChangedButton) == null && DefaultMouseControl)
			{
				switch (args.ChangedButton)
				{
				default: return;
				case MouseButton.Left:   MouseOperations.SetPending(args.ChangedButton, new MouseOpDefaultLButton(this)); break;
				case MouseButton.Middle: MouseOperations.SetPending(args.ChangedButton, new MouseOpDefaultMButton(this)); break;
				case MouseButton.Right:  MouseOperations.SetPending(args.ChangedButton, new MouseOpDefaultRButton(this)); break;
				case MouseButton.XButton1: UndoNavigation(); break;
				case MouseButton.XButton2: RedoNavigation(); break;
				}
			}

			// Start the next mouse op
			MouseOperations.BeginOp(args.ChangedButton);

			// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
			{
				op.m_btn_down = true;
				op.m_grab_client = location; // Note: in ChartControl space, not ChartPanel space
				op.m_grab_chart = ClientToChart(op.m_grab_client);
				op.m_hit_result = HitTestCS(op.m_grab_client, Keyboard.Modifiers, args.ToMouseBtns(), null);
				op.MouseDown(args);
				CaptureMouse();
			}
		}
		protected override void OnMouseMove(MouseEventArgs args)
		{
			var location = args.GetPosition(this);

			// Look for the mouse op to perform
			var op = MouseOperations.Active;
			if (op != null)
			{
				if (!op.Cancelled)
					op.MouseMove(args);
			}
			// Otherwise, provide mouse hover detection
			else if (SceneBounds != Rect_.Zero)
			{
				var hit = HitTestCS(location, Keyboard.Modifiers, args.ToMouseBtns(), null);
				var hovered = hit.Hits.Select(x => x.Element).ToHashSet(0);

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
				if (hit.Zone == EZone.Chart && ShowValueAtPointer)
					NotifyPropertyChanged(nameof(ValueAtPointer));
			}
		}
		protected override void OnMouseUp(MouseButtonEventArgs args)
		{
			// Only release the mouse when all buttons are up
			if (args.ToMouseBtns() == EMouseBtns.None)
				ReleaseMouseCapture();

			// Look for the mouse op to perform
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
				op.MouseUp(args);

			MouseOperations.EndOp(args.ChangedButton);
		}
		protected override void OnMouseWheel(MouseWheelEventArgs args)
		{
			// If there is a mouse op in progress, ignore the wheel
			var op = MouseOperations.Active;
			if (op != null && !op.Cancelled)
				return;

			var location = args.GetPosition(this);
			var along_ray = Options.MouseCentredZoom || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);
			var chart_pt = ClientToChart(location);
			var hit = HitTestZoneCS(location, Keyboard.Modifiers, args.ToMouseBtns());

			// Batch mouse wheel events into 100ms groups
			var defer_nav_checkpoint = DeferNavCheckpoints();
			Dispatcher_.BeginInvokeDelayed(() =>
			{
				Util.Dispose(ref defer_nav_checkpoint);
				SaveNavCheckpoint();
			}, TimeSpan.FromMilliseconds(100));

			var scale = 0.001f;
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) scale *= 0.1f;
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) scale *= 0.01f;
			var delta = Math_.Clamp(args.Delta * scale, -0.999f, 0.999f);
			var chg = (string)null;

			// If zooming is allowed on both axes, translate the camera
			if (hit.Zone == EZone.Chart && XAxis.AllowZoom && YAxis.AllowZoom)
			{
				// Translate the camera along a ray through 'point'
				var loc = Gui_.MapPoint(this, Scene, location);
				Scene.Window.MouseNavigateZ(loc.ToPointF(), args.ToMouseBtns(Keyboard.Modifiers), args.Delta, along_ray);
				chg = nameof(SetRangeFromCamera);
			}
			
			// Otherwise, zoom on the allowed axis only
			else if (hit.Zone == EZone.XAxis || (hit.Zone == EZone.Chart && !YAxis.AllowZoom))
			{
				if (hit.ModifierKeys.HasFlag(ModifierKeys.Control) && XAxis.AllowScroll)
				{
					// Scroll the XAxis
					XAxis.Shift(XAxis.Span * delta);
					chg = nameof(SetCameraFromRange);
				}
				else if (!hit.ModifierKeys.HasFlag(ModifierKeys.Control) && XAxis.AllowZoom)
				{
					// Change the aspect ratio by zooming on the XAxis
					var x = along_ray ? chart_pt.X : XAxis.Centre;
					var left = (XAxis.Min - x) * (1f - delta);
					var rite = (XAxis.Max - x) * (1f - delta);
					XAxis.Set(x + left, x + rite);
					if (Options.LockAspect != null)
						YAxis.Span *= (1f - delta);

					chg = nameof(SetCameraFromRange);
				}
			}
			else if (hit.Zone == EZone.YAxis || (hit.Zone == EZone.Chart && !XAxis.AllowZoom))
			{
				if (hit.ModifierKeys.HasFlag(ModifierKeys.Control) && YAxis.AllowScroll)
				{
					// Scroll the YAxis
					YAxis.Shift(YAxis.Span * delta);
					chg = nameof(SetCameraFromRange);
				}
				else if (!hit.ModifierKeys.HasFlag(ModifierKeys.Control) && YAxis.AllowZoom)
				{
					// Change the aspect ratio by zooming on the YAxis
					var y = along_ray ? chart_pt.Y : YAxis.Centre;
					var left = (YAxis.Min - y) * (1f - delta);
					var rite = (YAxis.Max - y) * (1f - delta);
					YAxis.Set(y + left, y + rite);
					if (Options.LockAspect != null)
						XAxis.Span *= (1f - delta);

					chg = nameof(SetCameraFromRange);
				}
			}
			
			switch (chg)
			{
			// Update the axes from the camera position
			case nameof(SetRangeFromCamera):
				SetRangeFromCamera();
				NotifyPropertyChanged(nameof(ValueAtPointer));
				Scene.Invalidate();
				break;

				// Set the camera position from the new axis ranges
			case nameof(SetCameraFromRange):
				SetCameraFromRange();
				NotifyPropertyChanged(nameof(ValueAtPointer));
				Scene.Invalidate();
				break;
			}
		}

		/// <summary>Handle key events</summary>
		protected override void OnKeyDown(KeyEventArgs args)
		{
			// *** Use PreviewKeyDown to add MouseOps ***
			SetCursor();

			var op = MouseOperations.Active;
			if (op != null && !args.Handled)
				op.OnKeyDown(args);

			// If the current mouse operation doesn't use the key,
			// see if it's a default keyboard shortcut.
			if (!args.Handled && DefaultKeyboardShortcuts)
				OnTranslateKey(args);
		}
		protected override void OnKeyUp(KeyEventArgs args)
		{
			SetCursor();

			var op = MouseOperations.Active;
			if (op != null && !args.Handled)
				op.OnKeyUp(args);
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

		/// <summary>Raised when the chart is dragged with the mouse</summary>
		public event EventHandler<ChartDraggedEventArgs> ChartDragged;
		protected virtual void OnChartDragged(ChartDraggedEventArgs args)
		{
			// This allows custom behaviour of a mouse drag on the chart
			ChartDragged?.Invoke(this, args);
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
						SelectElements(rect, Keyboard.Modifiers, args.MouseBtns);
						break;
					}
				case EAreaSelectMode.Zoom:
					{
						PositionChart(args.SelectionArea);
						break;
					}
				case EAreaSelectMode.ZoomIfNoSelection:
					{
						if (Selected.Count == 0)
							PositionChart(args.SelectionArea);
						break;
					}
				}
			}
		}
		private bool DoChartAreaSelect()
		{
			switch (AreaSelectMode)
			{
			default: throw new Exception($"Unknown area selection mode: {AreaSelectMode}");
			case EAreaSelectMode.Disabled: return false;
			case EAreaSelectMode.SelectElements: return true;
			case EAreaSelectMode.Zoom: return true;
			case EAreaSelectMode.ZoomIfNoSelection: return Selected.Count == 0;
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
		public virtual void OnTranslateKey(KeyEventArgs e)
		{
			if (e.Handled) return;

			// Allow users to handle the key
			TranslateKey?.Invoke(this, e);
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
					//if (AllowEditing)
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
		public event EventHandler<KeyEventArgs> TranslateKey;

		/// <summary>Remove all nav checkpoint history</summary>
		public void ClearNavHistory()
		{
			m_nav_history.Clear();
		}

		/// <summary>Add an undo record to the navigation history</summary>
		public void SaveNavCheckpoint()
		{
			m_nav_history.Add(new NavHistoryRecord(this));
		}

		/// <summary>Prevent navigation check points from being added to the history</summary>
		public IDisposable DeferNavCheckpoints()
		{
			return m_nav_history.Defer();
		}

		/// <summary>Go back to the previous chart position</summary>
		public void UndoNavigation()
		{
			m_nav_history.Undo();
		}

		/// <summary>Go forward to the next chart position</summary>
		public void RedoNavigation()
		{
			m_nav_history.Redo();
		}

		/// <summary>A snapshot of the axis range</summary>
		private class NavHistoryRecord
		{
			public NavHistoryRecord(ChartControl chart)
			{
				XRange = chart.XAxis.Range;
				YRange = chart.YAxis.Range;
			}

			/// <summary>The chart range at the snapshot</summary>
			public RangeF XRange { get; }
			public RangeF YRange { get; }

			/// <summary>True if 'lhs' and 'rhs' are very similar</summary>
			public static bool ApproxEqual(NavHistoryRecord lhs, NavHistoryRecord rhs)
			{
				// Use a tolerance of 1% of the smallest range
				var tol = 0.01 * Math.Min(Math.Min(lhs.XRange.Size, lhs.YRange.Size), Math.Min(rhs.XRange.Size, rhs.YRange.Size));
				return
					Math_.FEqlAbsolute(lhs.XRange.Beg, rhs.XRange.Beg, tol) &&
					Math_.FEqlAbsolute(lhs.XRange.End, rhs.XRange.End, tol) &&
					Math_.FEqlAbsolute(lhs.YRange.Beg, rhs.YRange.Beg, tol) &&
					Math_.FEqlAbsolute(lhs.YRange.End, rhs.YRange.End, tol);
			}
		}

		/// <summary>Area selection mode</summary>
		public enum EAreaSelectMode
		{
			Disabled,

			/// <summary>Elements within the dragged area become selected</summary>
			SelectElements,

			/// <summary>The dragged out area becomes the new chart bounds</summary>
			Zoom,

			/// <summary>Only area-zoom if there are no elements in the selected state</summary>
			ZoomIfNoSelection,
		}
	}
}

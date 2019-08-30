using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;
using Size_ = Rylogic.Extn.Windows.Size_;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Manages per-button mouse operations</summary>
		public class MouseOps :IDisposable
		{
			public MouseOps()
			{
				PendingOp = Enum<MouseButton>.Values.ToDictionary(k => k, k => (MouseOp)null);
			}
			public void Dispose()
			{
				Active = null;

				foreach (var op in PendingOp.Values.NotNull().ToList())
					op.Dispose();

				PendingOp.Clear();
			}

			/// <summary>The next mouse operation for each mouse button</summary>
			private Dictionary<MouseButton, MouseOp> PendingOp { get; }

			/// <summary>The currently active mouse op</summary>
			public MouseOp Active
			{
				get { return m_active; }
				private set
				{
					if (m_active == value) return;
					if (m_active != null)
					{
						// Clean up the previous active mouse op
						if (m_active.Cancelled)
							m_active.NotifyCancelled();

						m_active.Disposed -= HandleMouseOpDisposed;
						m_active.Dispose();
					}
					m_active = value;
					if (m_active != null)
					{
						// Watch for external disposing
						m_active.Disposed -= HandleMouseOpDisposed;
						m_active.Disposed += HandleMouseOpDisposed;

						// If the op starts immediately without a mouse down, fake
						// a mouse down event as soon as it becomes active.
						if (!m_active.StartOnMouseDown)
							m_active.MouseDown(null);
					}
				}
			}
			private MouseOp m_active;

			/// <summary>Return the op pending for button 'idx'</summary>
			public MouseOp Pending(MouseButton btn)
			{
				return PendingOp[btn];
			}

			/// <summary>Add a mouse op to be started on the next mouse down event for button 'idx'</summary>
			public void SetPending(MouseButton btn, MouseOp op)
			{
				if (PendingOp[btn] == op) return;
				if (PendingOp[btn] != null)
				{
					PendingOp[btn].Disposed -= HandleMouseOpDisposed;
					PendingOp[btn].Dispose();
				}
				PendingOp[btn] = op;
				if (PendingOp[btn] != null)
				{
					// Watch for external disposing
					PendingOp[btn].Disposed -= HandleMouseOpDisposed;
					PendingOp[btn].Disposed += HandleMouseOpDisposed;
				}
			}

			/// <summary>Start/End the next mouse op for button 'idx'</summary>
			public void BeginOp(MouseButton btn)
			{
				Active = PendingOp[btn];
				PendingOp[btn] = null;
			}
			public void EndOp(MouseButton btn)
			{
				Active = null;

				// If the next op starts immediately, begin it now
				if (PendingOp[btn] != null && !PendingOp[btn].StartOnMouseDown)
					BeginOp(btn);
			}

			/// <summary>Handle a mouse op being disposed externally</summary>
			private void HandleMouseOpDisposed(object sender, EventArgs args)
			{
				// This should never be called when 'MouseOps' is the one calling dispose
				// It's only to handle an external reference calling Dispose.
				var op = (MouseOp)sender;
				op.Disposed -= HandleMouseOpDisposed;

				if (m_active == op)
					m_active = null;

				foreach (var key in PendingOp.Keys.ToList())
					if (PendingOp[key] == op)
						PendingOp[key] = null;
			}
		}

		/// <summary>Base class for a mouse operation performed with the mouse 'down -> [drag] -> up' sequence</summary>
		public abstract class MouseOp : IDisposable
		{
			// The general process goes:
			//  - A mouse op is created and set as the pending operation in 'MouseOps'.
			//  - MouseDown on the chart calls 'BeginOp' which moves the pending op to 'Active'.
			//  - Mouse events on the chart are forwarded to the active op.
			//  - MouseUp ends the current Active op, if the pending op should start immediately
			//    then mouse up causes the next op to start (with a faked MouseDown event).
			//  - If at any point a mouse op is cancelled, no further mouse events are forwarded
			//    to the op. When EndOp is called, a notification can be sent by the op to indicate cancelled.

			// Selection data for a mouse button
			public bool m_btn_down;            // True while the corresponding mouse button is down
			public Point m_grab_client;        // The client space location of where the chart was "grabbed" (note: ChartControl, not ChartPanel space)
			public Point m_grab_chart;         // The chart space location of where the chart was "grabbed"
			public HitTestResult m_hit_result; // The hit test result on mouse down
			private bool m_is_click;           // True until the mouse is dragged beyond the click threshold

			public MouseOp(ChartControl chart)
			{
				Chart = chart;
				m_is_click = true;
				StartOnMouseDown = true;
				Cancelled = false;
			}
			public virtual void Dispose()
			{
				Disposed?.Invoke(this, EventArgs.Empty);
				Util.Dispose(ref m_suspended_chart_changed);
			}
			protected IDisposable m_suspended_chart_changed;
			public event EventHandler Disposed;

			/// <summary>The owning chart</summary>
			protected ChartControl Chart { get; }

			/// <summary>True if mouse down starts the op, false if the op should start as soon as possible</summary>
			public bool StartOnMouseDown { get; set; }

			/// <summary>True if the op was aborted</summary>
			public bool Cancelled { get; protected set; }

			/// <summary>True if the distance between 'location' and mouse down should be treated as a click. Once false, then always false</summary>
			public bool IsClick(Point location)
			{
				if (!m_is_click) return false;
				var grab = m_grab_client;
				var diff = location - grab;
				return m_is_click = diff.LengthSquared < Math_.Sqr(Chart.Options.MinDragPixelDistance);
			}

			/// <summary>Called on mouse down</summary>
			public virtual void MouseDown(MouseButtonEventArgs e) { }

			/// <summary>Called on mouse move</summary>
			public virtual void MouseMove(MouseEventArgs e) { }

			/// <summary>Called on mouse up</summary>
			public virtual void MouseUp(MouseButtonEventArgs e) { }

			/// <summary>Called on key down</summary>
			public virtual void OnKeyDown(KeyEventArgs e) { }

			/// <summary>Called on key up</summary>
			public virtual void OnKeyUp(KeyEventArgs e) { }

			/// <summary>Called when the mouse operation is cancelled</summary>
			public virtual void NotifyCancelled() { }
		}

		/// <summary>A mouse operation for dragging selected elements around, area selecting, or left clicking (Left Button)</summary>
		public class MouseOpDefaultLButton : MouseOp
		{
			private HitTestResult.Hit m_hit_selected;
			private IDisposable m_cleanup_selection_graphic;
			private IDisposable m_defer_nav_checkpoint;
			private Element m_dragging_element;
			private EDragState m_drag_state;
			private int m_click_count;
			private EAxis m_hit_axis;

			public MouseOpDefaultLButton(ChartControl chart) : base(chart)
			{}
			public override void Dispose()
			{
				Util.Dispose(ref m_cleanup_selection_graphic);
				base.Dispose();
			}
			public override void MouseDown(MouseButtonEventArgs e)
			{
				var location = e.GetPosition(Chart);
				m_drag_state = EDragState.Start;
				m_click_count = e.ClickCount;

				// See where mouse down occurred
				if (Chart.SceneBounds.Contains(location)) m_hit_axis = EAxis.None;
				if (Chart.XAxisBounds.Contains(location)) m_hit_axis = EAxis.XAxis;
				if (Chart.YAxisBounds.Contains(location)) m_hit_axis = EAxis.YAxis;

				// Look for a selected object that the mouse operation starts on
				m_hit_selected = m_hit_result.Hits.FirstOrDefault(x => x.Element.Selected);

				// Record the drag start positions for selected objects
				foreach (var elem in Chart.Selected)
					elem.DragStartPosition = elem.Position;

				// For 3D scenes, left mouse rotates if mouse down is within the chart bounds
				if (Chart.Options.NavigationMode == ENavMode.Scene3D && m_hit_axis == EAxis.None)
				{
					// Get the point in 'scene' space
					var point_ss = e.GetPosition(Chart.Scene).ToPointF();
					Chart.Scene.Window.MouseNavigate(point_ss, e.ToMouseBtns(Keyboard.Modifiers), View3d.ENavOp.Rotate, true);
				}

				// Prevent events while dragging the elements around
				m_suspended_chart_changed = Chart.SuspendChartChanged(raise_on_resume: true);
				m_defer_nav_checkpoint = Chart.DeferNavCheckpoints();

				// Don't swallow the event
				e.Handled = false;
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var location = e.GetPosition(Chart);

				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(location))
					return;

				// Pass the drag event out to users first
				var delta = Chart.ClientToChart(location) - m_grab_chart;
				var args = new ChartDraggedEventArgs(m_hit_result, delta, m_drag_state);
				m_drag_state = EDragState.Dragging;
				Chart.OnChartDragged(args);

				// See if the selected element handles dragging
				if (!args.Handled && m_hit_selected != null)
				{
					m_hit_selected.Element.HandleDraggedInternal(args);
					if (args.Handled)
						m_dragging_element = m_hit_selected.Element;
				}

				// See if selected element dragging is enabled
				if (!args.Handled && Chart.AllowElementDragging && m_hit_selected != null)
				{
					foreach (var elem in Chart.Selected)
						elem.DragTranslate(args.Delta, args.State);

					args.Handled = true;
				}

				// Otherwise, interpret drag as a navigation
				if (args.Handled)
				{ }
				else if (Chart.Options.NavigationMode == ENavMode.Chart2D)
				{
					if (Chart.DoChartAreaSelect())
					{
						// Otherwise change the selection area
						if (m_cleanup_selection_graphic == null)
						{
							m_cleanup_selection_graphic = Scope.Create(
								() => Chart.Scene.AddObject(Chart.Tools.AreaSelect),
								() => Chart.Scene.RemoveObject(Chart.Tools.AreaSelect));
						}

						// Position the selection graphic
						var selection_area = BRect.FromBounds(m_grab_chart.ToV2(), Chart.ClientToChart(location).ToV2());
						Chart.Tools.AreaSelect.O2P = m4x4.Scale(selection_area.SizeX, selection_area.SizeY, 1f, new v4(selection_area.Centre, Chart.HighestZ, 1));
					}
				}
				else if (Chart.Options.NavigationMode == ENavMode.Scene3D)
				{
					var point_ss = e.GetPosition(Chart.Scene).ToPointF();
					Chart.Scene.Window.MouseNavigate(point_ss, e.ToMouseBtns(Keyboard.Modifiers), View3d.ENavOp.Rotate, false);
				}
				Chart.Scene.Invalidate();

				e.Handled = args.Handled;
			}
			public override void MouseUp(MouseButtonEventArgs e)
			{
				Util.Dispose(ref m_suspended_chart_changed);
				Util.Dispose(ref m_defer_nav_checkpoint);
				var location = e.GetPosition(Chart);

				// If this is a click...
				if (IsClick(location))
				{
					// Pass the click event out to users first
					var args = new ChartClickedEventArgs(m_hit_result, e, m_click_count);
					Chart.OnChartClicked(args);

					// If a selected element was hit on mouse down, see if it handles the click
					if (!args.Handled && m_hit_selected != null)
					{
						m_hit_selected.Element.HandleClickedInternal(args);
					}

					// If no selected element was hit, try hovered elements
					if (!args.Handled && m_hit_result.Hits.Count != 0)
					{
						for (int i = 0; i != m_hit_result.Hits.Count && !args.Handled; ++i)
						{
							if (m_hit_result.Hits[i] == m_hit_selected) continue;
							m_hit_result.Hits[i].Element.HandleClickedInternal(args);
						}
					}

					// If the click is still unhandled, use the click to try to select something (if within the chart)
					if (!args.Handled && m_hit_result.Zone.HasFlag(EZone.Chart))
					{
						var selection_area = new Rect(m_grab_chart, Size_.Zero);
						Chart.SelectElements(selection_area, Keyboard.Modifiers, e.ToMouseBtns());
					}

					e.Handled = args.Handled;
				}
				// Otherwise this is a drag action
				else
				{
					// Commit if dragging hasn't been cancelled
					if (m_drag_state == EDragState.Dragging)
						m_drag_state = EDragState.Commit;

					// Pass the drag event out to users first
					var delta = Chart.ClientToChart(location) - m_grab_chart;
					var args = new ChartDraggedEventArgs(m_hit_result, delta, m_drag_state);
					Chart.OnChartDragged(args);

					// See if the selected element handles dragging
					if (!args.Handled && m_dragging_element != null)
					{
						m_dragging_element.HandleDraggedInternal(args);
						m_dragging_element = null;
						args.Handled = true;
					}

					// See if selected element dragging is enabled
					if (!args.Handled && Chart.AllowElementDragging && m_hit_selected != null)
					{
						foreach (var elem in Chart.Selected)
							elem.DragTranslate(args.Delta, args.State);

						args.Handled = true;
					}

					// Otherwise, interpret drag as a navigation
					if (args.Handled)
					{}
					else if (Chart.Options.NavigationMode == ENavMode.Chart2D)
					{
						// Otherwise create an area selection if the click started within the chart
						if (m_hit_result.Zone.HasFlag(EZone.Chart) && m_cleanup_selection_graphic != null)
						{
							var selection_area = BBox.From(new v4(m_grab_chart.ToV2(), 0, 1f), new v4(Chart.ClientToChart(location).ToV2(), 0f, 1f));
							Chart.OnChartAreaSelect(new ChartAreaSelectEventArgs(selection_area, e.ToMouseBtns()));
						}
					}
					else if (Chart.Options.NavigationMode == ENavMode.Scene3D)
					{
						// For 3D scenes, left mouse rotates
						var point_ss = e.GetPosition(Chart.Scene).ToPointF();
						Chart.Scene.Window.MouseNavigate(point_ss, e.ToMouseBtns(Keyboard.Modifiers), View3d.ENavOp.Rotate, true);
					}

					e.Handled = args.Handled;
				}

				Util.Dispose(ref m_cleanup_selection_graphic);
				Chart.Cursor = Cursors.Arrow;
				Chart.Scene.Invalidate();
			}
			public override void OnKeyDown(KeyEventArgs e)
			{
				base.OnKeyDown(e);
				if (e.Key == Key.Escape)
				{
					Cancelled = true;
					m_drag_state = EDragState.Cancel;

					// Abort dragging
					if (m_dragging_element != null)
					{
						m_dragging_element.HandleDraggedInternal(new ChartDraggedEventArgs(m_hit_result, default, m_drag_state));
						m_dragging_element = null;
					}

					// Remove the selection graphics
					Util.Dispose(ref m_cleanup_selection_graphic);

					// Refresh
					Chart.Scene.Invalidate();
				}
			}
		}

		/// <summary>A mouse operation for zooming (Middle Button)</summary>
		public class MouseOpDefaultMButton : MouseOp
		{
			//private HintBalloon m_tape_measure_balloon;
			private bool m_tape_measure_graphic_added;
			private IDisposable m_defer_nav_checkpoint;

			public MouseOpDefaultMButton(ChartControl chart) : base(chart)
			{
				//m_tape_measure_balloon = new HintBalloon
				//{
				//	AutoSizeMode = AutoSizeMode.GrowOnly,
				//	Font = new Font(FontFamily.GenericMonospace, 8f),
				//	Owner = chart.TopLevelControl as Form,
				//};
				m_tape_measure_graphic_added = false;
			}
			public override void MouseDown(MouseButtonEventArgs e)
			{
				m_defer_nav_checkpoint = Chart.DeferNavCheckpoints();
				var location = e.GetPosition(Chart);

				// If mouse down occurred within the chart, record it
				if (Chart.SceneBounds.Contains(location))
				{
					Chart.Cursor = Cursors.Cross;
				}
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var location = e.GetPosition(Chart);

				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(location) && !m_tape_measure_graphic_added)
					return;

				// Position the tape measure graphic
				var pt0 = Chart.Camera.O2W * Chart.ChartToCamera(m_grab_chart);
				var pt1 = Chart.Camera.O2W * Chart.ChartToCamera(Chart.ClientToChart(location));
				var delta = pt1 - pt0;
				Chart.Tools.TapeMeasure.O2P = Math_.TxfmFromDir(EAxisId.PosZ, delta, pt0) * m4x4.Scale(1f, 1f, delta.Length, v4.Origin);
				//m_tape_measure_balloon.Location = Chart.PointToScreen(e.Location);
				//m_tape_measure_balloon.Text =
				//	$"dX:  {delta.x}\r\n" +
				//	$"dY:  {delta.y}\r\n" +
				//	$"dZ:  {delta.z}\r\n" +
				//	$"Len: {delta.Length}\r\n";

				// Show the tape measure graphic (after the text has been initialised)
				if (!m_tape_measure_graphic_added)
				{
					Chart.Scene.AddObject(Chart.Tools.TapeMeasure);
					m_tape_measure_graphic_added = true;
					//m_tape_measure_balloon.Visible = true;
				}

				Chart.Scene.Invalidate();
			}
			public override void MouseUp(MouseButtonEventArgs e)
			{
				Util.Dispose(ref m_suspended_chart_changed);
				Util.Dispose(ref m_defer_nav_checkpoint);
				var location = e.GetPosition(Chart);

				// If this is a single click...
				if (IsClick(location))
				{
					// Pass the click event out to users first
					var args = new ChartClickedEventArgs(m_hit_result, e);
					Chart.OnChartClicked(args);

					if (!args.Handled)
					{
						if (m_hit_result.Zone.HasFlag(EZone.Chart))
						{ }
						else if (m_hit_result.Zone.HasFlag(EZone.XAxis))
						{ }
						else if (m_hit_result.Zone.HasFlag(EZone.YAxis))
						{ }
					}
				}
				// Otherwise this is a drag action
				else
				{
				}

				// Remove the tape measure graphic
				if (m_tape_measure_graphic_added)
				{
					Chart.Scene.RemoveObject(Chart.Tools.TapeMeasure);
					//m_tape_measure_balloon.Visible = false;
				}

				Chart.Cursor = Cursors.Arrow;
				Chart.Scene.Invalidate();
			}
		}

		/// <summary>A mouse operation for dragging the chart around or right clicking (Right Button)</summary>
		public class MouseOpDefaultRButton : MouseOp
		{
			private EAxis m_drag_axis_allow; // The allowed motion based on where the chart was grabbed
			private IDisposable m_defer_nav_checkpoint;

			public MouseOpDefaultRButton(ChartControl chart)
				: base(chart)
			{ }
			public override void MouseDown(MouseButtonEventArgs e)
			{
				var location = e.GetPosition(Chart);

				if (Chart.SceneBounds.Contains(location)) m_drag_axis_allow = EAxis.Both;
				if (Chart.XAxisBounds.Contains(location)) m_drag_axis_allow = EAxis.XAxis;
				if (Chart.YAxisBounds.Contains(location)) m_drag_axis_allow = EAxis.YAxis;
				if (!Chart.XAxis.AllowScroll) m_drag_axis_allow &= ~EAxis.XAxis;
				if (!Chart.YAxis.AllowScroll) m_drag_axis_allow &= ~EAxis.YAxis;

				// Right mouse translates for 2D and 3D scene
				var point_ss = e.GetPosition(Chart.Scene).ToPointF();
				Chart.Scene.Window.MouseNavigate(point_ss, e.ToMouseBtns(Keyboard.Modifiers), View3d.ENavOp.Translate, true);

				m_defer_nav_checkpoint = Chart.DeferNavCheckpoints();
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var location = e.GetPosition(Chart);

				// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
				if (IsClick(location))
					return;

				// Change the cursor once dragging
				Chart.Cursor = Cursors.SizeAll;

				// Limit the drag direction
				var drop_loc = Gui_.MapPoint(Chart, Chart.Scene, location);
				var grab_loc = Gui_.MapPoint(Chart, Chart.Scene, m_grab_client);
				if (!m_drag_axis_allow.HasFlag(EAxis.XAxis)) drop_loc.X = grab_loc.X;
				if (!m_drag_axis_allow.HasFlag(EAxis.YAxis)) drop_loc.Y = grab_loc.Y;

				Chart.Scene.Window.MouseNavigate(drop_loc.ToPointF(), e.ToMouseBtns(Keyboard.Modifiers), View3d.ENavOp.Translate, false);
				Chart.SetRangeFromCamera();
				Chart.Scene.Invalidate();
			}
			public override void MouseUp(MouseButtonEventArgs e)
			{
				Chart.Cursor = Cursors.Arrow;
				Util.Dispose(ref m_defer_nav_checkpoint);
				var location = e.GetPosition(Chart);

				// If we haven't dragged, treat it as a click instead
				if (IsClick(location))
				{
					var args = new ChartClickedEventArgs(m_hit_result, e);
					Chart.OnChartClicked(args);
					Chart.Scene.Invalidate();
				}
				else
				{
					// Limit the drag direction
					var drop_loc = Gui_.MapPoint(Chart, Chart.Scene, location);
					var grab_loc = Gui_.MapPoint(Chart, Chart.Scene, m_grab_client);
					if (!m_drag_axis_allow.HasFlag(EAxis.XAxis)) drop_loc.X = grab_loc.X;
					if (!m_drag_axis_allow.HasFlag(EAxis.YAxis)) drop_loc.Y = grab_loc.Y;

					Chart.Scene.Window.MouseNavigate(drop_loc.ToPointF(), e.ToMouseBtns(Keyboard.Modifiers), View3d.ENavOp.None, true);
					Chart.SetRangeFromCamera();
					Chart.Scene.Invalidate();
					e.Handled = true;
				}
			}
		}
	}
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Event args for the chart changed event</summary>
		public class ChartChangedEventArgs : EventArgs
		{
			// Note: many events are available by attaching to the Elements binding list
			public ChartChangedEventArgs(EChangeType ty)
			{
				ChgType = ty;
				Cancel = false;
			}

			/// <summary>The type of change that occurred</summary>
			public EChangeType ChgType { get; private set; }

			/// <summary>A cancel property for "about to change" events</summary>
			public bool Cancel { get; set; }
		}

		/// <summary>Event args for the ChartMoved event</summary>
		public class ChartMovedEventArgs : EventArgs
		{
			public ChartMovedEventArgs(EMoveType move_type)
			{
				MoveType = move_type;
			}

			/// <summary>How the chart was moved</summary>
			public EMoveType MoveType { get; internal set; }
		}

		/// <summary>Event args for the ChartClicked event</summary>
		public class ChartClickedEventArgs : EventArgs
		{
			private readonly MouseButtonEventArgs m_mouse_event;
			public ChartClickedEventArgs(HitTestResult hits, MouseButtonEventArgs mouse_event)
			{
				m_mouse_event = mouse_event;
				HitResult = hits;
				Handled = false;
			}

			/// <summary>Results of a hit test performed at the click location</summary>
			public HitTestResult HitResult { get; }

			/// <summary>The button that was clicked</summary>
			public MouseButton ChangedButton => m_mouse_event.ChangedButton;

			/// <summary>The state of the clicked button</summary>
			public MouseButtonState ButtonState => m_mouse_event.ButtonState;

			/// <summary>The number of times the changed button was clicked.</summary>
			public int ClickCount => m_mouse_event.ClickCount;

			/// <summary>the current state of the left mouse button.</summary>
			public MouseButtonState LeftButton => m_mouse_event.LeftButton;

			/// <summary>The current state of the right mouse button.</summary>
			public MouseButtonState RightButton => m_mouse_event.RightButton;

			/// <summary>The current state of the middle mouse button.</summary>
			public MouseButtonState MiddleButton => m_mouse_event.MiddleButton;

			/// <summary>The current state of the first extended mouse button.</summary>
			public MouseButtonState XButton1 => m_mouse_event.XButton1;

			/// <summary>The current state of the second extended mouse button.</summary>
			public MouseButtonState XButton2 => m_mouse_event.XButton2;

			/// <summary>The position of the mouse pointer relative to the specified element.</summary>
			public Point GetPosition(IInputElement relativeTo) => m_mouse_event.GetPosition(relativeTo);

			/// <summary>Set to true to suppress default chart click behaviour</summary>
			public bool Handled { get; set; }
		}

		/// <summary>Event args for area selection</summary>
		public class ChartAreaSelectEventArgs : EventArgs
		{
			public ChartAreaSelectEventArgs(BBox selection_area)
			{
				SelectionArea = selection_area;
				Handled = false;
			}

			/// <summary>The area (actually volume if you include Z) of the selection</summary>
			public BBox SelectionArea { get; }

			/// <summary>Set to true to suppress default chart click behaviour</summary>
			public bool Handled { get; set; }
		}

		/// <summary>Event args for FindingDefaultRange</summary>
		public class FindingDefaultRangeEventArgs : EventArgs
		{
			public FindingDefaultRangeEventArgs(EAxis axis, RangeF xrange, RangeF yrange)
			{
				Axis = axis;
				XRange = xrange;
				YRange = yrange;
			}

			/// <summary>The axis being ranged</summary>
			public EAxis Axis { get; }

			/// <summary>The XAxis range calculated from known chart elements data</summary>
			public RangeF XRange { get; set; }

			/// <summary>The YAxis range calculated from known chart elements data</summary>
			public RangeF YRange { get; set; }
		}

		/// <summary>Customise context menu event args</summary>
		public class AddUserMenuOptionsEventArgs : EventArgs
		{
			public AddUserMenuOptionsEventArgs(ContextMenu menu, HitTestResult hit_result)
			{
				Menu = menu;
				HitResult = hit_result;
			}

			/// <summary>The zone on the chart where the context menu is to be displayed</summary>
			public EZone Zone => HitResult.Zone;

			/// <summary>The menu to add menu items to</summary>
			public ContextMenu Menu { get; }

			/// <summary>The hit test result at the click location</summary>
			public HitTestResult HitResult { get; }
		}

		/// <summary>Event args for the post-paint add overlays call</summary>
		public class AddOverlaysOnPaintEventArgs : EventArgs
		{
			public AddOverlaysOnPaintEventArgs(ChartControl chart, DrawingContext gfx, m4x4 chart_to_client, EZone zone)
			{
				Chart = chart;
				Gfx = gfx;
				ChartToClient = chart_to_client;
				Zone = zone;
			}

			/// <summary>The device context to draw to</summary>
			public DrawingContext Gfx { get; }

			/// <summary>The chart being drawn on</summary>
			public ChartControl Chart { get; }

			/// <summary>Transform from Chart space to client space</summary>
			public m4x4 ChartToClient { get; }

			/// <summary>The part of the chart being painted</summary>
			public EZone Zone { get; }
		}

		/// <summary>Event args for the auto range event</summary>
		public class AutoRangeEventArgs : EventArgs
		{
			public AutoRangeEventArgs(View3d.ESceneBounds who, EAxis axes)
			{
				Who = who;
				Axes = axes;
				ViewBBox = BBox.Reset;
				Handled = false;
			}

			/// <summary>The scene elements to be auto ranged</summary>
			public View3d.ESceneBounds Who { get; }

			/// <summary>The axes to auto range on</summary>
			public EAxis Axes { get; }

			/// <summary>The bounding box of the range to view</summary>
			public BBox ViewBBox { get; set; }

			/// <summary>Set to true to indicate that the provided range should be used</summary>
			public bool Handled { get; set; }
		}
	}
}
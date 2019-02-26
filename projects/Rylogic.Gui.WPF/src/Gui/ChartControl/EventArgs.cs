using System;
using System.Drawing;
using System.Windows.Controls;
using System.Windows.Input;
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

		/// <summary>Event args for the ChartRendering event</summary>
		public class ChartRenderingEventArgs : EventArgs
		{
			public ChartRenderingEventArgs(ChartControl chart, View3d.Window window)
			{
				Chart = chart;
				Window = window;
			}

			/// <summary>The chart that is rendering</summary>
			public ChartControl Chart { get; private set; }

			/// <summary>The View3d window to add/remove objects to/from</summary>
			public View3d.Window Window { get; private set; }

			/// <summary>Add a view3d object to the chart scene</summary>
			public void AddToScene(View3d.Object obj)
			{
				Window.AddObject(obj);
			}

			/// <summary>Remove a single object from the chart scene</summary>
			public void RemoveFromScene(View3d.Object obj)
			{
				Window.RemoveObject(obj);
			}
		}

		/// <summary>Event args for the ChartClicked event</summary>
		public class ChartClickedEventArgs : MouseButtonEventArgs
		{
			public ChartClickedEventArgs(ChartControl.HitTestResult hits, MouseButtonEventArgs e)
				: base(e.MouseDevice, e.Timestamp, e.ChangedButton, e.StylusDevice)
			{
				HitResult = hits;
				Handled = false;
			}

			/// <summary>Results of a hit test performed at the click location</summary>
			public ChartControl.HitTestResult HitResult { get; private set; }

			///// <summary>Set to true to suppress default chart click behaviour</summary>
			//public bool Handled { get; set; }
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
			public BBox SelectionArea { get; private set; }

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
			public EAxis Axis { get; private set; }

			/// <summary>The XAxis range calculated from known chart elements data</summary>
			public RangeF XRange { get; set; }

			/// <summary>The YAxis range calculated from known chart elements data</summary>
			public RangeF YRange { get; set; }
		}

		/// <summary>Customise context menu event args</summary>
		public class AddUserMenuOptionsEventArgs : EventArgs
		{
			public AddUserMenuOptionsEventArgs(EType type, ContextMenu menu, ChartControl.HitTestResult hit_result)
			{
				Type = type;
				Menu = menu;
				HitResult = hit_result;
			}

			/// <summary>The context menu type</summary>
			public EType Type { get; private set; }
			public enum EType
			{
				Chart,
				XAxis,
				YAxis,
			}

			/// <summary>The menu to add menu items to</summary>
			public ContextMenu Menu { get; private set; }

			/// <summary>The hit test result at the click location</summary>
			public ChartControl.HitTestResult HitResult { get; private set; }
		}

		/// <summary>Event args for the post-paint add overlays call</summary>
		public class AddOverlaysOnPaintEventArgs : EventArgs
		{
			public AddOverlaysOnPaintEventArgs(Graphics gfx, ChartDims dims, m4x4 chart_to_client, EZone zone)
			{
				Gfx = gfx;
				Dims = dims;
				ChartToClient = chart_to_client;
				Zone = zone;
			}

			/// <summary>The device context to draw to</summary>
			public Graphics Gfx { get; private set; }

			/// <summary>Layout info about the chart</summary>
			public ChartDims Dims { get; private set; }

			/// <summary>The chart being drawn on</summary>
			public ChartControl Chart { get { return Dims.Chart; } }

			/// <summary>Transform from Chart space to client space</summary>
			public m4x4 ChartToClient { get; private set; }

			/// <summary>The part of the chart being painted</summary>
			public EZone Zone { get; private set; }
		}

		/// <summary>Event args for the auto range event</summary>
		public class AutoRangeEventArgs : EventArgs
		{
			public AutoRangeEventArgs(View3d.ESceneBounds who)
			{
				Who = who;
				ViewBBox = BBox.Reset;
				Handled = false;
			}

			/// <summary>The scene elements to be auto ranged</summary>
			public View3d.ESceneBounds Who { get; private set; }

			/// <summary>The bounding box of the range to view</summary>
			public BBox ViewBBox { get; set; }

			/// <summary>Set to true to indicate that the provided range should be used</summary>
			public bool Handled { get; set; }
		}
	}
}
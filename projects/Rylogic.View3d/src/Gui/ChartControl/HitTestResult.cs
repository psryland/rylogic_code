using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Results collection for a hit test</summary>
		public class HitTestResult
		{
			public HitTestResult(EZone zone, Point client_point, Point chart_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, IEnumerable<Hit> hits, View3d.Camera cam)
			{
				Zone = zone;
				ClientPoint = client_point;
				ChartPoint = chart_point;
				Hits = hits.ToList();
				ModifierKeys = modifier_keys;
				MouseBtns = mouse_btns;
				Camera = cam;
			}

			/// <summary>The zone on the chart that was hit</summary>
			public EZone Zone { get; }

			/// <summary>The client space location of where the hit test was performed</summary>
			public Point ClientPoint { get; }

			/// <summary>The chart space location of where the hit test was performed</summary>
			public Point ChartPoint { get; }

			/// <summary>The collection of hit objects</summary>
			public List<Hit> Hits { get; }

			/// <summary>Keys held down during the hit test</summary>
			public ModifierKeys ModifierKeys { get; }

			/// <summary>The mouse button state during the hit test</summary>
			public EMouseBtns MouseBtns { get; }

			/// <summary>The camera position when the hit test was performed (needed for chart to screen space conversion)</summary>
			public View3d.Camera Camera { get; }

			/// <summary></summary>
			public class Hit
			{
				public Hit(Element elem, Point elem_point, object? context)
				{
					Element = elem;
					Point = elem_point;
					Location = elem.Position;
					Context = context;
				}

				/// <summary>The element that was hit</summary>
				public Element Element { get; }

				/// <summary>Where on the element it was hit (in element space)</summary>
				public Point Point { get; }

				/// <summary>The element's chart location at the time it was hit</summary>
				public m4x4 Location { get; }

				/// <summary>Optional context information collected during the hit test</summary>
				public object? Context { get; }

				/// <summary></summary>
				public override string ToString() => Element.ToString() ?? string.Empty;
			}
		}
	}
}

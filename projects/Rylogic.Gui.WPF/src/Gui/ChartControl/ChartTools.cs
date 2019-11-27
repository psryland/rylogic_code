using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>A collection of graphics used by the chart itself</summary>
		public sealed class ChartTools : IDisposable
		{
			public static readonly Guid Id = new Guid("62D495BB-36D1-4B52-A067-1B7DB4011831");

			internal ChartTools(ChartControl chart)
			{
				Chart = chart;
				Resizer = CreateResizeGrabber();
			}
			public void Dispose()
			{
				Chart = null!;
				Resizer = null!;
			}

			/// <summary>The owner of these tools</summary>
			private ChartControl Chart
			{
				get => m_chart;
				set
				{
					if (m_chart == value) return;
					m_chart = value;
				}
			}
			private ChartControl m_chart = null!;

			/// <summary>Graphics for the resizing grab zones</summary>
			public ResizeGrabber[] Resizer
			{
				get => m_resizer;
				private set
				{
					if (m_resizer == value) return;
					Util.DisposeAll(m_resizer!);
					m_resizer = value;
				}
			}
			private ResizeGrabber[] m_resizer = null!;
			private ResizeGrabber[] CreateResizeGrabber()
			{
				return Array_.New(8, i => new ResizeGrabber(i));
			}
			public class ResizeGrabber : View3d.Object
			{
				public ResizeGrabber(int corner)
					: base($"*Box resizer_{corner} {{5}}", false, Id, null)
				{
					FlagsSet(View3d.EFlags.SceneBoundsExclude, true);
					switch (corner)
					{
					default:
						throw new Exception("Invalid corner");
					case 0:
						Cursor = Cursors.SizeNESW;
						Direction = Math_.Normalise(new v2(-1, -1));
						Update = (b, z) => O2P = m4x4.Translation(b.Lower.x, b.Lower.y, z);
						break;
					case 1:
						Cursor = Cursors.SizeNESW;
						Direction = Math_.Normalise(new v2(+1, +1));
						Update = (b, z) => O2P = m4x4.Translation(b.Upper.x, b.Upper.y, z);
						break;
					case 2:
						Cursor = Cursors.SizeNWSE;
						Direction = Math_.Normalise(new v2(-1, +1));
						Update = (b, z) => O2P = m4x4.Translation(b.Lower.x, b.Upper.y, z);
						break;
					case 3:
						Cursor = Cursors.SizeNWSE;
						Direction = Math_.Normalise(new v2(+1, -1));
						Update = (b, z) => O2P = m4x4.Translation(b.Upper.x, b.Lower.y, z);
						break;
					case 4:
						Direction = new v2(-1, 0);
						Cursor = Cursors.SizeWE;
						Update = (b, z) => O2P = m4x4.Translation(b.Lower.x, b.Centre.y, z);
						break;
					case 5:
						Direction = new v2(+1, 0);
						Cursor = Cursors.SizeWE;
						Update = (b, z) => O2P = m4x4.Translation(b.Upper.x, b.Centre.y, z);
						break;
					case 6:
						Direction = new v2(0, -1);
						Cursor = Cursors.SizeNS;
						Update = (b, z) => O2P = m4x4.Translation(b.Centre.x, b.Lower.y, z);
						break;
					case 7:
						Direction = new v2(0, +1);
						Cursor = Cursors.SizeNS;
						Update = (b, z) => O2P = m4x4.Translation(b.Centre.x, b.Upper.y, z);
						break;
					}
				}

				/// <summary>The direction that this grabber can resize in </summary>
				public v2 Direction { get; private set; }

				/// <summary>The cursor to display when this grabber is used</summary>
				public Cursor Cursor { get; set; }

				/// <summary>Updates the position of the grabber</summary>
				public Action<BRect, float> Update;
			}
		}
	}
}

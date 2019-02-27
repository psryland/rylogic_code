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
		public class ChartTools : IDisposable
		{
			public static readonly Guid Id = new Guid("62D495BB-36D1-4B52-A067-1B7DB4011831");

			internal ChartTools(ChartControl chart, OptionsData opts)
			{
				Chart = chart;
				//if (owner.IsInDesignMode())
				//	return;

				Options = opts;
				AreaSelect = CreateAreaSelect();
				Resizer = Array_.New(8, i => new ResizeGrabber(i));
				CrossHairH = CreateCrossHair(true);
				CrossHairV = CreateCrossHair(false);
				TapeMeasure = CreateTapeMeasure();
			}
			public void Dispose()
			{
				Options = null;
				AreaSelect = null;
				Resizer = null;
				CrossHairH = null;
				CrossHairV = null;
				TapeMeasure = null;
			}

			/// <summary>The owner of these tools</summary>
			private ChartControl Chart { get; }

			/// <summary>Chart options</summary>
			private OptionsData Options
			{
				[DebuggerStepThrough]
				get { return m_options; }
				set
				{
					if (m_options == value) return;
					if (m_options != null) m_options.PropertyChanged -= HandleOptionsChanged;
					m_options = value;
					if (m_options != null) m_options.PropertyChanged += HandleOptionsChanged;
					
					// Handlers
					void HandleOptionsChanged(object sender, PropertyChangedEventArgs e)
					{
						switch (e.PropertyName)
						{
						case nameof(OptionsData.SelectionColour):
							{
								AreaSelect.ColourSet(Options.SelectionColour);
								break;
							}
						//case nameof(OptionsData.ChartBkColour):
						//	{
						//		CrossHairH = CreateCrossHair(true);
						//		CrossHairV = CreateCrossHair(false);
						//	}
						}
					}
				}
			}
			private OptionsData m_options;

			/// <summary>Graphic for area selection</summary>
			public View3d.Object AreaSelect
			{
				get { return m_area_select; }
				private set
				{
					if (m_area_select == value) return;
					Util.Dispose(ref m_area_select);
					m_area_select = value;
				}
			}
			private View3d.Object m_area_select;
			private View3d.Object CreateAreaSelect()
			{
				var ldr = Ldr.Rect("selection", Options.SelectionColour, AxisId.PosZ, 1f, 1f, true, pos: v4.Origin);
				var obj = new View3d.Object(ldr, false, Id, null);
				obj.FlagsSet(View3d.EFlags.SceneBoundsExclude, true);
				return obj;
			}

			/// <summary>Graphics for the resizing grab zones</summary>
			public ResizeGrabber[] Resizer
			{
				get { return m_resizer; }
				private set
				{
					if (m_resizer == value) return;
					Util.DisposeAll(m_resizer);
					m_resizer = value;
				}
			}
			private ResizeGrabber[] m_resizer;
			public class ResizeGrabber : View3d.Object
			{
				public ResizeGrabber(int corner) : base($"*Box resizer_{corner} {{5}}", false, Id, null)
				{
					FlagsSet(View3d.EFlags.SceneBoundsExclude, true);
					switch (corner)
					{
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

			/// <summary>A vertical and horizontal line</summary>
			public View3d.Object CrossHairH
			{
				get { return m_cross_hair_h; }
				private set
				{
					if (m_cross_hair_h == value) return;
					Util.Dispose(ref m_cross_hair_h);
					m_cross_hair_h = value;
				}
			}
			public View3d.Object CrossHairV
			{
				get { return m_cross_hair_v; }
				private set
				{
					if (m_cross_hair_v == value) return;
					Util.Dispose(ref m_cross_hair_v);
					m_cross_hair_v = value;
				}
			}
			private View3d.Object m_cross_hair_h;
			private View3d.Object m_cross_hair_v;
			private View3d.Object CreateCrossHair(bool horiz)
			{
				var col = Chart.Scene.BackgroundColor.Intensity > 0.5f ? 0xFFFFFFFF : 0xFF000000;
				var str = horiz
					? Ldr.Line("chart_cross_hair_h", col, new v4(-0.5f, 0, 0, 1f), new v4(+0.5f, 0, 0, 1f))
					: Ldr.Line("chart_cross_hair_v", col, new v4(0, -0.5f, 0, 1f), new v4(0, +0.5f, 0, 1f));
				var obj = new View3d.Object(str, false, Id, null);
				obj.FlagsSet(View3d.EFlags.SceneBoundsExclude, true);
				return obj;
			}

			/// <summary>A line for measuring distances</summary>
			public View3d.Object TapeMeasure
			{
				get { return m_tape_measure; }
				private set
				{
					if (m_tape_measure == value) return;
					Util.Dispose(ref m_tape_measure);
					m_tape_measure = value;
				}
			}
			private View3d.Object m_tape_measure;
			private View3d.Object CreateTapeMeasure()
			{
				var col = Chart.Scene.BackgroundColor.Intensity > 0.5f ? 0xFFFFFFFF : 0xFF000000;
				var str = Ldr.Line("tape_measure", col, new v4(0, 0, 0, 1f), new v4(0, 0, 1f, 1f));
				var obj = new View3d.Object(str, false, Id, null);
				obj.FlagsSet(View3d.EFlags.SceneBoundsExclude, true);
				return obj;
			}
		}
	}
}

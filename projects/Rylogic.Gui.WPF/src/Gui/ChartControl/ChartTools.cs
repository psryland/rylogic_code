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
				AreaSelect = CreateAreaSelect();
				Resizer = CreateResizeGrabber();
				CrossHair = CreateCrossHair();
				TapeMeasure = CreateTapeMeasure();
			}
			public void Dispose()
			{
				Chart = null;
				AreaSelect = null;
				Resizer = null;
				CrossHair = null;
				TapeMeasure = null;
			}

			/// <summary>The owner of these tools</summary>
			private ChartControl Chart
			{
				get { return m_chart; }
				set
				{
					if (m_chart == value) return;
					if (m_chart != null)
					{
						m_chart.Options.PropertyChanged -= HandleOptionsChanged;
					}
					m_chart = value;
					if (m_chart != null)
					{
						m_chart.Options.PropertyChanged += HandleOptionsChanged;
					}

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
						case nameof(ChartDetail.ChartPanel.BackgroundColor):
							{
								CrossHair = CreateCrossHair();
								break;
							}
						}
					}
				}
			}
			private ChartControl m_chart;

			/// <summary>Chart options</summary>
			private OptionsData Options => Chart?.Options ?? new OptionsData();

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
				var ldr = Ldr.Rect("selection", Options.SelectionColour, EAxisId.PosZ, 1f, 1f, true, pos: v4.Origin);
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
			private ResizeGrabber[] CreateResizeGrabber()
			{
				return Array_.New(8, i => new ResizeGrabber(i));
			}
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
			public View3d.Object CrossHair
			{
				get { return m_cross_hair; }
				private set
				{
					if (m_cross_hair == value) return;
					Util.Dispose(ref m_cross_hair);
					m_cross_hair = value;
				}
			}
			private View3d.Object m_cross_hair;
			private View3d.Object CreateCrossHair()
			{
				// Use Alpha != 0xFF so that the model is added to the alpha group and drawn lasted
				var col = Chart.Scene.BackgroundColor.ToColour32().Intensity < 0.5f ? 0xFeFFFFFF : 0xFe000000;

				var ldr = new LdrBuilder();
				using (ldr.Group("cross_hair", col))
				{
					ldr.Line("h", col, new v4(-0.5f, 0, 0, 1f), new v4(+0.5f, 0, 0, 1f));
					ldr.Line("v", col, new v4(0, -0.5f, 0, 1f), new v4(0, +0.5f, 0, 1f));
				}

				var obj = new View3d.Object(ldr.ToString(), false, Id, null);
				obj.FlagsSet(View3d.EFlags.SceneBoundsExclude | View3d.EFlags.NoZTest | View3d.EFlags.NoZWrite, true);
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
				var col = Chart.Scene.BackgroundColor.ToColour32().Intensity > 0.5f ? 0xFFFFFFFF : 0xFF000000;
				var str = Ldr.Line("tape_measure", col, new v4(0, 0, 0, 1f), new v4(0, 0, 1f, 1f));
				var obj = new View3d.Object(str, false, Id, null);
				obj.FlagsSet(View3d.EFlags.SceneBoundsExclude, true);
				return obj;
			}
		}
	}
}

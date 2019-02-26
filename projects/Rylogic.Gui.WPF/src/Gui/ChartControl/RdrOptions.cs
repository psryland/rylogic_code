using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Xml.Linq;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;
using Matrix = System.Drawing.Drawing2D.Matrix;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		[TypeConverter(typeof(TyConv))]
		public class RdrOptions : INotifyPropertyChanged
		{
			private class TyConv : GenericTypeConverter<RdrOptions> { }

			public RdrOptions()
			{
				NavigationMode = ENavMode.Chart2D;
				LockAspect = null;
				BkColour = System.Drawing.SystemColors.Control;
				ChartBkColour = Colour32.White;
				TitleColour = Colour32.Black;
				TitleFont = new Font("tahoma", 12, System.Drawing.FontStyle.Bold);
				TitleTransform = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
				Margin = new Thickness(3);
				NoteFont = new Font("tahoma", 8, System.Drawing.FontStyle.Regular);
				SelectionColour = Colour32.DarkGray.Alpha(0x80);
				ShowGridLines = true;
				GridZOffset = 0.001f;
				ShowAxes = true;
				AntiAliasing = true;
				FillMode = View3d.EFillMode.Solid;
				CullMode = View3d.ECullMode.Back;
				Orthographic = false;
				MinSelectionDistance = 10f;
				MinDragPixelDistance = 5f;
				PerpendicularZTranslation = false;
				ResetForward = -v4.ZAxis;
				ResetUp = +v4.YAxis;
				XAxis = new Axis();
				YAxis = new Axis();
				// Don't forget to add new members to the other constructors!
			}
			public RdrOptions(RdrOptions rhs)
			{
				NavigationMode = rhs.NavigationMode;
				LockAspect = rhs.LockAspect;
				BkColour = rhs.BkColour;
				ChartBkColour = rhs.ChartBkColour;
				TitleColour = rhs.TitleColour;
				TitleFont = (Font)rhs.TitleFont.Clone();
				TitleTransform = rhs.TitleTransform;
				Margin = rhs.Margin;
				NoteFont = (Font)rhs.NoteFont.Clone();
				SelectionColour = rhs.SelectionColour;
				ShowGridLines = rhs.ShowGridLines;
				GridZOffset = rhs.GridZOffset;
				ShowAxes = rhs.ShowAxes;
				AntiAliasing = rhs.AntiAliasing;
				FillMode = rhs.FillMode;
				CullMode = rhs.CullMode;
				Orthographic = rhs.Orthographic;
				MinSelectionDistance = rhs.MinSelectionDistance;
				MinDragPixelDistance = rhs.MinDragPixelDistance;
				PerpendicularZTranslation = rhs.PerpendicularZTranslation;
				ResetForward = rhs.ResetForward;
				ResetUp = rhs.ResetUp;
				XAxis = new Axis(rhs.XAxis);
				YAxis = new Axis(rhs.YAxis);
			}
			public RdrOptions(XElement node) : this()
			{
				NavigationMode = node.Element(nameof(NavigationMode)).As(NavigationMode);
				LockAspect = node.Element(nameof(LockAspect)).As(LockAspect);
				BkColour = node.Element(nameof(BkColour)).As(BkColour);
				ChartBkColour = node.Element(nameof(ChartBkColour)).As(ChartBkColour);
				TitleColour = node.Element(nameof(TitleColour)).As(TitleColour);
				TitleTransform = node.Element(nameof(TitleTransform)).As(TitleTransform);
				Margin = node.Element(nameof(Margin)).As(Margin);
				TitleFont = node.Element(nameof(TitleFont)).As(TitleFont);
				NoteFont = node.Element(nameof(NoteFont)).As(NoteFont);
				SelectionColour = node.Element(nameof(SelectionColour)).As(SelectionColour);
				ShowAxes = node.Element(nameof(ShowAxes)).As(ShowAxes);
				ShowGridLines = node.Element(nameof(ShowGridLines)).As(ShowGridLines);
				GridZOffset = node.Element(nameof(GridZOffset)).As(GridZOffset);
				AntiAliasing = node.Element(nameof(AntiAliasing)).As(AntiAliasing);
				FillMode = node.Element(nameof(FillMode)).As(FillMode);
				CullMode = node.Element(nameof(CullMode)).As(CullMode);
				Orthographic = node.Element(nameof(Orthographic)).As(Orthographic);
				MinSelectionDistance = node.Element(nameof(MinSelectionDistance)).As(MinSelectionDistance);
				MinDragPixelDistance = node.Element(nameof(MinDragPixelDistance)).As(MinDragPixelDistance);
				PerpendicularZTranslation = node.Element(nameof(PerpendicularZTranslation)).As(PerpendicularZTranslation);
				ResetForward = node.Element(nameof(ResetForward)).As(ResetForward);
				ResetUp = node.Element(nameof(ResetUp)).As(ResetUp);
				XAxis = node.Element(nameof(XAxis)).As(XAxis);
				YAxis = node.Element(nameof(YAxis)).As(YAxis);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(NavigationMode), NavigationMode, false);
				node.Add2(nameof(LockAspect), LockAspect, false);
				node.Add2(nameof(BkColour), BkColour, false);
				node.Add2(nameof(ChartBkColour), ChartBkColour, false);
				node.Add2(nameof(TitleColour), TitleColour, false);
				node.Add2(nameof(TitleTransform), TitleTransform, false);
				node.Add2(nameof(Margin), Margin, false);
				node.Add2(nameof(TitleFont), TitleFont, false);
				node.Add2(nameof(NoteFont), NoteFont, false);
				node.Add2(nameof(SelectionColour), SelectionColour, false);
				node.Add2(nameof(ShowGridLines), ShowGridLines, false);
				node.Add2(nameof(GridZOffset), GridZOffset, false);
				node.Add2(nameof(ShowAxes), ShowAxes, false);
				node.Add2(nameof(AntiAliasing), AntiAliasing, false);
				node.Add2(nameof(FillMode), FillMode, false);
				node.Add2(nameof(CullMode), CullMode, false);
				node.Add2(nameof(Orthographic), Orthographic, false);
				node.Add2(nameof(MinSelectionDistance), MinSelectionDistance, false);
				node.Add2(nameof(MinDragPixelDistance), MinDragPixelDistance, false);
				node.Add2(nameof(PerpendicularZTranslation), PerpendicularZTranslation, false);
				node.Add2(nameof(ResetForward), ResetForward, false);
				node.Add2(nameof(ResetUp), ResetUp, false);
				node.Add2(nameof(XAxis), XAxis, false);
				node.Add2(nameof(YAxis), YAxis, false);
				return node;
			}
			public override string ToString()
			{
				return "Rendering Options";
			}

			/// <summary>Property changed</summary>
			public event PropertyChangedEventHandler PropertyChanged;
			private void SetProp<T>(ref T prop, T value, string name)
			{
				if (Equals(prop, value)) return;
				prop = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}

			/// <summary>The control method used for shifting the camera</summary>
			public ENavMode NavigationMode
			{
				get { return m_NavigationMode; }
				set { SetProp(ref m_NavigationMode, value, nameof(NavigationMode)); }
			}
			private ENavMode m_NavigationMode;

			/// <summary>Lock the aspect ratio for the chart (null means unlocked)</summary>
			public double? LockAspect
			{
				get { return m_LockAspect; }
				set { SetProp(ref m_LockAspect, value, nameof(LockAspect)); }
			}
			private double? m_LockAspect;

			/// <summary>The fill colour of the non-chart area of the control (e.g. behind the axis labels)</summary>
			public Colour32 BkColour
			{
				get { return m_BkColour; }
				set { SetProp(ref m_BkColour, value, nameof(BkColour)); }
			}
			private Colour32 m_BkColour;

			/// <summary>The fill colour of the chart plot area</summary>
			public Colour32 ChartBkColour
			{
				get { return m_ChartBkColour; }
				set { SetProp(ref m_ChartBkColour, value, nameof(ChartBkColour)); }
			}
			private Colour32 m_ChartBkColour;

			/// <summary>The colour of the title text</summary>
			public Colour32 TitleColour
			{
				get { return m_TitleColour; }
				set { SetProp(ref m_TitleColour, value, nameof(TitleColour)); }
			}
			private Colour32 m_TitleColour;

			/// <summary>Transform for position the chart title, offset from top centre</summary>
			public Matrix TitleTransform
			{
				get { return m_TitleTransform; }
				set { SetProp(ref m_TitleTransform, value, nameof(TitleTransform)); }
			}
			private Matrix m_TitleTransform;

			/// <summary>The distances from the edge of the control to the chart area</summary>
			public Thickness Margin
			{
				get { return m_Margin; }
				set { SetProp(ref m_Margin, value, nameof(Margin)); }
			}
			private Thickness m_Margin;

			/// <summary>Font to use for the title text</summary>
			public Font TitleFont
			{
				get { return m_TitleFont; }
				set { SetProp(ref m_TitleFont, value, nameof(TitleFont)); }
			}
			private Font m_TitleFont;

			/// <summary>Font to use for chart notes</summary>
			public Font NoteFont
			{
				get { return m_NoteFont; }
				set { SetProp(ref m_NoteFont, value, nameof(NoteFont)); }
			}
			private Font m_NoteFont;

			/// <summary>Area selection colour</summary>
			public Colour32 SelectionColour
			{
				get { return m_SelectionColour; }
				set { SetProp(ref m_SelectionColour, value, nameof(SelectionColour)); }
			}
			private Colour32 m_SelectionColour;

			/// <summary>Show grid lines (False overrides per-axis options)</summary>
			public bool ShowGridLines
			{
				get { return m_ShowGridLines; }
				set { SetProp(ref m_ShowGridLines, value, nameof(ShowGridLines)); }
			}
			private bool m_ShowGridLines;

			/// <summary>The offset from the origin for the grid, in the forward direction of the camera</summary>
			public float GridZOffset
			{
				get { return m_GridZOffset; }
				set { SetProp(ref m_GridZOffset, value, nameof(GridZOffset)); }
			}
			private float m_GridZOffset;

			/// <summary>Show/Hide the chart axes</summary>
			public bool ShowAxes
			{
				get { return m_ShowAxes; }
				set { SetProp(ref m_ShowAxes, value, nameof(ShowAxes)); }
			}
			private bool m_ShowAxes;

			/// <summary>Enable/Disable multi-sampling in the view3d view. Can only be changed before the view is created</summary>
			public bool AntiAliasing
			{
				get { return m_AntiAliasing; }
				set { SetProp(ref m_AntiAliasing, value, nameof(AntiAliasing)); }
			}
			private bool m_AntiAliasing;

			/// <summary>Fill mode, solid, wire, or both</summary>
			public View3d.EFillMode FillMode
			{
				get { return m_FillMode; }
				set { SetProp(ref m_FillMode, value, nameof(FillMode)); }
			}
			private View3d.EFillMode m_FillMode;

			/// <summary>Fill mode, solid, wire, or both</summary>
			public View3d.ECullMode CullMode
			{
				get { return m_CullMode; }
				set { SetProp(ref m_CullMode, value, nameof(CullMode)); }
			}
			private View3d.ECullMode m_CullMode;

			/// <summary>Get/Set orthographic camera projection</summary>
			public bool Orthographic
			{
				get { return m_Orthographic; }
				set { SetProp(ref m_Orthographic, value, nameof(Orthographic)); }
			}
			private bool m_Orthographic;

			/// <summary>How close a click has to be for selection to occur (in client space)</summary>
			public float MinSelectionDistance
			{
				get { return m_MinSelectionDistance; }
				set { SetProp(ref m_MinSelectionDistance, value, nameof(MinSelectionDistance)); }
			}
			private float m_MinSelectionDistance;

			/// <summary>Minimum distance in pixels before the chart starts dragging</summary>
			public float MinDragPixelDistance
			{
				get { return m_MinDragPixelDistance; }
				set { SetProp(ref m_MinDragPixelDistance, value, nameof(MinDragPixelDistance)); }
			}
			private float m_MinDragPixelDistance;

			/// <summary>True if the camera should move along a ray cast through the mouse point</summary>
			public bool PerpendicularZTranslation
			{
				get { return m_PerpendicularZTranslation; }
				set { SetProp(ref m_PerpendicularZTranslation, value, nameof(PerpendicularZTranslation)); }
			}
			private bool m_PerpendicularZTranslation;

			/// <summary>The forward direction of the camera when reset</summary>
			public v4 ResetForward
			{
				get { return m_ResetForward; }
				set { SetProp(ref m_ResetForward, value, nameof(ResetForward)); }
			}
			public v4 m_ResetForward;

			/// <summary>The up direction of the camera when reset</summary>
			public v4 ResetUp
			{
				get { return m_ResetUp; }
				set { SetProp(ref m_ResetUp, value, nameof(ResetUp)); }
			}
			public v4 m_ResetUp;

			/// <summary>XAxis rendering options</summary>
			public Axis XAxis
			{
				get { return m_XAxis; }
				private set
				{
					if (m_XAxis == value) return;
					if (m_XAxis != null) m_XAxis.PropertyChanged -= HandleXAxisPropertyChanged;
					m_XAxis = value;
					if (m_XAxis != null) m_XAxis.PropertyChanged += HandleXAxisPropertyChanged;

					// Handler
					void HandleXAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
					{
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(XAxis)));
					}
				}
			}
			private Axis m_XAxis;

			/// <summary>YAxis rendering options</summary>
			public Axis YAxis
			{
				get { return m_YAxis; }
				private set
				{
					if (m_YAxis == value) return;
					if (m_YAxis != null) m_YAxis.PropertyChanged -= HandleYAxisPropertyChanged;
					m_YAxis = value;
					if (m_YAxis != null) m_YAxis.PropertyChanged += HandleYAxisPropertyChanged;

					// Handler
					void HandleYAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
					{
						PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YAxis)));
					}
				}
			}
			private Axis m_YAxis;

			[TypeConverter(typeof(TyConv))]
			public class Axis : INotifyPropertyChanged
			{
				private class TyConv : GenericTypeConverter<Axis> { }

				public Axis()
				{
					AxisColour = Colour32.Black;
					LabelColour = Colour32.Black;
					GridColour = Colour32.WhiteSmoke;
					TickColour = Colour32.Black;
					LabelFont = new Font("tahoma", 10, System.Drawing.FontStyle.Regular);
					TickFont = new Font("tahoma", 8, System.Drawing.FontStyle.Regular);
					DrawTickMarks = true;
					DrawTickLabels = true;
					TickLength = 5;
					MinTickSize = 30;
					LabelTransform = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
					AxisThickness = 1f;
					PixelsPerTick = 30.0;
					ShowGridLines = true;
				}
				public Axis(Axis rhs)
				{
					AxisColour = rhs.AxisColour;
					LabelColour = rhs.LabelColour;
					GridColour = rhs.GridColour;
					TickColour = rhs.TickColour;
					LabelFont = (Font)rhs.LabelFont.Clone();
					TickFont = (Font)rhs.TickFont.Clone();
					DrawTickMarks = rhs.DrawTickMarks;
					DrawTickLabels = rhs.DrawTickLabels;
					TickLength = rhs.TickLength;
					MinTickSize = rhs.MinTickSize;
					LabelTransform = rhs.LabelTransform;
					AxisThickness = rhs.AxisThickness;
					PixelsPerTick = rhs.PixelsPerTick;
					ShowGridLines = rhs.ShowGridLines;
				}
				public Axis(XElement node) : this()
				{
					AxisColour = node.Element(nameof(AxisColour)).As(AxisColour);
					LabelColour = node.Element(nameof(LabelColour)).As(LabelColour);
					GridColour = node.Element(nameof(GridColour)).As(GridColour);
					TickColour = node.Element(nameof(TickColour)).As(TickColour);
					LabelFont = node.Element(nameof(LabelFont)).As(LabelFont);
					TickFont = node.Element(nameof(TickFont)).As(TickFont);
					DrawTickMarks = node.Element(nameof(DrawTickMarks)).As(DrawTickMarks);
					DrawTickLabels = node.Element(nameof(DrawTickLabels)).As(DrawTickLabels);
					TickLength = node.Element(nameof(TickLength)).As(TickLength);
					MinTickSize = node.Element(nameof(MinTickSize)).As(MinTickSize);
					LabelTransform = node.Element(nameof(LabelTransform)).As(LabelTransform);
					AxisThickness = node.Element(nameof(AxisThickness)).As(AxisThickness);
					PixelsPerTick = node.Element(nameof(PixelsPerTick)).As(PixelsPerTick);
					ShowGridLines = node.Element(nameof(ShowGridLines)).As(ShowGridLines);
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(AxisColour), AxisColour, false);
					node.Add2(nameof(LabelColour), LabelColour, false);
					node.Add2(nameof(GridColour), GridColour, false);
					node.Add2(nameof(TickColour), TickColour, false);
					node.Add2(nameof(LabelFont), LabelFont, false);
					node.Add2(nameof(TickFont), TickFont, false);
					node.Add2(nameof(DrawTickMarks), DrawTickMarks, false);
					node.Add2(nameof(DrawTickLabels), DrawTickLabels, false);
					node.Add2(nameof(TickLength), TickLength, false);
					node.Add2(nameof(MinTickSize), MinTickSize, false);
					node.Add2(nameof(LabelTransform), LabelTransform, false);
					node.Add2(nameof(AxisThickness), AxisThickness, false);
					node.Add2(nameof(PixelsPerTick), PixelsPerTick, false);
					node.Add2(nameof(ShowGridLines), ShowGridLines, false);
					return node;
				}
				public override string ToString()
				{
					return "Axis Options";
				}

				/// <summary>Property changed</summary>
				public event PropertyChangedEventHandler PropertyChanged;
				private void SetProp<T>(ref T prop, T value, string name)
				{
					if (Equals(prop, value)) return;
					prop = value;
					unchecked { ++ChangeCounter; }
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
				}
				public int ChangeCounter { get; private set; }

				/// <summary>The colour of the main axes</summary>
				public Colour32 AxisColour
				{
					get { return m_AxisColour; }
					set { SetProp(ref m_AxisColour, value, nameof(AxisColour)); }
				}
				private Colour32 m_AxisColour;

				/// <summary>The colour of the label text</summary>
				public Colour32 LabelColour
				{
					get { return m_LabelColour; }
					set { SetProp(ref m_LabelColour, value, nameof(LabelColour)); }
				}
				private Colour32 m_LabelColour;

				/// <summary>The colour of the grid lines</summary>
				public Colour32 GridColour
				{
					get { return m_GridColour; }
					set { SetProp(ref m_GridColour, value, nameof(GridColour)); }
				}
				private Colour32 m_GridColour;

				/// <summary>The colour of the tick text</summary>
				public Colour32 TickColour
				{
					get { return m_TickColour; }
					set { SetProp(ref m_TickColour, value, nameof(TickColour)); }
				}
				private Colour32 m_TickColour;

				/// <summary>The font to use for the axis label</summary>
				public Font LabelFont
				{
					get { return m_LabelFont; }
					set { SetProp(ref m_LabelFont, value, nameof(LabelFont)); }
				}
				private Font m_LabelFont;

				/// <summary>The font to use for tick labels</summary>
				public Font TickFont
				{
					get { return m_TickFont; }
					set { SetProp(ref m_TickFont, value, nameof(TickFont)); }
				}
				private Font m_TickFont;

				/// <summary>True if tick marks should be drawn</summary>
				public bool DrawTickMarks
				{
					get { return m_DrawTickMarks; }
					set { SetProp(ref m_DrawTickMarks, value, nameof(DrawTickMarks)); }
				}
				private bool m_DrawTickMarks;

				/// <summary>True if tick labels should be drawn</summary>
				public bool DrawTickLabels
				{
					get { return m_DrawTickLabels; }
					set { SetProp(ref m_DrawTickLabels, value, nameof(DrawTickLabels)); }
				}
				private bool m_DrawTickLabels;

				/// <summary>The length of the tick marks</summary>
				public int TickLength
				{
					get { return m_TickLength; }
					set { SetProp(ref m_TickLength, value, nameof(TickLength)); }
				}
				private int m_TickLength;

				/// <summary>The minimum space reserved for tick marks and labels</summary>
				public float MinTickSize
				{
					get { return m_MinTickSize; }
					set { SetProp(ref m_MinTickSize, value, nameof(MinTickSize)); }
				}
				private float m_MinTickSize;

				/// <summary>Offset transform from default label position</summary>
				public Matrix LabelTransform
				{
					get { return m_LabelTransform; }
					set { SetProp(ref m_LabelTransform, value, nameof(LabelTransform)); }
				}
				private Matrix m_LabelTransform;

				/// <summary>The thickness of the axis line</summary>
				public float AxisThickness
				{
					get { return m_AxisThickness; }
					set { SetProp(ref m_AxisThickness, value, nameof(AxisThickness)); }
				}
				private float m_AxisThickness;

				/// <summary>The preferred number of pixels between each grid line</summary>
				public double PixelsPerTick
				{
					get { return m_PixelsPerTick; }
					set { SetProp(ref m_PixelsPerTick, value, nameof(PixelsPerTick)); }
				}
				private double m_PixelsPerTick;

				/// <summary>Show grid lines for this axis. (This settings is overruled by the main chart options)</summary>
				public bool ShowGridLines
				{
					get { return m_ShowGridLines; }
					set { SetProp(ref m_ShowGridLines, value, nameof(ShowGridLines)); }
				}
				private bool m_ShowGridLines;
			}
		}
	}
}

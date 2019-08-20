using System.ComponentModel;
using System.Windows.Controls;
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
		public class OptionsData : INotifyPropertyChanged
		{
			private class TyConv : GenericTypeConverter<OptionsData> { }

			public OptionsData()
			{
				NavigationMode = ENavMode.Chart2D;
				LockAspect = null;
				BackgroundColour = Colour32.LightGray;
				SelectionColour = 0x8060A0E0;
				GridZOffset = 0.001;
				CrossHairZOffset = 0.005;
				ShowAxes = true;
				AntiAliasing = true;
				FillMode = View3d.EFillMode.Solid;
				CullMode = View3d.ECullMode.Back;
				Orthographic = false;
				MinSelectionDistance = 10.0;
				MinDragPixelDistance = 5.0;
				MouseCentredZoom = true;
				ResetForward = -v4.ZAxis;
				ResetUp = +v4.YAxis;
				XAxis = new Axis { Side = Dock.Bottom };
				YAxis = new Axis { Side = Dock.Left };
				// Don't forget to add new members to the other constructors!
			}
			public OptionsData(OptionsData rhs)
			{
				NavigationMode = rhs.NavigationMode;
				LockAspect = rhs.LockAspect;
				BackgroundColour = rhs.BackgroundColour;
				SelectionColour = rhs.SelectionColour;
				GridZOffset = rhs.GridZOffset;
				CrossHairZOffset = rhs.CrossHairZOffset;
				ShowAxes = rhs.ShowAxes;
				AntiAliasing = rhs.AntiAliasing;
				FillMode = rhs.FillMode;
				CullMode = rhs.CullMode;
				Orthographic = rhs.Orthographic;
				MinSelectionDistance = rhs.MinSelectionDistance;
				MinDragPixelDistance = rhs.MinDragPixelDistance;
				MouseCentredZoom = rhs.MouseCentredZoom;
				ResetForward = rhs.ResetForward;
				ResetUp = rhs.ResetUp;
				XAxis = new Axis(rhs.XAxis);
				YAxis = new Axis(rhs.YAxis);
			}
			public OptionsData(XElement node) : this()
			{
				NavigationMode = node.Element(nameof(NavigationMode)).As(NavigationMode);
				LockAspect = node.Element(nameof(LockAspect)).As(LockAspect);
				BackgroundColour = node.Element(nameof(BackgroundColour)).As(BackgroundColour);
				SelectionColour = node.Element(nameof(SelectionColour)).As(SelectionColour);
				ShowAxes = node.Element(nameof(ShowAxes)).As(ShowAxes);
				GridZOffset = node.Element(nameof(GridZOffset)).As(GridZOffset);
				CrossHairZOffset = node.Element(nameof(CrossHairZOffset)).As(CrossHairZOffset);
				AntiAliasing = node.Element(nameof(AntiAliasing)).As(AntiAliasing);
				FillMode = node.Element(nameof(FillMode)).As(FillMode);
				CullMode = node.Element(nameof(CullMode)).As(CullMode);
				Orthographic = node.Element(nameof(Orthographic)).As(Orthographic);
				MinSelectionDistance = node.Element(nameof(MinSelectionDistance)).As(MinSelectionDistance);
				MinDragPixelDistance = node.Element(nameof(MinDragPixelDistance)).As(MinDragPixelDistance);
				MouseCentredZoom = node.Element(nameof(MouseCentredZoom)).As(MouseCentredZoom);
				ResetForward = node.Element(nameof(ResetForward)).As(ResetForward);
				ResetUp = node.Element(nameof(ResetUp)).As(ResetUp);
				XAxis = node.Element(nameof(XAxis)).As(XAxis);
				YAxis = node.Element(nameof(YAxis)).As(YAxis);
			}
			public XElement ToXml(XElement node)
			{
				node.Add2(nameof(NavigationMode), NavigationMode, false);
				node.Add2(nameof(LockAspect), LockAspect, false);
				node.Add2(nameof(BackgroundColour), BackgroundColour, false);
				node.Add2(nameof(SelectionColour), SelectionColour, false);
				node.Add2(nameof(GridZOffset), GridZOffset, false);
				node.Add2(nameof(CrossHairZOffset), CrossHairZOffset, false);
				node.Add2(nameof(ShowAxes), ShowAxes, false);
				node.Add2(nameof(AntiAliasing), AntiAliasing, false);
				node.Add2(nameof(FillMode), FillMode, false);
				node.Add2(nameof(CullMode), CullMode, false);
				node.Add2(nameof(Orthographic), Orthographic, false);
				node.Add2(nameof(MinSelectionDistance), MinSelectionDistance, false);
				node.Add2(nameof(MinDragPixelDistance), MinDragPixelDistance, false);
				node.Add2(nameof(MouseCentredZoom), MouseCentredZoom, false);
				node.Add2(nameof(ResetForward), ResetForward, false);
				node.Add2(nameof(ResetUp), ResetUp, false);
				node.Add2(nameof(XAxis), XAxis, false);
				node.Add2(nameof(YAxis), YAxis, false);
				return node;
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
				get => m_NavigationMode;
				set => SetProp(ref m_NavigationMode, value, nameof(NavigationMode));
			}
			private ENavMode m_NavigationMode;

			/// <summary>Lock the aspect ratio for the chart (null means unlocked)</summary>
			public double? LockAspect
			{
				get => m_LockAspect;
				set => SetProp(ref m_LockAspect, value, nameof(LockAspect));
			}
			private double? m_LockAspect;

			/// <summary>The chart area background colour</summary>
			public Colour32 BackgroundColour
			{
				get => m_BackgroundColour;
				set => SetProp(ref m_BackgroundColour, value, nameof(BackgroundColour));
			}
			private Colour32 m_BackgroundColour;

			/// <summary>Area selection colour</summary>
			public Colour32 SelectionColour
			{
				get => m_SelectionColour;
				set => SetProp(ref m_SelectionColour, value, nameof(SelectionColour));
			}
			private Colour32 m_SelectionColour;

			/// <summary>The offset from the origin for the grid, in the forward direction of the camera (focus distance relative)</summary>
			public double GridZOffset
			{
				get => m_GridZOffset;
				set => SetProp(ref m_GridZOffset, value, nameof(GridZOffset));
			}
			private double m_GridZOffset;

			/// <summary>The depth position of the cross hair (focus distance relative)</summary>
			public double CrossHairZOffset
			{
				get => m_CrossHairZOffset;
				set => SetProp(ref m_CrossHairZOffset, value, nameof(CrossHairZOffset));
			}
			private double m_CrossHairZOffset;

			/// <summary>Show/Hide the chart axes</summary>
			public bool ShowAxes
			{
				get => m_ShowAxes;
				set => SetProp(ref m_ShowAxes, value, nameof(ShowAxes));
			}
			private bool m_ShowAxes;

			/// <summary>Show hide both X and Y Axis grid lines</summary>
			public bool ShowGridLines
			{
				get => XAxis.ShowGridLines || YAxis.ShowGridLines;
				set
				{
					var shown = ShowGridLines;
					XAxis.ShowGridLines = !shown;
					YAxis.ShowGridLines = !shown;
				}
			}

			/// <summary>Enable/Disable multi-sampling in the view3d view. Can only be changed before the view is created</summary>
			public bool AntiAliasing
			{
				get => m_AntiAliasing;
				set => SetProp(ref m_AntiAliasing, value, nameof(AntiAliasing));
			}
			private bool m_AntiAliasing;

			/// <summary>Fill mode, solid, wire, or both</summary>
			public View3d.EFillMode FillMode
			{
				get => m_FillMode;
				set => SetProp(ref m_FillMode, value, nameof(FillMode));
			}
			private View3d.EFillMode m_FillMode;

			/// <summary>Fill mode, solid, wire, or both</summary>
			public View3d.ECullMode CullMode
			{
				get => m_CullMode;
				set => SetProp(ref m_CullMode, value, nameof(CullMode));
			}
			private View3d.ECullMode m_CullMode;

			/// <summary>Get/Set orthographic camera projection</summary>
			public bool Orthographic
			{
				get => m_Orthographic;
				set => SetProp(ref m_Orthographic, value, nameof(Orthographic));
			}
			private bool m_Orthographic;

			/// <summary>How close a click has to be for selection to occur (in client space)</summary>
			public double MinSelectionDistance
			{
				get => m_MinSelectionDistance;
				set => SetProp(ref m_MinSelectionDistance, value, nameof(MinSelectionDistance));
			}
			private double m_MinSelectionDistance;

			/// <summary>Minimum distance in pixels before the chart starts dragging</summary>
			public double MinDragPixelDistance
			{
				get => m_MinDragPixelDistance;
				set => SetProp(ref m_MinDragPixelDistance, value, nameof(MinDragPixelDistance));
			}
			private double m_MinDragPixelDistance;

			/// <summary>True if the camera should move along a ray cast through the mouse point</summary>
			public bool MouseCentredZoom
			{
				get => m_MouseCentredZoom;
				set => SetProp(ref m_MouseCentredZoom, value, nameof(MouseCentredZoom));
			}
			private bool m_MouseCentredZoom;

			/// <summary>The forward direction of the camera when reset</summary>
			public v4 ResetForward
			{
				get => m_ResetForward;
				set => SetProp(ref m_ResetForward, value, nameof(ResetForward));
			}
			public v4 m_ResetForward;

			/// <summary>The up direction of the camera when reset</summary>
			public v4 ResetUp
			{
				get => m_ResetUp;
				set => SetProp(ref m_ResetUp, value, nameof(ResetUp));
			}
			public v4 m_ResetUp;

			/// <summary>XAxis rendering options</summary>
			public Axis XAxis
			{
				get => m_XAxis;
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
				get => m_YAxis;
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
					Side = Dock.Left;
					AxisColour = Colour32.Black;
					LabelColour = Colour32.Black;
					GridColour = Colour32.WhiteSmoke;
					TickColour = Colour32.Black;
					DrawTickMarks = true;
					DrawTickLabels = true;
					TickLength = 5;
					MinTickSize = 30;
					LabelTransform = new Matrix(1f, 0f, 0f, 1f, 0f, 0f);
					AxisThickness = 2f;
					PixelsPerTick = 30.0;
					ShowGridLines = true;
					TickTextTemplate = "-XXXXX.XX";
				}
				public Axis(Axis rhs)
				{
					Side = rhs.Side;
					AxisColour = rhs.AxisColour;
					LabelColour = rhs.LabelColour;
					GridColour = rhs.GridColour;
					TickColour = rhs.TickColour;
					DrawTickMarks = rhs.DrawTickMarks;
					DrawTickLabels = rhs.DrawTickLabels;
					TickLength = rhs.TickLength;
					MinTickSize = rhs.MinTickSize;
					LabelTransform = rhs.LabelTransform;
					AxisThickness = rhs.AxisThickness;
					PixelsPerTick = rhs.PixelsPerTick;
					ShowGridLines = rhs.ShowGridLines;
					TickTextTemplate = rhs.TickTextTemplate;
				}
				public Axis(XElement node) : this()
				{
					Side = node.Element(nameof(Side)).As(Side);
					AxisColour = node.Element(nameof(AxisColour)).As(AxisColour);
					LabelColour = node.Element(nameof(LabelColour)).As(LabelColour);
					GridColour = node.Element(nameof(GridColour)).As(GridColour);
					TickColour = node.Element(nameof(TickColour)).As(TickColour);
					DrawTickMarks = node.Element(nameof(DrawTickMarks)).As(DrawTickMarks);
					DrawTickLabels = node.Element(nameof(DrawTickLabels)).As(DrawTickLabels);
					TickLength = node.Element(nameof(TickLength)).As(TickLength);
					MinTickSize = node.Element(nameof(MinTickSize)).As(MinTickSize);
					LabelTransform = node.Element(nameof(LabelTransform)).As(LabelTransform);
					AxisThickness = node.Element(nameof(AxisThickness)).As(AxisThickness);
					PixelsPerTick = node.Element(nameof(PixelsPerTick)).As(PixelsPerTick);
					ShowGridLines = node.Element(nameof(ShowGridLines)).As(ShowGridLines);
					TickTextTemplate = node.Element(nameof(TickTextTemplate)).As(TickTextTemplate);
				}
				public XElement ToXml(XElement node)
				{
					node.Add2(nameof(Side), Side, false);
					node.Add2(nameof(AxisColour), AxisColour, false);
					node.Add2(nameof(LabelColour), LabelColour, false);
					node.Add2(nameof(GridColour), GridColour, false);
					node.Add2(nameof(TickColour), TickColour, false);
					node.Add2(nameof(DrawTickMarks), DrawTickMarks, false);
					node.Add2(nameof(DrawTickLabels), DrawTickLabels, false);
					node.Add2(nameof(TickLength), TickLength, false);
					node.Add2(nameof(MinTickSize), MinTickSize, false);
					node.Add2(nameof(LabelTransform), LabelTransform, false);
					node.Add2(nameof(AxisThickness), AxisThickness, false);
					node.Add2(nameof(PixelsPerTick), PixelsPerTick, false);
					node.Add2(nameof(ShowGridLines), ShowGridLines, false);
					node.Add2(nameof(TickTextTemplate), TickTextTemplate, false);
					return node;
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

				/// <summary>The side of the chart that this axis is on</summary>
				public Dock Side
				{
					get { return m_Side; }
					set { SetProp(ref m_Side, value, nameof(Side)); }
				}
				private Dock m_Side;

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
				public double TickLength
				{
					get { return m_TickLength; }
					set { SetProp(ref m_TickLength, value, nameof(TickLength)); }
				}
				private double m_TickLength;

				/// <summary>The minimum space reserved for tick marks and labels</summary>
				public double MinTickSize
				{
					get { return m_MinTickSize; }
					set { SetProp(ref m_MinTickSize, value, nameof(MinTickSize)); }
				}
				private double m_MinTickSize;

				/// <summary>Offset transform from default label position</summary>
				public Matrix LabelTransform
				{
					get { return m_LabelTransform; }
					set { SetProp(ref m_LabelTransform, value, nameof(LabelTransform)); }
				}
				private Matrix m_LabelTransform;

				/// <summary>The thickness of the axis line</summary>
				public double AxisThickness
				{
					get { return m_AxisThickness; }
					set { SetProp(ref m_AxisThickness, value, nameof(AxisThickness)); }
				}
				private double m_AxisThickness;

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

				/// <summary>Example text used to measure the tick text size for this axis</summary>
				public string TickTextTemplate
				{
					get { return m_TickTextTemplate; }
					set { SetProp(ref m_TickTextTemplate, value, nameof(TickTextTemplate)); }
				}
				private string m_TickTextTemplate;
			}
		}
	}
}

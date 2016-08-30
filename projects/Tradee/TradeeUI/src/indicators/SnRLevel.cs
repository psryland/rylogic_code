using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>A support/resistance level</summary>
	public class SnRLevel :INotifyPropertyChanged
	{
		// Notes:
		// - this is the core data for a support/resistance level

		public SnRLevel()
			:this(0, 0)
		{ }
		public SnRLevel(double price, double width_in_pips, ETimeFrame max_time_frame = ETimeFrame.Monthly)
		{
			Id           = Guid.NewGuid();
			Price        = price;
			WidthPips    = width_in_pips;
			MaxTimeFrame = max_time_frame;
		}

		/// <summary>Unique Id for this SnRLevel</summary>
		public Guid Id
		{
			get;
			private set;
		}

		/// <summary>The level</summary>
		public double Price
		{
			get { return m_price; }
			set { SetProp(ref m_price, value, nameof(Price)); }
		}
		private double m_price;

		/// <summary>The width of the SnR level</summary>
		public double WidthPips
		{
			get { return m_width_pips; }
			set { SetProp(ref m_width_pips, value, nameof(WidthPips)); }
		}
		private double m_width_pips;

		/// <summary>The highest time frame that this support level is visible at</summary>
		public ETimeFrame MaxTimeFrame
		{
			get { return m_max_time_frame; }
			set { SetProp(ref m_max_time_frame, value, nameof(MaxTimeFrame)); }
		}
		private ETimeFrame m_max_time_frame;

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T value, string name)
		{
			if (Equals(prop, value)) return;
			prop = value;
			PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
		}
	}

	/// <summary>Support and resistance level chart element</summary>
	public class SnRIndicator :IndicatorBase
	{
		// Notes:
		// - SnRLevels are saved in the DB with the instrument data so that they
		//   are available to other parts of the code, not just charts.
		// - The SnRLevelSettings are saved in the per chart data. This allows the
		//   chart to display the SnRLevel with specific colours etc. The settings
		//   are matched with SnRLevel instances via the 'Id'

		public SnRIndicator(SnRLevel snr)
			:base(snr.Id, "SnR Level", new SnRLevelSettings { })
		{
			Level = snr;
		}
		public SnRIndicator(XElement node)
			:base(node)
		{}
		protected override void Dispose(bool disposing)
		{
			Gfx = null;
			base.Dispose(disposing);
		}
		protected override void SetChartCore(ChartControl chart)
		{
			// Invalidate the graphics whenever the x axis moves
			if (Chart != null)
			{
				Chart.XAxis.Zoomed -= Invalidate;
			}
			base.SetChartCore(chart);
			if (Chart != null)
			{
				Chart.XAxis.Zoomed += Invalidate;
			}
		}
		protected override void UpdateGfxCore()
		{
			var line_colour   = (Selected ? Settings.Colour      .Lerp(Color.Gray, 0.2f) : Settings.Colour      ).ToArgbU();
			var region_colour = (Selected ? Settings.RegionColour.Lerp(Color.Gray, 0.2f) : Settings.RegionColour).ToArgbU();
			var width         = (float)Chart.XAxis.Span;

			// Create graphics for the horizontal line
			m_vbuf.Resize(2 + (Level.WidthPips != 0 ? 4 : 0));
			m_ibuf.Resize(2 + (Level.WidthPips != 0 ? 6 : 0));
			m_nbuf.Resize(1 + (Level.WidthPips != 0 ? 1 : 0));

			m_vbuf[0] = new View3d.Vertex(new v4(0, 0, 0, 1), line_colour);
			m_vbuf[1] = new View3d.Vertex(new v4(width, 0, 0, 1), line_colour);
			m_ibuf[0] = 0;
			m_ibuf[1] = 1;

			var mat = new View3d.Material(shader:View3d.EShader.ThickLineListGS, shader_data:new int[4] { Settings.LineWidth, 0, 0, 0 });
			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, 0, 2, 0, 2, !Bit.AllSet(line_colour, 0xFF000000), mat);

			if (Level.WidthPips != 0)
			{
				var hh = (float)(Instrument.PriceData.PipSize * Level.WidthPips / 2.0);
				m_vbuf[2] = new View3d.Vertex(new v4(0    , -hh, 0, 1), region_colour);
				m_vbuf[3] = new View3d.Vertex(new v4(width, -hh, 0, 1), region_colour);
				m_vbuf[4] = new View3d.Vertex(new v4(0    , +hh, 0, 1), region_colour);
				m_vbuf[5] = new View3d.Vertex(new v4(width, +hh, 0, 1), region_colour);
				m_ibuf[2] = 2;
				m_ibuf[3] = 3;
				m_ibuf[4] = 5;
				m_ibuf[5] = 5;
				m_ibuf[6] = 4;
				m_ibuf[7] = 2;

				m_nbuf[1] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Colr, 2, 6, 2, 8, !Bit.AllSet(region_colour, 0xFF000000));
			}

			// Create the graphics
			Gfx = new View3d.Object(Name, 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());

			base.UpdateGfxCore();
		}
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx != null)
			{
				// Graphics are created at the origin, position at XAxis.Max
				Gfx.O2P = m4x4.Translation((float)Chart.XAxis.Min, (float)Level.Price, ChartUI.Z.SnR);
				window.AddObject(Gfx);
			}
		}

		/// <summary>The level being represented on the chart</summary>
		public SnRLevel Level
		{
			get { return m_snr; }
			set
			{
				if (m_snr == value) return;
				if (m_snr != null)
				{
					m_snr.PropertyChanged -= HandlePropertyChanged;
				}
				m_snr = value;
				if (m_snr != null)
				{
					m_snr.PropertyChanged += HandlePropertyChanged;
				}
				Invalidate();
			}
		}
		private SnRLevel m_snr;

		/// <summary>Settings for this indicator</summary>
		public SnRLevelSettings Settings
		{
			[DebuggerStepThrough] get { return (SnRLevelSettings)IndicatorSettingsInternal; }
		}

		/// <summary>The Ask price line</summary>
		public View3d.Object Gfx
		{
			[DebuggerStepThrough] get { return m_impl_gfx; }
			set
			{
				if (m_impl_gfx == value) return;
				Util.Dispose(ref m_impl_gfx);
				m_impl_gfx = value;
			}
		}
		private View3d.Object m_impl_gfx;

		/// <summary>Allow this indicator to be dragged on a chart</summary>
		public override bool Dragable
		{
			get { return true; }
		}
		public override ChartControl.MouseOp CreateDragMouseOp()
		{
			return new DragLine(this);
		}

		/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in client space because typically hit testing uses pixel tolerances</summary>
		public override ChartControl.HitTestResult.Hit HitTest(Point client_point, Keys modifier_keys, View3d.CameraControls cam)
		{
			// Find the nearest point to 'client_point' on the line
			var chart_pt  = Chart.ClientToChart(client_point);
			var client_pt = Chart.ChartToClient(new PointF(chart_pt.X, (float)Level.Price));

			// If clicked within 'tolerance' of the line
			const int tolerance = 3;
			if (Math.Abs(client_pt.Y - client_point.Y) < tolerance)
				return new ChartControl.HitTestResult.Hit(this, new PointF(chart_pt.X, (float)Level.Price), null);

			return null;
		}

		/// <summary>Handle the underlying SnRLevel changing</summary>
		private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			InvalidateChart();
			if (e.PropertyName == nameof(SnRLevel.WidthPips))
				Invalidate();
		}

		/// <summary>A mouse operation for dragging the SnR level around</summary>
		private class DragLine :ChartControl.MouseOp
		{
			private readonly SnRIndicator m_snr;
			public DragLine(SnRIndicator snr) : base(snr.Chart)
			{
				m_snr = snr;
				m_snr.Selected = true;
			}
			public override void Dispose()
			{
				m_snr.Selected = false;
				base.Dispose();
			}
			public override void MouseMove(MouseEventArgs e)
			{
				var chart_pt = m_snr.Chart.ClientToChart(e.Location);
				m_snr.Level.Price = chart_pt.Y;
				m_snr.InvalidateChart();
				m_chart.Update();
			}
		}
	}

	#region Settings

	/// <summary>Settings for Support and Resistance</summary>
	[TypeConverter(typeof(TyConv))]
	public class SnRLevelSettings :SettingsXml<SnRLevelSettings>
	{
		public SnRLevelSettings()
		{
			LineWidth    = 2;
			Colour       = Color.LightBlue;
			RegionColour = Color.LightBlue.Alpha(0.5f);
		}
		public SnRLevelSettings(XElement node)
			:base(node)
		{}

		/// <summary>The thickness of the SnR centre line</summary>
		public int LineWidth
		{
			get { return get(x => x.LineWidth); }
			set { set(x => x.LineWidth, value); }
		}

		/// <summary>Colour of the line</summary>
		public Color Colour
		{
			get { return get(x => x.Colour); }
			set { set(x => x.Colour, value); }
		}

		/// <summary>Colour of the surrounding region</summary>
		public Color RegionColour
		{
			get { return get(x => x.RegionColour); }
			set { set(x => x.RegionColour, value); }
		}

		private class TyConv :GenericTypeConverter<SnRLevelSettings> {}
	}

	#endregion
}

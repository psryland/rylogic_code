using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI.GfxObjects
{
	public class Volume :Buffers
	{
		public Volume(Instrument instrument, ChartControl chart)
		{
			Chart = chart;
			Instrument = instrument;
			Cache = new ChartGfxCache(CreatePiece);

			Label = new TextBlock
			{
				Text = string.Empty,
				FontSize = 10.0,
				IsHitTestVisible = false,
			};
			Icon = new Polygon
			{
				Points = PointCollection.Parse("0,0 5,5 5,-5"),
				Stroke = Brushes.Black,
				Fill = Brushes.Black,
				Cursor = Cursors.SizeNS,
			};
			Icon.MouseLeftButtonDown += delegate
			{
				chart.MouseOperations.Pending[MouseButton.Left] = new DragVolumeSizeScaler(this) { StartOnMouseDown = false };
			};

			Scale = 1.0;
			IndicatorPosition = 0.25;
		}
		public override void Dispose()
		{
			Cache = null;
			Chart = null;
			Icon.Detach();
			Label.Detach();
			base.Dispose();
		}

		/// <summary>Context for candle graphics</summary>
		public static readonly Guid CtxId = Guid.NewGuid();

		/// <summary>The chart this indicator is displayed on</summary>
		private ChartControl Chart
		{
			get => m_chart;
			set
			{
				if (m_chart == value) return;
				if (m_chart != null)
				{
					m_chart.ChartMoved -= HandleChartMoved;
				}
				m_chart = value;
				if (m_chart != null)
				{
					m_chart.ChartMoved += HandleChartMoved;
				}

				void HandleChartMoved(object sender, ChartControl.ChartMovedEventArgs e)
				{
					if (Bit.AnySet(e.MoveType, ChartControl.EMoveType.YZoomed|ChartControl.EMoveType.YScrolled))
						UpdateScale();
				}
			}
		}
		private ChartControl m_chart;

		/// <summary></summary>
		private Instrument Instrument { get; }

		/// <summary>A cache of graphics objects than span X-axis ranges</summary>
		private ChartGfxCache Cache
		{
			get => m_cache;
			set
			{
				if (m_cache == value) return;
				Util.Dispose(ref m_cache);
				m_cache = value;
			}
		}
		private ChartGfxCache m_cache;

		/// <summary>The position of the indicator as a fraction of the chart width. 0 = Far right, 0.5 = middle, 1 = Far left</summary>
		private double IndicatorPosition
		{
			get => m_indicator_position;
			set
			{
				if (m_indicator_position == value) return;
				m_indicator_position = Math_.Clamp(value, 0.05, 0.95);
				UpdateScale();
			}
		}
		private double m_indicator_position;

		/// <summary>Size scaler for the volume graphics</summary>
		private double Scale { get; set; }
		private void UpdateScale()
		{
			// Determine a suitable scale value for the volume
			var max_visible_volume = 0.0;
			foreach (var candle in Instrument.CandleRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1)))
				max_visible_volume = Math.Max(max_visible_volume, candle.Volume);

			// Pick a "nice" value as the scale amount
			max_visible_volume = Math_.AestheticValue(max_visible_volume, -1);
			Label.Text = $"{max_visible_volume}";

			// Set the scale factor for the graphics
			Scale = IndicatorPosition * Chart.YAxis.Span / max_visible_volume;

			Cache.Invalidate();
		}

		/// <summary>The indicator showing the scale amount</summary>
		private Polygon Icon { get; }

		/// <summary>Text describing the scale amount</summary>
		private TextBlock Label { get; }

		/// <summary>Add the graphics objects to the scene</summary>
		public void BuildScene()
		{
			Chart.Scene.RemoveObjects(new[] { CtxId }, 1, 0);
			if (SettingsData.Settings.Chart.ShowVolume && Instrument != null)
			{
				var range = Instrument.IndexRange((int)(Chart.XAxis.Min - 1), (int)(Chart.XAxis.Max + 1));
				foreach (var gfx in Cache.Get(range).OfType<VolPiece>())
				{
					if (gfx.NoGfx) continue;
					gfx.Gfx.O2P = m4x4.Translation(new v4(gfx.Range.Begf, (float)Chart.YAxis.Min, CandleChart.ZOrder.Indicators, 1f));
					Chart.Scene.AddObject(gfx.Gfx);
				}

				{// Add the scale indicator
					var pt = new Point(Chart.SceneBounds.Right, Chart.SceneBounds.Height * (1.0 - IndicatorPosition));

					Canvas.SetLeft(Icon, pt.X - Icon.ActualWidth);
					Canvas.SetTop(Icon, pt.Y);

					Canvas.SetLeft(Label, pt.X - Label.ActualWidth - Icon.ActualWidth - 2 );
					Canvas.SetTop(Label, pt.Y - Label.ActualHeight / 2);

					Chart.Overlay.Adopt(Icon);
					Chart.Overlay.Adopt(Label);
				}
			}
			else
			{
				Icon.Detach();
				Label.Detach();
			}
		}

		/// <summary>Create graphics for an X-range spanning 'x'</summary>
		private VolPiece CreatePiece(double x, RangeF missing)
		{
			// The available data
			var data_range = new RangeF(0, Instrument.Count);

			// If 'x' is before the available data, return a null graphics piece up to the start of available data
			if (x < data_range.Beg)
				return new VolPiece(new RangeF(missing.Beg, Math.Min(data_range.Beg, missing.End)));

			// If 'x' is after the available data, return a null graphics piece from the end of available data
			if (x >= data_range.End)
				return new VolPiece(new RangeF(Math.Max(data_range.End, missing.Beg), missing.End));

			// Generate an index range based on 'missing', limited to the block size
			const int PieceBlockSize = 512;
			var idx = (int)Math.Floor(x);
			var idx0 = (int)Math_.Max(data_range.Beg, missing.Beg, idx - PieceBlockSize);
			var idx1 = (int)Math_.Min(data_range.End, missing.End, idx + PieceBlockSize);
			var count = idx1 - idx0;

			// Use TriList since each quad is separated
			m_vbuf.Resize(count * 4);
			m_ibuf.Resize(count * 6);
			m_nbuf.Resize(1);

			var colour_bullish = SettingsData.Settings.Chart.Q2BColour.Darken(0.5).Alpha(0.5);
			var colour_bearish = SettingsData.Settings.Chart.B2QColour.Darken(0.5).Alpha(0.5);

			// Create the geometry
			int vert = 0, indx = 0, candle_idx = 0;
			foreach (var candle in Instrument.CandleRange(idx0, idx1))
			{
				var c = (float)candle_idx++;
				var y = (float)(Scale * candle.Volume);
				var col = candle.Bullish ? colour_bullish : candle.Bearish ? colour_bearish : new Colour32(0xFFA0A0A0);

				var v = vert;
				m_vbuf[vert++] = new View3d.Vertex(new v4(c - 0.4f, 0f, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(c + 0.4f, 0f, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(c + 0.4f,  y, 0f, 1f), col);
				m_vbuf[vert++] = new View3d.Vertex(new v4(c - 0.4f,  y, 0f, 1f), col);

				m_ibuf[indx++] = (ushort)(v + 0);
				m_ibuf[indx++] = (ushort)(v + 1);
				m_ibuf[indx++] = (ushort)(v + 2);
				m_ibuf[indx++] = (ushort)(v + 0);
				m_ibuf[indx++] = (ushort)(v + 2);
				m_ibuf[indx++] = (ushort)(v + 3);
			}

			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert | View3d.EGeom.Colr, flags: View3d.ENuggetFlag.GeometryHasAlpha);

			// Create the graphics
			var gfx = new View3d.Object($"Volume-[{idx0},{idx1})", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), CtxId);
			return new VolPiece(new RangeI(idx0, idx1), gfx);
		}

		/// <summary>Graphics for a piece of the volume indicator</summary>
		private class VolPiece :IChartGfxPiece
		{
			public VolPiece(RangeF range)
			{
				Range = range;
				Gfx = null;
			}
			public VolPiece(RangeF range, View3d.Object gfx)
			{
				Range = range;
				Gfx = gfx;
			}
			public void Dispose()
			{
				Gfx = null;
			}

			/// <summary>True if this is a null graphics piece</summary>
			public bool NoGfx => Gfx == null;

			/// <summary>The X-Axis span covered by this piece</summary>
			public RangeF Range { get; }

			/// <summary>The graphics object</summary>
			public View3d.Object Gfx
			{
				get => m_gfx;
				private set
				{
					if (m_gfx == value) return;
					Util.Dispose(ref m_gfx);
					m_gfx = value;
				}
			}
			private View3d.Object m_gfx;
		}

		/// <summary>Mouse op for dragging the scale indicator</summary>
		private class DragVolumeSizeScaler :ChartControl.MouseOp
		{
			private double m_grab_position;

			public DragVolumeSizeScaler(Volume owner)
				: base(owner.Chart, allow_cancel: true)
			{
				Owner = owner;
				m_grab_position = Owner.IndicatorPosition;
			}
			private Volume Owner { get; }
			public override void MouseMove(MouseEventArgs e)
			{
				// Get the mouse position as a fraction of the horizontal range
				if (Cancelled) return;
				var pt = e.GetPosition(Owner.Chart.Overlay);
				Owner.IndicatorPosition = Math_.Clamp(1.0 - pt.Y / Chart.SceneBounds.Height, 0, 1);
			}
			public override void NotifyCancelled()
			{
				Owner.IndicatorPosition = m_grab_position;
			}
		}
	}
}

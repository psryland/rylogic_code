using System;
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
	public class MarketDepth :Buffers
	{
		public MarketDepth(CoinFlip.MarketDepth market, ChartControl chart)
		{
			Chart = chart;
			Market = market;

			Icon = new Polygon
			{
				Points = PointCollection.Parse("0,5 5,0 -5,0"),
				Stroke = Brushes.Black,
				Fill = Brushes.Black,
				Cursor = Cursors.SizeWE,
			};
			Label = new TextBlock
			{
				Text = string.Empty,
				FontSize = 10.0,
				IsHitTestVisible = false,
			};
			CursorVolume = new TextBlock
			{
				Text = string.Empty,
				FontSize = 10.0,
				Foreground = Chart.Scene.BackgroundColor.ToMediaBrush(),
				Background = Chart.Scene.BackgroundColor.InvertBW(0xFF333333, 0xFFCCCCCC).ToMediaBrush(),
				IsHitTestVisible = false,
				Margin= new Thickness(2),
			};
			Icon.MouseLeftButtonDown += delegate
			{
				chart.MouseOperations.Pending[MouseButton.Left] = new DragMarketSizeScaler(this) { StartOnMouseDown = false };
			};

			IndicatorPosition = 0.25;
		}
		public override void Dispose()
		{
			Gfx = null;
			Market = null;
			Icon.Detach();
			Label.Detach();
			CursorVolume.Detach();
			base.Dispose();
		}

		/// <summary>Context for candle graphics</summary>
		public static readonly Guid CtxId = Guid.NewGuid();

		/// <summary>The chart this indicator is displayed on</summary>
		private ChartControl Chart { get; }

		/// <summary>The market whose depth we're drawing</summary>
		private CoinFlip.MarketDepth Market
		{
			get => m_market;
			set
			{
				if (m_market == value) return;
				if (m_market != null)
				{
					m_market.OrderBookChanged -= HandleOrderBookChanged;
				}
				m_market = value;
				if (m_market != null)
				{
					m_market.OrderBookChanged += HandleOrderBookChanged;
				}

				// Handler
				void HandleOrderBookChanged(object sender, EventArgs e)
				{
					Invalidate();
				}
			}
		}
		private CoinFlip.MarketDepth m_market;

		/// <summary>The position of the indicator as a fraction of the chart width. 0 = Far right, 0.5 = middle, 1 = Far left</summary>
		private double IndicatorPosition
		{
			get => m_indicator_position;
			set
			{
				if (m_indicator_position == value) return;
				m_indicator_position = Math_.Clamp(value, 0.05, 0.95);

				var pt = new Point(Chart.SceneBounds.Width * (1.0 - m_indicator_position), 0.0);
				Canvas.SetLeft(Icon, pt.X);
				Canvas.SetTop(Icon, pt.Y);

				Icon.Measure(new Size(100.0,100.0));
				Canvas.SetLeft(Label, pt.X + Icon.DesiredSize.Width + 2);
				Canvas.SetTop(Label, pt.Y);

				Invalidate();
			}
		}
		private double m_indicator_position;

		/// <summary>The graphics object</summary>
		private View3d.Object Gfx
		{
			get => m_gfx;
			set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;

		/// <summary>The indicator showing the scale amount</summary>
		private Polygon Icon { get; }

		/// <summary>Text describing the scale amount</summary>
		private TextBlock Label { get; }

		/// <summary>The volume at the cursor position (displayed when the cross hair is visible)</summary>
		private TextBlock CursorVolume { get; }

		/// <summary>The scale of the market graphics (in volume / xaxis unit)</summary>
		private double Scale { get; set; }

		/// <summary>Add the graphics objects to the scene</summary>
		public void BuildScene()
		{
			if (SettingsData.Settings.Chart.ShowMarketDepth)
			{
				Gfx = Gfx ?? UpdateGfx();
				if (Gfx != null)
				{
					// Market depth graphics created at x = 0. Position the depth info at the far right of the chart.
					Gfx.O2P = m4x4.Translation((float)Chart.XAxis.Max, 0f, CandleChart.ZOrder.Indicators);
					Chart.Scene.AddObject(Gfx);
				}

				// Add the scale indicator
				Chart.Overlay.Adopt(Icon);
				Chart.Overlay.Adopt(Label);

				// Add the volume at the cursor if the cross hair is visible
				if (Chart.ShowCrossHair)
				{
					var client_pt = Mouse.GetPosition(Chart.Overlay);
					var chart_pt = Chart.ClientToChart(client_pt);
					var volume = ((Chart.XAxis.Max - chart_pt.X) / Scale)._(Market.Base);

					CursorVolume.Text = volume.ToString(8, true); ;
					Canvas.SetLeft(CursorVolume, client_pt.X - CursorVolume.ActualWidth / 2);
					Canvas.SetTop(CursorVolume, 0);
					Chart.Overlay.Adopt(CursorVolume);
				}
				else
				{
					CursorVolume.Detach();
				}
			}
			else
			{
				if (Gfx != null)
					Chart.Scene.RemoveObject(Gfx);

				Icon.Detach();
				Label.Detach();
				CursorVolume.Detach();
			}
		}

		/// <summary>Update the graphics model</summary>
		private View3d.Object UpdateGfx()
		{
			// Don't bother with a separate thread. This seems fast enough already.

			// No market data? no graphics...
			var b2q = Market.B2Q;
			var q2b = Market.Q2B;
			var count_b2q = b2q.Count;
			var count_q2b = q2b.Count;
			if (count_b2q == 0 && count_q2b == 0)
				return null;

			// Find the bounds on the order book. To determine a suitable scaling
			// factor, the maximum volume at the YMax/YMin prices on the chart are needed.
			var b2q_volume = 0.0;
			var q2b_volume = 0.0;
			var visible_volume = 0.0;
			var chart_yrange = Chart.YAxis.Range;
			var price_range = RangeF.Invalid;
			foreach (var order in b2q)
			{
				var price_q2b = (double)order.PriceQ2B.ToDouble();
				price_range.Encompass(price_q2b);
				b2q_volume += order.AmountBase.ToDouble();
				if (price_q2b.Within(chart_yrange))
					visible_volume = Math.Max(visible_volume, b2q_volume);
			}
			foreach (var order in q2b)
			{
				var price_q2b = (double)order.PriceQ2B.ToDouble();
				price_range.Encompass(price_q2b);
				q2b_volume += order.AmountBase.ToDouble();
				if (price_q2b.Within(chart_yrange))
					visible_volume = Math.Max(visible_volume, q2b_volume);
			}

			// Round the max visible volume to an aesthetic number
			var scale_volume = Math_.AestheticValue(visible_volume, -1);
			Label.Text = $"{scale_volume} {Market.Base}";

			// Scale the depth graphics so that 'scale_volume' extends 'IndicatorPosition' from the righthand edge of the chart.
			Scale = IndicatorPosition * Chart.XAxis.Span / scale_volume;

			// Update the graphics model
			// Model is a histogram from right to left.
			// Create triangles, 2 per order
			//  [..---''']
			//      [.--']
			//         [/] = q2b
			//          [] = Spot prices
			//       [   ] = b2q
			// [         ]

			// Note: B2Q = all offers to "sell" Base for Quote, i.e. they are the offers available to buyers.
			var b2q_colour = SettingsData.Settings.Chart.Q2BColour.Alpha(0.5f);
			var q2b_colour = SettingsData.Settings.Chart.B2QColour.Alpha(0.5f);

			// Resize the cache buffers
			var count = count_b2q + count_q2b;
			m_vbuf.Resize(4 * count);
			m_ibuf.Resize(6 * count);
			m_nbuf.Resize(1);

			var vert = 0;
			var indx = 0;

			//  b2q = /\
			var volume = 0.0;
			for (int i = 0; i != count_b2q; ++i)
			{
				volume += b2q[i].AmountBase.ToDouble();
				var y1 = (float)b2q[i].PriceQ2B.ToDouble();
				var y0 = i + 1 != count_b2q ? (float)b2q[i + 1].PriceQ2B.ToDouble() : y1;
				var dx = (float)(Scale * volume);
				var v = vert;

				m_vbuf[vert++] = new View3d.Vertex(new v4(-dx, y1, 0, 1f), b2q_colour);
				m_vbuf[vert++] = new View3d.Vertex(new v4(0, y1, 0, 1f), b2q_colour);
				m_vbuf[vert++] = new View3d.Vertex(new v4(-dx, y0, 0, 1f), b2q_colour);
				m_vbuf[vert++] = new View3d.Vertex(new v4(0, y0, 0, 1f), b2q_colour);

				m_ibuf[indx++] = (ushort)(v + 0);
				m_ibuf[indx++] = (ushort)(v + 2);
				m_ibuf[indx++] = (ushort)(v + 3);
				m_ibuf[indx++] = (ushort)(v + 3);
				m_ibuf[indx++] = (ushort)(v + 1);
				m_ibuf[indx++] = (ushort)(v + 0);
			}

			// q2b = \/
			volume = 0.0;
			for (int i = 0; i != count_q2b; ++i)
			{
				volume += q2b[i].AmountBase.ToDouble();
				var y0 = (float)q2b[i].PriceQ2B.ToDouble();
				var y1 = i + 1 != count_q2b ? (float)q2b[i + 1].PriceQ2B.ToDouble() : y0;
				var dx = (float)(Scale * volume);
				var v = vert;

				m_vbuf[vert++] = new View3d.Vertex(new v4(-dx, y1, 0, 1f), q2b_colour);
				m_vbuf[vert++] = new View3d.Vertex(new v4(0, y1, 0, 1f), q2b_colour);
				m_vbuf[vert++] = new View3d.Vertex(new v4(-dx, y0, 0, 1f), q2b_colour);
				m_vbuf[vert++] = new View3d.Vertex(new v4(0, y0, 0, 1f), q2b_colour);

				m_ibuf[indx++] = (ushort)(v + 0);
				m_ibuf[indx++] = (ushort)(v + 2);
				m_ibuf[indx++] = (ushort)(v + 3);
				m_ibuf[indx++] = (ushort)(v + 3);
				m_ibuf[indx++] = (ushort)(v + 1);
				m_ibuf[indx++] = (ushort)(v + 0);
			}

			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert | View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)indx, View3d.ENuggetFlag.GeometryHasAlpha);
			var gfx = new View3d.Object("MarketDepth", 0xFFFFFFFF, vert, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), CtxId);
			gfx.Flags = View3d.EFlags.SceneBoundsExclude;

			// Update the graphics model in the GUI thread
			return gfx;
		}

		/// <summary>Invalidate the graphics</summary>
		public void Invalidate()
		{
			Gfx = null;
		}

		/// <summary>Mouse op for dragging the scale indicator</summary>
		private class DragMarketSizeScaler :ChartControl.MouseOp
		{
			private double m_grab_position;

			public DragMarketSizeScaler(MarketDepth owner)
				:base(owner.Chart, allow_cancel: true)
			{
				Owner = owner;
				m_grab_position = Owner.IndicatorPosition;
			}
			private MarketDepth Owner { get; }
			public override void MouseMove(MouseEventArgs e)
			{
				// Get the mouse position as a fraction of the horizontal range
				if (Cancelled) return;
				var pt = e.GetPosition(Owner.Chart.Overlay);
				Owner.IndicatorPosition = Math_.Clamp(1.0 - pt.X / Chart.SceneBounds.Width, 0, 1);
			}
			public override void NotifyCancelled()
			{
				Owner.IndicatorPosition = m_grab_position;
			}
		}
	}
}

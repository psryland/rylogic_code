using System;
using System.Windows;
using System.Windows.Controls;
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
		public MarketDepth(CoinFlip.MarketDepth market)
		{
			Market = market;

			ScaleIndicator = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Background = Brushes.White,
				//Cursor = "SizeWE"
			};
			ScaleIndicator.Children.Add(new Polygon
			{
				Points = PointCollection.Parse("0,10 5,5 -5,5"),
				Stroke = Brushes.Black,
			});
			ScaleIndicator.Children.Add(new TextBlock
			{
				Text = "100 BTC",
				FontSize = 10.0,
			});
		}
		public override void Dispose()
		{
			Gfx = null;
			Market = null;
			ScaleIndicator = null;
			base.Dispose();
		}

		/// <summary>The market whose depth we're drawing</summary>
		private CoinFlip.MarketDepth Market
		{
			get { return m_market; }
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

		/// <summary>The maximum volume in the depth market (defines the scale)</summary>
		private decimal MaxVolume { get; set; }

		/// <summary>The graphics object</summary>
		private View3d.Object Gfx
		{
			get { return m_gfx ?? (m_gfx = UpdateGfx()); }
			set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx;

		/// <summary>The text label that indicates the volume</summary>
		private StackPanel ScaleIndicator
		{
			get => m_scale_indicator;
			set
			{
				if (m_scale_indicator == value) return;
				if (m_scale_indicator?.Parent is Canvas parent)
					parent.Children.Remove(m_scale_indicator);
				m_scale_indicator = value;
			}
		}
		private StackPanel m_scale_indicator;

		/// <summary>Invalidate the graphics</summary>
		public void Invalidate()
		{
			Gfx = null;
		}

		/// <summary>Add the graphics objects to the scene</summary>
		public void BuildScene(ChartControl chart, View3d.Window window, Canvas overlay)
		{
			if (Gfx != null)
			{
				// Market depth graphics created at x = 0 with an aspect ratio of 1:2.
				// Position the depth info at the far right of the chart
				var x_scale = (float)(0.25 * chart.XAxis.Span / chart.YAxis.Span);
				Gfx.O2P = m4x4.Translation((float)chart.XAxis.Max, 0f, CandleChart.ZOrder.Indicators) * m4x4.Scale(x_scale, 1f, 1f, v4.Origin);
				window.AddObject(Gfx);
			}

			// Add the scale indicator
			//Misc.AddToOverlay(ScaleIndicator, overlay);
			//
		}

		/// <summary>Update the graphics model</summary>
		private View3d.Object UpdateGfx()
		{
			// Don't bother with a separate thread. This seems fast enough already.
			var market = Market;

			// Not market data? no graphics...
			var b2q = market.B2Q;
			var q2b = market.Q2B;
			var count_b2q = b2q.Count;
			var count_q2b = q2b.Count;
			if (count_b2q == 0 && count_q2b == 0)
				return null;

			// Find the bounds on the order book
			var b2q_volume = 0.0;
			var q2b_volume = 0.0;
			var price_range = RangeF.Invalid;
			foreach (var order in b2q)
			{
				price_range.Encompass(order.Price);
				b2q_volume += order.AmountBase;
			}
			foreach (var order in q2b)
			{
				price_range.Encompass(order.Price);
				q2b_volume += order.AmountBase;
			}

			var max_volume = Math.Max(b2q_volume, q2b_volume);

			// Update the graphics model
			// Model is a histogram from right to left.
			// Create triangles, 2 per order
			//  [..---''']
			//      [.--']
			//         [/] = q2b
			//          [] = Spot prices
			//       [   ] = b2q
			// [         ]

			// Scale the depth graphics so the width/height ratio is 0.5;
			var count = count_b2q + count_q2b;
			var width_scale = 0.5 * price_range.Size / max_volume;

			// Note: B2Q = all offers to "sell" Base for Quote, i.e. they are the offers available to buyers.
			var b2q_colour = SettingsData.Settings.Chart.Q2BColour.Alpha(0.5f);
			var q2b_colour = SettingsData.Settings.Chart.B2QColour.Alpha(0.5f);

			// Resize the cache buffers
			m_vbuf.Resize(4 * count);
			m_ibuf.Resize(6 * count);
			m_nbuf.Resize(1);

			var vert = 0;
			var indx = 0;

			//  b2q = /\
			var volume = 0.0;
			for (int i = 0; i != count_b2q; ++i)
			{
				volume += b2q[i].AmountBase;
				var y1 = (float)b2q[i].Price;
				var y0 = i + 1 != count_b2q ? (float)b2q[i + 1].Price : y1;
				var dx = (float)(width_scale * volume);
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
				volume += q2b[i].AmountBase;
				var y0 = (float)q2b[i].Price;
				var y1 = i + 1 != count_q2b ? (float)q2b[i + 1].Price : y0;
				var dx = (float)(width_scale * volume);
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

			m_nbuf[0] = new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert | View3d.EGeom.Colr, 0, (uint)vert, 0, (uint)indx, true);
			var gfx = new View3d.Object("MarketDepth", 0xFFFFFFFF, vert, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray(), CandleChart.CtxId);
			gfx.Flags = View3d.EFlags.SceneBoundsExclude;

			// Update the graphics model in the GUI thread
			return gfx;
		}
	}
}

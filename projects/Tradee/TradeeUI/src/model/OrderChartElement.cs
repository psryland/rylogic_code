using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.ldr;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Base class for trades on a chart</summary>
	public class OrderChartElement :ChartElement
	{
		public OrderChartElement(Order order)
			:base(Guid.NewGuid(), "Order {0}".Fmt(order.Id))
		{
			Order = order;
		}
		protected override void Dispose(bool disposing)
		{
			Order = null;
			Gfx = null;
			base.Dispose(disposing);
		}
		protected override void SetChartCore(ChartControl chart)
		{
			// Invalidate the graphics whenever the x axis zooms or scrolls
			if (Chart != null)
			{
				Chart.XAxis.Scroll -= Invalidate;
				Chart.XAxis.Zoomed -= Invalidate;
			}
			base.SetChartCore(chart);
			if (Chart != null)
			{
				Chart.XAxis.Scroll += Invalidate;
				Chart.XAxis.Zoomed += Invalidate;
			}
		}
		protected override void UpdateGfxCore()
		{
			// Find the index range in the Instrument history that 'Order' maps to
			var t0 = new TFTime(Order.EntryTimeUTC.Ticks, Instrument.TimeFrame);
			var t1 = new TFTime(Order.ExitTimeUTC .Ticks, Instrument.TimeFrame);
			var range = Instrument.TimeToIndexRange(t0, t1);

			// If the expiry is in the future, add on the expected number of candles
			var t2 = t1.ExactUTC > Instrument.Latest.TimestampUTC
				? (int)new TFTime(t1.ExactUTC - Instrument.Latest.TimestampUTC, Instrument.TimeFrame).IntgTF : 0;

			// Determine the width and position of the order
			var x = (float)(range.Begi - range.Endi);
			var w = (float)(range.Sizei + t2);

			// Get the entry price, stop loss, and take profit values
			var ep = (float)Order.EntryPrice;
			var sl = (float)Order.StopLossRel;
			var tp = (float)Order.TakeProfitRel;

			// Create graphics for the order
			switch (Order.TradeType)
			{
			default:
				{
					Debug.Assert(false, "Unknown trade type");
					Gfx = null;
					break;
				}
			case ETradeType.None:
				{
					Gfx = null;
					break;
				}
			case ETradeType.Short:
			case ETradeType.Long:
				{
					var sign     = Order.TradeType == ETradeType.Long ? +1 : -1;
					var selected = Selected || Hovered;
					var alpha    = selected ? 0.5f : 0.25f;
					var z        = ChartUI.Z.Trades + (selected ? 0.0001f : 0f);
					var sl_col   = Settings.Chart.TradeLossColour  .Alpha(alpha);
					var tp_col   = Settings.Chart.TradeProfitColour.Alpha(alpha);
					var ep_col   = Color.DarkBlue;
					var line_col = Order.TradeType == ETradeType.Long ? Settings.UI.BullishColour.Alpha(0x60) : Settings.UI.BearishColour.Alpha(0x60);
					var xmin     = selected ? (float)Chart.XAxis.Min : x - 0;
					var xmax     = selected ? (float)Chart.XAxis.Max : x + w;
		
					m_vbuf.Clear();
					m_ibuf.Clear();
					m_nbuf.Clear();
					var V = 0U;
					var I = 0U;

					// Take profit area
					if (tp > 0)
					{
						var v = m_vbuf.Count;
						var y0 = sign > 0 ? Math.Max(ep, ep - sl) : ep - tp;
						var y1 = sign > 0 ? ep + tp : Math.Min(ep, ep + sl);
						m_vbuf.Add(new View3d.Vertex(new v4(x    , y0, z, 1), tp_col.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(x + w, y0, z, 1), tp_col.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(x    , y1, z, 1), tp_col.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(x + w, y1, z, 1), tp_col.ToArgbU()));
						m_ibuf.AddRange(new[] { (ushort)v, (ushort)(v+1), (ushort)(v+3), (ushort)(v+3), (ushort)(v+2), (ushort)v});
					}
					// Stop loss area
					if (sl > 0)
					{
						var v = m_vbuf.Count;
						var y0 = sign > 0 ? ep - sl : Math.Max(ep, ep - tp);
						var y1 = sign > 0 ? Math.Min(ep, ep + tp) : ep + sl;
						m_vbuf.Add(new View3d.Vertex(new v4(x    , y0, z, 1), sl_col.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(x + w, y0, z, 1), sl_col.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(x    , y1, z, 1), sl_col.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(x + w, y1, z, 1), sl_col.ToArgbU()));
						m_ibuf.AddRange(new[] { (ushort)v, (ushort)(v+1), (ushort)(v+3), (ushort)(v+3), (ushort)(v+2), (ushort)v});
					}
					m_nbuf.Add(new View3d.Nugget(View3d.EPrim.TriList, View3d.EGeom.Vert|View3d.EGeom.Colr, V, (uint)m_vbuf.Count, I, (uint)m_ibuf.Count, true));
					V = (uint)m_vbuf.Count;
					I = (uint)m_ibuf.Count;

					// Entry Price line
					{
						var v = m_vbuf.Count;
						m_vbuf.Add(new View3d.Vertex(new v4(xmin, ep, z, 1), Color.DarkBlue.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(xmax, ep, z, 1), Color.DarkBlue.ToArgbU()));
						m_ibuf.AddRange(new[] { (ushort)v, (ushort)(v+1) });

					}
					// Take Profit line
					{
						var v = m_vbuf.Count;
						m_vbuf.Add(new View3d.Vertex(new v4(xmin, ep + sign*tp, z, 1), Color.DarkGreen.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(xmax, ep + sign*tp, z, 1), Color.DarkGreen.ToArgbU()));
						m_ibuf.AddRange(new[] { (ushort)v, (ushort)(v+1) });
					}
					// Stop Loss line
					{
						var v = m_vbuf.Count;
						m_vbuf.Add(new View3d.Vertex(new v4(xmin, ep - sign*sl, z, 1), Color.DarkRed.ToArgbU()));
						m_vbuf.Add(new View3d.Vertex(new v4(xmax, ep - sign*sl, z, 1), Color.DarkRed.ToArgbU()));
						m_ibuf.AddRange(new[] { (ushort)v, (ushort)(v+1) });
					}
					m_nbuf.Add(new View3d.Nugget(View3d.EPrim.LineList, View3d.EGeom.Vert|View3d.EGeom.Colr, V, (uint)m_vbuf.Count, I, (uint)m_ibuf.Count, true));

					// Create the geometry
					Gfx = new View3d.Object("Order", 0xFFFFFFFF, m_vbuf.Count, m_ibuf.Count, m_nbuf.Count, m_vbuf.ToArray(), m_ibuf.ToArray(), m_nbuf.ToArray());
					break;
				}
			}
			base.UpdateGfxCore();
		}
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx == null) return;
			window.AddObject(Gfx);
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			get { return Order.Instrument.Model.Settings; }
		}

		/// <summary>The trade represented by this chart element</summary>
		public Order Order
		{
			[DebuggerStepThrough] get { return m_order; }
			set
			{
				if (m_order == value) return;
				if (m_order != null)
				{
					m_order.Changed -= Invalidate;
					Instrument = null;
				}
				m_order = value;
				if (m_order != null)
				{
					Instrument = m_order.Instrument;
					m_order.Changed += Invalidate;
				}
				Invalidate();
			}
		}
		private Order m_order;

		/// <summary>The graphics object for this trade</summary>
		private View3d.Object Gfx
		{
			get { return m_impl_gfx; }
			set
			{
				if (m_impl_gfx == value) return;
				Util.Dispose(ref m_impl_gfx);
				m_impl_gfx = value;
			}
		}
		private View3d.Object m_impl_gfx;

		/// <summary>Hittable locations on this graphic</summary>
		public EHitSite HitSite
		{
			[DebuggerStepThrough] get { return m_hovered_zone; }
			set { SetProp(ref m_hovered_zone, value, nameof(HitSite), true, true); }
		}
		private EHitSite m_hovered_zone;
		public enum EHitSite
		{
			None,
			EntryPrice,
			StopLoss,
			TakeProfit,
			EntryTime,
			Expiry,
		}

		/// <summary>Hit test this order</summary>
		public override ChartControl.HitTestResult.Hit HitTest(PointF chart_point, Point client_point, Keys modifier_keys, View3d.CameraControls cam)
		{
			// Chart elements can only be modified when selected
			// Chart elements can only be selected when control is held down

			// Only visible to hit if selected
			if (!Selected)
				return null;

			// This Order graphic is hit if the mouse is over one of the control lines.
			// Hit testing is done in client space so we can use pixel tolerance
			Func<EHitSite, ChartControl.HitTestResult.Hit> Hit = site =>
			{
				var elem_point = m4x4.Invert(Position) * new v4(chart_point, 0f, 1f);
				return new ChartControl.HitTestResult.Hit(this, new PointF(elem_point.x, elem_point.y), site);
			};
			const int tolerance = 5;

			// Test the price levels
			if (Order.State.CanMoveEntryPrice())
			{
				var client_ep = Chart.ChartToClient(new PointF((float)Chart.XAxis.Min, (float)Order.EntryPrice));
				if (Math.Abs(client_ep.Y - client_point.Y) < tolerance)
					return Hit(EHitSite.EntryPrice);
			}
			if (Order.State.CanMoveStopLoss())
			{
				var client_sl = Chart.ChartToClient(new PointF((float)Chart.XAxis.Min, (float)Order.StopLossAbs));
				if (Math.Abs(client_sl.Y - client_point.Y) < tolerance)
					return Hit(EHitSite.StopLoss);
			}
			if (Order.State.CanMoveTakeProfit())
			{
				var client_tp = Chart.ChartToClient(new PointF((float)Chart.XAxis.Min, (float)Order.TakeProfitAbs));
				if (Math.Abs(client_tp.Y - client_point.Y) < tolerance)
					return Hit(EHitSite.TakeProfit);
			}

			// If the order is still modifiable, test the entry/exit times
			if (Order.State.CanMoveEntryTime())
			{
				var x = Instrument.ChartXValue(new TFTime(Order.EntryTimeUTC, Instrument.TimeFrame));
				var client_x = Chart.ChartToClient(new PointF((float)x, (float)Chart.YAxis.Min));
				if (Math.Abs(client_x.X - client_point.X) < tolerance)
					return Hit(EHitSite.EntryTime);
			}
			if (Order.State.CanMoveExpiry())
			{
				var x  = Instrument.ChartXValue(new TFTime(Order.ExitTimeUTC, Instrument.TimeFrame));
				var client_x = Chart.ChartToClient(new PointF((float)x, (float)Chart.YAxis.Min));
				if (Math.Abs(client_x.X - client_point.X) < tolerance)
					return Hit(EHitSite.Expiry);
			}

			// No hit...
			return null;
		}

		/// <summary>Handle mouse click</summary>
		public override void HandleClicked(ChartControl.ChartClickedEventArgs args)
		{
			var hit  = args.HitResult.Hits.First(x => x.Element == this);
			var site = (EHitSite)hit.Context;
			switch (site)
			{
			default: throw new Exception("Unknown hit location on OrderChartElement");
			case EHitSite.EntryPrice:
				{
					if (Order.State.CanMoveEntryPrice())
					{
						// Mouse op to move the entry price
					}
					break;
				}
			case EHitSite.StopLoss:
				{
					if (Order.State.CanMoveStopLoss())
					{
						Chart.MouseOperations.SetPending(MouseButtons.Left, new MouseOpMovePriceLevel(Chart));
					}
					break;
				}
			case EHitSite.TakeProfit:
				{
					break;
				}
			case EHitSite.EntryTime:
				{
					break;
				}
			case EHitSite.Expiry:
				{
					break;
				}
			}
		}


		/// <summary>A mouse operation for moving an Order's price level</summary>
		private class MouseOpMovePriceLevel :ChartControl.MouseOp
		{
			public MouseOpMovePriceLevel(ChartControl chart)
				:base(chart)
			{
			}
			public override void MouseDown(MouseEventArgs e)
			{
				throw new NotImplementedException();
			}

			public override void MouseMove(MouseEventArgs e)
			{
				throw new NotImplementedException();
			}

			public override void MouseUp(MouseEventArgs e)
			{
				throw new NotImplementedException();
			}
		}
	}
}

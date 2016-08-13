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
	public class OrderChartElement :ChartControl.Element
	{
		public OrderChartElement(Order order)
			:base(Guid.NewGuid(), m4x4.Identity, "Order {0}".Fmt(order.Id))
		{
			Order = order;
			Highlighted = false;
			VisibleToFindRange = false;
		}
		protected override void Dispose(bool disposing)
		{
			Order = null;
			Gfx = null;
			base.Dispose(disposing);
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
				}
				m_order = value;
				if (m_order != null)
				{
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

		/// <summary>The instrument associated with the trade</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return Order.Instrument; }
		}

		/// <summary>True if this Order is the centre of attention</summary>
		public bool Highlighted
		{
			[DebuggerStepThrough] get { return m_highlight; }
			set { SetProp(ref m_highlight, value, nameof(Highlighted), true, true); }
		}
		private bool m_highlight;

		/// <summary>Generate the graphics for this trade type</summary>
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
			var x = range.Begini - range.Endi;
			var w = range.Sizei + t2;

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
					return;
				}
			case ETradeType.None:
				{
					Gfx = null;
					break;
				}
			case ETradeType.Short:
			case ETradeType.Long:
				{
					var ldr = new LdrBuilder();
					using (ldr.Group("Order"))
					{
						var sign     = Order.TradeType == ETradeType.Long ? +1 : -1;
						var line_col = Order.TradeType == ETradeType.Long ? Settings.UI.BullishColour.Alpha(0x60) : Settings.UI.BearishColour.Alpha(0x60);
						var alpha    = Hovered || Highlighted ? 0x80 : 0x40;
						var sl_col   = Settings.Chart.TradeLossColour  .Alpha(alpha);
						var tp_col   = Settings.Chart.TradeProfitColour.Alpha(alpha);

						// Draw the TP and SL regions
						ldr.Rect("take_profit", tp_col, AxisId.PosZ, w, Math.Abs(tp), true, new v4(x + w/2f, ep + sign*tp/2f, ChartUI.Z.Trades, 1));
						ldr.Rect("stop_loss"  , sl_col, AxisId.PosZ, w, Math.Abs(sl), true, new v4(x + w/2f, ep - sign*sl/2f, ChartUI.Z.Trades, 1));

						// Add lines for the EntryPrice, TP and SL levels
						ldr.Line("entry_price", line_col, new v4((float)Chart.XAxis.Min, ep          , ChartUI.Z.Trades, 1), new v4((float)Chart.XAxis.Max, ep          , ChartUI.Z.Trades, 1));
						ldr.Line("take_profit", line_col, new v4((float)Chart.XAxis.Min, ep + sign*tp, ChartUI.Z.Trades, 1), new v4((float)Chart.XAxis.Max, ep + sign*tp, ChartUI.Z.Trades, 1));
						ldr.Line("stop_loss"  , line_col, new v4((float)Chart.XAxis.Min, ep - sign*sl, ChartUI.Z.Trades, 1), new v4((float)Chart.XAxis.Max, ep - sign*sl, ChartUI.Z.Trades, 1));
					}
					Gfx = new View3d.Object(ldr.ToString(), file: false);
					break;
				}
			}
			base.UpdateGfxCore();
		}

		/// <summary>Add our graphics to the chart during render</summary>
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx == null) return;
			window.AddObject(Gfx);
		}

		/// <summary>Hit test this order</summary>
		public override ChartControl.HitTestResult.Hit HitTest(Point client_point, View3d.CameraControls cam)
		{
			// This Order graphic is hit if the mouse is over one of the control lines.
			// Hit testing is done in client space so we can use pixel tolerance
			Func<EHitSite, ChartControl.HitTestResult.Hit> Hit = site =>
			{
				var elem_point = m4x4.Invert(Position) * new v4(Chart.ClientToChart(client_point), 0f, 1f);
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
			}
		}

		/// <summary>Hittable locations on this graphic</summary>
		private enum EHitSite
		{
			EntryPrice,
			StopLoss,
			TakeProfit,
			EntryTime,
			Expiry,
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

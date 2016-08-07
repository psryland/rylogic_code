using System;
using System.Diagnostics;
using System.Drawing;
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
		public OrderChartElement(Order order, ChartControl chart_ctrl)
			:base(Guid.NewGuid(), m4x4.Identity)
		{
			Order = order;
			Chart = chart_ctrl;
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
			get { return m_order; }
			set
			{
				if (m_order == value) return;
				if (m_order != null)
				{
					m_order.PropertyChanged -= Invalidate;
				}
				m_order = value;
				if (m_order != null)
				{
					m_order.PropertyChanged += Invalidate;
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
			get { return Order.Instrument; }
		}

		/// <summary>Generate the graphics for this trade type</summary>
		protected override void UpdateGfxCore()
		{
			// Determine the width of the order.
			var t_now = Instrument.Latest.TimestampUTC;
			var x0 = (float)Misc.TicksToTimeFrame((Order.EntryTimeUTC      - t_now).Ticks, Instrument.TimeFrame);
			var x1 = (float)Misc.TicksToTimeFrame((Order.ExpirationTimeUTC - t_now).Ticks, Instrument.TimeFrame);
			if (Order.State == Trade.EState.ActivePosition) x1 = Settings.Chart.ViewCandlesAhead;
			var w = x1 - x0;

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
			case ETradeType.Long:
				{
					var ldr = new LdrBuilder();
					using (ldr.Group("Order"))
					{
						// Draw the TP and SL regions
						ldr.Rect("take_profit", Settings.Chart.TradeProfitColour, AxisId.PosZ, w, Math.Abs(tp), true, new v4(x0, ep + tp/2f, ChartUI.Z.Trades, 1));
						ldr.Rect("stop_loss"  , Settings.Chart.TradeLossColour  , AxisId.PosZ, w, Math.Abs(sl), true, new v4(x0, ep - sl/2f, ChartUI.Z.Trades, 1));

						// Add lines for the EntryPrice, TP and SL levels
						ldr.Line("entry_price", Settings.UI.BullishColour.Alpha(0x60), new v4((float)Chart.XAxis.Min, ep     , ChartUI.Z.Trades, 1), new v4((float)Chart.XAxis.Max, ep     , ChartUI.Z.Trades, 1));
						ldr.Line("take_profit", Settings.UI.BullishColour.Alpha(0x60), new v4((float)Chart.XAxis.Min, ep + tp, ChartUI.Z.Trades, 1), new v4((float)Chart.XAxis.Max, ep + tp, ChartUI.Z.Trades, 1));
						ldr.Line("stop_loss"  , Settings.UI.BullishColour.Alpha(0x60), new v4((float)Chart.XAxis.Min, ep - sl, ChartUI.Z.Trades, 1), new v4((float)Chart.XAxis.Max, ep - sl, ChartUI.Z.Trades, 1));
					}
					Gfx = new View3d.Object(ldr.ToString(), file: false);
					break;
				}
			case ETradeType.Short:
				{
					var ldr = new LdrBuilder();
					using (ldr.Group("Order"))
					{
						ldr.Rect("take_profit", Color.FromArgb(0x80, Settings.UI.BullishColour), AxisId.PosZ, w, Math.Abs(tp), true, new v4(0f, -tp/2f, 0, 1));
						ldr.Rect("stop_loss"  , Color.FromArgb(0x80, Settings.UI.BearishColour), AxisId.PosZ, w, Math.Abs(sl), true, new v4(0f, +sl/2f, 0, 1));
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
	}
}

using System;
using System.Diagnostics;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Base class for trades on a chart</summary>
	public class TradeChartElement :ChartControl.Element
	{
		public TradeChartElement(Trade trade)
			:base(Guid.NewGuid(), m4x4.Identity)
		{
			Trade = trade;
		}
		protected override void Dispose(bool disposing)
		{
			Trade = null;
			Gfx = null;
			base.Dispose(disposing);
		}

		/// <summary>The trade represented by this chart element</summary>
		public Trade Trade
		{
			get { return m_trade; }
			set
			{
				if (m_trade == value) return;
				if (m_trade != null)
				{
					m_trade.TradeTypeChanged -= Invalidate;
				}
				m_trade = value;
				if (m_trade != null)
				{
					m_trade.TradeTypeChanged += Invalidate;
				}
				Invalidate();
			}
		}
		private Trade m_trade;

		/// <summary>The graphics object for this trade</summary>
		private View3d.Object Gfx
		{
			get { return m_impl_gfx; }
			set
			{
				if (m_impl_gfx == value) return;
				if (m_impl_gfx != null)
				{
					Util.Dispose(ref m_impl_gfx);
				}
				m_impl_gfx = value;
			}
		}
		private View3d.Object m_impl_gfx;

		/// <summary>Generate the graphics for this trade type</summary>
		protected override void UpdateGfxCore()
		{
			switch (Trade.TradeType)
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
			case ETradeType.Long:
				{
					Gfx = new View3d.Object("*Quad trade FF00FF00 { 1.001 100 }", file:false);
					break;
				}
			case ETradeType.Short:
				{
					break;
				}
			}
			base.UpdateGfxCore();
		}

		/// <summary>Add our graphics to the chart during render</summary>
		protected override void AddToSceneCore(View3d.Window window)
		{
			if (Gfx != null)
				window.AddObject(Gfx);
			base.AddToSceneCore(window);
		}
	}
}

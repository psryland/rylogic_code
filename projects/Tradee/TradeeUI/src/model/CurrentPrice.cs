using System;
using System.Diagnostics;
using pr.gfx;
using pr.gui;
using pr.ldr;
using pr.maths;
using pr.util;

namespace Tradee
{
	public class CurrentPrice :ChartControl.Element
	{
		public CurrentPrice(MainModel model, Instrument instrument)
			:base(Guid.NewGuid(), m4x4.Identity)
		{
			Model = model;
			Instrument = instrument;
			VisibleToFindRange = false;
		}
		protected override void Dispose(bool disposing)
		{
			Instrument = null;
			GfxAsk = null;
			GfxBid = null;
			base.Dispose(disposing);
		}
		public override void SetChartCore(ChartControl chart)
		{
			// Invalidate the graphics whenever the x axis zooms
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
			// Create lines that fit the chart
			var w = (float)Chart.XAxis.Span;
			GfxBid = new View3d.Object(Ldr.Line("BidPrice", Model.Settings.UI.BidColour, v4.Origin, new v4(-w, 0, 0, 1)), file:false);
			GfxAsk = new View3d.Object(Ldr.Line("AskPrice", Model.Settings.UI.AskColour, v4.Origin, new v4(-w, 0, 0, 1)), file:false);
			base.UpdateGfxCore();
		}
		protected override void AddToSceneCore(View3d.Window window)
		{
			var pd = Instrument.PriceData;
			if (GfxAsk != null)
			{
				GfxAsk.O2P = m4x4.Translation((float)Chart.XAxis.Max, (float)pd.AskPrice, 0);
				window.AddObject(GfxAsk);
			}
			if (GfxBid != null)
			{
				GfxBid.O2P = m4x4.Translation((float)Chart.XAxis.Max, (float)pd.BidPrice, 0);
				window.AddObject(GfxBid);
			}
		}

		/// <summary>The App model</summary>
		public MainModel Model
		{
			get;
			private set;
		}

		/// <summary>Access to the market data</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instrument; }
			private set
			{
				if (m_instrument == value) return;
				if (m_instrument != null)
				{
					m_instrument.PriceDataUpdated -= UpdateGfx;
				}
				m_instrument = value;
				if (m_instrument != null)
				{
					m_instrument.PriceDataUpdated += UpdateGfx;
				}
			}
		}
		private Instrument m_instrument;

		/// <summary>The Ask price line</summary>
		public View3d.Object GfxAsk
		{
			[DebuggerStepThrough] get { return m_impl_gfx_ask; }
			set
			{
				if (m_impl_gfx_ask == value) return;
				Util.Dispose(ref m_impl_gfx_ask);
				m_impl_gfx_ask = value;
			}
		}
		private View3d.Object m_impl_gfx_ask;

		/// <summary>The Bid price line</summary>
		public View3d.Object GfxBid
		{
			[DebuggerStepThrough] get { return m_impl_gfx_bid; }
			set
			{
				if (m_impl_gfx_bid == value) return;
				Util.Dispose(ref m_impl_gfx_bid);
				m_impl_gfx_bid = value;
			}
		}
		private View3d.Object m_impl_gfx_bid;
	}
}

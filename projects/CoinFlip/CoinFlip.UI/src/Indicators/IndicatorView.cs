using System;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace CoinFlip.UI.Indicators
{
	public abstract class IndicatorView :ChartControl.Element, IIndicatorView
	{
		public IndicatorView(Guid id, string name, IChartView chart, IIndicator indicator)
			: base(id, m4x4.Identity, name)
		{
			Indicator = indicator;
			Instrument = chart.Instrument;
			if (chart is CandleChart candle_chart)
				Chart = candle_chart.Chart;
		}
		public override void Dispose()
		{
			Instrument = null;
			Indicator = null;
			base.Dispose();
		}

		/// <summary>The size in pixel to 'glow' by</summary>
		public const double GlowRadius = 10.0;

		/// <summary>The Id of the indicator instance</summary>
		public Guid IndicatorId => Id;

		/// <summary>The indicator this view is based on</summary>
		public IIndicator Indicator
		{
			get => m_indicator;
			private set
			{
				if (m_indicator == value) return;
				if (m_indicator != null)
				{
					m_indicator.SettingChange -= HandleSettingChange;
				}
				m_indicator = value;
				if (m_indicator != null)
				{
					m_indicator.SettingChange += HandleSettingChange;
				}
			}
		}
		private IIndicator m_indicator;
		protected virtual void HandleSettingChange(object sender, SettingChangeEventArgs args)
		{ }

		/// <summary></summary>
		public Colour32 Colour => Indicator.Colour;

		/// <summary>The instrument that this indicator is associated with</summary>
		public Instrument Instrument
		{
			get { return m_instrument; }
			set
			{
				if (m_instrument == value) return;
				SetInstrumentCore(value);
			}
		}
		private Instrument m_instrument;
		protected virtual void SetInstrumentCore(Instrument instr)
		{
			m_instrument = instr;
		}

		/// <summary>Update per-frame elements</summary>
		void IIndicatorView.BuildScene(IChartView chart)
		{
			// Not needed for ChartControl.Element types, they use
			// UpdateSceneCore and UpdateGfxCore instead.
		}
	}
}

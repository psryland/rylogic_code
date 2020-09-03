using System;
using System.ComponentModel;
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
			Visible = indicator.Visible;

			PropertyChanged += HandlePropertyChanged;
		}
		protected override void Dispose(bool disposing)
		{
			PropertyChanged -= HandlePropertyChanged;
			Instrument = null;
			Indicator = null;
			base.Dispose(disposing);
		}

		/// <summary>The size in pixels to 'glow' by</summary>
		public const double GlowRadius = 10.0;

		/// <summary>The size in pixels of the grab handles</summary>
		public const double GrabRadius = 5.0;

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

		/// <summary>The string description of the indicator</summary>
		public string Label => Indicator.Label;

		/// <summary>Indicator colour</summary>
		public override Colour32 Colour => Indicator.Colour;

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

		/// <summary>Handle properties changing</summary>
		protected virtual void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
			case nameof(ChartControl.Element.Visible):
				{
					Indicator.Visible = Visible;
					break;
				}
			}
		}

		/// <summary>Handle indicator settings changing</summary>
		protected virtual void HandleSettingChange(object sender, SettingChangeEventArgs args)
		{
			if (args.After)
			{
				switch (args.Key)
				{
				case nameof(IIndicator.Name):
					NotifyPropertyChanged(nameof(Label));
					break;
				case nameof(IIndicator.Label):
					NotifyPropertyChanged(nameof(Label));
					break;
				case nameof(IIndicator.Colour):
					NotifyPropertyChanged(nameof(Colour));
					break;
				}
			}
		}

		/// <summary>Handle double click to show the UI</summary>
		protected override void HandleClicked(ChartControl.ChartClickedEventArgs args)
		{
			if (Selected && args.ClickCount == 2)
			{
				ShowOptionsUI();
				args.Handled = true;
			}
		}

		/// <summary>Display the options UI</summary>
		public void ShowOptionsUI()
		{
			ShowOptionsUICore();
		}
		protected virtual void ShowOptionsUICore()
		{}

		/// <summary>Update per-frame elements</summary>
		void IIndicatorView.BuildScene(IChartView chart)
		{
			// Not needed for ChartControl.Element types, they use
			// UpdateSceneCore and UpdateGfxCore instead.
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;
using ComboBox = Rylogic.Gui.WinForms.ComboBox;

namespace CoinFlip
{
	public class ExchPairTimeFrameCombos
	{
		private Model m_model;
		private ComboBox m_cb_exchange;
		private ComboBox m_cb_pair;
		private ComboBox m_cb_timeframe;

		/// <summary>Configure two combo boxes for use as exchange, pair, time-frame drop downs</summary>
		public ExchPairTimeFrameCombos(Model model, ComboBox exch_cb, ComboBox pair_cb, ComboBox timeframe_cb)
		{
			m_model = model;
			m_cb_exchange = exch_cb ?? new ComboBox();
			m_cb_pair = pair_cb ?? new ComboBox();
			m_cb_timeframe = timeframe_cb ?? new ComboBox();

			// Combo for choosing the exchange providing the source data
			UpdateAvailableExchanges();
			m_cb_exchange.ValueType = typeof(Exchange);
			m_cb_exchange.DisplayProperty = nameof(Exchange.Name);
			m_cb_exchange.DropDown += (s, a) =>
			{
				ComboBox_.DropDownWidthAutoSize(m_cb_exchange);
				UpdateAvailableExchanges();
			};
			m_cb_exchange.DropDownClosed += (s, a) =>
			{
				UpdateAvailablePairs();
			};

			// Combo for choosing the pair to display
			UpdateAvailablePairs();
			m_cb_pair.ValueType = typeof(TradePair);
			m_cb_pair.DisplayProperty = nameof(TradePair.Name);
			m_cb_pair.DropDown += (s, a) =>
			{
				ComboBox_.DropDownWidthAutoSize(m_cb_pair);
				UpdateAvailablePairs();
			};
			m_cb_pair.SelectedIndexChanged += (s, a) =>
			{
				if (m_in_update_available_pairs != 0) return;
				UpdateAvailableTimeFrames();
				OnSelection?.Invoke(this, EventArgs.Empty);
			};

			// Combo for choosing the time frame
			UpdateAvailableTimeFrames();
			m_cb_timeframe.ValueType = typeof(ETimeFrame);
			m_cb_timeframe.TextToValue = t => Enum<ETimeFrame>.Parse(t);
			m_cb_timeframe.SelectedItem = ETimeFrame.None;
			m_cb_timeframe.DropDown += (s, a) =>
			{
				ComboBox_.DropDownWidthAutoSize(m_cb_timeframe);
				UpdateAvailableTimeFrames();
			};
			m_cb_timeframe.SelectedIndexChanged += (s, a) =>
			{
				if (m_in_update_time_frames != 0) return;
				OnSelection?.Invoke(this, EventArgs.Empty);
			};
		}

		/// <summary>Raised when the selected exchange, pair, or timeframe changes</summary>
		public event EventHandler OnSelection;

		/// <summary>The currently selected exchange</summary>
		public Exchange Exchange { get { return (Exchange)m_cb_exchange.Value; } }

		/// <summary>The currently selected pair</summary>
		public TradePair Pair { get { return (TradePair)m_cb_pair.Value; } }

		/// <summary>The currently selected timeframe</summary>
		public ETimeFrame TimeFrame { get { return (ETimeFrame)m_cb_timeframe.Value; } }

		/// <summary>Filter function for available exchanges</summary>
		public Func<Exchange, bool> FilterExchanges { get; set; }

		/// <summary>Filter function for available pairs on the selected exchange</summary>
		public Func<Exchange, TradePair, bool> FilterPairs { get; set; }

		/// <summary>Filter function for available time frames on the selected pair</summary>
		public Func<Exchange, TradePair, ETimeFrame, bool> FilterTimeframes { get; set; }

		/// <summary>Called to set the default exchange</summary>
		public Func<Exchange> DefaultExchange { get; set; }

		/// <summary>Called to set the default pair</summary>
		public Func<TradePair> DefaultPair { get; set; }

		/// <summary>Called to set the default timeframe</summary>
		public Func<ETimeFrame> DefaultTimeFrame { get; set; }

		/// <summary>The exchanges to include in the exchange drop down</summary>
		public IEnumerable<Exchange> AvailableExchanges
		{
			get { return m_model.TradingExchanges.Where(FilterExchanges ?? (x => true)); }
		}

		/// <summary>The pairs on the selected exchange with chart data available</summary>
		public IEnumerable<TradePair> AvailablePairs
		{
			get
			{
				if (m_cb_exchange.SelectedItem is Exchange exch)
				{
					var pred = FilterPairs ?? ((e, p) => true);
					foreach (var pair in exch.Pairs.Values.Where(x => pred(exch, x)))
						yield return pair;
				}
			}
		}

		/// <summary>The time frames available for the current instrument</summary>
		public IEnumerable<ETimeFrame> AvailableTimeFrames
		{
			get
			{
				yield return ETimeFrame.None;
				if (m_cb_exchange.SelectedItem is Exchange exch && m_cb_pair.SelectedItem is TradePair pair)
				{
					var pred = FilterTimeframes ?? ((e,p,t) => true);
					foreach (var tf in Enum<ETimeFrame>.Values.Where(x => pred(exch, pair, x)))
						yield return tf;
				}
			}
		}

		/// <summary>Refresh the combo data</summary>
		public void Update()
		{
			UpdateAvailableExchanges();
			UpdateAvailablePairs();
			UpdateAvailableTimeFrames();
		}

		/// <summary>Populate the combo with the available exchanges</summary>
		private void UpdateAvailableExchanges()
		{
			using (m_cb_exchange.PreserveSelectedItem())
			using (Scope.Create(() => ++m_in_update_available_exchanges, () => --m_in_update_available_exchanges))
			{
				m_cb_exchange.Items.Sync(AvailableExchanges);
				m_cb_exchange.Items.Sort();
			}
			if (m_cb_exchange.SelectedItem == null)
				m_cb_exchange.SelectedItem = DefaultExchange != null ? DefaultExchange() : AvailableExchanges.FirstOrDefault();
		}
		private int m_in_update_available_exchanges;

		/// <summary>Populate the combo with the available pairs</summary>
		private void UpdateAvailablePairs()
		{
			using (m_cb_pair.PreserveSelectedItem())
			using (Scope.Create(() => ++m_in_update_available_pairs, () => --m_in_update_available_pairs))
			{
				m_cb_pair.Items.Sync(AvailablePairs);
				m_cb_pair.Items.Sort();
			}
			if (m_cb_pair.SelectedItem == null)
				m_cb_pair.SelectedItem = DefaultPair != null ? DefaultPair() : AvailablePairs.FirstOrDefault();
		}
		private int m_in_update_available_pairs;

		/// <summary>Populate the combo with the available timeframes</summary>
		private void UpdateAvailableTimeFrames()
		{
			using (m_cb_timeframe.PreserveSelectedItem())
			using (Scope.Create(() => ++m_in_update_time_frames, () => --m_in_update_time_frames))
			{
				m_cb_timeframe.Items.Sync(AvailableTimeFrames);
				m_cb_timeframe.Items.Sort();
			}
			if ((ETimeFrame?)m_cb_timeframe.SelectedItem == ETimeFrame.None)
				m_cb_timeframe.SelectedItem = DefaultTimeFrame != null ? DefaultTimeFrame() : AvailableTimeFrames.FirstOrDefault();
		}
		private int m_in_update_time_frames;
	}
}

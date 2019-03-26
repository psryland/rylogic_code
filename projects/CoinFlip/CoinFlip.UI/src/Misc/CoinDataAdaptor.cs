using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	/// <summary>A wrapper for CoinData to provide live value data</summary>
	public class CoinDataAdaptor : INotifyPropertyChanged
	{
		private readonly Model m_model;
		private readonly ICollectionView m_exch_source;
		public CoinDataAdaptor(CoinData cd, Model model, ICollectionView exch_source)
		{
			CoinData = cd;
			m_model = model;
			m_exch_source = exch_source;

			// Notify when external data changes
			cd.LivePriceChanged += WeakRef.MakeWeak(UpdateLiveValueChanged, h => cd.LivePriceChanged -= h);
			void UpdateLiveValueChanged(object sender = null, EventArgs args = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Balance)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LiveValueAvailable)));
			}

			cd.BalanceChanged += WeakRef.MakeWeak(UpdateBalanceChanged, h => cd.BalanceChanged -= h);
			void UpdateBalanceChanged(object sender = null, EventArgs args = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Available)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Balance)));
			}

			exch_source.CurrentChanged += WeakRef.MakeWeak(UpdateSourceChanged, h => exch_source.CurrentChanged -= h);
			void UpdateSourceChanged(object sender = null, EventArgs args = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Available)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Balance)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LiveValueAvailable)));
			}
		}

		/// <summary>The wrapped coin data</summary>
		public CoinData CoinData { get; }

		/// <summary>Coin symbol code</summary>
		public string Symbol => CoinData.Symbol;

		/// <summary>Value of the coin</summary>
		public decimal Value => CoinData.ShowLivePrices ? LiveValue : CoinData.AssignedValue;

		/// <summary></summary>
		public decimal Balance => LiveValue * NettTotal;

		/// <summary>The sum of available balance across all exchanges</summary>
		public decimal Available => NettAvailable;

		/// <summary>The sum of account balances across all exchanges for this coin</summary>
		public decimal Total => NettTotal;

		/// <summary>Return the exchanges to consider when averaging/summing values</summary>
		private IEnumerable<Exchange> SourceExchanges
		{
			get
			{
				var src = (string)m_exch_source.CurrentItem;
				if (src == SpecialExchangeSources.All)
				{
					return m_model.TradingExchanges;
				}
				else if (src == SpecialExchangeSources.Current)
				{
					var exch = (Exchange)CollectionViewSource.GetDefaultView(m_model.Exchanges).CurrentItem;
					if (exch != null && !(exch is CrossExchange))
						return new[] { exch };
				}
				else
				{
					var exch = m_model.TradingExchanges.FirstOrDefault(x => x.Name == src);
					if (exch != null)
						return new[] { exch };
				}
				return Enumerable.Empty<Exchange>();
			}

		}

		/// <summary>The average value of this coin</summary>
		private decimal LiveValue
		{
			get
			{
				// Find the average price on the available exchanges
				var value = new Average<decimal>();
				foreach (var exch in SourceExchanges)
				{
					var coin = exch.Coins[Symbol];
					if (coin == null || !coin.LivePriceAvailable) continue;
					value.Add(coin.ValueOf(1m));
				}
				return value.Mean._(Symbol);
			}
		}

		/// <summary>The sum of account balances across all exchanges for this coin</summary>
		private Unit<decimal> NettTotal
		{
			get
			{
				var total = 0m;
				foreach (var exch in SourceExchanges)
				{
					var coin = exch.Coins[Symbol];
					if (coin == null) continue;
					total += coin.Balances.NettTotal;
				}
				return total._(Symbol);
			}
		}

		/// <summary>The sum of available balance across all exchanges</summary>
		private Unit<decimal> NettAvailable
		{
			get
			{
				var avail = 0m;
				foreach (var exch in SourceExchanges)
				{
					var coin = exch.Coins[Symbol];
					if (coin == null) continue;
					avail += coin.Balances.NettAvailable;
				}
				return avail._(Symbol);
			}
		}

		/// <summary>True if a live value could be calculated</summary>
		public bool LiveValueAvailable => SourceExchanges.Any(x => x.Coins[Symbol]?.LivePriceAvailable ?? false);

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Implicit conversion to CoinData</summary>
		public static implicit operator CoinData(CoinDataAdaptor x) { return x.CoinData; }
	}
}

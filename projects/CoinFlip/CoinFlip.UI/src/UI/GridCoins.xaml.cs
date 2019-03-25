using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridCoins : Grid, IDockable, IDisposable
	{
		// Notes:
		//  - This grid shows balance and coin information based on averages over
		//    all exchanges, the currently selected exchange, or a specific exchange.

		static GridCoins()
		{
			CurrentProperty = Gui_.DPRegister<GridCoins>(nameof(Current));
		}
		public GridCoins(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Coins");
			ExchangeSource = new ListCollectionView(new List<string> { SpecialExchangeSources.All, SpecialExchangeSources.Current });
			Coins = new ListCollectionView(ListAdapter.Create(model.Coins, c => new CoinDataAdaptor(c, model, ExchangeSource), c => c?.CoinData));
			Model = model;

			// Commands
			AddCoin = Command.Create(this, () =>
			{
				var dlg = new PromptUI(Window.GetWindow(this))
				{
					Title = "Add Coin",
					Prompt = "Enter the Symbol Code for the coin to add",
				};
				if (dlg.ShowDialog() == true)
					Model.Coins.Add(new CoinData(dlg.Value, 1m, of_interest:true));
			});
			RemoveCoin = Command.Create(this, () =>
			{
				if (Current == null) return;
				Model.Coins.Remove(Current.CoinData);
			});
			EditLiveValueConversion = Command.Create(this, () =>
			{
				if (Current == null) return;
				var dlg = new PromptUI(Window.GetWindow(this))
				{
					Title = "Live Price Conversion",
					Prompt = "Set the symbols used to convert to a live price value (comma separated)",
					Value = Current.CoinData.LivePriceSymbols,
				};
				if (dlg.ShowDialog() == true)
					Current.CoinData.LivePriceSymbols = dlg.Value;
			});

			DataContext = this;
		}
		public void Dispose()
		{
			Model = null;
			DockControl = null;
		}

		/// <summary></summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Exchanges.CollectionChanged -= HandleExchangesChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Exchanges.CollectionChanged += HandleExchangesChanged;
				}

				// Handler
				void HandleExchangesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					// Preserve the current item
					var list = (List<string>)ExchangeSource.SourceCollection;
					using (Scope.Create(() => ExchangeSource.CurrentItem, ci => ExchangeSource.MoveCurrentTo(ci)))
					{
						list.Clear();
						list.Add(SpecialExchangeSources.All);
						list.Add(SpecialExchangeSources.Current);
						list.AddRange(Model.TradingExchanges.Select(x => x.Name));
						ExchangeSource.Refresh();
					}
				}
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>The data source for coin data</summary>
		public ICollectionView ExchangeSource { get; }

		/// <summary>The view of the available exchanges</summary>
		public ICollectionView Coins { get; }

		/// <summary>The currently selected exchange</summary>
		public CoinDataAdaptor Current
		{
			get { return (CoinDataAdaptor)GetValue(CurrentProperty); }
			set { SetValue(CurrentProperty, value); }
		}
		public static readonly DependencyProperty CurrentProperty;

		/// <summary>Add a coin to the collection</summary>
		public Command AddCoin { get; }

		/// <summary>Remove a coin from the collection</summary>
		public Command RemoveCoin { get; }

		/// <summary>Edit the sequence used to obtain the live value of a coin</summary>
		public Command EditLiveValueConversion { get; }

		/// <summary>Tidy separators on in an opening context menu</summary>
		private void TidySeparators(object sender, ContextMenuEventArgs e)
		{
			var cmenu = (ContextMenu)sender;
			cmenu.Items.TidySeparators();
		}
	}

	/// <summary>A wrapper for CoinData to provide live value data</summary>
	public class CoinDataAdaptor :INotifyPropertyChanged
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
			}
		}

		/// <summary>The wrapped coin data</summary>
		public CoinData CoinData { get; }

		/// <summary>Coin symbol code</summary>
		public string Symbol => CoinData.Symbol;

		/// <summary>Value of the coin</summary>
		public decimal Value => CoinData.ShowLivePrices ? LiveValue : CoinData.AssignedValue; //.ToString("C")

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

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Implicit conversion to CoinData</summary>
		public static implicit operator CoinData(CoinDataAdaptor x) { return x.CoinData; }
	}

	/// <summary></summary>
	internal static class SpecialExchangeSources
	{
		public const string All = "All Exchanges (Sum/Average)";
		public const string Current = "Selected Exchange";
	}
}

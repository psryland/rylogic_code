using System;
using System.ComponentModel;
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
	public partial class GridCoins : DataGrid, IDockable, IDisposable
	{
		// Notes:
		//  - Coins are associated with exchanges.
		//  - This grid shows meta data for the coins of interest

		static GridCoins()
		{
			CurrentProperty = Gui_.DPRegister<GridCoins>(nameof(Current));
		}
		public GridCoins(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Coins");
			Coins = new ListCollectionView(ListAdapter.Create(model.Coins, c => new CoinDataAdaptor(c, model), c => c?.CoinData));
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
				{}
				m_model = value;
				if (m_model != null)
				{
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
	}

	/// <summary>A wrapper for CoinData to provide live value data</summary>
	public class CoinDataAdaptor :INotifyPropertyChanged
	{
		private readonly Model m_model;
		public CoinDataAdaptor(CoinData cd, Model model)
		{
			CoinData = cd;
			m_model = model;

			// Watch for market data updates
			cd.LivePriceChanged += WeakRef.MakeWeak(UpdateLiveValueChanged, h => cd.LivePriceChanged -= h);
			cd.BalanceChanged += WeakRef.MakeWeak(UpdateBalanceChanged, h => cd.BalanceChanged -= h);
		}

		/// <summary>The wrapped coin data</summary>
		public CoinData CoinData { get; }

		/// <summary>Coin symbol code</summary>
		public string Symbol => CoinData.Symbol;

		/// <summary>Value of the coin</summary>
		public string Value => (CoinData.ShowLivePrices ? LiveValue : CoinData.AssignedValue).ToString("C");

		/// <summary>The sum of available balance across all exchanges</summary>
		public string Available => NettAvailable.ToString("F8");

		/// <summary>The sum of account balances across all exchanges for this coin</summary>
		public string Total => NettTotal.ToString("F8");

		/// <summary></summary>
		public string Balance => (LiveValue * NettTotal).ToString("C");

		/// <summary>The average value of this coin</summary>
		private decimal LiveValue
		{
			get
			{
				// Find the average price on the available exchanges
				var value = new Average<decimal>();
				foreach (var exch in m_model.TradingExchanges)
				{
					var coin = exch.Coins[Symbol];
					if (coin == null) continue;
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
				foreach (var exch in m_model.TradingExchanges)
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
				foreach (var exch in m_model.TradingExchanges)
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

		/// <summary>Called to raise property changed notification when the live value changes</summary>
		public void UpdateLiveValueChanged(object sender = null, EventArgs args = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Balance)));
		}

		/// <summary>Called to raise property changed notification when the balance changes</summary>
		public void UpdateBalanceChanged(object sender = null, EventArgs args = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Available)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Total)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Balance)));
		}

		/// <summary>Implicit conversion to CoinData</summary>
		public static implicit operator CoinData(CoinDataAdaptor x) { return x.CoinData; }
	}
}

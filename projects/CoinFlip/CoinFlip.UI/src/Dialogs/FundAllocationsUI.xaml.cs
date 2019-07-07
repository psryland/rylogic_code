using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CoinFlip.Settings;
using Rylogic.Container;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class FundAllocationsUI : Window, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - The exchange has a total and a held value.
		//  - For a given fund, the maximum it can be allocated is the exchange's 'available' amount
		//    plus the held amount of the fund, because the funds total includes it's held amount.

		public FundAllocationsUI(Window owner, Model model)
		{
			InitializeComponent();
			Icon = owner?.Icon;
			Owner = owner;
			Model = model;

			Filter = new CoinFilter(x => (CoinDataAdapter)x);
			Exchanges = new ListCollectionView(model.TradingExchanges.ToList());
			Funds = new ListCollectionView(model.Funds) { Filter = x => ((Fund)x).Id != Fund.Main };
			Coins = new ListCollectionView(ListAdapter.Create(model.Coins, x => new CoinDataAdapter(this, x), x => x.CoinData)) { Filter = Filter.Predicate };

			Exchanges.CurrentChanged += delegate { Coins.Refresh(); };
			Funds.CurrentChanged += delegate { Coins.Refresh(); };
			Filter.PropertyChanged += delegate { Coins.Refresh(); };

			Accept = Command.Create(this, Close);
			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Model.SaveFundBalances();
			Dispose();
		}
		public void Dispose()
		{
			Model = null;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					//m_model.Exchanges.CollectionChanged -= HandleExchangesChanged;
					//m_model.Coins.CollectionChanged -= HandleCoinsChanged;
					//m_model.Funds.CollectionChanged -= HandleFundsChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					//m_model.Funds.CollectionChanged += HandleFundsChanged;
					//m_model.Coins.CollectionChanged += HandleCoinsChanged;
					//m_model.Exchanges.CollectionChanged += HandleExchangesChanged;
				}

				//// Handle funds being created or destroyed
				//void HandleFundsChanged(object sender, NotifyCollectionChangedEventArgs e)
				//{
				//	CreateFundColumns();
				//}
				//void HandleCoinsChanged(object sender, NotifyCollectionChangedEventArgs e)
				//{
				//	Coins.Refresh();
				//}
				//void HandleExchangesChanged(object sender, NotifyCollectionChangedEventArgs e)
				//{
				//	var list = (List<Exchange>)Exchanges.SourceCollection;
				//	using (Scope.Create(() => Exchanges.CurrentItem, ci => Exchanges.MoveCurrentTo(ci)))
				//		list.Assign(Model.TradingExchanges);

				//	Exchanges.Refresh();
				//	Coins.Refresh();
				//}
			}
		}
		private Model m_model;

		/// <summary>The available exchanges</summary>
		public ICollectionView Exchanges { get; }

		/// <summary>The available funds</summary>
		public ICollectionView Funds { get; }

		/// <summary>The available currencies</summary>
		public ICollectionView Coins { get; }

		/// <summary>Filter support for 'Coins'</summary>
		public CoinFilter Filter { get; }

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }

		/// <summary>Check for any errors in the fund allocation. Returns null if valid</summary>
		public Exception Validate
		{
			get
			{
				foreach (var exch in Model.Exchanges)
				{
					foreach (var bal in exch.Balance.Values)
					{
						var result = bal.Validate();
						if (result != null)
							return new Exception($"Exchange: {exch.Name}, Coin: {bal.Coin.Symbol}, {result.Message}");
					}
				}
				return null;
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>Binding wrapper for CoinData</summary>
		private class CoinDataAdapter :INotifyPropertyChanged, IValueTotalAvail
		{
			private readonly FundAllocationsUI m_owner;
			public CoinDataAdapter(FundAllocationsUI owner, CoinData cd)
			{
				m_owner = owner;
				CoinData = cd;
			}

			/// <summary>The coin data</summary>
			public CoinData CoinData { get; }

			/// <summary>The coin name</summary>
			public string Symbol => CoinData.Symbol;

			/// <summary>Get the Coin on the currently selected exchange (or null)</summary>
			public Coin Coin => SelectedExchange?.Coins[Symbol];

			/// <summary>The live value of this coin</summary>
			public decimal Value => Coin?.ValueOf(1m) ?? 0m;

			/// <summary>The total amount of the coin (in coin currency)</summary>
			public Unit<decimal> Total => Coin?.Balances.Sum(x => x.Total) ?? 0m._(Symbol);

			/// <summary>The available amount of the coin (in coin currency)</summary>
			public Unit<decimal> Available => Coin?.Balances.Sum(x => x.Available) ?? 0m._(Symbol);

			/// <summary>The currently selected exchange</summary>
			private Exchange SelectedExchange => (Exchange)m_owner.Exchanges.CurrentItem;

			/// <summary>The currently selected fund</summary>
			private Fund SelectedFund => (Fund)m_owner.Funds.CurrentItem;

			/// <summary>The maximum amount that can be allocated to the fund</summary>
			public decimal MaxAvailable
			{
				get
				{
					var balances = Coin?.Balances;
					if (balances == null)
						return 0m;

					// The max available to the currently selected fund is:
					// + the exchange's available amount (because you can't trade with held funds)
					// + the fund's current held amount (because the fund's held amount is included in it's total)
					// - the amounts allocated to other funds (because allocations can only go to one fund)
					// - the amount already allocated to the fund (because as more is allocated to the fund, the available goes down).
					var max_available = balances.NettAvailable;
					foreach (var bal in balances.FundsExceptMain)
					{
						if (bal.FundId == SelectedFund?.Id)
							max_available += bal.HeldOnExch - bal.Total;
						else
							max_available -= bal.Total;
					}
					return max_available;
				}
			}

			/// <summary>The balance of this coin in the selected fund</summary>
			public decimal Allocated
			{
				get
				{
					var fund_id = SelectedFund?.Id;
					if (fund_id == null) return 0m;
					return Coin?.Balances[fund_id].Total ?? 0m;
				}
				set
				{
					var fund_id = SelectedFund?.Id;
					if (fund_id == null)
						return;

					var coin = Coin;
					if (coin == null)
						return;

					value = Math.Max(value, 0m);
					coin.Balances.AssignFundBalance(fund_id, value._(coin), null, Model.UtcNow);
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxAvailable)));
					m_owner.NotifyPropertyChanged(nameof(FundAllocationsUI.Validate));
				}
			}

			/// <summary></summary>
			public event PropertyChangedEventHandler PropertyChanged;
		}
	}
}

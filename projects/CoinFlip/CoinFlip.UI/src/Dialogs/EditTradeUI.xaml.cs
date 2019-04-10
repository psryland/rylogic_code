using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class EditTradeUI : Window, INotifyPropertyChanged
	{
		// Notes:
		//  - This dialog is used to edit the properties of a trade or order.
		//    Applying these changes it done by Model.CreateOrder(Trade) or
		//    Model.ModifyOrder(Trade)

		public EditTradeUI(Window owner, Model model, Trade trade, long? existing_order_id)
		{
			InitializeComponent();
			Title = existing_order_id != null ? "Modify Order" : "Create Order";
			Icon = owner?.Icon;
			Owner = owner;

			Model = model;
			Trade = trade;
			Original = new Trade(trade);
			ExistingOrderId = existing_order_id;

			// Find the exchanges that offer the trade pair
			ExchangesOfferingPair = new ListCollectionView(Model.Exchanges.Where(x => x.Pairs.ContainsKey(trade.Pair.UniqueKey)).ToList());
			ExchangesOfferingPair.MoveCurrentTo(trade.Pair.Exchange);

			SetSellAmountToMaximum = Command.Create(this, SetSellAmountToMaximumInternal);
			DataContext = this;
		}

		/// <summary>Model</summary>
		private Model Model { get; }

		/// <summary>The trade being modified</summary>
		public Trade Trade { get; }

		/// <summary>The trade provided to this editor. Used to detect changes</summary>
		private Trade Original { get; }

		/// <summary>Non-null if 'Original' is actually an order, live on an exchange</summary>
		private long? ExistingOrderId { get; }

		/// <summary>The exchanges that allow trades of 'Pair'</summary>
		public ICollectionView ExchangesOfferingPair
		{
			get { return m_exchanges; }
			private set
			{
				if (m_exchanges == value) return;
				m_exchanges = value;
				m_exchanges.CurrentChanged += HandleSelectedExchangeChanged;

				// Handlers
				void HandleSelectedExchangeChanged(object sender, EventArgs e)
				{
					var exch = (Exchange)ExchangesOfferingPair.CurrentItem;
					Trade.Pair = exch.Pairs[Trade.Pair.UniqueKey];
				}
			}
		}
		private ICollectionView m_exchanges;

		/// <summary>The exchange that the trade is/will be on</summary>
		public Exchange Exchange => Trade.Pair.Exchange;

		/// <summary>The side of the pair for the trade</summary>
		public ETradeType TradeType
		{
			get { return Trade.TradeType; }
			set
			{
				if (TradeType == value) return;
				Trade.TradeType = value;
				NotifyPropertyChanged(nameof(TradeType));
				NotifyPropertyChanged(nameof(OrderType));
			}
		}

		/// <summary>The type of order, implied by the trade type and the current spot price</summary>
		public EPlaceOrderType OrderType => Trade.OrderType;

		/// <summary>The trade price</summary>
		public string PriceQ2B
		{
			get { return Trade.PriceQ2B.ToString("G8", false); }
			set
			{
				Trade.PriceQ2B = decimal.Parse(value)._(Trade.Pair.RateUnits);
				NotifyPropertyChanged(nameof(PriceQ2B));
				NotifyPropertyChanged(nameof(OrderType));
			}
		}
		public Func<string, ValidationResult> ValidatePrice
		{
			get => s =>
			{
				if (!decimal.TryParse(s, out var v) || v <= 0) return new ValidationResult(false, "Price must be positive number");
				if (!Trade.PriceRange.Contains(v._(Trade.Pair.RateUnits))) return new ValidationResult(false, $"Price must be within {Trade.PriceRange.ToString()}");
				return ValidationResult.ValidResult;
			};
		}

		/// <summary>The currency sold</summary>
		public Coin CoinIn => Trade.CoinIn;

		/// <summary>The currency received</summary>
		public Coin CoinOut => Trade.CoinOut;

		/// <summary>The amount to trade</summary>
		public string AmountIn
		{
			get { return Trade.AmountIn.ToString("G8", false); }
			set
			{
				Trade.AmountIn = decimal.Parse(value)._(CoinIn);
				NotifyPropertyChanged(nameof(AmountIn));
				NotifyPropertyChanged(nameof(AmountOut));
			}
		}
		public Func<string, ValidationResult> ValidateAmountIn
		{
			get => s =>
			{
				if (!decimal.TryParse(s, out var v) || v <= 0) return new ValidationResult(false, "Amount must be positive number");
				if (!Trade.AmountRangeIn.Contains(v._(CoinIn))) return new ValidationResult(false, $"Amount must be within {Trade.AmountRangeIn.ToString()}");
				return ValidationResult.ValidResult;
			};
		}

		/// <summary>The amount to trade</summary>
		public string AmountOut
		{
			get { return Trade.AmountOut.ToString("G8", false); }
			set
			{
				Trade.AmountOut = decimal.Parse(value)._(CoinOut);
				NotifyPropertyChanged(nameof(AmountOut));
				NotifyPropertyChanged(nameof(AmountIn));
			}
		}
		public Func<string, ValidationResult> ValidateAmountOut
		{
			get => s =>
			{
				if (!decimal.TryParse(s, out var v) || v <= 0) return new ValidationResult(false, "Amount must be positive number");
				if (!Trade.AmountRangeOut.Contains(v._(CoinIn))) return new ValidationResult(false, $"Amount must be within {Trade.AmountRangeOut.ToString()}");
				return ValidationResult.ValidResult;
			};
		}

		/// <summary>Return the available balance of currency to sell. Includes the 'm_initial.AmountIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableIn => Exchange.Balance[CoinIn][Trade.FundId].Available + AdditionalIn;
		private Unit<decimal> AdditionalIn => ExistingOrderId != null ? Original.AmountIn : 0m._(CoinIn);

		/// <summary>Return the available balance of currency to buy. Includes the 'm_initial.VolumeIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableOut => Exchange.Balance[CoinOut][Trade.FundId].Available;

		/// <summary>Description of the amount sold in the trade</summary>
		public string TradeDescriptionIn => $"Trading {Math_.Clamp(Math_.Div((decimal)Trade.AmountIn * 100m, (decimal)AvailableIn, 0m), 0m, 100m):G4}% of {CoinIn} balance";

		/// <summary>Description of the amount received from the trade</summary>
		public string TradeDescriptionOut => $"After Fees: {Trade.AmountNett.ToString("G6", true)}";

		/// <summary>A description of the validation errors</summary>
		public string ValidationResults => Trade.Validate(additional_balance_in: AdditionalIn).ToErrorDescription();

		/// <summary>Set the trade sell amount to the maximum available</summary>
		public Command SetSellAmountToMaximum { get; }
		private void SetSellAmountToMaximumInternal()
		{
			Trade.AmountIn = AvailableIn;
		}

		public void Apply()
		{
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

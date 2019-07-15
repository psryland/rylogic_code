using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CoinFlip.Settings;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class EditTradeUI : Window, INotifyPropertyChanged
	{
		// Notes:
		//  - This dialog is used to edit the properties of a trade or order.

		public EditTradeUI(Window owner, Model model, Trade trade, long? existing_order_id)
		{
			InitializeComponent();
			Title = existing_order_id != null ? "Modify Order" : "Create Order";
			Icon = owner?.Icon;
			Owner = owner;

			Model = model;
			Trade = trade;
			PriceQ2B = Trade.PriceQ2B.ToString();
			AmountIn = Trade.AmountIn.ToString();
			AmountOut = Trade.AmountOut.ToString();
			Original = new Trade(trade);
			ExistingOrderId = existing_order_id;
			CreatorName = string.Empty;//todo

			// Find the exchanges that offer the trade pair
			ExchangesOfferingPair = new ListCollectionView(Model.Exchanges.Where(x => x.Pairs.ContainsKey(trade.Pair.UniqueKey)).ToList());
			ExchangesOfferingPair.MoveCurrentTo(trade.Pair.Exchange);

			SetSellAmountToMaximum = Command.Create(this, SetSellAmountToMaximumInternal);

			Trade.PropertyChanged += HandleTradePropertyChanged;
			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Trade.PropertyChanged -= HandleTradePropertyChanged;

			// If the trade is actually a live order, and it hasn't
			// actually changed ensure the 'Result' is 'cancelled'
			if (Result == true && ExistingOrderId != null && Original.Equals(Trade))
				Result = false;

			// If the dialog was used modally, set 'DialogResult'
			if (this.IsModal())
				DialogResult = Result;
		}

		/// <summary>Model</summary>
		private Model Model { get; }

		/// <summary>The trade being modified</summary>
		public Trade Trade { get; }

		/// <summary>The trade provided to this editor. Used to detect changes</summary>
		private Trade Original { get; }

		/// <summary>Non-null if 'Original' is actually an order, live on an exchange</summary>
		public long? ExistingOrderId { get; }

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
					NotifyPropertyChanged(string.Empty);
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
				NotifyPropertyChanged(string.Empty);
			}
		}

		/// <summary>The type of order, implied by the trade type and the current spot price</summary>
		public EPlaceOrderType? OrderType => Trade.OrderType;

		/// <summary>User text associated with the trade</summary>
		public string CreatorName { get; set; }

		/// <summary>The trade price</summary>
		public string PriceQ2B
		{
			get { return m_price_q2b; }
			set
			{
				if (m_in_price_q2b != 0) return;
				using (Scope.Create(() => ++m_in_price_q2b, () => --m_in_price_q2b))
				{
					// Default to an invalid price
					var price_q2b = 0m._(Trade.Pair.RateUnits);
					m_price_q2b = value;

					// Convert the value into a price
					value = value.Trim();
					if (decimal.TryParse(value, out var v) && v > 0)
						price_q2b = v._(Trade.Pair.RateUnits);

					// Update the trade price. Property changed
					// notification happens in HandleTradePropertyChanged.
					Trade.PriceQ2B = price_q2b;
					if (price_q2b != 0)
						m_price_q2b = Trade.PriceQ2B.ToString(8, false);
				}
			}
		}
		private string m_price_q2b;
		private int m_in_price_q2b;

		/// <summary>The currency sold</summary>
		public Coin CoinIn => Trade.CoinIn;

		/// <summary>The currency received</summary>
		public Coin CoinOut => Trade.CoinOut;

		/// <summary>The amount to trade</summary>
		public string AmountIn
		{
			get { return m_amount_in; }
			set
			{
				if (m_in_amount_in != 0) return;
				using (Scope.Create(() => ++m_in_amount_in, () => --m_in_amount_in))
				{
					// Default to an invalid amount
					var amount_in = 0m._(CoinIn);
					m_amount_in = value;

					// Convert the value into an amount
					value = value.Trim();
					if (value.EndsWith("%")) // Percentage of available in
					{
						value = value.Trim('%', ' ', '\t');
						if (decimal.TryParse(value, out var pc) && pc > 0)
							amount_in = AvailableIn * pc * 0.01m;
					}
					else
					{
						if (decimal.TryParse(value, out var v) && v > 0)
							amount_in = v._(CoinIn);
					}

					// Update the trade amount. Property changed
					// notification happens in HandleTradePropertyChanged.
					Trade.AmountIn = amount_in;
					if (amount_in != 0m)
						m_amount_in = Trade.AmountIn.ToString(CoinIn.SD, false);
				}
			}
		}
		private string m_amount_in;
		private int m_in_amount_in;

		/// <summary>The amount to trade</summary>
		public string AmountOut
		{
			get { return m_amount_out; }
			set
			{
				if (m_in_amount_out != 0) return;
				using (Scope.Create(() => ++m_in_amount_out, () => --m_in_amount_out))
				{
					// Default to an invalid amount
					var amount_out = 0m._(CoinOut);
					m_amount_out = value;

					// Convert the value into an amount
					value = value.Trim();
					if (decimal.TryParse(value, out var v) && v > 0)
						amount_out = v._(CoinOut);

					// Update the trade amount. Property changed
					// notification happens in HandleTradePropertyChanged.
					Trade.AmountOut = amount_out;
					if (amount_out != 0)
						m_amount_out = Trade.AmountOut.ToString(CoinOut.SD, false);
				}
			}
		}
		private string m_amount_out;
		private int m_in_amount_out;

		/// <summary>Return the available balance of currency to sell. Includes the 'm_initial.AmountIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableIn => Exchange.Balance[CoinIn][Trade.FundId].Available + AdditionalIn;
		private Unit<decimal> AdditionalIn => ExistingOrderId != null ? Original.AmountIn : 0m._(CoinIn);

		/// <summary>Return the available balance of currency to buy. Includes the 'm_initial.VolumeIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableOut => Exchange.Balance[CoinOut][Trade.FundId].Available;

		/// <summary>Description of the amount sold in the trade</summary>
		public string TradeDescriptionIn => $"Trading {Math_.Clamp(Math_.Div((decimal)Trade.AmountIn, (decimal)AvailableIn, 0m), 0m, 1m):P2} of {CoinIn} balance";

		/// <summary>Description of the amount received from the trade</summary>
		public string TradeDescriptionOut => $"After Fees: {Trade.AmountNett.ToString("F8", true)}";

		/// <summary>The colour associated with the trade type</summary>
		public Colour32 TradeTypeBgColour =>
			TradeType == ETradeType.Q2B ? SettingsData.Settings.Chart.B2QColour.Lerp(Colour32.White,0.5f) :
			TradeType == ETradeType.B2Q ? SettingsData.Settings.Chart.Q2BColour.Lerp(Colour32.White,0.5f) :
			throw new Exception($"Unknown trade type: {TradeType}");

		/// <summary>Validate the trade data</summary>
		public EValidation Validation => Trade.Validate(additional_balance_in: AdditionalIn);

		/// <summary>A description of the validation errors</summary>
		public string ValidationResults => Validation.ToErrorDescription();

		/// <summary>True if all trade data is valid</summary>
		public bool IsValid => Validation == EValidation.Valid;

		/// <summary>True if the price value is valid</summary>
		public bool IsPriceValid => !Bit.AnySet(Validation, EValidation.PriceIsInvalid | EValidation.PriceOutOfRange);

		/// <summary>True if the amount value is valid</summary>
		public bool IsAmountInValid => !Bit.AnySet(Validation, EValidation.AmountInIsInvalid | EValidation.AmountInOutOfRange | EValidation.InsufficientBalance);

		/// <summary>True if the amount value is valid</summary>
		public bool IsAmountOutValid => !Bit.AnySet(Validation, EValidation.AmountOutIsInvalid | EValidation.AmountOutOutOfRange | EValidation.InsufficientBalance);

		/// <summary>Set the trade sell amount to the maximum available</summary>
		public Command SetSellAmountToMaximum { get; }
		private void SetSellAmountToMaximumInternal()
		{
			Trade.AmountIn = AvailableIn;
			NotifyPropertyChanged(string.Empty);
		}

		/// <summary>Close the dialog in the 'accepted' state</summary>
		public void Accept()
		{
			// Grab focus to ensure values are validated
			if (!m_btn_accept.Focus())
				throw new Exception("Create button cannot take input focus");

			// Ignore accept until the trade is valid
			if (!IsValid)
				return;

			Result = true;
			Close();
		}

		/// <summary>Close the dialog in the 'cancelled' state</summary>
		public void Cancel()
		{
			Result = false;
			Close();
		}

		/// <summary>The dialog result when closed</summary>
		public bool? Result { get; private set; }

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
		private void HandleTradePropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			// Don't make 'Trade' a prprop because we don't want 'Trade' to be null after the window is closed
			switch (args.PropertyName)
			{
			case nameof(Trade.PriceQ2B):
			case nameof(Trade.AmountBase):
				PriceQ2B = Trade.PriceQ2B.ToString();
				AmountIn = Trade.AmountIn.ToString();
				AmountOut = Trade.AmountOut.ToString();
				break;
			}
			NotifyPropertyChanged(string.Empty);
		}
	}
}

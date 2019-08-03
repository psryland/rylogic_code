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
	public partial class EditTradeUI :Window, INotifyPropertyChanged, IDisposable
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

			// The list of available funds
			Funds = new ListCollectionView(Model.Funds);

			// Commands
			SetSellAmountToMaximum = Command.Create(this, SetSellAmountToMaximumInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			ExchangesOfferingPair = null;
			Funds = null;
			Trade = null;
			Model = null;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			// If the trade is actually a live order, and it hasn't
			// actually changed ensure the 'Result' is 'cancelled'
			if (Result == true && ExistingOrderId != null && Original.Equals(Trade))
				Result = false;

			// If the dialog was used modally, set 'DialogResult'
			if (this.IsModal())
				DialogResult = Result;

			Dispose();
		}

		/// <summary>Model</summary>
		private Model Model
		{
			get => m_model;
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.DataChanging -= HandleDataChanging;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.DataChanging += HandleDataChanging;
				}

				// Handler
				void HandleDataChanging(object sender, DataChangingEventArgs e)
				{
					if (e.Before) return;
					if (OrderType == EOrderType.Market)
					{
						Trade.PriceQ2B = Pair.MarketTrade(Trade.Fund, Trade.TradeType, Trade.AmountIn).PriceQ2B;
						NotifyPropertyChanged(nameof(PriceQ2B));
					}
				}
			}
		}
		private Model m_model;

		/// <summary>The trade being modified</summary>
		public Trade Trade
		{
			get => m_trade;
			private set
			{
				if (m_trade == value) return;
				if (m_trade != null)
				{
					m_trade.PropertyChanged -= HandleTradePropertyChanged;
				}
				m_trade = value;
				if (m_trade != null)
				{
					m_trade.PropertyChanged += HandleTradePropertyChanged;
				}

				// Handler
				void HandleTradePropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (m_in_trade_property_changed != 0) return;
					using (Scope.Create(() => ++m_in_trade_property_changed, () => --m_in_trade_property_changed))
					{
						// Ensure the trade remains consistent
						switch (args.PropertyName)
						{
						case nameof(Trade.OrderType):
							{
								if (Trade.OrderType == EOrderType.Market)
								{
									Trade.PriceQ2B = Trade.SpotPriceQ2B;
									Trade.AmountOut = Trade.AmountIn * Trade.Price;
								}
								else if (Trade.AmountOut != 0 && Trade.AmountIn != 0)
								{
									Trade.Price = Trade.AmountOut / Trade.AmountIn;
								}
								break;
							}
						case nameof(Trade.PriceQ2B):
							{
								if (Trade.AmountIn != 0)
								{
									Trade.AmountOut = Trade.AmountIn * Trade.Price;
								}
								else if (Trade.AmountOut != 0)
								{
									Trade.AmountIn = Trade.AmountOut / Trade.Price;
								}
								break;
							}
						case nameof(Trade.AmountIn):
							{
								// When the in amount changes, update the out amount 
								// for market orders or the price for other order types.
								if (Trade.OrderType == EOrderType.Market)
								{
									Trade.AmountOut = Trade.AmountIn * Trade.Price;
								}
								else if (Trade.AmountIn != 0 && Trade.PriceQ2B != 0)
								{
									Trade.AmountOut = Trade.AmountIn * Trade.Price;
								}
								break;
							}
						case nameof(Trade.AmountOut):
							{
								// When the out amount changes, update the in amount 
								// for market orders or the price for other order types.
								if (Trade.OrderType == EOrderType.Market)
								{
									Trade.AmountIn = Trade.AmountOut / Trade.Price;
								}
								else if (Trade.AmountOut != 0 && Trade.PriceQ2B != 0)
								{
									Trade.AmountIn = Trade.AmountOut / Trade.Price;
								}
								break;
							}
						}

						// When the trade values change update the displayed strings.
						// These will be ignored if the update comes from the setters.
						// Use default 'ToString' here because these are user amounts
						// which don't need to display the full number of SD.
						PriceQ2B = Trade.PriceQ2B.ToString(8, false);
						AmountIn = Trade.AmountIn.ToString();
						AmountOut = Trade.AmountOut.ToString();
						NotifyPropertyChanged(string.Empty);
					}
				}
			}
		}
		private Trade m_trade;
		private int m_in_trade_property_changed;

		/// <summary>The trade provided to this editor. Used to detect changes</summary>
		private Trade Original { get; }

		/// <summary>Non-null if 'Original' is actually an order, live on an exchange</summary>
		public long? ExistingOrderId { get; }

		/// <summary>The exchanges that allow trades of 'Pair'</summary>
		public ICollectionView ExchangesOfferingPair
		{
			get => m_exchanges;
			private set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged -= HandleSelectedExchangeChanged;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged += HandleSelectedExchangeChanged;
					m_exchanges.MoveCurrentTo(Trade.Pair.Exchange);
				}

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

		/// <summary>The available funds</summary>
		public ICollectionView Funds
		{
			get => m_funds;
			private set
			{
				if (m_funds == value) return;
				if (m_funds != null)
				{
					m_funds.CurrentChanged -= HandleCurrentFundChanged;
				}
				m_funds = value;
				if (m_funds != null)
				{
					m_funds.MoveCurrentTo(Trade.Fund);
					m_funds.CurrentChanged += HandleCurrentFundChanged;
				}

				// Handlers
				void HandleCurrentFundChanged(object sender, EventArgs e)
				{
					Trade.Fund = (Fund)Funds.CurrentItem;
				}
			}
		}
		private ICollectionView m_funds;

		/// <summary>The pair being traded</summary>
		public TradePair Pair => Trade.Pair;

		/// <summary>The exchange that the trade is/will be on</summary>
		public Exchange Exchange => Pair.Exchange;

		/// <summary>The side of the pair for the trade</summary>
		public ETradeType TradeType
		{
			get => Trade.TradeType;
			set
			{
				if (TradeType == value) return;
				Trade.TradeType = value;
				NotifyPropertyChanged(string.Empty);
			}
		}

		/// <summary>The type of order, implied by the trade type and the current spot price</summary>
		public EOrderType OrderType
		{
			get => Trade.OrderType;
			set
			{
				if (OrderType == value) return;
				Trade.OrderType = value;
				NotifyPropertyChanged(string.Empty);
			}
		}

		/// <summary>True if the price can be set, false if using market price</summary>
		public bool CanChoosePrice => OrderType != EOrderType.Market;

		/// <summary>User text associated with the trade</summary>
		public string CreatorName { get; set; }

		/// <summary>The amount to trade</summary>
		public string AmountIn
		{
			get => m_amount_in;
			set
			{
				if (m_amount_in == value || m_in_amount_in != 0) return;
				using (Scope.Create(() => ++m_in_amount_in, () => --m_in_amount_in))
				{
					// Default to an invalid amount
					var amount_in = 0.0._(CoinIn);
					m_amount_in = value;

					// Convert the value into an amount
					value = value.Trim();
					if (value.EndsWith("%")) // Percentage of available in
					{
						value = value.Trim('%', ' ', '\t');
						if (double.TryParse(value, out var pc) && pc > 0)
							amount_in = AvailableIn * pc * 0.01;
					}
					else
					{
						if (double.TryParse(value, out var v) && v > 0)
							amount_in = v._(CoinIn);
					}

					// Update the trade amount.
					Trade.AmountIn = amount_in;

					// For valid amounts, update the displayed string
					if (amount_in != 0)
						m_amount_in = Trade.AmountIn.ToString();
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
				if (m_amount_out == value || m_in_amount_out != 0) return;
				using (Scope.Create(() => ++m_in_amount_out, () => --m_in_amount_out))
				{
					// Default to an invalid amount
					var amount_out = 0.0._(CoinOut);
					m_amount_out = value;

					// Convert the value into an amount
					value = value.Trim();
					if (double.TryParse(value, out var v) && v > 0)
						amount_out = v._(CoinOut);

					// Update the trade amount.
					Trade.AmountOut = amount_out;

					// For valid amounts, update the displayed string
					if (amount_out != 0)
						m_amount_out = Trade.AmountOut.ToString();
				}
			}
		}
		private string m_amount_out;
		private int m_in_amount_out;

		/// <summary>The trade price</summary>
		public string PriceQ2B
		{
			get => m_price_q2b;
			set
			{
				// Display strings so that the user's text doesn't change unexpectedly.
				if (m_price_q2b == value || m_in_price_q2b != 0) return;
				using (Scope.Create(() => ++m_in_price_q2b, () => --m_in_price_q2b))
				{
					// Default to an invalid price
					var price_q2b = 0.0._(Pair.RateUnits);
					m_price_q2b = value;

					// Try to convert the value into a price
					value = value.Trim();
					if (double.TryParse(value, out var v) && v > 0)
						price_q2b = v._(Pair.RateUnits);

					// Update the trade price.
					Trade.PriceQ2B = price_q2b;

					// Update the string when a valid
					if (price_q2b != 0)
						m_price_q2b = Trade.PriceQ2B.ToString();
				}
			}
		}
		private string m_price_q2b;
		private int m_in_price_q2b;

		/// <summary>Return the available balance of currency to sell. Includes the 'm_initial.AmountIn' when modifying 'Trade'</summary>
		public Unit<double> AvailableIn => Trade.Fund[CoinIn].Available + AdditionalIn;
		private Unit<double> AdditionalIn => ExistingOrderId != null ? Original.AmountIn : 0.0._(CoinIn);

		/// <summary>Return the available balance of currency to buy. Includes the 'm_initial.VolumeIn' when modifying 'Trade'</summary>
		public Unit<double> AvailableOut => Trade.Fund[CoinOut].Available;

		/// <summary>The currency sold</summary>
		public Coin CoinIn => Trade.CoinIn;

		/// <summary>The currency received</summary>
		public Coin CoinOut => Trade.CoinOut;

		/// <summary>Description of the amount sold in the trade</summary>
		public string TradeDescriptionIn => $"Trading {Math_.Clamp(Math_.Div<double>(Trade.AmountIn, AvailableIn, 0), 0, 1):P2} of {CoinIn} balance";

		/// <summary>Description of the amount received from the trade</summary>
		public string TradeDescriptionOut => $"Fee: {Trade.Commission.ToString(8, true)} ({Trade.CommissionCoin.ValueOf(Trade.Commission).ToString(6,true)})";

		/// <summary>The colour associated with the trade type</summary>
		public Colour32 TradeTypeBgColour =>
			TradeType == ETradeType.Q2B ? SettingsData.Settings.Chart.Q2BColour.Lerp(Colour32.White,0.9f) :
			TradeType == ETradeType.B2Q ? SettingsData.Settings.Chart.B2QColour.Lerp(Colour32.White,0.9f) :
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
	}
}

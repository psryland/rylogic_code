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

namespace CoinFlip.UI.Dialogs
{
	public partial class EditTradeUI :Window, INotifyPropertyChanged, IDisposable
	{
		// Notes:
		//  - This dialog is used to edit the properties of a trade or order.
		//  - When modifying an existing order, the trade type cannot be changed because
		//    the 'AdditionalIn' amount is based on the held amount of 'CoinIn' currency.
		//    Changing the trade side means 'AdditionalIn' is the wrong currency and value.

		public EditTradeUI(Window owner, Model model, Trade trade, bool existing_order)
		{
			InitializeComponent();
			Title = existing_order ? "Modify Order" : "Create Order";
			Icon = owner.Icon;
			Owner = owner;
			Left = owner.Left + 80;
			Top = owner.Top + (owner.Height - Height) / 2;

			Model = model;
			Trade = trade;
			IsNewTrade = !existing_order;
			PriceQ2B = Trade.PriceQ2B.ToString();
			AmountIn = Trade.AmountIn.ToString();
			AmountOut = Trade.AmountOut.ToString();
			AcceptText = existing_order ? "Modify" : "Create";

			// Find the exchanges that offer the trade pair
			ExchangesOfferingPair = new ListCollectionView(Model.Exchanges.Where(x => x.Pairs.ContainsKey(trade.Pair.UniqueKey)).ToList());

			// The list of available funds
			Funds = new ListCollectionView(Model.Funds);

			// Commands
			Accept = Command.Create(this, AcceptInternal);
			Cancel = Command.Create(this, CancelInternal);
			SetSellAmountToMaximum = Command.Create(this, SetSellAmountToMaximumInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			ExchangesOfferingPair = null!;
			Funds = null!;
			Trade = null!;
			Model = null!;
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

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
					CoinData.BalanceChanged -= HandleBalanceChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					CoinData.BalanceChanged += HandleBalanceChanged;
					m_model.DataChanging += HandleDataChanging;
				}

				// Handler
				void HandleDataChanging(object? sender, DataChangingEventArgs e)
				{
					if (e.Before) return;
					if (OrderType == EOrderType.Market)
					{
						Trade.PriceQ2B = Pair.MarketTrade(Trade.Fund, Trade.TradeType, Trade.AmountIn).PriceQ2B;
						NotifyPropertyChanged(nameof(PriceQ2B));
					}
				}
				void HandleBalanceChanged(object? sender, CoinEventArgs e)
				{
					if (e.Coin == CoinIn)
					{
						NotifyPropertyChanged(nameof(AvailableIn));
						NotifyPropertyChanged(nameof(TradeDescriptionIn));
						NotifyPropertyChanged(nameof(TradeDescriptionIn));
						NotifyPropertyChanged(nameof(ValidationResults));
						NotifyPropertyChanged(nameof(IsAmountInValid));
						NotifyPropertyChanged(nameof(IsValid));
					}
				}
			}
		}
		private Model m_model = null!;

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
					m_existing_held = m_trade.AmountIn;
					m_existing_trade_type = m_trade.TradeType;
					m_trade.PropertyChanged += HandleTradePropertyChanged;
				}

				// Handler
				void HandleTradePropertyChanged(object? sender, PropertyChangedEventArgs args)
				{
					if (m_in_trade_property_changed != 0) return;
					using (Scope.Create(() => ++m_in_trade_property_changed, () => --m_in_trade_property_changed))
					{
						// Ensure the trade remains consistent
						var update_amounts = false;
						switch (args.PropertyName)
						{
							case nameof(Trade.OrderType):
							{
								if (Trade.OrderType == EOrderType.Market)
								{
									Trade.PriceQ2B = Trade.SpotPriceQ2B;
									Trade.AmountOut = Trade.AmountIn * Trade.Price;
									update_amounts = true;
								}
								else if (Trade.AmountOut != 0 && Trade.AmountIn != 0)
								{
									Trade.Price = Trade.AmountOut / Trade.AmountIn;
									update_amounts = true;
								}
								break;
							}
							case nameof(Trade.PriceQ2B):
							{
								if (Trade.AmountIn != 0 && Trade.Price != 0)
								{
									Trade.AmountOut = Trade.AmountIn * Trade.Price;
									update_amounts = true;
								}
								else if (Trade.AmountOut != 0 && Trade.Price != 0)
								{
									Trade.AmountIn = Trade.AmountOut / Trade.Price;
									update_amounts = true;
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
									update_amounts = true;
								}
								else if (Trade.AmountIn != 0 && Trade.PriceQ2B != 0)
								{
									Trade.AmountOut = Trade.AmountIn * Trade.Price;
									update_amounts = true;
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
									update_amounts = true;
								}
								else if (Trade.AmountOut != 0 && Trade.PriceQ2B != 0)
								{
									Trade.AmountIn = Trade.AmountOut / Trade.Price;
									update_amounts = true;
								}
								break;
							}
						}

						// When the trade values change update the displayed strings.
						// These will be ignored if the update comes from the setters.
						// Use default 'ToString' here because these are user amounts
						// which don't need to display the full number of SD.
						if (update_amounts)
						{
							Exchange.Canonicalise(Trade);
							PriceQ2B = Trade.PriceQ2B.ToString();
							AmountIn = Trade.AmountIn.ToString();
							AmountOut = Trade.AmountOut.ToString();
							Invalidate();
						}
					}
				}
			}
		}
		private int m_in_trade_property_changed;
		private Trade m_trade = null!;

		/// <summary>True if we're not editing an existing order</summary>
		public bool IsNewTrade { get; }

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
				void HandleSelectedExchangeChanged(object? sender, EventArgs e)
				{
					var exch = (Exchange)ExchangesOfferingPair.CurrentItem;
					if (exch.Pairs[Trade.Pair.UniqueKey] is TradePair pair) Trade.Pair = pair;
					NotifyPropertyChanged(string.Empty);
				}
			}
		}
		private ICollectionView m_exchanges = null!;

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
				void HandleCurrentFundChanged(object? sender, EventArgs e)
				{
					Trade.Fund = (Fund)Funds.CurrentItem;
				}
			}
		}
		private ICollectionView m_funds = null!;

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
				if (!IsNewTrade)
					throw new Exception("Existing orders cannot change trade type");

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
		public string CreatorName
		{
			get => Trade.CreatorName;
			set => Trade.CreatorName = value;
		}

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

					// Update the trade amount.
					Trade.AmountIn = amount_in;

					// For valid amounts, update the displayed string
					if (amount_in != 0)
						m_amount_in = Trade.AmountIn.ToString("G29");

					// Notify that the string value has changed. Other properties are
					// notified as changed in the Trade property changed handler
					NotifyPropertyChanged(nameof(AmountIn));
				}
			}
		}
		private string m_amount_in = string.Empty;
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
					var amount_out = 0m._(CoinOut);
					m_amount_out = value;

					// Convert the value into an amount
					value = value.Trim();
					if (decimal.TryParse(value, out var v) && v > 0)
						amount_out = v._(CoinOut);

					// Update the trade amount.
					Trade.AmountOut = amount_out;

					// For valid amounts, update the displayed string
					if (amount_out != 0)
						m_amount_out = Trade.AmountOut.ToString("G29");

					// Notify that the string value has changed. Other properties are
					// notified as changed in the Trade property changed handler
					NotifyPropertyChanged(nameof(AmountOut));
				}
			}
		}
		private string m_amount_out = string.Empty;
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
					var price_q2b = 0m._(Pair.RateUnits);
					m_price_q2b = value;

					// Try to convert the value into a price
					value = value.Trim();
					if (decimal.TryParse(value, out var v) && v > 0)
						price_q2b = v._(Pair.RateUnits);

					// Update the trade price.
					Trade.PriceQ2B = price_q2b;

					// Update the string when a valid
					if (price_q2b != 0)
						m_price_q2b = Trade.PriceQ2B.ToString("G29");

					// Notify that the string value has changed. Other properties are
					// notified as changed in the Trade property changed handler
					NotifyPropertyChanged(nameof(PriceQ2B));
				}
			}
		}
		private string m_price_q2b = string.Empty;
		private int m_in_price_q2b;

		/// <summary>The available balance of currency to sell. Includes the 'm_initial.AmountIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableIn => Trade.Fund[CoinIn].Available + ExistingHeld;

		/// <summary>The available balance of currency to buy. Includes the 'm_initial.VolumeIn' when modifying 'Trade'</summary>
		public Unit<decimal> AvailableOut => Trade.Fund[CoinOut].Available;

		/// <summary>The total account balance of currency to sell</summary>
		public Unit<decimal> BalanceIn => Trade.Fund[CoinIn].Total;

		/// <summary>The currency sold</summary>
		public Coin CoinIn => Trade.CoinIn;

		/// <summary>The currency received</summary>
		public Coin CoinOut => Trade.CoinOut;

		/// <summary>Description of the amount sold in the trade</summary>
		public string TradeDescriptionIn => 
			$"{Math_.Clamp(Math_.Div<decimal>(Trade.AmountIn, AvailableIn, 0m), 0m, 1m):P1} of available {CoinIn}\n" +
			$"{Math_.Clamp(Math_.Div<decimal>(Trade.AmountIn, BalanceIn, 0m), 0m, 1m):P1} of account balance.\n";

		/// <summary>Description of the amount received from the trade</summary>
		public string TradeDescriptionOut => $"Fee: {Trade.Commission.ToString(8, true)} ({Trade.CommissionCoin.ValueOf(Trade.Commission).ToString(6,true)})";

		/// <summary>The colour associated with the trade type</summary>
		public Colour32 TradeTypeBgColour =>
			TradeType == ETradeType.Q2B ? SettingsData.Settings.Chart.Q2BColour.LerpRGB(Colour32.White,0.9f) :
			TradeType == ETradeType.B2Q ? SettingsData.Settings.Chart.B2QColour.LerpRGB(Colour32.White,0.9f) :
			throw new Exception($"Unknown trade type: {TradeType}");

		/// <summary>The amount held for the existing trade</summary>
		private Unit<decimal> ExistingHeld => (IsNewTrade || TradeType != m_existing_trade_type) ? 0m._(CoinIn) : m_existing_held;
		private ETradeType m_existing_trade_type;
		private Unit<decimal> m_existing_held;

		/// <summary>Signal re-evaluation of the validation state</summary>
		private void Invalidate()
		{
			m_validation = null;
			NotifyPropertyChanged(nameof(IsValid));
			NotifyPropertyChanged(nameof(ValidationResults));
			NotifyPropertyChanged(nameof(TradeDescriptionIn));
			NotifyPropertyChanged(nameof(TradeDescriptionOut));
			NotifyPropertyChanged(nameof(IsAmountInValid));
			NotifyPropertyChanged(nameof(IsAmountOutValid));
		}

		/// <summary>Validate the trade data</summary>
		public EValidation Validation
		{
			get => (m_validation ?? (m_validation = Trade.Validate(additional_balance_in: ExistingHeld))).Value;
		}
		private EValidation? m_validation;

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

		/// <summary>Text to display on the accept button</summary>
		public string AcceptText { get; }

		/// <summary>The dialog result when closed</summary>
		public bool? Result { get; private set; }

		/// <summary>Close the dialog in the 'accepted' state</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			// If the ok button isn't focused, ignore the command
			if (!m_btn_accept.IsFocused)
			{
				// Grab focus to ensure values are validated
				if (!m_btn_accept.Focus())
					throw new Exception("Create button cannot take input focus");

				return;
			}

			// Ignore accept until the trade is valid
			if (!IsValid)
				return;

			Result = true;
			Close();
		}

		/// <summary>Close the dialog in the 'cancelled' state</summary>
		public Command Cancel { get; }
		private void CancelInternal()
		{
			Result = false;
			Close();
		}

		/// <summary>Set the trade sell amount to the maximum available</summary>
		public Command SetSellAmountToMaximum { get; }
		private void SetSellAmountToMaximumInternal()
		{
			Trade.AmountIn = AvailableIn;
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}

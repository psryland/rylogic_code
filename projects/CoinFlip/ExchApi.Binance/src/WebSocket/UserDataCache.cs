using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Binance.API.DomainObjects;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;
using WebSocketSharp;

namespace Binance.API
{
	public class UserDataCache :IDisposable
	{
		public UserDataCache(BinanceApi api)
		{
			Api = api;
			Balances = new BalancesData();
			Orders = new OrdersStream(Api);
			History = new HistoryStream(Api);
			UserDataKeepAliveTimeout = TimeSpan.FromMinutes(30);
		}
		public void Dispose()
		{
			// Don't worry about closing the user stream, dropping the connection takes care of it
			Socket = null;
		}
		public async Task InitAsync()
		{
			Balances = await Api.GetBalances();
			await Orders.InitAsync();
			await History.InitAsync();

			// Start the user data stream. User data times out after 60minutes
			ListenKey = await Api.StartUserDataStream();
			m_listen_key_bump = DateTimeOffset.Now;

			// Create the socket (requires the ListenKey in the endpoint URL)
			Socket = new WebSocket(EndPoint);
			Socket.Connect();
		}

		/// <summary>The owning API instance</summary>
		private BinanceApi Api { get; }

		/// <summary>How often to kick the user data watchdog</summary>
		private TimeSpan UserDataKeepAliveTimeout { get; }

		/// <summary>A token used for user data identification</summary>
		private string ListenKey { get; set; }
		private DateTimeOffset m_listen_key_bump;

		/// <summary>Account balance data</summary>
		public BalancesData Balances
		{
			get // Worker thread context
			{
				lock (m_balance_data)
					return new BalancesData(m_balance_data);
			}
			private set
			{
				m_balance_data = value;
			}
		}
		private BalancesData m_balance_data;

		/// <summary>Currently active orders on the exchange (per currency pair)</summary>
		public OrdersStream Orders { get; }

		/// <summary>Completed trades on the exchange (per currency pair)</summary>
		public HistoryStream History { get; }

		/// <summary>The Url endpoint for the user data stream</summary>
		private string EndPoint => $"{Api.UrlSocketAddress}ws/{ListenKey}";

		/// <summary></summary>
		private WebSocket Socket
		{
			get { return m_socket; }
			set
			{
				if (m_socket == value) return;
				if (m_socket != null)
				{
					m_socket.OnClose -= HandleClosed;
					m_socket.OnError -= HandleError;
					m_socket.OnMessage -= HandleMessage;
					m_socket.OnOpen -= HandleOpened;
					Util.Dispose(ref m_socket);
				}
				m_socket = value;
				if (m_socket != null)
				{
					m_socket.OnOpen += HandleOpened;
					m_socket.OnMessage += HandleMessage;
					m_socket.OnError += HandleError;
					m_socket.OnClose += HandleClosed;
				}

				// Handlers
				void HandleOpened(object sender, EventArgs e)
				{
					BinanceApi.Log.Write(ELogLevel.Info, $"WebSocket stream opened for user data {ListenKey}");
				}
				void HandleClosed(object sender, CloseEventArgs e)
				{
					BinanceApi.Log.Write(ELogLevel.Info, $"WebSocket stream closed for user data {ListenKey}");
					Socket = null;
				}
				void HandleError(object sender, ErrorEventArgs e)
				{
					BinanceApi.Log.Write(ELogLevel.Error, e.Exception, $"WebSocket stream error for user data {ListenKey}");
					Socket = null;
					return;
				}
				void HandleMessage(object sender, MessageEventArgs e)
				{
					var jtok = JToken.Parse(e.Data);
					var event_type = jtok["e"].Value<string>();
					switch (event_type)
					{
					default:
						{
							BinanceApi.Log.Write(ELogLevel.Warn, $"Unknown event type received in user data stream");
							break;
						}
					case WebSocketEvent.EventName.AccountInfo:
						{
							ApplyUpdate(jtok.ToObject<AccountUpdate>());
							break;
						}
					case WebSocketEvent.EventName.OrderTrade:
						{
							ApplyUpdate(jtok.ToObject<TradeOrderUpdate>());
							break;
						}
					}

					// Kick the watchdog if it's time
					if (DateTimeOffset.Now - m_listen_key_bump > UserDataKeepAliveTimeout)
					{
						ListenKey = Api.KeepAliveUserDataStream(ListenKey).Result;
						m_listen_key_bump = DateTimeOffset.Now;
					}
				}
			}
		}
		private WebSocket m_socket;

		/// <summary>Update account data</summary>
		private void ApplyUpdate(AccountUpdate update)
		{
			lock (m_balance_data)
			{
				var map = update.Balances.ToDictionary(x => x.Asset, x => x);
				foreach (var bal in m_balance_data.Balances)
				{
					if (map.TryGetValue(bal.Asset, out var upd))
					{
						bal.Free = upd.Free;
						bal.Locked = upd.Locked;
					}
				}
				m_balance_data.UpdateTime = update.Updated;
			}
		}

		/// <summary>Update account data</summary>
		private void ApplyUpdate(TradeOrderUpdate update)
		{
			// Apply the consequences of this update to the orders and the history
			Orders.ApplyUpdate(update);
			History.ApplyUpdate(update);
		}

		/// <summary>Proxy object for lists of active orders</summary>
		public class OrdersStream
		{
			private readonly BinanceApi m_api;
			private readonly Dictionary<CurrencyPair, List<Order>> m_orders;

			public OrdersStream(BinanceApi api)
			{
				m_api = api;
				m_orders = new LazyDictionary<CurrencyPair, List<Order>>();
			}
			public async Task InitAsync()
			{
				// Get all open orders at start up.
				// That way any pair missing in 'm_orders' does not have any orders
				var orders = await m_api.GetOpenOrders();
				lock (m_orders)
				{
					foreach (var order in orders)
						m_orders[order.Pair] = new List<Order> { order };
				}
			}

			/// <summary>Access all orders for the given currency pair</summary>
			public List<Order> this[CurrencyPair pair] // Worker thread context
			{
				get
				{
					var orders_per_pair = (List<Order>)null;
					lock (m_orders)
					{
						if (!m_orders.TryGetValue(pair, out orders_per_pair))
							orders_per_pair = m_orders[pair] = new List<Order>();
					}
					lock (orders_per_pair)
					{
						return new List<Order>(orders_per_pair);
					}
				}
			}

			/// <summary>Apply an update to the trade history</summary>
			internal void ApplyUpdate(TradeOrderUpdate update)
			{
				// Get the orders associated with the pair in the update
				var orders_per_pair = (List<Order>)null;
				lock (m_orders)
				{
					if (!m_orders.TryGetValue(update.Pair, out orders_per_pair))
						orders_per_pair = m_orders[update.Pair] = new List<Order>();
				}

				// Apply the update to the specific order
				lock (orders_per_pair)
				{
					// If there is an existing order with this order id, update it
					var existing = orders_per_pair.FirstOrDefault(x => x.OrderId == update.OrderId);
					if (existing != null)
					{
						existing.ClientOrderId = update.ClientOrderId;
						existing.Price = update.Price;
						existing.Amount = update.AmountBase;
						existing.AmountCompleted = update.AmountBaseCumulativeFilled;
						existing.CummulativeAmountQuote = update.AmountQuoteCumulativeFilled;
						existing.Status = update.Status;
						existing.TimeInForce = update.TimeInForce;
						existing.OrderType = update.OrderType;
						existing.OrderSide = update.OrderSide;
						existing.StopPrice = update.StopPrice;
						existing.IcebergAmount = update.IcebergAmountBase;
						existing.Created = update.Created;
						existing.Updated = update.Updated;
						existing.IsWorking = update.IsOrderWorking;
					}

					// Do something to the order based on execution type
					switch (update.ExecutionType)
					{
					default: throw new Exception($"Unknown order update execution type: {update.ExecutionType}");
					case EExecutionType.NEW:
						{
							orders_per_pair.RemoveIf(x => x.OrderId == update.OrderId);
							orders_per_pair.Add(update);
							break;
						}
					case EExecutionType.CANCELED:
					case EExecutionType.REJECTED:
					case EExecutionType.EXPIRED:
						{
							orders_per_pair.RemoveIf(x => x.OrderId == update.OrderId);
							break;
						}
					case EExecutionType.REPLACED:
						{
							// Notes: the API docs say this is currently unused.
							// The order has been replaced with an updated version
							orders_per_pair.Remove(existing);
							orders_per_pair.Add(update);
							throw new NotSupportedException("Supposed to not be used according to the Binance API docs");
						}
					case EExecutionType.TRADE:
						{
							// A trade was made in relation to this order
							// The order was filled, so remove it from the orders list
							if (update.Status == EOrderStatus.PARTIALLY_FILLED) {}
							else if (update.Status == EOrderStatus.FILLED)
								orders_per_pair.RemoveIf(x => x.OrderId == update.OrderId);
							else
								throw new Exception("Order update with unhandled order status");
							break;
						}
					}
				}
			}
		}

		/// <summary>Proxy object for lists of active orders</summary>
		public class HistoryStream
		{
			private readonly BinanceApi m_api;
			private readonly Dictionary<CurrencyPair, List<OrderFill>> m_history;

			public HistoryStream(BinanceApi api)
			{
				m_api = api;
				m_history = new Dictionary<CurrencyPair, List<OrderFill>>();
			}
			public async Task InitAsync()
			{
				await Task.CompletedTask;
			}

			/// <summary>Access all historic trades for the given currency pair, since 'since' if given</summary>
			public List<OrderFill> this[CurrencyPair pair, DateTimeOffset? since = null, long? from_id = null]
			{
				get // Worker thread context
				{
					var history_per_pair = (List<OrderFill>)null;
					lock (m_history)
					{
						if (!m_history.TryGetValue(pair, out history_per_pair))
						{
							// Note: can't delay the 'GetTradeHistory' call because the caller needs
							// accurate results on *this* call, not at some later time.
							history_per_pair = m_history[pair] = m_api.GetTradeHistory(pair, from_id ?? 0L).Result;
							history_per_pair.Sort(x => x.Created);
						}
					}
					lock (history_per_pair)
					{
						if (since != null)
						{
							var idx = history_per_pair.BinarySearch(x => x.Created.CompareTo(since.Value), find_insert_position:true);
							return history_per_pair.GetRange(idx, history_per_pair.Count - idx);
						}
						else
							return new List<OrderFill>(history_per_pair);
					}
				}
			}

			/// <summary>Apply an update to the trade history</summary>
			internal void ApplyUpdate(TradeOrderUpdate update)
			{
				// Get the order fill list associated with the pair in the update
				var orderfills_per_pair = (List<OrderFill>)null;
				lock (m_history)
				{
					if (!m_history.TryGetValue(update.Pair, out orderfills_per_pair))
						orderfills_per_pair = m_history[update.Pair] = new List<OrderFill>();
				}

				// Add the update as a new OrderFill item
				lock (orderfills_per_pair)
				{
					orderfills_per_pair.RemoveIf(x => x.OrderId == update.OrderId && x.TradeId == update.TradeId);
					orderfills_per_pair.Add(update);
					orderfills_per_pair.Sort(x => x.Created);
				}
			}
		}

		/// <summary>Account balance update</summary>
		private class AccountUpdate
		{
			/// <summary></summary>
			[JsonProperty("e")]
			public string EventType { get; set; }

			/// <summary></summary>
			[JsonProperty("E"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset EventTime { get; set; }

			/// <summary></summary>
			[JsonProperty("m")]
			public decimal MakerCommissionRate { get; set; }

			/// <summary></summary>
			[JsonProperty("t")]
			public decimal TakerCommissionRate { get; set; }

			/// <summary></summary>
			[JsonProperty("b")]
			public decimal BuyerCommissionRate { get; set; }

			/// <summary></summary>
			[JsonProperty("s")]
			public decimal SellerCommissionRate { get; set; }

			/// <summary></summary>
			[JsonProperty("T")]
			public bool CanTrade { get; set; }

			/// <summary></summary>
			[JsonProperty("W")]
			public bool CanWithdraw { get; set; }

			/// <summary></summary>
			[JsonProperty("D")]
			public bool CanDeposit { get; set; }

			/// <summary></summary>
			[JsonProperty("u"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset Updated { get; set; }

			/// <summary></summary>
			[JsonProperty("B")]
			public List<BalanceData> Balances { get; set; }

			/// <summary></summary>
			public class BalanceData
			{
				[JsonProperty("a")]
				public string Asset { get; set; }

				[JsonProperty("f")]
				public decimal Free { get; set; }

				[JsonProperty("l")]
				public decimal Locked { get; set; }
			}
		}

		/// <summary>Order or OrderFill update</summary>
		internal class TradeOrderUpdate
		{
			/// <summary>Event type</summary>
			[JsonProperty("e")]
			public string EventType { get; set; }

			/// <summary>Event time</summary>
			[JsonProperty("E"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset EventTime { get; set; }

			/// <summary>Transaction instrument</summary>
			public CurrencyPair Pair { get; set; }
			[JsonProperty("s")] private string PairInternal { set => Pair = CurrencyPair.Parse(value); }

			/// <summary>Client order id</summary>
			[JsonProperty("c")]
			public string ClientOrderId { get; set; }

			/// <summary>Trade side</summary>
			[JsonProperty("S"), JsonConverter(typeof(ToEnum<EOrderSide>))]
			public EOrderSide OrderSide { get; set; }

			/// <summary>Order type</summary>
			[JsonProperty("o"), JsonConverter(typeof(ToEnum<EOrderType>))]
			public EOrderType OrderType { get; set; }

			/// <summary>Time in force</summary>
			[JsonProperty("f"), JsonConverter(typeof(ToEnum<ETimeInForce>))]
			public ETimeInForce TimeInForce { get; set; }

			/// <summary>Order quantity</summary>
			[JsonProperty("q")]
			public decimal AmountBase { get; set; }

			/// <summary>Order price</summary>
			[JsonProperty("p")]
			public decimal Price { get; set; }

			/// <summary>Order stop price</summary>
			[JsonProperty("P")]
			public decimal StopPrice { get; set; }

			/// <summary>Iceberg amount</summary>
			[JsonProperty("F")]
			public decimal IcebergAmountBase { get; set; }

			/// <summary>Original client order Id. This is the ID of the order being canceled</summary>
			[JsonProperty("C")]
			public string OriginalClientOrderId { get; set; }

			/// <summary>Current execution type</summary>
			[JsonProperty("x"), JsonConverter(typeof(ToEnum<EExecutionType>))]
			public EExecutionType ExecutionType { get; set; }

			/// <summary>Current order status</summary>
			[JsonProperty("X"), JsonConverter(typeof(ToEnum<EOrderStatus>))]
			public EOrderStatus Status { get; set; }

			/// <summary>Order rejection reason</summary>
			[JsonProperty("r"), JsonConverter(typeof(ToEnum<EOrderRejectReason>))]
			public EOrderRejectReason OrderRejectReason { get; set; }

			/// <summary>Order Id</summary>
			[JsonProperty("i")]
			public long OrderId { get; set; }

			/// <summary>Last executed quantity</summary>
			[JsonProperty("l")]
			public decimal AmountBaseLastExecuted { get; set; }

			/// <summary>Last quote asset transacted quantity (i.e. lastPrice * lastQty)</summary>
			[JsonProperty("Y")]
			public decimal AmountQuoteLastExecuted { get; set; }

			/// <summary>Cumulative filled quantity</summary>
			[JsonProperty("z")]
			public decimal AmountBaseCumulativeFilled { get; set; }

			/// <summary>Cumulative quote asset transacted amount</summary>
			[JsonProperty("Z")]
			public decimal AmountQuoteCumulativeFilled { get; set; }

			/// <summary>Last executed price</summary>
			[JsonProperty("L")]
			public decimal PriceLastExecuted { get; set; }

			/// <summary>Commission amount</summary>
			[JsonProperty("n")]
			public decimal Commission { get; set; }

			/// <summary>Commission asset</summary>
			[JsonProperty("N")]
			public string CommissionAsset { get; set; }

			/// <summary>Order created timestamp</summary>
			[JsonProperty("O"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset Created { get; set; }

			/// <summary>Transaction time</summary>
			[JsonProperty("T"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
			public DateTimeOffset Updated { get; set; }

			/// <summary>Trade ID</summary>
			[JsonProperty("t")]
			public long TradeId { get; set; }

			/// <summary>Is the order working? Stops will have</summary>
			[JsonProperty("w")]
			public bool IsOrderWorking { get; set; }

			/// <summary>Is this trade the maker side?</summary>
			[JsonProperty("m")]
			public bool IsMaker { get; set; }

			public static implicit operator Order(TradeOrderUpdate update)
			{
				return new Order
				{
					Pair = update.Pair,
					OrderId = update.OrderId,
					ClientOrderId = update.ClientOrderId,
					Price = update.Price,
					Amount = update.AmountBase,
					AmountCompleted = update.AmountBaseCumulativeFilled,
					CummulativeAmountQuote = update.AmountQuoteCumulativeFilled,
					Status = update.Status,
					TimeInForce = update.TimeInForce,
					OrderType = update.OrderType,
					OrderSide = update.OrderSide,
					StopPrice = update.StopPrice,
					IcebergAmount = update.IcebergAmountBase,
					Created = update.Created,
					Updated = update.Updated,
					IsWorking = update.IsOrderWorking,
				};
			}
			public static implicit operator OrderFill(TradeOrderUpdate update)
			{
				return new OrderFill
				{
					Pair = update.Pair,
					OrderId = update.OrderId,
					TradeId = update.TradeId,
					Side = update.OrderSide,
					Price = update.Price,
					AmountBase = update.AmountBase,
					AmountQuote = update.AmountBase * update.Price,
					Commission = update.Commission,
					CommissionAsset = update.CommissionAsset,
					Created = update.Updated,
					IsMaker = update.IsMaker,
				};
			}
		}
	}
}

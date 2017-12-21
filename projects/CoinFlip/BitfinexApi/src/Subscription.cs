using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API
{
	[DebuggerDisplay("{ChannelName} {ChannelId}")]
	public abstract class Subscription
	{
		private ClientWebSocket m_web_socket;
		public Subscription(string channel_name)
		{
			ChannelName = channel_name;
			ChannelId = null;
			State = EState.Initial;
		}

		/// <summary>The owning BitfinexApi instance</summary>
		public BitfinexApi Api { get; internal set; }

		/// <summary>The channel that this is a subscription for</summary>
		public string ChannelName { get; private set; }

		/// <summary>The channel identifier</summary>
		public int? ChannelId { get; set; }

		/// <summary>The current state of this subscription</summary>
		public EState State { get; private set; }

		/// <summary>True until a response is received that indicates the channel id associated with this subscription</summary>
		public bool Pending
		{
			get { return ChannelId == null; }
		}

		/// <summary>Subscribe to the channel. Must be called when an open web socket connection is available</summary>
		public void Start()
		{
			if (State != EState.Initial)
				throw new Exception($"Cannot start a subscription from state: {State}");

			// Prevent duplicate subscriptions
			Stop();

			try
			{
				// Record the web socket used to subscribe with.
				m_web_socket = Api.WebSocket;
				if (Api.WebSocket?.State == WebSocketState.Open)
				{
					// Send the subscription request
					State = EState.Starting;
					var msg = SubscribeRequest();
					using (Misc.NoSyncContext())
						Api.WebSocket.SendAsync(msg, Api.CancelToken).Wait();
				}
			}
			catch
			{
				State = EState.Initial;
				throw;
			}
		}

		/// <summary>Stop the subscription</summary>
		public void Stop()
		{
			// Stop if in the running state
			if (State != EState.Running && State != EState.Starting)
				return;

			try
			{
				// Only disconnect from the same web socket
				if (m_web_socket != Api.WebSocket) return;
				if (Api.WebSocket?.State == WebSocketState.Open && ChannelId != null)
				{
					// Send the unsubscribe request
					State = EState.Stopping;
					var msg = UnsubscribeRequest();
					using (Misc.NoSyncContext())
						Api.WebSocket.SendAsync(msg, Api.CancelToken).Wait();
				}
			}
			catch
			{
				State = EState.Initial;
				throw;
			}
		}

		/// <summary>Parse a snapshot or update message associated with this subscription</summary>
		internal void ParseUpdate(JArray j)
		{
			// All subscriptions can receive heart beat messages when there is no activity
			if (j.Count > 1 && j[1].Type == JTokenType.String)
			{
				var update_type = j[1].Value<string>();
				if (update_type == UpdateType.HeartBeat)
				{
					OnHeartBeat();
					return;
				}
			}

			// Forward to derived class
			ParseUpdateInternal(j);
		}

		/// <summary>Parse the response to the request message</summary>
		internal void ParseResponse(MsgBase msg)
		{
			ChannelId = msg.ChannelId;
			ChannelName = msg.ChannelName;

			// Handle the message
			ParseResponseInternal(msg);
		}

		/// <summary>Unsubscribe json message</summary>
		private string UnsubscribeRequest()
		{
			return Misc.JsonEncode(
				new KV("event", "unsubscribe"),
				new KV("chanId", ChannelId));
		}

		/// <summary>Return the request json message</summary>
		protected abstract string SubscribeRequest();

		/// <summary>Called when a heart beat message is received</summary>
		protected virtual void OnHeartBeat()
		{}

		/// <summary>Parse a snapshot or update message associated with this subscription</summary>
		protected abstract void ParseUpdateInternal(JArray j);

		/// <summary>Parse the response to the request message</summary>
		protected abstract void ParseResponseInternal(MsgBase msg);

		/// <summary>States of a subscription</summary>
		public enum EState
		{
			Initial,
			Starting,
			Running,
			Stopping,
		}

		#region Equals
		public bool Equals(Subscription rhs)
		{
			return rhs != null && ChannelName == rhs.ChannelName;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Subscription);
		}
		public override int GetHashCode()
		{
			return ChannelName.GetHashCode();
		}
		#endregion
	}

	/// <summary>A subscription object for handling account update messages</summary>
	[DebuggerDisplay("Acct {ChannelName} {ChannelId}")]
	public class SubscriptionAccount :Subscription
	{
		private MsgAuth m_auth;

		public SubscriptionAccount()
			:base(API.ChannelName.Account)
		{}

		/// <summary>Return the request json message</summary>
		protected override string SubscribeRequest()
		{
			// Authenticate / subscribe to account data
			var nonce = Misc.Nonce;
			var payload = $"AUTH{nonce}";
			var signature = Misc.ToStringHex(Api.Hasher.ComputeHash(Encoding.UTF8.GetBytes(payload)));
			return Misc.JsonEncode(
				new KV("event", "auth"),
				new KV("apiKey", Api.Key),
				new KV("authSig", signature),
				new KV("authPayload", payload),
				new KV("authNonce", nonce),
				new KV("filter", new[]{ "trading", "wallet", "balance" }));
		}
		
		/// <summary>Parse the response to the request message</summary>
		protected override void ParseResponseInternal(MsgBase msg)
		{
			m_auth = (MsgAuth)msg;
		}

		/// <summary>Parse a snapshot or update message associated with this subscription</summary>
		protected override void ParseUpdateInternal(JArray j)
		{
			if (j.Count > 1 && j[1].Type == JTokenType.String)
			{
				var update_type = j[1].Value<string>();
				switch (update_type)
				{
				default:
					{
						throw new Exception($"Unknown update type: {update_type}");
					}
				case UpdateType.WalletSnapshot:
					{
						// Snapshots have an array of wallet states
						foreach (var jtok in (JArray)j[2])
							Api.Wallet.ParseUpdate((JArray)jtok);

						Api.RaiseWalletChanged();
						return;
					}
				case UpdateType.WalletUpdate:
					{
						Api.Wallet.ParseUpdate((JArray)j[2]);
						Api.RaiseWalletChanged();
						return;
					}
				case UpdateType.OrderSnapshot:
					{
						Api.Orders.Clear();
						foreach (var jtok in (JArray)j[2])
							Api.Orders.ParseUpdate(UpdateType.OrderNew, (JArray)jtok);

						Api.RaiseOrdersChanged();
						return;
					}
				case UpdateType.OrderNew:
				case UpdateType.OrderCancel:
				case UpdateType.OrderUpdate:
					{
						Api.Orders.ParseUpdate(update_type, (JArray)j[2]);
						Api.RaiseOrdersChanged();
						return;
					}
				case UpdateType.TradeExecuted:
				case UpdateType.TradeUpdate:
					{
						Api.History.ParseUpdate(update_type, (JArray)j[2]);
						return;
					}

				case UpdateType.PositionSnapshot:
					{
						foreach (var jtok in (JArray)j[2])
							throw new NotImplementedException();

						return;
					}
				case UpdateType.PositionUpdate:
					{
						throw new NotImplementedException();
					}
				}
			}
			throw new Exception($"Unknown update");
		}
	}

	/// <summary>An order book subscription</summary>
	[DebuggerDisplay("OrderBook {ChannelName} {ChannelId} {Pair}")]
	public class SubscriptionOrderBook :Subscription
	{
		public SubscriptionOrderBook(CurrencyPair pair, int depth = 25, EFrequency freq = EFrequency.F0, EPrecision prec = EPrecision.P0)
			:base(API.ChannelName.Book)
		{
			Pair = pair;
			Frequency = freq;
			Depth = depth;
			Precision = prec;
		}

		/// <summary>The pair that the order book is for</summary>
		public CurrencyPair Pair { get; private set; }

		/// <summary>The number of entries in the order book</summary>
		public int Depth { get; private set; }

		/// <summary>Update frequency of order book</summary>
		public EFrequency Frequency { get; private set; }

		/// <summary>Level of price aggregation</summary>
		public EPrecision Precision { get; private set; }

		/// <summary>Return the request json message</summary>
		protected override string SubscribeRequest()
		{
			return Misc.JsonEncode(
				new KV("event", "subscribe"),
				new KV("channel", ChannelName),
				new KV("symbol", Pair.Id),
				new KV("prec", Precision.ToString()),
				new KV("freq", Frequency.ToString()),
				new KV("len", Depth));
		}

		/// <summary>Parse the response to the request message</summary>
		protected override void ParseResponseInternal(MsgBase msg)
		{}

		/// <summary>Parse a snapshot or update message associated with this subscription</summary>
		protected override void ParseUpdateInternal(JArray j)
		{
			if (j.Count <= 1)
				throw new Exception("Invalid snapshot/update message");
			if (j[0].Value<int>() != ChannelId.Value)
				throw new Exception("Order book update for a different channel");

			Api.Market.ParseUpdate(Pair, (JArray)j[1]);
			Api.RaiseMarketChanged();
		}

		#region Equals
		public bool Equals(SubscriptionOrderBook rhs)
		{
			return base.Equals(rhs) && Equals(Pair, rhs.Pair);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as SubscriptionOrderBook);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode() * 23 + Pair.GetHashCode();
		}
		#endregion
	}

	/// <summary>Candle data updates</summary>
	[DebuggerDisplay("CandleData {ChannelName} {ChannelId} {Pair}")]
	public class SubscriptionCandleData :Subscription
	{
		public SubscriptionCandleData(CurrencyPair pair, EMarketTimeFrames tf)
			:base(API.ChannelName.Candles)
		{
			Pair = pair;
			TimeFrame = tf;
		}

		/// <summary>The pair that the order book is for</summary>
		public CurrencyPair Pair { get; private set; }

		/// <summary>The time frame of the candle data to get</summary>
		public EMarketTimeFrames TimeFrame { get; private set; }

		/// <summary>Return the request json message</summary>
		protected override string SubscribeRequest()
		{
			return Misc.JsonEncode(
				new KV("event", "subscribe"),
				new KV("channel", ChannelName),
				new KV("key", $"trade:{Misc.ToRequestString(TimeFrame)}:{Pair.Id}"));
		}

		/// <summary>Parse the response to the request message</summary>
		protected override void ParseResponseInternal(MsgBase msg)
		{}

		/// <summary>Parse a snapshot or update message associated with this subscription</summary>
		protected override void ParseUpdateInternal(JArray j)
		{
			if (j.Count <= 1)
				throw new Exception("Invalid snapshot/update message");
			if (j[0].Value<int>() != ChannelId.Value)
				throw new Exception("Order book update for a different channel");

			Api.Candles.ParseUpdate(Pair, TimeFrame, (JArray)j[1]);
			Api.RaiseCandlesChanged();
		}

		#region Equals
		public bool Equals(SubscriptionCandleData rhs)
		{
			return base.Equals(rhs) && Equals(Pair, rhs.Pair);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as SubscriptionCandleData);
		}
		public override int GetHashCode()
		{
			return base.GetHashCode() * 23 + new { Pair, TimeFrame }.GetHashCode();
		}
		#endregion
	}
}

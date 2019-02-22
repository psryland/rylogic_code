using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API
{
	public enum EMsgType
	{
		Unknown,
		Info,
		Auth,
		Error,
		Subscribed,
		Unsubscribed
	}
	public enum EErrorCode
	{
		ERR_UNK            = 10000, // Unknown error
		ERR_GENERIC        = 10001, // Generic error
		ERR_CONCURRENCY    = 10008, // Concurrency error
		ERR_PARAMS         = 10020, // Request parameters error
		ERR_CONF_FAIL      = 10050, // Configuration set up failed
		ERR_AUTH_FAIL      = 10100, // Failed authentication
		ERR_AUTH_PAYLOAD   = 10111, // Error in authentication request payload
		ERR_AUTH_SIG       = 10112, // Error in authentication request signature
		ERR_AUTH_HMAC      = 10113, // Error in authentication request encryption
		ERR_AUTH_NONCE     = 10114, // Error in authentication request nonce
		ERR_UNAUTH_FAIL    = 10200, // Error in un-authentication request
		ERR_SUB_FAIL       = 10300, // Failed channel subscription
		ERR_SUB_MULTI      = 10301, // Failed channel subscription: already subscribed
		ERR_UNSUB_FAIL     = 10400, // Failed channel un-subscription: channel not found
		ERR_NOT_SUBSCRIBED = 10401, // Not subscribed
		ERR_READY          = 11000, // Not ready, try again later
		EVT_STOP           = 20051, // Web socket server stopping... please reconnect later
		EVT_RESYNC_START   = 20060, // Web socket server re-syncing... please reconnect later
		EVT_RESYNC_STOP    = 20061, // Web socket server re-sync complete. please reconnect
		EVT_INFO           = 5000 , // Info message
	}
	public enum EInfoMessageCode
	{
		APIVersion = 0,
		ReconnectWebSocket = 20051,
		MaintenanceModeBeg = 20060,
		MaintenanceModeEnd = 20061,
	}
	public enum EOrderStatus
	{
		Active,
		Executed,
		PartiallyFilled,
		Cancelled,
	}
	public enum EWalletType
	{
		Exchange,
		Margin,
		Funding,
	}
	public enum EFrequency
	{
		F0, // = Real time
		F1, // = Every 2 Sec
	}
	public enum EPrecision
	{
		P0,
		P1,
		P2,
		P3,
	}
	internal static class ChannelName
	{
		public const string Account = "";
		public const string Ticker  = "ticker";
		public const string Trades  = "trades";
		public const string Book    = "book";
		public const string Candles = "candles";
	}
	internal static class UpdateType
	{
		public const string HeartBeat                        = "hb";
		public const string BalanceUpdate                    = "bu";
		public const string PositionSnapshot                 = "ps";
		public const string NewPosition                      = "pn";
		public const string PositionUpdate                   = "pu";
		public const string PositionClose                    = "pc";
		public const string WalletSnapshot                   = "ws";
		public const string WalletUpdate                     = "wu";
		public const string OrderSnapshot                    = "os";
		public const string OrderNew                         = "on";
		public const string OrderUpdate                      = "ou";
		public const string OrderCancel                      = "oc";
		public const string OrderCancelRequest               = "oc-req";
		public const string TradeExecuted                    = "te";
		public const string TradeUpdate                      = "tu";
		public const string FundingTradeExecution            = "fte";
		public const string FundingTradeUpdate               = "ftu";
		public const string HistoricalOrderSnapshot          = "hos";
		public const string MarginInformationSnapshot        = "mis";
		public const string MarginInformationUpdate          = "miu";
		public const string Notification                     = "n";
		public const string FundingOfferSnapshot             = "fos";
		public const string FundingOfferNew                  = "fon";
		public const string FundingOfferUpdate               = "fou";
		public const string FundingOfferCancel               = "foc";
		public const string HistoricalFundingOfferSnapshot   = "hfos";
		public const string FundingCreditsSnapshot           = "fcs";
		public const string FundingCreditsNew                = "fcn";
		public const string FundingCreditsUpdate             = "fcu";
		public const string FundingCreditsClose              = "fcc";
		public const string HistoricalFundingCreditsSnapshot = "hfcs";
		public const string FundingLoanSnapshot              = "fls";
		public const string FundingLoanNew                   = "fln";
		public const string FundingLoanUpdate                = "flu";
		public const string FundingLoanClose                 = "flc";
		public const string HistoricalFundingLoanSnapshot    = "hfls";
		public const string HistoricalFundingTradeSnapshot   = "hfts";
		public const string UserCustomPriceAlert             = "uac";
	}

	#region Event Messages

	/// <summary>Base class for all event messages</summary>
	[DebuggerDisplay("{EventType}")]
	public class MsgBase
	{
		public MsgBase()
			:this(EMsgType.Unknown)
		{}
		public MsgBase(EMsgType msg_type)
		{
			EventType = msg_type;
		}

		/// <summary>The event type</summary>
		public EMsgType EventType { get; private set; }
		[JsonProperty("event")] public string EventTypeString
		{
			get { return m_event_type; }
			set
			{
				m_event_type = value;
				EventType = Enum.TryParse(value, true, out EMsgType event_type) ? event_type : EMsgType.Unknown;
			}
		}
		private string m_event_type;

		/// <summary>The channel id associated with this message response (null if not associated with a channel)</summary>
		[JsonProperty("chanId")]
		public int? ChannelId { get; private set; }

		/// <summary>The name of the channel subscribed to (null if not associated with a channel)</summary>
		[JsonProperty("channel")]
		public string ChannelName { get; private set; }
	}

	/// <summary>An error response message</summary>
	[DebuggerDisplay("{EventType} {Message}")]
	public class MsgError :MsgBase
	{
		/// <summary>The error message</summary>
		[JsonProperty("msg")]
		public string Message { get; private set; }

		/// <summary>The error code</summary>
		public EErrorCode Code { get; private set; }
		[JsonProperty("code")] private int CodeInternal
		{
			set { Code = (EErrorCode)value; }
		}
	}

	/// <summary>API version message</summary>
	[DebuggerDisplay("{EventType} {Version}")]
	public class MsgInfo :MsgBase
	{
		/// <summary>Bitfinex API version</summary>
		[JsonProperty("version")]
		public int? Version { get; private set; }

		/// <summary>Info message code</summary>
		public EInfoMessageCode Code { get; private set; }
		[JsonProperty("code")] private int CodeInternal
		{
			set { Code = (EInfoMessageCode)value; }
		}

		/// <summary>Info message code</summary>
		[JsonProperty("msg")]
		public string Message { get; private set; }
	}

	/// <summary>API version message</summary>
	[DebuggerDisplay("{EventType} {ClientId}")]
	public class MsgPong
	{
		/// <summary>The current server time</summary>
		public DateTimeOffset Time { get; private set; }
		[JsonProperty("ts")] private ulong TimeInternal
		{
			set { Time = Misc.ToDateTimeOffset(value/1000); }
		}

		/// <summary>The Client Id</summary>
		[JsonProperty("cid")]
		public int ClientId { get; private set; }
	}

	/// <summary>Auth acknowledgement message</summary>
	[DebuggerDisplay("{EventType} {Status}")]
	public class MsgAuth :MsgBase
	{
		/// <summary></summary>
		[JsonProperty("status")]
		public string Status { get; private set; }

		/// <summary></summary>
		[JsonProperty("userId")]
		public int UserId { get; private set; }

		/// <summary></summary>
		public Guid AuthId { get; private set; }
		[JsonProperty("auth_id")] private string AuthIdInternal
		{
			set { AuthId = Guid.Parse(value); }
		}

		/// <summary></summary>
		[JsonProperty("caps")]
		public CapsData Caps { get; private set; }

		public class CapsData
		{
			/// <summary></summary>
			[JsonProperty("orders")]
			public Cap Orders { get; private set; }

			/// <summary></summary>
			[JsonProperty("account")]
			public Cap Account { get; private set; }

			/// <summary></summary>
			[JsonProperty("funding")]
			public Cap Funding { get; private set; }

			/// <summary></summary>
			[JsonProperty("history")]
			public Cap History { get; private set; }

			/// <summary></summary>
			[JsonProperty("wallets")]
			public Cap Wallets { get; private set; }

			/// <summary></summary>
			[JsonProperty("withdraw")]
			public Cap Withdraw { get; private set; }

			/// <summary></summary>
			[JsonProperty("positions")]
			public Cap Positions { get; private set; }
		}
		[DebuggerDisplay("R={Read} W={Write}")]
		public struct Cap
		{
			[JsonProperty("read")] public bool Read { get; private set; }
			[JsonProperty("write")] public bool Write { get; private set; }
		}
	}

	#region Subscription Response Events

	/// <summary>Response to a 'book' channel subscription</summary>
	[DebuggerDisplay("{EventType} {ChannelId} {Symbol}")]
	public class MsgSubscribedBook :MsgBase
	{
		/// <summary>The currency of the order book</summary>
		[JsonProperty("symbol")]
		public string Symbol { get; private set; }

		/// <summary>The level of price aggregation</summary>
		[JsonProperty("prec")]
		public string Precision { get; private set; }

		/// <summary>How frequently updates to the order book are received</summary>
		[JsonProperty("freq")]
		public string Frequency { get; private set; }

		/// <summary>Depth of the order book</summary>
		[JsonProperty("len")]
		public string Depth { get; private set; }
	}

	#endregion

	/// <summary>Response to an unsubscribe request</summary>
	[DebuggerDisplay("{EventType} {ChannelId} {Status}")]
	public class MsgUnsubscribed :MsgBase
	{
		/// <summary>The name of the channel subscribed to</summary>
		[JsonProperty("status")]
		public string Status { get; private set; }
	}

	#endregion

	#region Input Messages

	public class CalcRequest
	{
	}

	#endregion
}

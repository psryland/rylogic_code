using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Rylogic.Extn;

namespace ExchApi.Common
{
#if false
	[DebuggerDisplay("{ChannelName} {ChannelId}")]
	public abstract class Subscription
	{
		public Subscription(string channel_name)
		{
			State = EState.Initial;
			ChannelName = channel_name;
			ChannelId = null;
		}

		/// <summary>The web socket used for the subscription</summary>
		private ClientWebSocket WebSocket { get; set; }

		/// <summary>The owning BitfinexApi instance</summary>
		public IExchangeApi Api { get; set; }

		/// <summary>The channel that this is a subscription for</summary>
		public string ChannelName { get; }

		/// <summary>The channel identifier</summary>
		public int? ChannelId { get; set; }

		/// <summary>The current state of this subscription</summary>
		public EState State { get; private set; }

		/// <summary>True until a response is received that indicates the channel id associated with this subscription</summary>
		public bool Pending => ChannelId == null;

		/// <summary>Subscribe to the channel. Must be called when an open web socket connection is available</summary>
		public async Task Start()
		{
			if (State != EState.Initial)
				throw new Exception($"Cannot start a subscription from state: {State}");

			// Prevent duplicate subscriptions
			await Stop();

			try
			{
				// Record the web socket used to subscribe with.
				WebSocket = Api.WebSocket;
				if (WebSocket?.State == WebSocketState.Open)
				{
					// Send the subscription request
					State = EState.Starting;
					var json_request = SubscribeRequest();
					using (Task_.NoSyncContext())
						await WebSocket.SendAsync(json_request, Api.Shutdown);
				}
			}
			catch
			{
				State = EState.Initial;
				throw;
			}
		}

		/// <summary>Stop the subscription</summary>
		public async Task Stop()
		{
			// Stop if in the running state
			if (State != EState.Running && State != EState.Starting)
				return;

			try
			{
				// Only disconnect from the same web socket
				if (WebSocket == null || WebSocket != Api.WebSocket)
					return;

				// Disconnect if open
				if (WebSocket.State == WebSocketState.Open && ChannelId != null)
				{
					// Send the unsubscribe request
					State = EState.Stopping;
					var json_request = UnsubscribeRequest();
					using (Task_.NoSyncContext())
						await Api.WebSocket.SendAsync(json_request, Api.Shutdown);
				}
			}
			catch
			{
				State = EState.Initial;
				throw;
			}
		}

		/// <summary>Return the request json message</summary>
		protected abstract string SubscribeRequest();

		/// <summary>Unsubscribe json message</summary>
		protected abstract string UnsubscribeRequest();

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
#endif
}
#if false
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

		/// <summary>Called when a heart beat message is received</summary>
		protected virtual void OnHeartBeat()
		{ }

		/// <summary>Parse a snapshot or update message associated with this subscription</summary>
		protected abstract void ParseUpdateInternal(JArray j);

		/// <summary>Parse the response to the request message</summary>
		protected abstract void ParseResponseInternal(MsgBase msg);

#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Binance.API.DomainObjects;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Binance.API
{
	public class MarketDataSubscription : IDisposable
	{
		// Notes:
		//  - Manages a local order book for the given trading pair.
		// Steps:
		//  - Open a stream to wss://stream.binance.com:9443/ws/<stream>@depth
		//  - Buffer the events you receive from the stream
		//  - Get a depth snapshot from https://www.binance.com/api/v1/depth?symbol=BNBBTC&limit=1000
		//  - Drop any event where u is <= lastUpdateId in the snapshot
		//  - The first processed should have U <= lastUpdateId+1 AND u >= lastUpdateId+1
		//  - While listening to the stream, each new event's U should be equal to the previous event's u+1
		//  - The data in each event is the absolute quantity for a price level
		//  - If the quantity is 0, remove the price level
		//  - Receiving an event that removes a price level that is not in your local order book can happen and is normal.

		public MarketDataSubscription(CurrencyPair pair, string wss_stream, CancellationToken shutdown)
		{
			Pair = pair;
			Stream = wss_stream;
			Shutdown = shutdown;
			Dispatcher = Dispatcher.CurrentDispatcher;
			OrderBook = new MarketDepth();
			Active = true;
		}
		public void Dispose()
		{
			Active = false;
		}

		/// <summary>The trading pair </summary>
		public CurrencyPair Pair { get; }

		/// <summary>The socket stream</summary>
		private string Stream { get; }

		/// <summary></summary>
		private CancellationToken Shutdown { get; }

		/// <summary>The main thread dispatcher</summary>
		private Dispatcher Dispatcher { get; }

		/// <summary>The order book for 'Pair'</summary>
		private MarketDepth OrderBook { get; }

		/// <summary>Start/Stop the web socket stream</summary>
		private bool Active
		{
			get { return m_worker != null; }
			set
			{
				if (Active == value) return;
				if (Active)
				{
					m_cancel.Cancel();
					m_worker.Join();
				}
				m_worker = value ? new Thread(new ParameterizedThreadStart(WorkerEntryPoint)) : null;
				if (Active)
				{
					m_cancel = CancellationTokenSource.CreateLinkedTokenSource(Shutdown);
					m_worker.Start(new Uri(Stream));
				}

				// Handler
				async void WorkerEntryPoint(object arg)
				{
					try
					{
						Thread.CurrentThread.Name = $"MarketDataSub-{Pair.Id}";
						using (var socket = new ClientWebSocket())
						{
							var buffer = new byte[0x10000];
							var last_ping_time = DateTimeOffset.UtcNow;

							// Loop until cancelled or done
							for (var done = false; !m_cancel.IsCancellationRequested && !done;)
							{
								switch (socket.State)
								{
								default:
									{
										throw new Exception($"Unknown socket state: {socket.State}");
									}
								// Initialise
								case WebSocketState.None:
									{
										await socket.ConnectAsync(new Uri(Stream), m_cancel.Token);
										break;
									}
								// The connection is negotiating the handshake with the remote endpoint.
								case WebSocketState.Connecting:
									{
										// Wait...
										break;
									}
								// The initial state after the HTTP handshake has been completed.
								case WebSocketState.Open:
									{
										// Send a 'pong' every 30mins to keep the connection alive
										if (DateTimeOffset.UtcNow - last_ping_time > TimeSpan.FromMinutes(30))
										{
											// Pong
											last_ping_time = DateTimeOffset.UtcNow;
										}

										// Receive data
										var chunk = new ArraySegment<byte>(buffer);
										var res = await socket.ReceiveAsync(chunk, m_cancel.Token);
										if (res.MessageType == WebSocketMessageType.Close)
											await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, m_cancel.Token);
										else
											HandleMessage(res);
										break;
									}
								// A close message was sent to the remote endpoint.
								case WebSocketState.CloseSent:
									{
										// Wait...
										break;
									}
								// A close message was received from the remote endpoint.
								case WebSocketState.CloseReceived:
									{
										// Shutdown
										await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, m_cancel.Token);
										break;
									}
								// Indicates the WebSocket close handshake completed gracefully.
								case WebSocketState.Closed:
									{
										done = true;
										break;
									}
								// Aborted
								case WebSocketState.Aborted:
									{
										await Dispatcher.BeginInvoke(new Action(() => Active = false));
										break;
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Unexpected thread exit: {ex.MessageFull()}");
						await Dispatcher.BeginInvoke(new Action(() => Active = false));
					}
				}
				void HandleMessage(WebSocketReceiveResult res)
				{
					switch (res.MessageType)
					{
					default:
						{
							throw new Exception($"Unexpected message type: {res.MessageType}");
						}
					case WebSocketMessageType.Binary:
						{
							break;
						}
					case WebSocketMessageType.Text:
						{
							break;
						}
					}
				}
			}
		}
		private CancellationTokenSource m_cancel;
		private Thread m_worker;
	}
}

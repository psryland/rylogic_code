using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;
using SolarHotWater.Common;
using WebSocket = Rylogic.Net.WebSocket;

namespace EweLink
{
	public sealed class EweLinkAPI :IDisposable
	{
		// Notes:
		//  - REST interface to the eWeLink API
		private const string APP_ID = "YzfeftUVcZ6twZw1OoVKPRFYTrGEg01Q";
		private const string APP_SECRET = "4G91qSoboqYO4Y0XJ0LPPKIsq8reHdfa";

		public EweLinkAPI(CancellationToken shutdown)
		{
			Shutdown = shutdown;
			Client = new HttpClient();
			WebSocket = new WebSocket(Shutdown);
			Hasher = new HMACSHA256(Encoding.UTF8.GetBytes(APP_SECRET));
			DeviceList = new EweDeviceList();
		}
		public void Dispose()
		{
			WebSocket = null!;
			Client = null!;
		}

		/// <summary>EweLink REST API</summary>
		private string Url => "https://us-api.coolkit.cc:8080/";

		/// <summary>EweLink web socket endpoint</summary>
		private string EndPoint => "wss://us-pconnect3.coolkit.cc:8080/api/ws";

		/// <summary>Shutdown token</summary>
		private CancellationToken Shutdown { get; }

		/// <summary>The Http client for REST requests</summary>
		private HttpClient Client
		{
			get => m_client;
			set
			{
				if (m_client == value) return;
				if (m_client != null)
				{
					Util.Dispose(ref m_client!);
				}
				m_client = value;
				if (m_client != null)
				{
					m_client.BaseAddress = new Uri(Url);
					m_client.Timeout = TimeSpan.FromSeconds(10);
				}
			}
		}
		private HttpClient m_client = null!;

		/// <summary>Web socket connection to EweLink</summary>
		private WebSocket WebSocket
		{
			get => m_socket;
			set
			{
				if (m_socket == value) return;
				if (m_socket != null)
				{
					m_socket.OnClose -= HandleClosed;
					m_socket.OnError -= HandleError;
					m_socket.OnHeartbeat -= HandleHeartbeat;
					m_socket.OnMessage -= HandleMessage;
					m_socket.OnOpen -= HandleOpened;
					Util.Dispose(ref m_socket!);
				}
				m_socket = value;
				if (m_socket != null)
				{
					m_socket.OnOpen += HandleOpened;
					m_socket.OnMessage += HandleMessage;
					m_socket.OnHeartbeat += HandleHeartbeat;
					m_socket.OnError += HandleError;
					m_socket.OnClose += HandleClosed;
				}

				// Handlers
				void HandleOpened(object? sender, WebSocket.OpenEventArgs e)
				{
				}
				void HandleClosed(object? sender, WebSocket.CloseEventArgs e)
				{
				}
				void HandleError(object? sender, WebSocket.ErrorEventArgs e)
				{
				}
				async void HandleHeartbeat(object? sender, EventArgs e)
				{
					try
					{
						if (!(WebSocket is WebSocket ws)) return;
						if (!ws.Healthy || !ws.Listening || Cred == null)
							throw new Exception("eWeLink WebSocket connection lost");

						await ws.SendAsync("ping", Shutdown);
					}
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, ex, $"Error during eWeLink HandleHeartbeat");
						await Logout();
					}
				}
				async void HandleMessage(object? sender, WebSocket.MessageEventArgs e)
				{
					try
					{
						Log.Write(ELogLevel.Debug, e.Text != "pong" ? e.Text : "heartbeat");

						// Heartbeat response
						if (e.Text == "pong")
							return;

						// Otherwise expect a JSON response
						var jobj = JObject.Parse(e.Text);

						// Look for errors
						if (jobj["error"]?.Value<int>() is int err && err != 0)
						{
							var msg = jobj["msg"]?.Value<string>() ?? string.Empty;
							Log.Write(ELogLevel.Error, $"EweLink Error Received: {err} {msg}");
							await Logout();
							return;
						}

						// Login replies contain a config
						if (jobj["config"] is JObject config)
						{
							// Configure the heartbeat if the message says to
							if (config["hb"]?.Value<int>() is int hb && hb != 0 &&
								config["hbInterval"]?.Value<int>() is int hb_interval)
							{
								WebSocket.Heartbeat = TimeSpan.FromSeconds(hb_interval);
							}
							else
							{
								WebSocket.Heartbeat = TimeSpan.Zero;
							}
						}

						// All messages seem to have an api key, check it against our credentials
						if (jobj["apikey"]?.Value<string>() is string apikey)
						{
							if (Cred?.User.ApiKey != apikey)
							{
								Log.Write(ELogLevel.Warn, $"eWeLink API key changed");
								await Logout();
								return;
							}
						}

						// Update action?
						if (jobj["action"]?.Value<string>() is string action)
						{
							switch (action)
							{
								// Message from the device itself
								case "update":
								{
									// Find the device that this update is for
									var device_id = jobj["deviceid"]?.Value<string>();
									var device = Devices.FirstOrDefault(x => x.DeviceID == device_id);
									if (device == null)
										break;

									// Apply the updates to the state of the device
									if (jobj["params"] is JObject parms)
										device.Update(parms);

									break;
								}

								// Message from the cloud about the device
								case "sysmsg":
								{
									// Find the device that this update is for
									var device_id = jobj["deviceid"]?.Value<string>();
									var device = Devices.FirstOrDefault(x => x.DeviceID == device_id);
									if (device == null)
										break;

									// Apply the updates to the state of the device
									if (jobj["params"] is JObject parms)
										device.Update(parms);

									break;
								}

								// Messages I haven't seen yet
								default:
								{
									throw new Exception($"Unknown action received: {action}\n{jobj}");
								}
							}
						}
					}
					catch (OperationCanceledException) { }
					catch (Exception ex)
					{
						Debugger.Break();
						Log.Write(ELogLevel.Error, ex, $"Unknown error during eWeLink HandleMessage");
						await Logout();
					}
				}
			}
		}
		private WebSocket m_socket = null!;

		/// <summary></summary>
		private HMACSHA256 Hasher { get; }

		/// <summary>EweLink credentials</summary>
		public EweCred? Cred { get; private set; }

		/// <summary>The Devices associated with this EweLink account</summary>
		public IEweDeviceList Devices => DeviceList;
		private EweDeviceList DeviceList { get; }

		/// <summary>Get the login credentials</summary>
		public async Task Login(string username, string password, CancellationToken? cancel = null)
		{
			// Already logged on
			if (Cred != null)
				return;

			Log.Write(ELogLevel.Info, $"EweLinkAPI: Logging In as {username}");
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;

			// REST request for the credentials
			{
				var parms = new Params
				{
					{ "appid", APP_ID },
					{ "email", username },
					{ "password", password },
					{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
					{ "version", 8 },
					{ "nonce", Nonce },
				};

				var body = JObject.FromObject(parms).ToString();
				var hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(body));
				var sign = Convert.ToBase64String(hash);

				// Create the request
				var url = $"{Url}api/user/login";
				var req = new HttpRequestMessage(HttpMethod.Post, url);
				req.Headers.Authorization = new AuthenticationHeaderValue("Sign", sign);
				req.Content = new StringContent(body);

				// Submit the request
				var response = await Client.SendAsync(req, cancel_token);
				var reply = await response.Content.ReadAsStringAsync();
				var jtok = JToken.Parse(reply);

				// Check for an error
				if (jtok["error"]?.Value<int>() is int error && error != 0)
				{
					var msg = jtok["msg"]?.Value<string>() ?? string.Empty;
					throw new Exception($"eWeLink: Error requested credentials: {error} {msg}");
				}

				// Save the credentials
				var cred = jtok.ToObject<EweCred>() ?? throw new Exception("Credentials not available");
				if (cred.AccessToken.Length == 0)
					throw new Exception("Credentials did not contain an access token");

				Cred = cred;
			}

			// Populate the devices collection
			{
				var parms = new Params()
				{
					{ "appid", APP_ID },
					{ "lang", "en" },
					{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
					{ "version", 8 },
					{ "getTags", 1 },
				};

				var url = $"{Url}api/user/device{Http_.UrlEncode(parms)}";
				var req = new HttpRequestMessage(HttpMethod.Get, url);
				req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Cred.AccessToken);

				// Submit the request
				var response = await Client.SendAsync(req, cancel_token);
				var reply = await response.Content.ReadAsStringAsync();
				var jobj = JObject.Parse(reply);

				// Check for an error
				if (jobj["error"]?.Value<int>() is int error && error != 0)
					throw new Exception($"eWeLink: Error retrieving device list: {error}");

				// Populate the device list
				if (jobj["devicelist"] is JArray devicelist)
				{
					foreach (var jdevice in devicelist.Cast<JObject>())
					{
						var device_type = jdevice["type"]?.Value<string>() ?? string.Empty;
						switch (device_type)
						{
							case "10":
							{
								DeviceList.Add(new EweSwitch(jdevice));
								break;
							}
							default:
							{
								throw new Exception($"Unknown device type: {device_type}");
							}
						}
					}
				}
			}
		}
		public async Task Logout()
		{
			Log.Write(ELogLevel.Info, $"EweLinkAPI: Logging out");
			await WebSocket.Close();
			DeviceList.Clear();
			Cred = null;
		}

		/// <summary>Create a web socket connection to the EweLink cloud server</summary>
		public async Task OpenWebSocket(CancellationToken? cancel = null)
		{
			Cred = Cred ?? throw new Exception("Credentials not available");
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;

			// Open the web socket
			var parms = new Params
			{
				{ "action",  "userOnline" },
				{ "at", Cred.AccessToken },
				{ "apikey", Cred.User.ApiKey },
				{ "appid", APP_ID },
				{ "nonce", Nonce },
				{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
				{ "userAgent", "ewelink-api" },
				{ "sequence", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
				{ "version", 8 },
			};

			// Open the websocket connection
			Log.Write(ELogLevel.Info, $"Opening WebSocket connection to {EndPoint}");
			await WebSocket.Connect(EndPoint);

			// Start listening
			WebSocket.Listening = true;

			// Send the login message
			var msg = JObject.FromObject(parms).ToString();
			await WebSocket.SendAsync(msg, cancel_token);
		}

		/// <summary>Get info about a specific device</summary>
		public async Task<JObject> GetDeviceInfo(string device_id, CancellationToken? cancel = null)
		{
			Cred = Cred ?? throw new Exception("Credentials not available");
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;

			// Body data for the request
			var parms = new Params
			{
				{ "appid", APP_ID },
				{ "deviceid", device_id },
				{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
				{ "version", 8 },
				{ "nonce", Nonce },
			};

			// Create the request
			var url = $"{Url}api/user/device/{device_id}{Http_.UrlEncode(parms)}";
			var req = new HttpRequestMessage(HttpMethod.Get, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Cred.AccessToken);

			// Submit the request
			var response = await Client.SendAsync(req, cancel_token);
			var reply = await response.Content.ReadAsStringAsync();
			var jobj = JObject.Parse(reply);

			// Check for an error
			if (jobj["error"]?.Value<int>() is int error && error != 0)
				throw new Exception($"eWeLink: Error getting device info: {error}");
		
			// Return the device info
			return jobj;
		}

		/// <summary>Set the state of a switch</summary>
		public async Task SwitchState(EweSwitch sw, EweSwitch.ESwitchState state, int channel = 0, CancellationToken? cancel = null)
		{
			Cred = Cred ?? throw new Exception("Credentials not available");
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;

			// Simple switches just have a 'switch' field.
			// Multi-channel switches have a switches array.
			var sw_parms = new JObject();
			if (sw.Params["switch"] is JToken)
			{
				if (channel != 0)
					throw new Exception($"Switch does not have multiple channels");

				sw_parms["switch"] = state.ToString().ToLower();
			}
			if (sw.Params["switches"] is JArray jarr)
			{
				if (channel < 0 || channel >= jarr.Count)
					throw new Exception($"Channel index '{channel}' is out-of-range. Switch only has {jarr.Count} channels");

				jarr = (JArray)jarr.DeepClone();
				jarr[channel] = state.ToString().ToLower();
				sw_parms["switches"] = jarr;
			}

			// Body data for the request
			var parms = new Params
			{
				{ "appid", APP_ID },
				{ "deviceid", sw.DeviceID },
				{ "params", sw_parms },
				{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
				{ "version", 8 },
				{ "nonce", Nonce },
			};
			var body = JObject.FromObject(parms).ToString();

			// Create the request
			var url = $"{Url}api/user/device/status";
			var req = new HttpRequestMessage(HttpMethod.Post, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Cred.AccessToken);
			req.Content = new StringContent(body, Encoding.UTF8, new Mime("application", "json"));

			// Submit the request
			var response = await Client.SendAsync(req, cancel_token);
			var reply = await response.Content.ReadAsStringAsync();
			var jobj = JObject.Parse(reply);

			// Check for an error
			if (jobj["error"]?.Value<int>() is int error && error != 0)
				throw new Exception($"eWeLink: Error setting switch state: {error}");

			// Assume success
			sw.State = state;
		}


#if false
		/// <summary>Get the device info directly over the LAN, rather than via the EweLink server</summary>
		public async Task<JObject> GetDeviceInfoLocal(string ip, string device_id, CancellationToken? cancel = null)
		{
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;

			// Body data for the request
			var parms = new Params
			{
				{ "apikey", "a7774931-aa61-4506-96ac-a8a0f01c4ae8" },
				{ "deviceid", device_id },
				{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
				{ "version", 8 },
				{ "nonce", Nonce },
			};

			// Create the request
			var url = $"http://{ip}:8081/zeroconf/info";
			var req = new HttpRequestMessage(HttpMethod.Post, url);
			//req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Cred.AccessToken);

			// Submit the request
			var response = await Client.SendAsync(req, cancel_token);
			var reply = await response.Content.ReadAsStringAsync();
			var jobj = JObject.Parse(reply);

			// Check for an error
			if (jobj["error"]?.Value<int>() is int error && error != 0)
				throw new Exception($"eWeLink: Error getting device info: {error}");

			// Return the device info
			return jobj;
		}

		/// <summary>Set the state of a switch</summary>
		public async Task SwitchState(string ip, EweSwitch sw, EweSwitch.ESwitchState state, int channel = 0, CancellationToken? cancel = null)
		{
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;

			// Simple switches just have a 'switch' field.
			// Multi-channel switches have a switches array.
			var sw_parms = new JObject();
			if (sw.Params["switch"] is JToken)
			{
				if (channel != 0)
					throw new Exception($"Switch does not have multiple channels");

				sw_parms["switch"] = state.ToString().ToLower();
			}
			if (sw.Params["switches"] is JArray jarr)
			{
				if (channel < 0 || channel >= jarr.Count)
					throw new Exception($"Channel index '{channel}' is out-of-range. Switch only has {jarr.Count} channels");

				jarr = (JArray)jarr.DeepClone();
				jarr[channel] = state.ToString().ToLower();
				sw_parms["switches"] = jarr;
			}

			// Body data for the request
			var parms = new Params
			{
				{ "appid", APP_ID },
				{ "deviceid", sw.DeviceID },
				{ "params", sw_parms },
				{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
				{ "version", 8 },
				{ "nonce", Nonce },
			};
			var body = JObject.FromObject(parms).ToString();

			// Create the request
			var url = $"http://{ip}:8081/zeroconf/api/user/device/status";
			var req = new HttpRequestMessage(HttpMethod.Post, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Cred.AccessToken);
			req.Content = new StringContent(body, Encoding.UTF8, new Mime("application", "json"));

			// Submit the request
			var response = await Client.SendAsync(req, cancel_token);
			var reply = await response.Content.ReadAsStringAsync();
			var jobj = JObject.Parse(reply);

			// Check for an error
			if (jobj["error"]?.Value<int>() is int error && error != 0)
				throw new Exception($"eWeLink: Error setting switch state: {error}");

			// Assume success
			sw.State = state;
		}
#endif


		/// <summary>Helper for generating a 'nonce'</summary>
		private static string Nonce
		{
			get
			{
				lock (m_nonce_lock)
				{
					var nonce = DateTimeOffset.UtcNow.Ticks;
					m_nonce = Math.Max(m_nonce + 1, nonce);
					return m_nonce.ToString();
				}
			}
		}
		private static long m_nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
		private static object m_nonce_lock = new object();

		/// <summary></summary>
		public class Params :Dictionary<string, object> { }

		/// <summary>Public interface for the device list</summary>
		public interface IEweDeviceList :IReadOnlyList<EweDevice>, INotifyCollectionChanged, INotifyPropertyChanged { }
		private class EweDeviceList :ObservableCollection<EweDevice>, IEweDeviceList { }
	}
}

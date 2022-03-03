/**
 * A simple C# OAuth2 auth-code wrapper library.
 * 
 * @author
 * Stian Hanger (pdnagilum@gmail.com)
 * 
 * @source
 * https://github.com/nagilum/oauth2-csharp
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Net
{
	public sealed class OAuth2 :IDisposable
	{
		// OAuth2 Protocol goes like this:
		//  1) Register your app with the service that you are developing it for,
		//     e.g.Twitter, SoundCloud etc. You will receive a consumer key and secret.
		//  2) You, the developer of the app, then initiates the OAuth process by passing
		//     the consumer key and the consumer secret.
		//  3) The service will return a Request Token to you.
		//  4) The user then needs to grant approval for the app to run requests.
		//  5) Once the user has granted permission you need to exchange the request
		//     token for an access token.
		//  6) Now that you have received an access token, you use this to sign all http
		//     requests with your credentials and access token.
		// Notes:

		public OAuth2(string client_id, string client_secret, HttpClient? client = null)
		{
			m_client_owned = client == null;
			m_client = client ?? new HttpClient();
			ClientID = client_id;
			ClientSecret = client_secret;
			AccessTokenUri = string.Empty;
			AuthUri = string.Empty;
			Scope = string.Empty;
			State = string.Empty;
			Offline = false;
			Locale = "en";
		}
		public void Dispose()
		{
			Client = null!;
		}

		/// <summary></summary>
		private HttpClient Client
		{
			get => m_client ?? new HttpClient();
			set
			{
				if (m_client == value) return;
				if (m_client_owned)
					Util.Dispose(ref m_client);

				m_client = value;
			}
		}
		private HttpClient? m_client;
		private bool m_client_owned;

		/// <summary></summary>
		public string ClientID { get; }

		/// <summary></summary>
		public string ClientSecret { get; }

		/// <summary></summary>
		public string AccessTokenUri { get; set; }

		/// <summary></summary>
		public string AuthUri { get; set; }

		/// <summary></summary>
		public string Scope { get; set; }

		/// <summary></summary>
		public string State { get; set; }

		/// <summary></summary>
		public bool Offline { get; set; }

		/// <summary></summary>
		public string Locale { get; set; }

		/// <summary>Builds a URL to visit so that the user can give authorization</summary>
		public string BuildAuthUrl(string redirect_url)
		{
			var parms = new Dictionary<string, object>
			{
				{ "client_id", ClientID },
				{ "redirect_uri", redirect_url },
				{ "response_type", "code" },
				{ "display", "page" },
				{ "locale", Locale },
			};
			if (Scope.HasValue())
				parms["scope"] = Scope;
			if (State.HasValue())
				parms["state"] = State;
			if (Offline)
				parms["access_type"] = "offline";

			var url = $"{AuthUri}{Http_.UrlEncode(parms)}";
			return url;
		}

		/// <summary>Request an access token by exchanging an auth code.</summary>
		public async Task<Response> AuthenticateByCode(string redirectUri, string code, CancellationToken cancel)
		{
			// Authentication request
			var parms = new Dictionary<string, object>
			{
				{ "client_id", ClientID },
				{ "client_secret", ClientSecret },
				{ "redirect_uri", redirectUri },
				{ "code", code },
				{ "grant_type", "authorization_code" }
			};
			if (Scope.HasValue())
				parms["scope"] = Scope;
			if (State.HasValue())
				parms["state"] = State;

			// 
			var reply = await Client.Request(HttpMethod.Post, AccessTokenUri, cancel, parms);
			return ParseReply(reply);
		}

		/// <summary>Request a new access token using a refresh token</summary>
		public async Task<Response> AuthenticateByToken(string refresh_token, CancellationToken cancel)
		{
			var parms = new Dictionary<string, object>
			{
				{ "client_id", ClientID },
				{ "client_secret", ClientSecret },
				{ "refresh_token", refresh_token },
				{ "grant_type", "refresh_token" },
			};
			if (Scope.HasValue())
				parms["scope"] = Scope;
			if (State.HasValue())
				parms["state"] = State;

			// 
			var reply = await Client.Request(HttpMethod.Post, AccessTokenUri, cancel, parms);
			return ParseReply(reply);
		}

		/// <summary>Get user info from the providers user endpoint.</summary>
		public async Task<string> GetUserInfo(string url, string access_token, CancellationToken cancel)
		{
			// This is more of an example on how to use the authentication token once authenticated
			var parameters = new Dictionary<string, object>
			{
				{ "access_token", access_token }
			};

			var reply = await Client.Request(HttpMethod.Get, url, cancel, parameters);
			return reply;
		}

		/// <summary>Interpret the reply from the auth-call.</summary>
		private Response ParseReply(string reply)
		{
			// Parse the response
			// Try to guess the format of the response

			// Simple Json reply, rather than add a dependency, just parse raw Json
			if (reply.StartsWith("{") && reply.EndsWith("}"))
			{
				var kv = new Dictionary<string, string>();
				foreach (var field in reply.Trim('{', '}').Split(','))
				{
					var m = Regex.Match(field, @"""(.*?)""\s*:\s*(.*)");
					kv.Add(m.Groups[1].Value, m.Groups[2].Value);
				}

				var response = new Response
				{
					AccessToken  = kv.TryGetValue("access_token", out var v0) ? v0.Trim() : string.Empty,
					RefreshToken = kv.TryGetValue("refresh_token", out var v1) ? v1.Trim() : string.Empty,
					State        = kv.TryGetValue("state", out var v2) ? v2.Trim() : string.Empty,
					Expires      = (kv.TryGetValue("expires", out var e) || kv.TryGetValue("expires_in", out e)) ? DateTimeOffset.Now.AddSeconds(int.Parse(e.Trim())) : default,
				};
				return response;
			}

			// URL encoded string?
			if (reply.Contains('&'))
			{
				var response = new Response { };
				foreach (var field in reply.Split('&'))
				{
					var kv = field.Split('=');
					if (kv.Length != 2)
						continue;

					switch (kv[0].Trim())
					{
					case "access_token":
						response.AccessToken = kv[1].Trim();
						break;
					case "refresh_token":
						response.RefreshToken = kv[1].Trim();
						break;
					case "state":
						response.State = kv[1].Trim();
						break;
					case "expires":
					case "expires_in":
						response.Expires = DateTimeOffset.Now.AddSeconds(int.Parse(kv[1].Trim()));
						break;
					}
				}
				return response;
			}

			throw new Exception("Failed to parse response. Format unrecognised");
		}

		/// <summary>Response</summary>
		public class Response
		{
			public Response()
			{
				AccessToken = string.Empty;
				RefreshToken = string.Empty;
				Expires = default;
				State = string.Empty;
			}

			/// <summary></summary>
			public string AccessToken { get; set; }

			/// <summary></summary>
			public string RefreshToken { get; set; }

			/// <summary></summary>
			public DateTimeOffset Expires { get; set; }

			/// <summary></summary>
			public string State { get; set; }
		}
	}
}

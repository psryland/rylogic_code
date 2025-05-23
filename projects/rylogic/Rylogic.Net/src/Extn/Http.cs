﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Rylogic.Extn
{
	public static class Http_
	{
		/// <summary>Encode 'parameters' into a URL encoded parameter list, starting with '?'</summary>
		public static string UrlEncode(IDictionary<string,object> parameters)
		{
			var s = new StringBuilder();
			foreach (var kv in parameters)
			{
				var value = (kv.Value as string)?.Replace(' ', '+') ?? kv.Value.ToString();
				s.Append("&").Append(kv.Key).Append("=").Append(value);
			}
			if (s.Length != 0) s[0] = '?';
			return s.ToString();
		}

		/// <summary>Encode 'parameters' into a Json string, starting with '?'</summary>
		public static string JsonEncode(IDictionary<string, object> parameters)
		{
			return JObject.FromObject(parameters).ToString();
		}

		/// <summary>Helper for GETs</summary>
		public static async Task<string> Request(this HttpClient client, HttpMethod method, string url, CancellationToken cancel, IDictionary<string,object> parameters, string content_type = "application/x-www-form-urlencoded")
		{
			// If called from the UI thread, disable the SynchronisationContext
			// to prevent deadlocks when waiting for Async results.
			using var nosync = Task_.NoSyncContext();
	
			if (method == HttpMethod.Get)
				url += UrlEncode(parameters);

			var body =
				content_type == "application/x-www-form-urlencoded" ? UrlEncode(parameters).TrimStart('?') :
				content_type == "application/json" ? JsonEncode(parameters) :
				throw new Exception("Unsupported HTTP Request body type");

			// Construct the request
			var req = new HttpRequestMessage(method, url)
			{
				Content = new StringContent(body, Encoding.UTF8, content_type)
			};

			// Submit the request
			var res = await client.SendAsync(req, cancel);
			if (!res.IsSuccessStatusCode)
				throw new HttpRequestException($"{res.ReasonPhrase} ({res.StatusCode})");

			// Return the reply
			var reply = await res.Content.ReadAsStringAsync();
			return reply;
		}

		/// <summary>Cancel-able web request get response</summary>
		public static async Task<HttpWebResponse> GetResponseAsync(this HttpWebRequest request, CancellationToken cancel)
		{
			using var reg = cancel.Register(() => request.Abort(), useSynchronizationContext: false);

			try
			{
				var response = await request.GetResponseAsync();
				return (HttpWebResponse)response;
			}
			catch (WebException ex)
			{
				// WebException is thrown when request.Abort() is called, but there may be
				// many other reasons, propagate the WebException to the caller correctly
				if (cancel.IsCancellationRequested)
				{
					// the WebException will be available as Exception.InnerException
					throw new OperationCanceledException(ex.Message, ex, cancel);
				}

				// cancellation hasn't been requested, rethrow the original WebException
				throw;
			}
		}
	}
}

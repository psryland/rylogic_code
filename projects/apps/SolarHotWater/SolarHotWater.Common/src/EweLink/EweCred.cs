using System;
using Newtonsoft.Json;

namespace EweLink
{
	public class EweCred
	{
		// This is basically an OAuth2 response

		/// <summary></summary>
		[JsonProperty("at")]
		public string AccessToken { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("rt")]
		public string RequestToken { get; set; } = string.Empty;
		
		/// <summary></summary>
		[JsonProperty("region")]
		public string Region = string.Empty;

		/// <summary>User data</summary>
		[JsonProperty("user")]
		public UserData User = new();
		public class UserData
		{
			/// <summary></summary>
			[JsonProperty("email")]
			public string Email = string.Empty;

			/// <summary></summary>
			[JsonProperty("password")]
			public string Password = string.Empty;

			/// <summary></summary>
			[JsonProperty("apikey")]
			public string ApiKey { get; set; } = string.Empty;

			/// <summary></summary>
			[JsonProperty("online")]
			public bool Online { get; set; } = false;

			public string _id = string.Empty;
			public string appId = string.Empty;
			public bool isAccepEmailAd;
			public DateTimeOffset createdAt;
			public int __v;
			public string lang = string.Empty;
			public string ip = string.Empty;
			public string location = string.Empty;
			public string userStatus = string.Empty;
			public string countryCode = string.Empty;
			public string currentFamilyId = string.Empty;
			public DateTimeOffset onlineTime;
			public DateTimeOffset offlineTime;

			/// <summary></summary>
			[JsonProperty("clientInfo")]
			public ClientInfoData ClientInfo = new();
			public class ClientInfoData
			{
				public string model = string.Empty;
				public string os = string.Empty;
				public string imei = string.Empty;
				public string romVersion = string.Empty;
				public string appVersion = string.Empty;
			}

			/// <summary></summary>
			[JsonProperty("appInfos")]
			public AppInfoData[] AppInfo = Array.Empty<AppInfoData>();
			public class AppInfoData
			{
				public string os = string.Empty;
				public string appVersion = string.Empty;
			}
		}
	}
}

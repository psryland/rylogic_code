using System;

namespace SolarHotWater
{
	public class EweCred
	{
		public string at = string.Empty;
		public string rt = string.Empty;
		public UserData user = new UserData();
		public string region = string.Empty;

		public class UserData
		{
			public ClientInfo clientInfo = new ClientInfo();
			public string _id = string.Empty;
			public string email = string.Empty;
			public string password = string.Empty;
			public string appId = string.Empty;
			public bool isAccepEmailAd;
			public DateTimeOffset createdAt;
			public string apikey = string.Empty;
			public int __v;
			public string lang = string.Empty;
			public bool online;
			public DateTimeOffset onlineTime;
			public AppInfo[] appInfos = Array.Empty<AppInfo>();
			public string ip = string.Empty;
			public string location = string.Empty;
			public DateTimeOffset offlineTime;
			public string userStatus = string.Empty;
			public string countryCode = string.Empty;
			public string currentFamilyId = string.Empty;

			public class AppInfo
			{
				public string os = string.Empty;
				public string appVersion = string.Empty;
			}
			public class ClientInfo
			{
				public string model = string.Empty;
				public string os = string.Empty;
				public string imei = string.Empty;
				public string romVersion = string.Empty;
				public string appVersion = string.Empty;
			}
		}
	}
}

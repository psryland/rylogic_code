using System;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Crypt;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>User details</summary>
	[DebuggerDisplay("{Name,nq}")]
	public class User
	{
		public User(string username, string password)
		{
			Name = username;
			Cred = Crypt.CredHash(username, password, "RylogicLimitedIsAwesome!");
		}

		/// <summary>User name</summary>
		public string Name { get; }

		/// <summary>Hashed user credentials</summary>
		public Binary Cred { get; }

		/// <summary>The location of the keys file for this user</summary>
		public string KeysFilepath => Misc.ResolveUserPath($"{Name}.keys");

		/// <summary>Return an object that can encrypt stuff based on the user name and given password</summary>
		public ICryptoTransform Encryptor => Crypto.CreateEncryptor();

		/// <summary>Return an object that can decrypt stuff based on the user name and given password</summary>
		public ICryptoTransform Decryptor => Crypto.CreateDecryptor();

		/// <summary>Crypto service provider</summary>
		private AesCryptoServiceProvider Crypto
		{
			get
			{
				var crypto = new AesCryptoServiceProvider { Padding = PaddingMode.PKCS7 };
				crypto.Key = Cred.ToArray();
				crypto.IV = Array_.New(crypto.BlockSize / 8, i => crypto.Key[(i * 13) % crypto.Key.Length]);
				//Debug.WriteLine($"Key={crypto.Key.ToHexString()}");
				//Debug.WriteLine($"IV={crypto.IV.ToHexString()}");
				return crypto;
			}
		}

		/// <summary>Check that a valid keys file exists</summary>
		public EResult CheckKeys()
		{
			return GetKeys(out var _);
		}

		/// <summary>Create a new keys file for this user</summary>
		public void NewKeys()
		{
			var keys = new XElement("root");

			// Write the keys file back to disk (encrypted)
			var keys_xml = keys.ToString(SaveOptions.None);
			using (var fs = new FileStream(KeysFilepath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var cs = new CryptoStream(fs, Encryptor, CryptoStreamMode.Write))
			using (var sw = new StreamWriter(cs))
				sw.Write(keys_xml);
		}

		/// <summary>Save the api keys for the given exchange</summary>
		public void SetKeys(string exchange, string apikey, string secret)
		{
			// Load the keys XML
			var result = GetKeys(out var keys);
			if (result == EResult.BadPassword)
				throw new Exception("User data is invalid");
			if (result == EResult.NotFound)
				keys = new XElement("root");

			// Add/Replace the element for 'exchange'
			keys.RemoveNodes(exchange);
			var exch_xml = keys.Add2(new XElement(exchange));
			exch_xml.Add2(XmlTag.APIKey, apikey, false);
			exch_xml.Add2(XmlTag.Secret, secret, false);

			// Write the keys file back to disk (encrypted)
			var keys_xml = keys.ToString(SaveOptions.None);
			using (var fs = new FileStream(KeysFilepath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var cs = new CryptoStream(fs, Encryptor, CryptoStreamMode.Write))
			using (var sw = new StreamWriter(cs))
				sw.Write(keys_xml);
		}

		/// <summary>Return the API keys for the given exchange</summary>
		public EResult GetKeys(string exchange, out string apikey, out string secret)
		{
			apikey = string.Empty;
			secret = string.Empty;

			// Load the keys XML
			var result = GetKeys(out var keys);
			if (result != EResult.Success)
				return result;

			// Find the element for 'exchange'
			var exch_xml = keys.Element(exchange);
			if (exch_xml == null)
				return EResult.NotFound;

			// Read the key/secret
			apikey = exch_xml.Element(XmlTag.APIKey).As<string>();
			secret = exch_xml.Element(XmlTag.Secret).As<string>();
			return EResult.Success;
		}

		/// <summary>Load the keys data for the given username and password</summary>
		private EResult GetKeys(out XElement keys)
		{
			keys = null;

			// Get the key file name. This is an encrypted XML file
			if (!Path_.FileExists(KeysFilepath))
				return EResult.NotFound;

			// Read the file contents into memory and decrypt it
			try
			{
				// This code throw a CryptographicException when the 'User' has an incorrect password.
				// This is not an error, it's just that CrytographicException are not disabled by default
				// in the debugger exception settings.
				var keys_xml = string.Empty;
				using (var fs = new FileStream(KeysFilepath, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (var cs = new CryptoStream(fs, Decryptor, CryptoStreamMode.Read))
				using (var sr = new StreamReader(cs))
					keys_xml = sr.ReadToEnd();

				keys = XElement.Parse(keys_xml, LoadOptions.None);
				return EResult.Success;
			}
			catch
			{
				// If the xml is not valid, assume bad password
				return EResult.BadPassword;
			}
		}

		/// <summary></summary>
		public enum EResult
		{
			Success,
			NotFound,
			BadPassword,
		}

		/// <summary></summary>
		private static class XmlTag
		{
			public const string APIKey = "APIKey";
			public const string Secret = "APISecret";
		}
	}
}

using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;

namespace CoinFlip
{
	public partial class Model
	{
		/// <summary>Return the key/secret for this exchange</summary>
		private bool LoadAPIKeys(User user, string exch, out string key, out string secret)
		{
			var crypto = new AesCryptoServiceProvider{ Padding = PaddingMode.PKCS7 };

			// Get the key file name. This is an encrypted XML file
			var key_file = Misc.ResolveUserPath($"{user.Username}.keys");

			// If there is no keys file, or the keys file does not contain API keys for this exchange, prompt for them
			if (!Path_.FileExists(key_file))
				return CreateAPIKeys(user, exch, out key, out secret);

			// Read the file contents to memory and decrypt it
			string keys_xml;
			var decryptor = crypto.CreateDecryptor(user.Cred, InitVector(user, crypto.BlockSize));
			using (var fs = new FileStream(key_file, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
			using (var sr = new StreamReader(cs))
				keys_xml = sr.ReadToEnd();

			// Find the element for this exchange
			var root = XDocument.Parse(keys_xml, LoadOptions.None).Root;
			var exch_xml = root?.Element(exch);
			if (exch_xml == null)
				return CreateAPIKeys(user, exch, out key, out secret);

			// Read the key/secret
			key    = exch_xml.Element(XmlTag.APIKey).As<string>();
			secret = exch_xml.Element(XmlTag.APISecret).As<string>();
			return true;
		}

		/// <summary>Write the key/secret pair to the keys file for 'user'</summary>
		private void SaveAPIKeys(User user, string exch, string key, string secret)
		{
			var crypto = new AesCryptoServiceProvider{ Padding = PaddingMode.PKCS7 };

			// Get the key file name. This is an encrypted XML file
			var key_file = Misc.ResolveUserPath($"{user.Username}.keys");

			// If there is an existing keys file, decrypt and the XML content, otherwise create from new
			var root = (XElement)null;
			if (Path_.FileExists(key_file))
			{
				// Read the file contents to memory and decrypt it
				var decryptor = crypto.CreateDecryptor(user.Cred, InitVector(user, crypto.BlockSize));
				using (var fs = new FileStream(key_file, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (var cs = new CryptoStream(fs, decryptor, CryptoStreamMode.Read))
				using (var sr = new StreamReader(cs))
					root = XDocument.Parse(sr.ReadToEnd(), LoadOptions.None).Root;
			}
			if (root == null)
				root = new XElement("root");

			// Add/replace the element for this exchange
			root.RemoveNodes(exch);
			var exch_xml = root.Add2(new XElement(exch));
			exch_xml.Add2(XmlTag.APIKey, key, false);
			exch_xml.Add2(XmlTag.APISecret, secret, false);
			var keys_xml = root.ToString(SaveOptions.None);

			// Write the keys file back to disk (encrypted)
			var encryptor = crypto.CreateEncryptor(user.Cred, InitVector(user, crypto.BlockSize));
			using (var fs = new FileStream(key_file, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var cs = new CryptoStream(fs, encryptor, CryptoStreamMode.Write))
			using (var sw = new StreamWriter(cs))
				sw.Write(keys_xml);
		}

		/// <summary>Create or replace API keys for this exchange</summary>
		private bool CreateAPIKeys(User user, string exch, out string key, out string secret)
		{
			key    = null;
			secret = null;

			// Prompt for the API keys
			var dlg = new APIKeysUI
			{
				Icon = UI.Icon,
				StartPosition = FormStartPosition.CenterScreen,
				Text = $"{exch} API Keys",
				Desc = $"Enter the API key and secret for your account on {exch}\r\n"+
						$"These will be stored in an encrypted file here:\r\n"+
						$"\"{Misc.ResolveUserPath($"{user.Username}.keys")}\""+
						$"\r\n"+
						$"Changing API Keys will require restarting",
			};
			using (dlg)
			{
				if (dlg.ShowDialog(UI) != DialogResult.OK) return false;
				SaveAPIKeys(user, exch, dlg.APIKey, dlg.APISecret);
				return true;
			}
		}

		/// <summary>Display and/or change the API keys for 'exch'</summary>
		public void ChangeAPIKeys(Exchange exch)
		{
			// Prompt for the user password again
			var user = Misc.LogIn(UI, Settings);
			if (user.Username != User.Username || !Array_.Equal(user.Cred, User.Cred))
			{
				MsgBox.Show(UI, "Invalid username or password", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			// Load the API keys
			if (LoadAPIKeys(User, exch.Name, out var key, out var secret))
			{
				var dlg = new APIKeysUI
				{
					Icon = UI.Icon,
					StartPosition = FormStartPosition.CenterScreen,
					Text = $"{exch} API Keys",
					Desc = $"Enter the API key and secret for your account on {exch}\r\n"+
							$"These will be stored in an encrypted file here:\r\n"+
							$"\"{Misc.ResolveUserPath($"{User.Username}.keys")}\""+
							$"\r\n"+
							$"Changing API Keys will require restarting",
					APIKey = key,
					APISecret = secret,
				};
				using (dlg)
				{
					if (dlg.ShowDialog(UI) != DialogResult.OK)
						return;

					// Save if the keys have changed
					if (dlg.APIKey != key || dlg.APISecret != secret)
					{
						SaveAPIKeys(User, exch.Name, dlg.APIKey, dlg.APISecret);
						var res = MsgBox.Show(UI,
							"A restart is required to use the new API keys.\r\n"+
							"\r\n"+
							"Restart now?",
							Application.ProductName,
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question);
						if (res == DialogResult.Yes)
							Application.Restart();
					}
				}
			}
		}

		/// <summary>Generate an initialisation vector for the encryption service provider</summary>
		private byte[] InitVector(User user, int size_in_bits)
		{
			var buf = new byte[size_in_bits / 8];
			for (int i = 0; i != buf.Length; ++i)
				buf[i] = user.Cred[(i * 13) % user.Cred.Length];
			return buf;
		}
	}
}

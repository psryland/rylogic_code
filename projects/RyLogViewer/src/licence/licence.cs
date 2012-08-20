using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Linq;
using RyLogViewer.Properties;
using pr.common;

namespace RyLogViewer
{
	public partial class Main :Form
	{
		/// <summary>
		/// Creates a serial number for the app. The private key xml 
		/// file must be in the same directory that the app is running in.</summary>
		public static string GenerateSerialNumber()
		{
			var exe_dir = Path.GetDirectoryName(Application.ExecutablePath) ?? @".\";
			var priv_key_path = Path.Combine(exe_dir, "private_key.xml");
			var priv_key = File.ReadAllText(priv_key_path);
			var key = ActivationCode.Generate(priv_key);
			return key;
		}

		/// <summary>Generates a licence xml file in the current executable directory</summary>
		public void GenerateLicenceFile(string user_name, string company, string email, string serial)
		{
			// Create a licence xml file
			var doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) throw new ApplicationException("Failed to generate licence xml file");
			
			// Add personal data to discourage sharing of licence files
			doc.Root.Add(new XElement(XmlTag.UserName, user_name));
			doc.Root.Add(new XElement(XmlTag.Company, company));
			doc.Root.Add(new XElement(XmlTag.EmailAddr, email));
			
			// Add the client's product serial number
			doc.Root.Add(new XElement(XmlTag.SerialNo, serial));
			
			// Read the bytes of the executable into a buffer
			var exebytes = File.ReadAllBytes(Application.ExecutablePath);

			// Generate the finger print by combining the
			// above data with the bits of the executable.
			var alg = new RSACryptoServiceProvider();


			var exe_dir = Path.GetDirectoryName(Application.ExecutablePath) ?? @".\";
		}

		private void CheckLicence(StartupOptions su)
		{
			// What is the licence?
			// User name       }
			// company         } personal data to discourage sharing
			// Email address   }
			// kagi issued serial number (self checkable)
			// exe

			// Look for a licence file in the same directory as the exe
			var licence_path = Path.Combine(su.ExeDir, "licence.xml");

			var exebuf = File.ReadAllBytes(Application.ExecutablePath);
			
			var sha1 = new SHA1CryptoServiceProvider();
			var hash = sha1.ComputeHash(exebuf);


			var pub_key = Resources.public_key;
			
			// Generate the thumbprint from the contents of the licence.xml
			// file and compare it to the thumbprint given in that file.
			// If equal, then it's a valid licence file
			
			//SerialNumber.Validate();

		}
	}

	/// <summary>A class for managing crippled functionality</summary>
	public static class Cripple
	{

	}
}
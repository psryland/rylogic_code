using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using RyLogViewer.Properties;
using pr.common;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Represents loaded licence information</summary>
	public class Licence
	{
		/// <summary>The name of the licence holder</summary>
		public string LicenceHolder { get; set; }

		/// <summary>The associated company name</summary>
		public string Company { get; set; }

		/// <summary>The activation code</summary>
		public string ActivationCode { get; set; }

		/// <summary>Loads the licence info</summary>
		public Licence()
		{
			try
			{
				// Check the local directory first in case we're running as a portable instance
				var dir = Path.GetDirectoryName(Application.ExecutablePath) ?? @".\";
				var lic = Path.Combine(dir, "licence.xml");
				if (!File.Exists(lic))
				{
					dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					lic = Path.Combine(dir, "licence.xml");
					if (!File.Exists(lic))
						throw new FileNotFoundException("Licence file not found");
				}
				
				// ReSharper disable PossibleNullReferenceException
				var doc = XDocument.Load(lic, LoadOptions.None);
				if (doc.Root == null) throw new InvalidDataException("licence file invalid");
				LicenceHolder  = doc.Root.Element(XmlTag.LicenceHolder).Value;
				Company        = doc.Root.Element(XmlTag.Company).Value;
				ActivationCode = doc.Root.Element(XmlTag.ActivationCode).Value;
				// ReSharper restore PossibleNullReferenceException
				return;
			}
			catch (FileNotFoundException) { Log.Info(this, "Licence file not found"); }
			catch (Exception ex) { Log.Exception(this, ex, "Licence file invalid"); }
			
			// No valid licence file found. Create a default evaluation licence
			LicenceHolder = Constants.UnregistedUser;
			Company = "";
			ActivationCode = "Paste your Activation licence here";
		}
		
		/// <summary>Use the unpredictability of the GC to do a file signing test</summary>
		~Licence()
		{
			Main.PerformSigningVerification();
		}
	}

	public partial class Main :Form
	{
		/// <summary>Check that the app has a correct signature</summary>
		public static void PerformSigningVerification()
		{
			Action test = () =>
				{
					if (pr.crypt.Crypt.Validate(Application.ExecutablePath, Resources.public_key, false)) return;
					MessageBox.Show(null,
						"WARNING! This executable has been tampered with!\r\n" +
						"Using this program may compromise your computer.\r\n" +
						"\r\n" +
						"Please contact Rylogic Limited (support@rylogic.co.nz) with " +
						"information about where you received this copy of the application."
						,"Tampered Executable Detected!"
						,MessageBoxButtons.OK
						,MessageBoxIcon.Exclamation);
					Application.Exit();
				};
			
			if (ActiveForm != null)
				ActiveForm.BeginInvoke(test);
		}

		/// <summary>Display the activation dialog</summary>
		private void ShowActivation()
		{
			var dg = new Activation();
			if (dg.ShowDialog(this) != DialogResult.OK) return;

		}

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
			doc.Root.Add(new XElement(XmlTag.LicenceHolder, user_name));
			doc.Root.Add(new XElement(XmlTag.Company, company));
			doc.Root.Add(new XElement(XmlTag.EmailAddr, email));
			
			// Add the client's product serial number
			doc.Root.Add(new XElement(XmlTag.ActivationCode, serial));
			
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
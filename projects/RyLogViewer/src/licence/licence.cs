using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using RyLogViewer.Properties;
using pr.common;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Represents loaded licence information</summary>
	public class Licence
	{
		/// <summary>Returns true if the code has the correct length</summary>
		public bool ValidLength { get { return pr.common.ActivationCode.CheckLength(ActivationCode, Resources.public_key); } }

		/// <summary>Returns true if the code is made up of valid characters</summary>
		public bool ValidCharacters { get { return pr.common.ActivationCode.CheckChars(ActivationCode); } }

		/// <summary>Returns true if the code at least looks right</summary>
		public bool ValidCrC { get { return pr.common.ActivationCode.CheckCrc(ActivationCode); } }

		/// <summary>True if the activation code is valid</summary>
		public bool Valid { get { return pr.common.ActivationCode.Validate(ActivationCode, Resources.public_key); } }

		/// <summary>True if the licence data has been modified</summary>
		public bool Changed { get; private set; }

		/// <summary>The name of the licence holder</summary>
		public string LicenceHolder
		{
			get { return m_licence_holder; }
			set { if (value != m_licence_holder) { m_licence_holder = value; Changed = true; } }
		}
		private string m_licence_holder;

		/// <summary>The associated company name</summary>
		public string Company
		{
			get { return m_company; }
			set { if (value != m_company) { m_company = value; Changed = true; } }
		}
		private string m_company;

		/// <summary>The activation code</summary>
		public string ActivationCode
		{
			get { return m_activation_code; }
			set { if (value != m_activation_code) { m_activation_code = value; Changed = true; } }
		}
		private string m_activation_code;

		/// <summary>Returns a hash of the user details</summary>
		private byte[] UserDetailsHash
		{
			get
			{
				var user_details = "Rylogic-"+LicenceHolder+"-Limited-"+Company+"-isAwesome";
				var hash = new SHA1CryptoServiceProvider().ComputeHash(Encoding.Unicode.GetBytes(user_details));
				return hash;
			}
		}

		/// <summary>The software key is the combination of the activation code and user details</summary>
		private string SoftwareKey
		{
			get
			{
				// Return a software key that is generated from the current activation code
				var bytes = Base32Encoding.ToBytes(Base32Encoding.Sanitise(ActivationCode));
				var lcg = new LCG(UserDetailsHash.Sum(b => b));
				for (int i = 0, iend = bytes.Length; i != iend; ++i, lcg.next())
					bytes[i] = (byte)(bytes[i] ^ (byte)lcg.value);
				return Base32Encoding.ToString(bytes);
			}
			set
			{
				// Set the activation code from the provided software key
				var bytes = Base32Encoding.ToBytes(Base32Encoding.Sanitise(value));
				var lcg = new LCG(UserDetailsHash.Sum(b => b));
				for (int i = 0, iend = bytes.Length; i != iend; ++i, lcg.next())
					bytes[i] = (byte)(bytes[i] ^ (byte)lcg.value);
				ActivationCode = Base32Encoding.ToString(bytes);
			}
		}

		/// <summary>Linear congruential generator</summary>
		private class LCG
		{
			public int value;
			public void next() { value = (16807 * value + 0) % 2147483647; }
			public LCG(int seed) { value = seed; }
		}

		/// <summary>Loads the licence info</summary>
		public Licence(string dir)
		{
			try
			{
				var lic = Path.Combine(dir, "licence.xml");
				if (!File.Exists(lic)) throw new FileNotFoundException("Licence file not found");
				
				// Load the licence file
				var doc = XDocument.Load(lic, LoadOptions.None);
				if (doc.Root == null) throw new InvalidDataException("licence file invalid");
				
				// ReSharper disable PossibleNullReferenceException
				LicenceHolder  = doc.Root.Element(XmlTag.LicenceHolder).Value;
				Company        = doc.Root.Element(XmlTag.Company).Value;
				SoftwareKey    = doc.Root.Element(XmlTag.SoftwareKey).Value;
				// ReSharper restore PossibleNullReferenceException
				
				return;
			}
			catch (FileNotFoundException) { Log.Info(this, "Licence file not found"); }
			catch (Exception ex) { Log.Exception(this, ex, "Licence file invalid"); }
			
			// No valid licence file found. Create a default evaluation licence
			LicenceHolder  = Constants.EvalLicence;
			Company        = Constants.EvalLicence;
			ActivationCode = "<paste your activation licence here>";
		}

		/// <summary>Use the unpredictability of the GC to do a file signing test</summary>
		~Licence()
		{
			Main.PerformSigningVerification();
		}

		/// <summary>Output the licence details to a licence file</summary>
		public void WriteFile(string dir)
		{
			// Create the licence xml document
			var doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) throw new Exception("Failed to create licence.xml root node");
			
			// Save the elements
			doc.Root.Add(new XElement(XmlTag.LicenceHolder, LicenceHolder));
			doc.Root.Add(new XElement(XmlTag.Company, Company));
			doc.Root.Add(new XElement(XmlTag.SoftwareKey, SoftwareKey));
			
			// Save the licence file
			var lic = Path.Combine(dir, "licence.xml");
			doc.Save(lic);
		}

		/// <summary>Returns a text summary of the licence info</summary>
		public Rtf.Builder InfoStringRtf()
		{
			var rtf = new Rtf.Builder();
			if (!Valid)
			{
				rtf.Append(new Rtf.TextStyle{FontSize = 10, FontStyle = Rtf.EFontStyle.Bold, ForeColourIndex = rtf.ColourIndex(Color.DarkRed)});
				rtf.AppendLine("EVALUATION LICENCE");
				rtf.Append(new Rtf.TextStyle{FontSize = 8, FontStyle = Rtf.EFontStyle.Regular});
				rtf.AppendLine("This copy of RyLogViewer is for evaluation purposes only.");
				rtf.AppendLine("If you find it useful, please consider purchasing a license.");
			}
			else
			{
				rtf.AppendLine("Licenced To:");
				rtf.Append(new Rtf.TextStyle{FontSize = 8, ForeColourIndex = rtf.ColourIndex(Color.DarkBlue)});
				rtf.AppendLine(LicenceHolder);
				if (Company.HasValue()) rtf.AppendLine(Company);
			}
			return rtf;
		}
	}

	public partial class Main :Form
	{
		/// <summary>Check that the app has a correct signature</summary>
		public static void PerformSigningVerification()
		{
			// Do the signing test in a background thread
			ThreadPool.QueueUserWorkItem(x =>
				{
					// Only test for a signed exe once / 5mins
					if (Environment.TickCount - m_signing_last_tested < RetestPeriodMS)
						return;
					
					// Perform the signing test
					m_signing_last_tested = Environment.TickCount;
					if (pr.crypt.Crypt.Validate(Application.ExecutablePath, Resources.public_key, false))
						return;
					
					// Notify if it fails
					Action failed_notification = () =>
						{
							MessageBox.Show(null,
								"WARNING! This executable has been tampered with!\r\n" +
								"Using this program may compromise your computer.\r\n" +
								"\r\n" +
								"Please contact Rylogic Limited ("+Constants.SupportEmail+") with " +
								"information about where you received this copy of the application."
								,"Tampered Executable Detected!"
								,MessageBoxButtons.OK
								,MessageBoxIcon.Exclamation);
							Application.Exit();
						};
					
					var form = ActiveForm;
					if (form != null) form.BeginInvoke(failed_notification);
				});
		}
		private const int RetestPeriodMS = 5 * 60 * 1000;
		private static int m_signing_last_tested = Environment.TickCount - RetestPeriodMS;

		/// <summary>Display the activation dialog</summary>
		private void ShowActivation()
		{
			// Load the licence file
			var licence = new Licence(m_startup_options.AppDataDir);
			bool initially_valid = licence.Valid;
			
			// Display the UI for entering licence info
			var dg = new Activation(licence);
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			
			// Write the updated licence file (if changed)
			if (!licence.Valid) return;
			if (!licence.Changed) return;
			try
			{
				licence.WriteFile(m_startup_options.AppDataDir);
				if (!initially_valid)
				{
					MessageBox.Show(this,
						"Thank you for activating "+Application.ProductName+".\r\n" +
						"Your support is greatly appreciated."
						,"Activation Successful"
						,MessageBoxButtons.OK ,MessageBoxIcon.Information);
					UpdateUI();
				}
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Failed to write licence file in directory "+m_startup_options.AppDataDir);
				Misc.ShowErrorMessage(this, ex,
					"An error occurred while creating the licence file.\r\n" +
					"Please contact "+Constants.SupportEmail+" with details of this error."
					,"Activation Error");
			}
		}

		/// <summary>Launch a browser to the online store</summary>
		private void VisitStore()
		{
			Process p = new Process();
			p.StartInfo.Arguments = "url.dll,FileProtocolHandler " + "http://store.kagi.com/cgi-bin/store.cgi?storeID=6FFFY_LIVE";
			p.StartInfo.FileName = "rundll32";
			p.StartInfo.UseShellExecute = false;
			p.Start();
		}
	}

	/// <summary>A class for managing crippled functionality</summary>
	public static class Cripple
	{
		// have a timed "uncripple"... whenever a cripple limit is reached
		// show a dialog saying:
		// "This feature has been limited in the evaluation version."
		// "You can remove these limits for 5 minutes, after which time "
		// "the limits will be automatically reapplied."
		// "Please consider purchasing an activation code."
		//       [Visit Store] [Remove Limits] [Continue]   <- switch these buttons randomly
	}
}
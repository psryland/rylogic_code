using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace RyLogViewer
{
	/// <summary>Represents loaded licence information</summary>
	public class Licence
	{
		public Licence()
		{
			LicenceHolder = Constants.FreeLicence;
			EmailAddress = string.Empty;
			Company = string.Empty;
			ActivationCode = string.Empty;
			VersionMask = "*.*.*";
			Changed = false;
		}
		public Licence(string lic_file)
			: this()
		{
			if (!Path_.FileExists(lic_file))
				throw new FileNotFoundException($"Licence file '{lic_file}' not found");

			// Load the licence file
			var root = XDocument.Load(lic_file, LoadOptions.None).Root;
			if (root == null)
				throw new InvalidDataException("licence file invalid");

			LicenceHolder  = root.Element(nameof(LicenceHolder )).As(LicenceHolder);
			EmailAddress   = root.Element(nameof(EmailAddress  )).As(EmailAddress);
			Company        = root.Element(nameof(Company       )).As(Company);
			VersionMask    = root.Element(nameof(VersionMask   )).As(VersionMask);
			ActivationCode = root.Element(nameof(ActivationCode)).As(ActivationCode);
			Changed = false;
		}
		public Licence(Licence rhs)
		{
			LicenceHolder  = rhs.LicenceHolder;
			EmailAddress   = rhs.EmailAddress;
			Company        = rhs.Company;
			VersionMask    = rhs.VersionMask;
			ActivationCode = rhs.ActivationCode;
			Changed        = false;
		}
		~Licence()
		{
			// Use the unpredictability of the GC to do a file signing test
			PerformSigningVerification();
		}

		/// <summary>True if this is a free licence</summary>
		public bool IsFreeLicence
		{
			get { return LicenceHolder == Constants.FreeLicence; }
		}

		/// <summary>The name of the licence holder</summary>
		public string LicenceHolder
		{
			get { return m_licence_holder; }
			set { SetProp(ref m_licence_holder, value); }
		}
		private string m_licence_holder;

		/// <summary>The email address associated with the licence</summary>
		public string EmailAddress
		{
			get { return m_email_address; }
			set { SetProp(ref m_email_address, value); }
		}
		private string m_email_address;

		/// <summary>The optional associated company name</summary>
		public string Company
		{
			get { return m_company; }
			set { SetProp(ref m_company, value); }
		}
		private string m_company;

		/// <summary>The application version that the licence was issued for</summary>
		public string VersionMask
		{
			get { return m_app_version; }
			set { SetProp(ref m_app_version, value); }
		}
		private string m_app_version;

		/// <summary>The code provided by the RyLogViewer web site</summary>
		public string ActivationCode
		{
			get { return Convert.ToBase64String(m_activation_code, Base64FormattingOptions.InsertLineBreaks); }
			set
			{
				try
				{
					var code = Convert.FromBase64String(value);
					SetProp(ref m_activation_code, code);
				}
				catch (Exception) { }
			}
		}
		private byte[] m_activation_code;

		/// <summary>Set the value of a property if different and notify</summary>
		private void SetProp<T>(ref T prop, T value)
		{
			if (Equals(prop, value)) return;
			prop = value;
			OnChanged?.Invoke(this, EventArgs.Empty);
			Changed = true;
		}

		/// <summary>Raised whenever the licence data changes</summary>
		public event EventHandler OnChanged;
		public bool Changed;

		/// <summary>Returns a hash of the user details</summary>
		private string UserDetails
		{
			// This combination of user details is duplicated in the key gen script
			get { return $"{LicenceHolder}\n{EmailAddress}\n{Company}\n{VersionMask}\nRylogic Limited Is Awesome"; }
		}

		/// <summary>True if the licence is valid with itself</summary>
		public bool ValidActivationCode
		{
			get { return Rylogic.Common.ActivationCode.Validate(UserDetails, m_activation_code, Resources.public_key); }
		}

		/// <summary>True if the version in the licence covers this version of the app</summary>
		public bool ValidForThisVersion
		{
			get
			{
				// Check the licence is for this version
				var version0 = Util.AppVersion.SubstringRegex(@"(.*)\.(.*)\.(.*)");
				var version1 = VersionMask.SubstringRegex(@"(.*)\.(.*)\.(.*)");
				if (version0.Length != 3 || version1.Length != 3)
					return false;

				// Check the versions
				for (int i = 0; i != 3; ++i)
				{
					if (version1[i] == "*") continue;

					var v0 = int_.TryParse(version0[i]);
					var v1 = int_.TryParse(version1[i]);
					if (v0 != null && v1 != null && v0.Value <= v1.Value)
						continue;

					return false;
				}

				return true;
			}
		}

		/// <summary>True if the licence is valid but it's not for this version</summary>
		public bool NotForThisVersion
		{
			get { return ValidActivationCode && !ValidForThisVersion; }
		}

		/// <summary>True if this licence is valid</summary>
		public bool Valid
		{
			get { return ValidActivationCode && ValidForThisVersion; }
		}

		/// <summary>Output the licence details to a licence file</summary>
		public void WriteLicenceFile(string lic)
		{
			// Save the elements
			var root = new XElement("root");
			root.Add2(nameof(LicenceHolder), LicenceHolder, false);
			root.Add2(nameof(EmailAddress), EmailAddress, false);
			root.Add2(nameof(Company), Company, false);
			root.Add2(nameof(ActivationCode), ActivationCode, false);
			root.Add2(nameof(VersionMask), VersionMask, false);

			// Save the licence file
			root.Save(lic);
		}

		/// <summary>Check that the app has a correct signature</summary>
		public static void PerformSigningVerification()
		{
			var dispatcher = Dispatcher.CurrentDispatcher;

			// Do the signing test in a background thread
			ThreadPool.QueueUserWorkItem(_ =>
			{
				// Only test for a signed exe once / 5mins
				if (Environment.TickCount - m_signing_last_tested < RetestPeriodMS)
					return;

				// Perform the signing test
				m_signing_last_tested = Environment.TickCount;
				if (Rylogic.Crypt.Crypt.Validate(Util.ExecutablePath, Resources.public_key, false))
					return;

				// Notify if it fails
				Action failed_notification = () =>
				{
					MsgBox.Show(null,
						$"WARNING! This executable has been tampered with!\r\n" +
						$"Using this program may compromise your computer.\r\n" +
						$"\r\n" +
						$"Please contact ({Util.AppCompany}) ({Constants.SupportEmail}) with " +
						$"information about where you received this copy of the application.",
						"Tampered Executable Detected!",
						MsgBox.EButtons.OK, MsgBox.EIcon.Exclamation);

					Application.Current.Shutdown();
				};
				dispatcher?.BeginInvoke(failed_notification);
			});
		}
		private static int m_signing_last_tested = Environment.TickCount - RetestPeriodMS;
		private const int RetestPeriodMS = 5 * 60 * 1000;
	}
}

///// <summary>Returns a text summary of the licence info</summary>
//public Rtf.Builder InfoStringRtf()
//{
//	var rtf = new Rtf.Builder();
//	if (!Valid)
//	{
//		rtf.Append(new Rtf.TextStyle { FontSize = 10, FontStyle = Rtf.EFontStyle.Bold, ForeColourIndex = rtf.ColourIndex(Color.DarkRed) });
//		rtf.AppendLine("Free Edition Licence");
//		rtf.Append(new Rtf.TextStyle { FontSize = 8, FontStyle = Rtf.EFontStyle.Regular });
//		rtf.AppendLine("This copy of RyLogViewer is using the free edition licence.");
//		rtf.AppendLine("All features are available, however limits are in place.");
//		rtf.AppendLine("If you find RyLogViewer useful, please consider purchasing a licence.");
//	}
//	else
//	{
//		var blck = new Rtf.TextStyle { FontSize = 8, ForeColourIndex = rtf.ColourIndex(Color.Black) };
//		var blue = new Rtf.TextStyle { FontSize = 8, ForeColourIndex = rtf.ColourIndex(Color.DarkBlue) };
//		rtf.Append(blck).Append("Licence Holder:\t").Append(blue).AppendLine(LicenceHolder);
//		rtf.Append(blck).Append("Email Address: \t").Append(blue).AppendLine(EmailAddress);
//		rtf.Append(blck).Append("Company:       \t").Append(blue).AppendLine(Company);
//		rtf.Append(blck).Append("Versions:      \t").Append(blue).AppendLine(VersionMask);
//	}
//	return rtf;
//}

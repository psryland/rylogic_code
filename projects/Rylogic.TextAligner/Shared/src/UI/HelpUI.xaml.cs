using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
	public partial class HelpUI :Window
	{
		public HelpUI(UIElement owner)
		{
			InitializeComponent();
			Owner = Window.GetWindow(owner);

			DoDonate = Command.Create(this, DoDonateInternal);
			Accept = Command.Create(this, AcceptInternal);
			DataContext = this;
		}

		/// <summary>About info</summary>
		public string AboutText =>
			$"Rylogic Text Aligner\r\n" +
			$"Version: {Util.AssemblyVersion(Assembly.GetAssembly(typeof(HelpUI)))}\r\n" +
			$"Copyright © Rylogic Ltd 2007 - All Rights Reserved";

		/// <summary>Accept donations</summary>
		public Command DoDonate { get; }
		private void DoDonateInternal()
		{
			// This is mapped to my personal paypal account
			var business = "accounts@rylogic.co.nz"; // your paypal email
			var description = "Donation";            // '%20' represents a space. remember HTML!
			var country = "NZ";                      // AU  , US, etc.
			var currency = "NZD";                    // AUD , USD, etc.
			var PaypalDonateLink =
				$"https://www.paypal.com/cgi-bin/webscr" +
				$"?cmd={"_donations"}" +
				$"&business={business}" +
				$"&lc={country}" +
				$"&item_name={description}" +
				$"&currency_code={currency}" +
				$"&bn={"PP%2dDonationsBF"}";

			Process.Start(PaypalDonateLink);
		}

		/// <summary>Close the dialog</summary>
		public Command Accept { get; }
		private void AcceptInternal()
		{
			Close();
		}
	}
}

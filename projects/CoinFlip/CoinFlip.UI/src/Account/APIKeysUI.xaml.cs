using System.Windows;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI
{
	public partial class APIKeysUI : Window
	{
		static APIKeysUI()
		{
			APIKeyProperty = Gui_.DPRegister<APIKeysUI>(nameof(APIKey));
			APISecretProperty = Gui_.DPRegister<APIKeysUI>(nameof(APISecret));
		}
		public APIKeysUI(User user, string exchange_name)
		{
			InitializeComponent();
			Title = $"{exchange_name} API Keys";
			User = user;
			Exchange = exchange_name;
			DataContext = this;

			Loaded += (s, a) =>
			{
				var res = User.GetKeys(exchange_name, out var key, out var secret);
				if (res == User.EResult.Success)
				{
					APIKey = key;
					APISecret = secret;
				}
			};
		}

		/// <summary>The user that owns the keys</summary>
		private User User { get; }

		/// <summary>The exchange that the keys are for</summary>
		public string Exchange { get; }

		/// <summary>Prompt message for the dialog</summary>
		public string Prompt =>
			$"Enter the API Key and Secret for your account on {Exchange}.\r\n" +
			$"These will be stored in an encrypted file here:\r\n" +
			$"\"{Misc.ResolveUserPath($"{User.Name}.keys")}\"";

		/// <summary>API Key</summary>
		public string APIKey
		{
			get { return (string)GetValue(APIKeyProperty); }
			set { SetValue(APIKeyProperty, value); }
		}
		public static readonly DependencyProperty APIKeyProperty;

		/// <summary>API secret</summary>
		public string APISecret
		{
			get { return (string)GetValue(APISecretProperty); }
			set { SetValue(APISecretProperty, value); }
		}
		public static readonly DependencyProperty APISecretProperty;

		/// <summary>Ok Button click</summary>
		private void HandleOk(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}

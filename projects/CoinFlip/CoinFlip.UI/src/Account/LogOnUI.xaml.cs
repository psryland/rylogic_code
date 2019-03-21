using System;
using System.Windows;

namespace CoinFlip.UI
{
	public partial class LogOnUI : Window
	{
		public LogOnUI()
		{
			InitializeComponent();
			User = new User(string.Empty, string.Empty);
		}
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			UpdateUI();

			m_tb_username.TextChanged += (s, a) =>
			{
				UpdateUI();
			};
			m_tb_password.PasswordChanged += (s, a) =>
			{
				UpdateUI();
			};
			m_btn_ok.Click += (s, a) =>
			{
				DialogResult = true;
				Close();
			};
		}

		/// <summary></summary>
		public string Username
		{
			get { return m_tb_username.Text; }
			set { m_tb_username.Text = value; }
		}

		/// <summary></summary>
		public string Password
		{
			get { return m_tb_password.Password; }
			set { m_tb_password.Password = value; }
		}

		/// <summary></summary>
		public User User { get; private set; }

		/// <summary></summary>
		private void UpdateUI()
		{
			User = new User(Username, Password);
			var result = User.CheckKeys();
			switch (result)
			{
			default: throw new Exception($"Unknown APIKeys result: {result}");
			case User.EResult.Success:
				{
					m_btn_ok.Content = "Log On";
					m_btn_ok.IsEnabled = true;
					break;
				}
			case User.EResult.BadPassword:
				{
					m_btn_ok.Content = "Log On";
					m_btn_ok.IsEnabled = false;
					break;
				}
			case User.EResult.NotFound:
				{
					m_btn_ok.Content = "Create";
					m_btn_ok.IsEnabled = true;
					break;
				}
			}
		}
	}
}

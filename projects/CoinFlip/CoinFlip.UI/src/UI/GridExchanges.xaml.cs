﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridExchanges : DataGrid, IDockable
	{
		public GridExchanges(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Exchanges");
			Model = model;

			// Commands
			ToggleEnabled = Command.Create(this, () =>
			{
				if (Current == null) return;
				Current.Enabled = !Current.Enabled;
			});
			TogglePublicAPIOnly = Command.Create(this, () =>
			{
				if (Current == null) return;
				Current.ExchSettings.PublicAPIOnly = !Current.ExchSettings.PublicAPIOnly;
			});
			SetApiKeys = Command.Create(this, () =>
			{
				if (Current == null) return;
				ChangeApiKeys(Current);
			});
			RefreshTradePairs = Command.Create(this, () =>
			{
				if (Current == null) return;
				Current.PairsUpdateRequired = true;
			});
			ContextMenuOpening += (s, a) => ContextMenu.Items.TidySeparators();

			DataContext = this;
		}
		protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
			// Hide the context menu if there is no current exchange
			base.OnMouseRightButtonUp(e);
			e.Handled = Current == null;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				m_model = value;
				Exchanges = CollectionViewSource.GetDefaultView(m_model?.Exchanges);
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

		/// <summary>The view of the available exchanges</summary>
		public ICollectionView Exchanges { get; private set; }

		/// <summary>The currently selected exchange</summary>
		public Exchange Current
		{
			get => (Exchange)Exchanges.CurrentItem;
			set => Exchanges.MoveCurrentTo(value);
		}

		/// <summary>Enable/Disable the current exchange</summary>
		public Command ToggleEnabled { get; }

		/// <summary>Enable/Disable accessing private account data from the selected exchange</summary>
		public Command TogglePublicAPIOnly { get; }

		/// <summary>Set the API keys for the current exchange</summary>
		public Command SetApiKeys { get; }

		/// <summary>Single an update of the available pairs for the current exchange</summary>
		public Command RefreshTradePairs { get; }

		/// <summary>Change the API keys for 'exch'</summary>
		private void ChangeApiKeys(Exchange exch)
		{
			{// Require the user to enter the password first
				var ui = new LogOnUI { Username = Model.User.Name, Owner = Window.GetWindow(this) };
				if (ui.ShowDialog() != true)
				{
					return;
				}
				if (!ui.User.Cred.Equals(Model.User.Cred))
				{
					MessageBox.Show(Window.GetWindow(this), "Incorrect password", "API Keys", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}
			}

			{// Show the API Keys dialog
				Model.User.GetKeys(exch.Name, out var prev_key, out var prev_secret);

				var ui = new APIKeysUI(Model.User, exch.Name) { Owner = Window.GetWindow(this) };
				if (ui.ShowDialog() == true && (ui.APIKey != prev_key || ui.APISecret != prev_secret))
				{
					// Update the keys
					Model.User.SetKeys(ui.Exchange, ui.APIKey, ui.APISecret);
					exch.ExchSettings.PublicAPIOnly = ui.APIKey.HasValue() && ui.APISecret.HasValue();

					// Prompt to restart now
					var res = MessageBox.Show(Window.GetWindow(this), "Restart now to use updated keys?", "Update API Keys", MessageBoxButton.YesNo, MessageBoxImage.Question);
					if (res == MessageBoxResult.Yes)
						App.Current.Restart();
				}
			}
		}
	}
}
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CoinFlip.Bots;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Plugin;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridBots : Grid, IDockable, IDisposable
	{
		public GridBots(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Bots");
			Bots = new ListCollectionView(model.Bots) { Filter = x => ((IBot)x)?.BackTesting == Model.BackTesting };
			AvailableFunds = new ListCollectionView(model.Funds);
			Model = model;

			// Commands
			CreateNewBot = Command.Create(this, CreateNewBotInternal);
			AddExistingBot = Command.Create(this, AddExistingBotInternal);
			RemoveBot = Command.Create(this, RemoveBotInternal);
			ToggleActive = Command.Create(this, ToggleActiveInternal);
			RenameBot = Command.Create(this, RenameBotInternal);
			ShowConfig = Command.Create(this, ShowConfigInternal);

			m_grid.ContextMenu.Opened += delegate { m_grid.ContextMenu.Items.TidySeparators(); };
			DataContext = this;
		}
		public void Dispose()
		{
			Model = null;
			DockControl = null;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					Model.BackTestingChange -= HandleBackTestingChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					Model.BackTestingChange += HandleBackTestingChanged;
				}

				// Handler
				void HandleBackTestingChanged(object sender, PrePostEventArgs e)
				{
					if (e.Before) return;
					Bots.Refresh();
				}
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

		/// <summary>The data source for bots</summary>
		public ICollectionView Bots { get; }
		public IBot Current
		{
			get => (IBot)Bots.CurrentItem;
			set => Bots.MoveCurrentTo(value);
		}

		/// <summary>The Funds to choose from</summary>
		public ICollectionView AvailableFunds { get; }

		/// <summary>Create a new bot instance</summary>
		public Command CreateNewBot { get; }
		private void CreateNewBotInternal()
		{
			// Present a list of available bots
			var dlg = new NewBotUI(Window.GetWindow(this));
			if (dlg.ShowDialog() == true)
			{
				// Add a new instance of this plugin
				var bot_data = new BotData { Id = Guid.NewGuid(), Name = dlg.BotName, BackTesting = Model.BackTesting };
				Model.Bots.Add((IBot)dlg.SelectedBot.Create(bot_data, Model));
				Model.Bots.SaveToSettings();
			}
		}

		/// <summary>Add an existing bot instance</summary>
		public Command AddExistingBot { get; }
		private void AddExistingBotInternal()
		{

		}

		/// <summary>Remove an existing bot instance</summary>
		public Command RemoveBot { get; }
		private void RemoveBotInternal()
		{
			var bot = (IBot)Bots.CurrentItem;
			if (bot == null) return;
			bot.Active = false;
			Model.Bots.Remove(bot);
			Model.Bots.SaveToSettings();
		}

		/// <summary>Activate/Deactivate a bot</summary>
		public Command ToggleActive { get; }
		private void ToggleActiveInternal()
		{
			var bot = (IBot)Bots.CurrentItem;
			if (bot == null) return;
			bot.Active = !bot.Active;

			// Save the active state in the settings
			Model.Bots.PersistActiveState(bot);
		}

		/// <summary>Rename a bot</summary>
		public Command RenameBot { get; }
		private void RenameBotInternal()
		{
			if (Current == null) return;
			var dlg = new PromptUI(Window.GetWindow(this))
			{
				Title = "Rename Bot",
				Prompt = "Choose a new name for this bot",
				Value = Current.Name,
				ShowWrapCheckbox = false,
			};
			if (dlg.ShowDialog() == true)
			{
				Current.Name = dlg.Value;
				SettingsData.Settings.Save();
			}
		}

		/// <summary>Display the configuration options for the selected bot</summary>
		public Command ShowConfig { get; }
		private void ShowConfigInternal()
		{
			var bot = (IBot)Bots.CurrentItem;
			if (bot == null) return;
			bot.Configure(Window.GetWindow(this)).Wait();
		}

		/// <summary></summary>
		private void HandleActiveCellClick(object sender, MouseButtonEventArgs e)
		{
			ToggleActive.Execute();
		}
	}
}

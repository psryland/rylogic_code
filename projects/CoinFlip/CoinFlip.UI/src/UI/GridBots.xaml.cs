using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoinFlip.Bots;
using Rylogic.Gui.WPF;
using Rylogic.Extn;
using Rylogic.Utility;
using Rylogic.Plugin;

namespace CoinFlip.UI
{
	public partial class GridBots : Grid, IDockable, IDisposable
	{
		public GridBots(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Bots");
			Bots = new ListCollectionView(model.Bots);
			Model = model;

			// Commands
			CreateNewBot = Command.Create(this, CreateNewBotInternal);
			AddExistingBot = Command.Create(this, AddExistingBotInternal);
			RemoveBot = Command.Create(this, RemoveBotInternal);
			ToggleActive = Command.Create(this, ToggleActiveInternal);
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
				m_model = value;
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

		/// <summary>Create a new bot instance</summary>
		public Command CreateNewBot { get; }
		private void CreateNewBotInternal()
		{
			// Present a list of available bots
			var dlg = new ListUI(Window.GetWindow(this))
			{
				Title = "Available Bots",
				Prompt = "Select the Bot type to create",
				SelectionMode = SelectionMode.Single,
				DisplayMember = nameof(PluginFile.Name),
				AllowCancel = true,
			};
			dlg.Items.AddRange(IBot.AvailableBots());
			if (dlg.ShowDialog() == true)
			{
				// Add a new instance of this plugin
				var bot_plugin = (PluginFile)dlg.SelectedItem;
				Model.Bots.Add((IBot)bot_plugin.Create(Guid.NewGuid(), Model));
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
		}

		/// <summary>Activate/Deactivate a bot</summary>
		public Command ToggleActive { get; }
		private void ToggleActiveInternal()
		{
			var bot = (IBot)Bots.CurrentItem;
			if (bot == null) return;
			bot.Active = !bot.Active;
		}

		/// <summary>Display the configuration options for the selected bot</summary>
		public Command ShowConfig { get; }
		private void ShowConfigInternal()
		{
			var bot = (IBot)Bots.CurrentItem;
			if (bot == null) return;
			bot.Configure(Window.GetWindow(this));
		}
	}
}

using System.Collections.ObjectModel;
using System.Linq;
using CoinFlip.Bots;
using CoinFlip.Settings;
using Rylogic.Utility;

namespace CoinFlip
{
	public class BotContainer : ObservableCollection<IBot>
	{
		private readonly Model m_model;
		public BotContainer(Model model)
		{
			m_model = model;
		}

		public void RemoveAll()
		{
			// Deactivate and dispose all existing bots
			foreach (var bot in this)
				bot.Active = false;

			Util.DisposeRange(this);
			Clear();
		}

		/// <summary>Create instances of the bots from settings</summary>
		public void LoadFromSettings()
		{
			var available_bot_plugins = IBot.AvailableBots();

			// Select the source of bots based on the backtesting state.
			var bot_data = Model.BackTesting ? SettingsData.Settings.BackTesting.TestBots : SettingsData.Settings.LiveBots;

			// Create bot instances from the settings
			foreach (var bd in bot_data)
			{
				var bot_plugin_file = available_bot_plugins.FirstOrDefault(x => x.Type.FullName == bd.TypeName);
				if (bot_plugin_file == null)
				{
					Model.Log.Write(ELogLevel.Warn, $"Trading bot of type '{bd.TypeName}' could not be found");
					continue;
				}

				// Create an instance of the bot and activate it
				var bot = (IBot)bot_plugin_file.Create(bd, m_model);
				bot.Active = bd.Active;
				Add(bot);
			}
		}

		/// <summary>Persist the current bot collection to settings</summary>
		public void SaveToSettings()
		{
			var bot_data_array = this.Select(x => x.BotData).ToArray();
			if (Model.BackTesting)
				SettingsData.Settings.BackTesting.TestBots = bot_data_array;
			else
				SettingsData.Settings.LiveBots = bot_data_array;
		}

		/// <summary>Persist the active state of 'bot' in the settings</summary>
		public void PersistActiveState(IBot bot)
		{
			var bot_data_array = Model.BackTesting
				? SettingsData.Settings.BackTesting.TestBots
				: SettingsData.Settings.LiveBots;
			
			var bot_data = bot_data_array.First(x => x.Id == bot.Id);
			bot_data.Active = bot.Active;
			SettingsData.Settings.Save();
		}
	}
}

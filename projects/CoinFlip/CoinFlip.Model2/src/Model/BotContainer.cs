using System.Collections.ObjectModel;
using System.Linq;
using CoinFlip.Bots;
using CoinFlip.Settings;
using Rylogic.Utility;

namespace CoinFlip
{
	public class BotContainer : ObservableCollection<IBot>
	{
		public BotContainer(Model model)
		{
			var available_bot_plugins = IBot.AvailableBots();

			// Create bot instances from the settings
			foreach (var bd in SettingsData.Settings.Bots)
			{
				// Create all bots, both backtesting and live bots.
				// Only the appropriate bots will be visible/activated.
				var bot_plugin_file = available_bot_plugins.FirstOrDefault(x => x.Type.FullName == bd.TypeName);
				if (bot_plugin_file == null)
				{
					Model.Log.Write(ELogLevel.Warn, $"Trading bot of type '{bd.TypeName}' could not be found");
					continue;
				}

				// Create an instance of each bot and activate it
				var bot = (IBot)bot_plugin_file.Create(bd, model);
				bot.Active = bd.Active && bot.BackTesting == Model.BackTesting;
				Add(bot);
			}
		}

		/// <summary>Persist the current bot collection to settings</summary>
		public void SaveToSettings()
		{
			SettingsData.Settings.Bots = this.Select(x => x.BotData).ToArray();
		}
	}
}

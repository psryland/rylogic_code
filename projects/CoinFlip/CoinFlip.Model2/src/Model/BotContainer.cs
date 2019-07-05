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
				var bot_plugin_file = available_bot_plugins.FirstOrDefault(x => x.Type.FullName == bd.TypeName);
				if (bot_plugin_file == null)
				{
					Model.Log.Write(ELogLevel.Warn, $"Trading bot of type '{bd.TypeName}' could not be found");
					continue;
				}

				// Create an instance of the bot and activate it
				var bot = (IBot)bot_plugin_file.Create(bd.Id, model);
				bot.Active = bd.Active;
				Add(bot);
			}

			// Handle collection changed after we've created the initial bots
			CollectionChanged += (s, a) =>
			{
				// Update the settings data whenever the Bots collection changes
				SettingsData.Settings.Bots = this.Select(x => new BotData(x)).ToArray();
			};
		}
	}
}

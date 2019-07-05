using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinFlip.Settings;
using Rylogic.Plugin;

namespace CoinFlip.Bots
{
	public abstract class IBot : IDisposable
	{
		// Notes:
		//  - Bots must be designed to start and stop at any point. All data must be persisted to
		//    disk immediately.
		//  - Every new bot instance is assigned a GUID to distingush between multiple instances
		//    of the same bot.
		//
		// How to:
		//  - Create a .NET framework 4.7.2 class library project called 'Bot.<BotName>'
		//  - Add references to 'CoinFlip.Model' and 'Rylogic'
		//  - Sub-class 'IBot' and decorate with '[Plugin(typeof(IBot))]'
		//  - Add a project dependency to CoinFlip.UI on this new Bot so that it's built before CoinFlip.UI
		//  - Set the post build event to: @"py $(ProjectDir)..\post_build_bot.py $(TargetPath) $(ProjectDir) $(ConfigurationName)"
		//  - The derived type must have a constructor with the same signature as IBot

		public IBot(Guid id, Model model)
		{
			Id = id;
			Model = model;
			Fund = model.Funds[Fund.Main];
			Name = string.Empty;
		}
		public virtual void Dispose()
		{
			m_disposing = true;
			Active = false;
			Model = null;
		}
		private bool m_disposing;

		/// <summary>The unique ID for this bot instance</summary>
		public Guid Id { get; }

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				m_model = value;
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model;

		/// <summary>The fund available to this bot</summary>
		public Fund Fund { get; set; }

		/// <summary>User assigned name for the bot</summary>
		public string Name { get; set; }

		/// <summary>Activate/Deactivate the bot</summary>
		public bool Active
		{
			get { return m_active; }
			set
			{
				if (m_active == value) return;
				if (m_active)
				{
				}
				m_active = value;
				if (m_active)
				{
				}

				// Record the active state in the settings
				if (!m_disposing)
				{
					var settings = SettingsData.Settings.Bots.First(x => x.Id == Id);
					settings.Active = value;
					SettingsData.Settings.Save();
				}
			}
		}
		private bool m_active;

		/// <summary>Application shutdown signal</summary>
		public CancellationToken Shutdown => Model.Shutdown.Token;

		/// <summary>The filepath to use for settings for this bot</summary>
		public string SettingsFilepath => Misc.ResolveUserPath("Bots", $"settings.{Id}.xml");

		/// <summary>Configure this bot</summary>
		public virtual Task Configure(object owner)
		{
			// Notes:
			//  - Configure can be called at any point, even when the bot is running.
			//  - Configure is called in the UI thread, so the bot can display UI. 'owner' is a WPF Window

			return Task.CompletedTask;
		}

		/// <summary>Returns a dynamically checked list of the available bots</summary>
		public static IList<PluginFile> AvailableBots()
		{
			// Enumerate the available bots
			var bot_files = Plugins<IBot>.Enumerate(Misc.ResolveBotPath(), regex_filter: Misc.BotFilterRegex).ToArray();
			return bot_files;
		}
	}
}

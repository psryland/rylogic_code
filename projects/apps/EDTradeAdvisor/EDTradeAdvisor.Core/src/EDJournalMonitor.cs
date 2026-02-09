using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace EDTradeAdvisor
{
	public class EDJournalMonitor : IDisposable
	{
		// Notes:
		//  - Monitors the journal files in the ED user data directory
		private static readonly Regex JournalRegex = new(@"Journal\..*\.log");

		public EDJournalMonitor(CancellationToken shutdown)
		{
			Settings.Instance.SettingChange += HandleSettingChange;
			Location = new LocationNames();
			Shutdown = shutdown;
			InitJournalFiles();
		}
		public void Dispose()
		{
			Run = false;
			Settings.Instance.SettingChange -= HandleSettingChange;
		}

		/// <summary>App shutdown token</summary>
		private CancellationToken Shutdown { get; }

		/// <summary>Start/Stop the journal monitoring</summary>
		public bool Run
		{
			get { return m_watcher != null; }
			set
			{
				if (Run == value) return;
				if (Run)
				{
					Util.Dispose(ref m_watcher);
				}
				m_watcher = value ? new FileWatch(Shutdown, HandleFileChanged) : null;
				if (Run)
				{
					m_watcher.Add(Settings.Instance.JournalFilesDir, HandleFileChanged);
					m_watcher.PollPeriod = TimeSpan.FromMilliseconds(500);
					LocationChanged?.Invoke(this, EventArgs.Empty);
				}

				// Handlers
				bool HandleFileChanged(string filepath, object ctx)
				{
					var filename = Path_.FileName(filepath);

					// If the changed file is a journal file
					if (JournalRegex.IsMatch(Path_.FileName(filepath)))
					{
						// The current journal file has changed
						if (JournalFilepath != null && string.Compare(filepath, JournalFilepath, true) == 0)
						{
							// If the file no longer exists, forget it
							if (!Path_.FileExists(JournalFilepath))
								JournalFilepath = null;
							else
								JournalOffset = Parse(JournalFilepath, JournalOffset);
						}
						// If a newer journal file was found
						else if (JournalFilepath == null || string.Compare(filepath, JournalFilepath, true) > 0)
						{
							// Parse the remainder of the current journal (if there is one)
							if (JournalFilepath != null)
								JournalOffset = Parse(JournalFilepath, JournalOffset);

							// Read the new journal
							JournalFilepath = filepath;
							JournalOffset = Parse(JournalFilepath, 0L);
						}
					}

					// If the changed file is the market data file
					else if (string.Compare(filename, "Market.json", true) == 0)
					{
						MarketUpdate?.Invoke(this, new MarketUpdateEventArgs(filepath));
					}

					return true;
				}
			}
		}
		private FileWatch m_watcher;

		/// <summary>The current player location</summary>
		public LocationNames Location { get; }
		public event EventHandler LocationChanged;

		/// <summary>The player's ship cargo space</summary>
		public int? CargoCapacity
		{
			get;
			private set
			{
				if (field == value) return;
				field = value;
				CargoCapacityChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		public event EventHandler CargoCapacityChanged;

		/// <summary>The max jump range (unladen)</summary>
		public double? MaxJumpRange
		{
			get;
			private set
			{
				if (field == value) return;
				field = value;
				MaxJumpRangeChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		public event EventHandler MaxJumpRangeChanged;

		/// <summary>Raised when the Market.json file is created or changed</summary>
		public event EventHandler<MarketUpdateEventArgs> MarketUpdate;

		/// <summary>The latest journal name, currently being watched</summary>
		private string JournalFilepath
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				Log.Write(ELogLevel.Info, $"Reading ED Journal file: {value}");
			}
		}

		/// <summary>The offset into the journal that has been successfully parsed</summary>
		private long JournalOffset
		{
			get;
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException();
				field = value;
			}
		}

		/// <summary>Parse events in a journal file, reading from 'fileofs'</summary>
		private long Parse(string filepath, long fileofs)
		{
			// Assume 'fileofs' is the first character of a line
			using (var file = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) { Position = fileofs })
			{
				// Parse events from the journal
				var buffer = new byte[65536];
				int i = 0, b = 0;
				for (;;)
				{
					try
					{
						// Save the position of the start of the next line
						fileofs = file.Position;

						// Read a full line from the journal
						for (i = 0; i != buffer.Length && (b = file.ReadByte()) != -1 && b != '\n'; ++i)
							buffer[i] = (byte)b;

						// Buffer overflow, read to the next newline, then try to read the next line
						if (i == buffer.Length)
						{
							for (; (b = file.ReadByte()) != -1 && b != '\n';) { }
							if (b == '\n') continue;
							if (b == -1) break;
						}

						// Reached EoF before a full line was read
						if (b == -1)
							break;

						// Convert to a string
						var line = Encoding.UTF8.GetString(buffer, 0, i).TrimEnd('\r');
						var jobj = JObject.Parse(line);

						// Parse the line as Json
						var evt = jobj["event"].Value<string>();
						switch (evt)
						{
						case "Location":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseLocation(jobj);
							break;
						case "StartJump":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseStartJump(jobj);
							break;
						case "FSDJump":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseFSDJump(jobj);
							break;
						case "SupercruiseEntry":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseSupercruiseEntry(jobj);
							break;
						case "SupercruiseExit":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseSupercruiseExit(jobj);
							break;
						case "Docked":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseDocked(jobj);
							break;
						case "Undocked":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseUndocked(jobj);
							break;
						case "Loadout":
							Log.Write(ELogLevel.Debug, $"{filepath}:({fileofs}) - {evt} event");
							ParseLoadout(jobj);
							break;
						}
					}
					catch (Exception ex)
					{
						// Assume reader exceptions mean partial line reads
						if (!(ex is JsonReaderException)) Log.Write(ELogLevel.Error, ex, $"Error reading journal file: {JournalFilepath}");
						break;
					}
				}
				return fileofs;
			}
		}

		/// <summary>Parse a journal location event</summary>
		private void ParseLocation(JObject jobj)
		{
			Location.StarSystem = jobj["StarSystem"]?.Value<string>();
			Location.Station = jobj["StationName"]?.Value<string>();
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal docked event</summary>
		private void ParseDocked(JObject jobj)
		{
			Location.StarSystem = jobj["StarSystem"].Value<string>();
			Location.Station = jobj["StationName"].Value<string>();
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary></summary>
		private void ParseUndocked(JObject jobj)
		{
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal FSDJump event</summary>
		private void ParseStartJump(JObject jobj)
		{
			Location.StarSystem = null;
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal FSDJump event</summary>
		private void ParseFSDJump(JObject jobj)
		{
			Location.StarSystem = jobj["StarSystem"].Value<string>();
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal super cruise entry event</summary>
		private void ParseSupercruiseEntry(JObject jobj)
		{
			Location.StarSystem = null;
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Parse a journal super cruise exit event</summary>
		private void ParseSupercruiseExit(JObject jobj)
		{
			Location.StarSystem = jobj["StarSystem"].Value<string>();
			Location.Station = null;
			LocationChanged?.Invoke(this, EventArgs.Empty);
		}


		/// <summary>Parse a journal load out event</summary>
		private void ParseLoadout(JObject jobj)
		{
			CargoCapacity = jobj["CargoCapacity"].Value<int>();
			MaxJumpRange = jobj["MaxJumpRange"].Value<double>();
		}

		/// <summary></summary>
		private void InitJournalFiles()
		{
			// Parse all of the available journal files, in order
			var journal_files = Path_.EnumFileSystem(Settings.Instance.JournalFilesDir, SearchOption.TopDirectoryOnly, JournalRegex).OrderBy(x => x.Name);
			foreach (var file in journal_files)
			{
				JournalFilepath = file.FullName;
				JournalOffset = Parse(JournalFilepath, 0L);
			}
		}

		/// <summary>Handle settings changed</summary>
		private void HandleSettingChange(object sender, SettingChangeEventArgs e)
		{
			if (e.Before) return;
			if (e.Key == nameof(Settings.JournalFilesDir))
				InitJournalFiles();
		}

		/// <summary>Current player location</summary>
		[DebuggerDisplay("{Description,nq}")]
		public class LocationNames
		{
			public LocationNames(string system = null, string station = null)
			{
				StarSystem = system;
				Station = station;
			}

			/// <summary>The system that the player is currently in</summary>
			public string StarSystem { get; set; }

			/// <summary>The station that the player is currently in</summary>
			public string Station { get; set; }

			/// <summary></summary>
			public string Description => $"{StarSystem}/{Station}";
		}
	}

	#region EventArgs
	public class MarketUpdateEventArgs
	{
		public MarketUpdateEventArgs(string market_data_filepath)
		{
			MarketDataFilepath = market_data_filepath;
		}

		public string MarketDataFilepath { get; }
	}
	#endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Rylogic.Common;
using Rylogic.Plugin;
using Rylogic.Utility;
using RyLogViewer.Options;

namespace RyLogViewer
{
	public class Main : IReadOnlyList<ILine>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		private readonly IReport m_report;
		public Main(StartupOptions su, Settings settings, IReport report)
		{
			m_report = report;
			Settings = settings;

			Highlights = new HighLightContainer();
			Filters = new FilterContainer();

			LoadPlugins();
		}

		/// <summary>Notify property changes</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Raised when the collection of log data lines changes</summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>Application settings</summary>
		private Settings Settings
		{
			get { return m_settings; }
			set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{
					m_settings.SettingChange -= HandleSettingsChange;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChange += HandleSettingsChange;
				}

				// Handler
				void HandleSettingsChange(object sender, SettingChangeEventArgs e)
				{

				}
			}
		}
		private Settings m_settings;

		/// <summary>The highlight patterns</summary>
		public HighLightContainer Highlights { get; private set; }

		/// <summary>The filter patterns</summary>
		public FilterContainer Filters { get; private set; }

		/// <summary>Lists the names of the available formatters</summary>
		public IEnumerable<string> AvailableFormatters
		{
			get
			{
				yield return SingleLineFormatter.Name;
				yield return DelimitedLineFormatter.Name;
				yield return JsonLineFormatter.Name;
				foreach (var fmt in m_plugin_formatters.Instances)
					yield return fmt.Name;
			}
		}

		/// <summary>The name of the encoding currently in use</summary>
		public string EncodingName
		{
			get
			{
				if (Lines != null)
					return Tools.Humanise(Lines.LineIndex.Encoding.EncodingName);
				if (!string.IsNullOrEmpty(Settings.Format.Encoding))
					return Settings.Format.Encoding;
				return string.Empty;
			}
		}

		/// <summary>The line ending sequence currently in use</summary>
		public string LineEnding
		{
			get
			{
				if (Lines != null)
					return Tools.Humanise(Encoding.UTF8.GetString(Lines.LineIndex.LineEnd));
				if (!string.IsNullOrEmpty(Settings.Format.LineEnding))
					return Settings.Format.LineEnding;
				return string.Empty;
			}
		}

		/// <summary>The byte range of the current log data source</summary>
		public Range FileByteRange
		{
			get { return Lines?.LineIndex.FileByteRange ?? Range.Zero; }
		}

		/// <summary>Get/Set the log data to be displayed</summary>
		public ILogDataSource LogDataSource
		{
			[DebuggerStepThrough] get { return m_log_data_source; }
			set
			{
				if (m_log_data_source == value) return;
				if (m_log_data_source != null)
				{
					// Clear any references to the current log data
					//todo Bookmarks.Clear();

					Lines = null;
					Util.Dispose(ref m_log_data_source);
				}
				m_log_data_source = value;
				if (m_log_data_source != null)
				{
					// Determine the data source encoding
					var encoding = ChooseEncoding(m_log_data_source);
					var line_end = ChooseLineEnding(m_log_data_source, encoding);

					// Create an index for identifying lines
					var line_index = new LineIndex(m_log_data_source, encoding, line_end, m_settings);

					// Create a formatter for the lines
					var formatter = CreateFormatter(m_log_data_source, encoding);

					// Create a cache of lines to reduce formatting load
					Lines = new LineCache(line_index, formatter, m_report);

					// Build the log index
					Build(LogOpenPosition, true);
				}

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogDataSource)));
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			}
		}
		private ILogDataSource m_log_data_source;

		/// <summary>The cache of lines for the current log data source</summary>
		private LineCache Lines
		{
			[DebuggerStepThrough] get { return m_lines; }
			set
			{
				if (m_lines == value) return;
				if (m_lines != null)
				{
					m_lines.CollectionChanged -= HandleCollectionChanged;
					Util.Dispose(ref m_lines);
				}
				m_lines = value;
				if (m_lines != null)
				{
					m_lines.CollectionChanged += HandleCollectionChanged;
				}

				// Handlers
				void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					CollectionChanged?.Invoke(this, e);
				}
			}
		}
		private LineCache m_lines;

		/// <summary>Trigger an update of the line index</summary>
		private void Build(long filepos, bool reload)
		{
			// Compile a list of filters
			var filters = new List<IFilter>(Filters);
			if (Settings.QuickFilterEnabled)
			{
				// Add a RejectAll so that non-highlighted means discard
				filters.AddRange(Highlights);
				filters.Add(Filter.RejectAll);
			}

			// Build the line index
			Lines.LineIndex.Build(filepos, reload, filters);
		}

		/// <summary>Create a new instance of a formatter of the currently selected type</summary>
		private ILineFormatter CreateFormatter(ILogDataSource src, Encoding encoding)
		{
			switch (Settings.Format.Formatter)
			{
			case SingleLineFormatter.Name:
				{
					return new SingleLineFormatter(encoding);
				}
			case DelimitedLineFormatter.Name:
				{
					return new DelimitedLineFormatter();
				}
			case JsonLineFormatter.Name:
				{
					return new JsonLineFormatter();
				}
			default:
				{
					foreach (var fmt in m_plugin_formatters.Instances)
					{
						if (fmt.Name != Settings.Format.Formatter) continue;
						return fmt;
					}
					return new SingleLineFormatter(encoding);
				}
			}
		}

		/// <summary>Choose an encoding to apply to 'src'</summary>
		private Encoding ChooseEncoding(ILogDataSource src)
		{
			var auto_detect = string.IsNullOrEmpty(Settings.Format.Encoding);
			if (!auto_detect)
				return Tools.GetEncoding(Settings.Format.Encoding);

			// Auto detect the encoding
			using (var stream = src.OpenStream())
				return Tools.GuessEncoding(stream, out var _);
		}

		/// <summary>Choose the byte sequence to mark line endings</summary>
		private byte[] ChooseLineEnding(ILogDataSource src, Encoding encoding)
		{
			var auto_detect = string.IsNullOrEmpty(Settings.Format.LineEnding);
			if (!auto_detect)
				return Tools.GetLineEnding(Settings.Format.LineEnding);

			// Auto detect the line ending
			using (var stream = src.OpenStream())
				return Tools.GuessLineEnding(stream, encoding, Settings.LogData.MaxLineLength, out var _);
		}

		/// <summary>Load plugin assemblies</summary>
		private void LoadPlugins()
		{
			var plugin_dir = Util.ResolveAppPath("plugins");
			Path_.CreateDirs(plugin_dir);

			// Load line formatters
			m_plugin_formatters = new Plugins<ILineFormatter>()
				.Load(plugin_dir);

			// Report failures
			if (m_plugin_formatters.Failures.Count != 0)
			{
				var sb = new StringBuilder();
				sb.AppendLine("Plugin Load Failures:");
				foreach (var fail in m_plugin_formatters.Failures)
					sb.AppendLine(fail.Name).AppendLine(fail.Error.Message).AppendLine();

				// This will need delaying until the window is visible
				m_report.ErrorPopup(sb.ToString());
			}
		}
		private Plugins<ILineFormatter> m_plugin_formatters;

		/// <summary>The position in the log to open</summary>
		private long LogOpenPosition
		{
			get
			{
				if (Settings.LogData.OpenAtEnd)
				{
					using (var src = m_log_data_source.OpenStream())
						return src.Length;
				}
				return 0L;
			}
		}

		#region IReadOnlyList<ILine>
		public int Count => Lines?.Count ?? 0;
		public ILine this[int index] => Lines?[index] ?? new ErrorLine("Line Index out of range", Range.Zero);
		#endregion
		#region IEnumerable<ILine>
		public IEnumerator<ILine> GetEnumerator()
		{
			var lines = (IReadOnlyList<ILine>)Lines;
			return (lines ?? new ILine[0]).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}

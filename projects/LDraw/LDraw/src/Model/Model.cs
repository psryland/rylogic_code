using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using LDraw.UI;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw
{
	public sealed class Model :IDisposable
	{
		public Model(string[] args)
		{
			Sync = SynchronizationContext.Current ?? throw new Exception("No synchronisation context available");
			View3d = View3d.Create();
			StartupOptions = new StartupOptions(args);
			Settings = new SettingsData(StartupOptions.SettingsPath);
			FileWatchTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			Scenes = new ObservableCollection<SceneUI>();
			Scripts = new ObservableCollection<ScriptUI>();
			Assets = new ObservableCollection<AssetUI>();
		}
		public void Dispose()
		{
			FileWatchTimer = null!;
			View3d = null!;
			GC.SuppressFinalize(this);
		}

		/// <summary>The main thread synchronisation context</summary>
		private SynchronizationContext Sync { get; }

		/// <summary>The view3d dll context </summary>
		public View3d View3d
		{
			get => m_view3d;
			set
			{
				if (m_view3d == value) return;
				if (m_view3d != null)
				{
					m_view3d.AddFileProgress -= HandleAddFileProgress;
					m_view3d.OnSourcesChanged -= HandleSourcesChanged;
					m_view3d.Error -= ReportError;
					Util.Dispose(ref m_view3d!);
				}
				m_view3d = value;
				if (m_view3d != null)
				{
					m_view3d.Error += ReportError;
					m_view3d.OnSourcesChanged += HandleSourcesChanged;
					m_view3d.AddFileProgress += HandleAddFileProgress;
				}

				// Handlers
				void ReportError(object sender, View3d.ErrorEventArgs e)
				{
					Log.Write(ELogLevel.Error, e.Message, e.Filepath, e.FileLine);
				}
				void HandleSourcesChanged(object sender, View3d.SourcesChangedEventArgs e)
				{
					// Just prior to reloading sources
					if (e.Before && Settings.ClearErrorLogOnReload)
						Log.Clear();
				}
				void HandleAddFileProgress(object sender, View3d.AddFileProgressEventArgs e) // worker thread context
				{
					// Marshal to the main thread and update progress
					Sync.Post(x =>
					{
						var args = (View3d.AddFileProgressEventArgs)x;
						ParsingProgress ??= new ParsingProgressData(args.ContextId);

						// Only update with info from the same file
						if (ParsingProgress.ContextId != args.ContextId)
							return;

						if (args.Complete)
						{
							ParsingProgress = null;
						}
						else
						{
							ParsingProgress.DataSourceName = args.Filepath;
							ParsingProgress.DataOffset = args.FileOffset;
						}
					}, e);
				}
			}
		}
		private View3d m_view3d = null!;

		/// <summary>Parsed command line options</summary>
		public StartupOptions StartupOptions { get; }

		/// <summary>Application settings</summary>
		public SettingsData Settings
		{
			get => m_settings;
			set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{
					m_settings.SettingChange -= HandleSettingChange;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChange += HandleSettingChange;
				}

				// Handlers
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
					case nameof(SettingsData.CheckForChangesPollPeriodS):
						{
							FileWatchTimer.Interval = TimeSpan.FromSeconds(Settings.CheckForChangesPollPeriodS);
							break;
						}
					}
				}
			}
		}
		private SettingsData m_settings = null!;

		/// <summary>The scene instances</summary>
		public ObservableCollection<SceneUI> Scenes { get; }

		/// <summary>The script instances</summary>
		public ObservableCollection<ScriptUI> Scripts { get; }

		/// <summary>The asset instances</summary>
		public ObservableCollection<AssetUI> Assets { get; }

		/// <summary>Log events collection</summary>
		public ObservableCollection<LogControl.LogEntry> LogEntries
		{ 
			get => m_log_entries;
			set
			{
				if (m_log_entries == value) return;
				if (m_log_entries != null)
				{
					m_log_entries.CollectionChanged -= HandleLogEntriesChanged;
				}
				m_log_entries = value ?? new ObservableCollection<LogControl.LogEntry>();
				if (m_log_entries != null)
				{
					m_log_entries.CollectionChanged += HandleLogEntriesChanged;
				}

				// Handlers
				void HandleLogEntriesChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					if (!m_log_update_signalled && (
						e.Action == NotifyCollectionChangedAction.Add ||
						e.Action == NotifyCollectionChangedAction.Remove ||
						e.Action == NotifyCollectionChangedAction.Reset))
					{
						m_log_update_signalled = true;
						Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
						{
							LogEntriesChanged?.Invoke(this, EventArgs.Empty);
							m_log_update_signalled = false;
						}));
					}
				}
			}
		}
		private ObservableCollection<LogControl.LogEntry> m_log_entries = null!;
		private bool m_log_update_signalled;

		/// <summary>Raised when new log entries are added</summary>
		public event EventHandler? LogEntriesChanged;

		/// <summary>Notify of a file about to be opened</summary>
		public event EventHandler<ValueEventArgs<string>>? FileOpening;
		public void NotifyFileOpening(string filepath)
		{
			FileOpening?.Invoke(this, new ValueEventArgs<string>(filepath));
		}

		/// <summary>Progress updates for parsing. Null while not parsing</summary>
		public ParsingProgressData? ParsingProgress
		{
			get => m_parsing_progress;
			private set
			{
				if (m_parsing_progress == value) return;
				m_parsing_progress = value;
				ParsingProgressChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		private ParsingProgressData? m_parsing_progress;
		public event EventHandler? ParsingProgressChanged;

		/// <summary>Timer used to watch for file changes</summary>
		public DispatcherTimer FileWatchTimer
		{
			get => m_file_watch_timer;
			set
			{
				if (m_file_watch_timer == value) return;
				if (m_file_watch_timer != null)
				{
					m_file_watch_timer.Stop();
					m_file_watch_timer.Tick -= HandleCheckForChangedFiles;
				}
				m_file_watch_timer = value;
				if (m_file_watch_timer != null)
				{
					m_file_watch_timer.Interval = TimeSpan.FromSeconds(Settings.CheckForChangesPollPeriodS);
					m_file_watch_timer.Tick += HandleCheckForChangedFiles;
					m_file_watch_timer.Start();
				}

				// Handlers
				void HandleCheckForChangedFiles(object sender, EventArgs e)
				{
					foreach (var script in Scripts)
						script.CheckForChangedScript();

					if (Settings.AutoRefresh)
						m_view3d.CheckForChangedSources();
				}
			}
		}
		private DispatcherTimer m_file_watch_timer = null!;

		/// <summary>Return a generated name for a new scene UI</summary>
		public string GenerateSceneName()
		{
			for (; ; )
			{
				var name = $"{UITag.Scene}{++m_scene_number}";
				if (!Scenes.Any(x => string.Compare(x.SceneName, name, true) == 0))
					return name;
			}
		}
		private int m_scene_number;

		/// <summary>Return a generated name for a new script UI</summary>
		public string GenerateScriptName()
		{
			for (; ; )
			{
				var name = $"{UITag.Script}{++m_script_number}";
				if (!Scripts.Any(x => string.Compare(x.ScriptName, name, true) == 0))
					return name;
			}
		}
		private int m_script_number;

		/// <summary>Return the full filepath for a temporary script file</summary>
		public string GenerateTempScriptFilepath(out Guid context_id)
		{
			context_id = Guid.NewGuid();
			var filename = $"{UITag.Script}-{context_id}.ldr";
			var filepath = Path_.CombinePath(TempScriptDirectory, filename);
			return IsTempScriptFilepath(filepath) ? filepath : throw new Exception("Temp script filepath generation is incorrect");
		}

		/// <summary>True if 'name' is a generated scene name</summary>
		public bool IsGeneratedSceneName(string name)
		{
			var generated_name_pattern = $@"{UITag.Scene}\d*";
			return Regex.IsMatch(name, generated_name_pattern);
		}

		/// <summary>True if 'name' is a generated script name</summary>
		public bool IsGeneratedScriptName(string name)
		{
			var generated_name_pattern = $@"{UITag.Script}\d*";
			return Regex.IsMatch(name, generated_name_pattern);
		}

		/// <summary>True if filepath is a temporary script filepath</summary>
		public bool IsTempScriptFilepath(string filepath)
		{
			var temp_script_pattern = $@"^{UITag.Script}[-]{Guid_.RegexPattern}\.ldr$";
			return
				Path_.Compare(Path_.Directory(filepath), TempScriptDirectory) == 0 &&
				Regex.IsMatch(Path_.FileName(filepath), temp_script_pattern);
		}

		/// <summary>Extract the Guid from the temporary script filepath</summary>
		public Guid TempScriptFilepathToGuid(string filepath)
		{
			if (!IsTempScriptFilepath(filepath))
				throw new Exception($"'{filepath}' in not a valid temporary script filepath");

			var m = Regex.Match(Path_.FileName(filepath), Guid_.RegexPattern);
			return new Guid(m.Groups[1].Value);
		}

		/// <summary>Clear all instances from all scenes</summary>
		public void Clear()
		{
			foreach (var scene in Scenes)
			{
				var view = scene.SceneView;
				view.Scene.RemoveAllObjects();
				view.Scene.Invalidate();
			}
		}

		/// <summary>Clear all instances from one or more scenes</summary>
		public void Clear(IEnumerable<SceneUI> scenes)
		{
			Clear(scenes, Array.Empty<Guid>(), 0, 0);
		}

		/// <summary>Clear instances from one or more scenes</summary>
		public void Clear(SceneUI scene, Guid context_id) => Clear(new[] { scene }, new[] { context_id }, 1, 0);
		public void Clear(IEnumerable<SceneUI> scenes, Guid context_id) => Clear(scenes, new[] { context_id }, 1, 0);
		public void Clear(IEnumerable<SceneUI> scenes, Guid[] context_ids, int include_count, int exclude_count)
		{
			// Remove objects from the scenes
			foreach (var scene in scenes)
			{
				var view = scene.SceneView;
				view.Scene.RemoveObjects(context_ids, include_count, exclude_count);
				view.Scene.Invalidate();
			}

			// Delete unused objects
			if (context_ids.Length != 0)
			{
				View3d.DeleteUnused(context_ids, include_count, exclude_count);
			}
		}

		// Add objects associated with 'id' to the scenes
		public void AddObjects(SceneUI scene, Guid context_id) => AddObjects(new[] { scene }, new[] { context_id }, 1, 0);
		public void AddObjects(IEnumerable<SceneUI> scenes, Guid context_id) => AddObjects(scenes, new[] { context_id }, 1, 0);
		public void AddObjects(IEnumerable<SceneUI> scenes, Guid[] context_ids, int include_count, int exclude_count)
		{
			// Add the objects from 'id' this scene.
			foreach (var scene in scenes)
			{
				var view = scene.SceneView;
				view.Scene.AddObjects(context_ids, include_count, exclude_count);

				// Auto range the view
				if (scene.AutoRange)
					view.AutoRange();
				else
					view.Invalidate();
			}
		}

		/// <summary>Delete temporary scripts that are not currently open</summary>
		public void CleanTemporaryScripts()
		{
			// The filenames of temporary scripts that are currently open
			var currently_open = Scripts
				.Where(x => IsTempScriptFilepath(x.Filepath))
				.Select(x => Path_.FileName(x.Filepath))
				.ToHashSet(0);

			// Delete temporary scripts that aren't in 'currently_open'
			foreach (var file in TemporaryScripts())
			{
				if (currently_open.Contains(file.Name)) continue;
				file.Delete();
			}
		}

		/// <summary>The filepaths of existing temporary scripts</summary>
		public IEnumerable<FileSystemInfo> TemporaryScripts()
		{
			// Treat anything in the temporary script directory as a temporary script
			foreach (var file in Path_.EnumFileSystem(TempScriptDirectory, SearchOption.TopDirectoryOnly, exclude: FileAttributes.Directory))
				yield return file;
		}

		/// <summary>Return a collection of scenes to add objects to</summary>
		public IList<SceneUI> ChooseScenes(string prompt_text)
		{
			if (Scenes.Count == 0)
				throw new Exception($"No 3D scenes available");

			// If there's only one option, no need to prompt
			if (Scenes.Count == 1)
				return new[] { Scenes[0] };

			// Allow the objects to be added to the selected scenes
			var dlg = new ListUI(App.Current.MainWindow)
			{
				Title = "Select Scenes",
				Prompt = prompt_text,
				SelectionMode = SelectionMode.Extended,
				DisplayMember = nameof(SceneUI.SceneName),
				AllowCancel = true,
			};
			dlg.Items.AddRange(Scenes);

			// Prompt for the scenes to use
			if (dlg.ShowDialog() != true || dlg.SelectedItems.Count == 0)
				return Array.Empty<SceneUI>();

			return dlg.SelectedItems.Cast<SceneUI>().ToArray();
		}

		/// <summary>The directory to contain temporary scripts in</summary>
		private string TempScriptDirectory => Path_.CombinePath(StartupOptions.UserDataDir, "Temporary Scripts");

		/// <summary></summary>
		public static readonly string SupportedFilesFilter = Util.FileDialogFilter(
			"Supported Files", "*.ldr", "*.p3d", "*.3ds", "*.stl", "*.csv",
			"Ldr Script", "*.ldr",
			"Binary Model File", "*.p3d",
			"3D Studio Max Model File", "*.3ds",
			"STL CAD Model File", "*.stl",
			"Comma Separated Values", "*.csv",
			"All Files", "*.*");

		/// <summary>Text file types that can be edited in the script UI</summary>
		public static readonly string EditableFilesFilter = Util.FileDialogFilter(
			"Script Files", "*.ldr",
			"Text Files", "*.txt",
			"Comma Separated Values", "*.csv",
			"All Files", "*.*");

		/// <summary>Text file types that can be edited in the script UI</summary>
		public static readonly string AssetFilesFilter = Util.FileDialogFilter(
			"Binary Model File", "*.p3d",
			"3D Studio Max Model File", "*.3ds",
			"STL CAD Model File", "*.stl",
			"Comma Separated Values", "*.csv",
			"All Files", "*.*");

		/// <summary></summary>
		private static class UITag
		{
			public const string Scene = "Scene";
			public const string Script = "Script";
		}
	}
}

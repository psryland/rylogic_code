using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		public Model(StartupOptions options)
		{
			StartupOptions = options;
			Sync = SynchronizationContext.Current ?? throw new Exception("No synchronisation context available");
			View3d = View3d.Create();
			Settings = new SettingsData(StartupOptions.SettingsPath);
			FileWatchTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			Sources = [];
			Scenes = [];
			Scripts = [];

			// Ensure the temporary script directory exists
			Path_.CreateDirs(TempScriptDirectory);
		}
		public void Dispose()
		{
			FileWatchTimer = null!;
			View3d = null!;
			GC.SuppressFinalize(this);
		}

		/// <summary>Parsed command line options</summary>
		public StartupOptions StartupOptions { get; }

		/// <summary>The main thread synchronisation context</summary>
		private SynchronizationContext Sync { get; }

		/// <summary>The view3d DLL context </summary>
		public View3d View3d
		{
			get => m_view3d;
			set
			{
				if (m_view3d == value) return;
				if (m_view3d != null)
				{
					m_view3d.ParsingProgress -= HandleParsingProgress;
					m_view3d.OnSourcesChanged -= HandleSourcesChanged;
					m_view3d.Error -= ReportError;
					Util.Dispose(ref m_view3d!);
				}
				m_view3d = value;
				if (m_view3d != null)
				{
					m_view3d.Error += ReportError;
					m_view3d.OnSourcesChanged += HandleSourcesChanged;
					m_view3d.ParsingProgress += HandleParsingProgress;
				}

				// Handlers
				void ReportError(object? sender, View3d.ErrorEventArgs e)
				{
					Log.Write(ELogLevel.Error, e.Message, e.Filepath, e.FileLine);
				}
				void HandleSourcesChanged(object? sender, View3d.SourcesChangedEventArgs e)
				{
					// Refresh the collection of sources
					if (e.After)
					{
						Dictionary<Guid, Source> sources = [];
						m_view3d.EnumSources(src => sources.Add(src.ContextId, new Source(this, src)));
						Sources.SyncStable(sources.Values, (l, r) => l.ContextId == r.ContextId, (s, i) => s);
						SourcesChanged?.Invoke(this, EventArgs.Empty);
					}
					// This implements auto range on load... but sources can change for reasons that don't require
					// an auto range (e.g. measure tool graphics).

					//	// Just prior to reloading sources
					//	if (e.Before && Settings.ClearErrorLogOnReload)
					//		Log.Clear();
					//	
					//	// After a source change, reset
					//	if (e.After && Settings.ResetOnLoad)
					//		foreach (var scene in Scenes)
					//			scene.SceneView.AutoRange();
				}
				void HandleParsingProgress(object? sender, View3d.ParsingProgressEventArgs e) // worker thread context
				{
					// Marshal to the main thread and update progress
					Sync.Post(x =>
					{
						var args = (View3d.ParsingProgressEventArgs)x!;
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
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
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

		/// <summary>The collection of current Ldraw object sources</summary>
		public List<Source> Sources { get; }
		public event EventHandler? SourcesChanged;

		/// <summary>The scene instances</summary>
		public ObservableCollection<SceneUI> Scenes { get; }

		/// <summary>The script instances</summary>
		public ObservableCollection<ScriptUI> Scripts { get; }

		/// <summary>Notify of a file about to be opened</summary>
		public event EventHandler<ValueEventArgs<string>>? FileOpening;
		public void NotifyFileOpening(string filepath) => FileOpening?.Invoke(this, new ValueEventArgs<string>(filepath));

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
				void HandleCheckForChangedFiles(object? sender, EventArgs e)
				{
					try
					{
						//TODO: This should be done by the native code right?
						//foreach (var script in Scripts)
						//	script.CheckForChangedScript();
						//
						//if (Settings.AutoRefresh)
						//	m_view3d.CheckForChangedSources();
					}
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, ex, "Error during CheckForChangedFiles", string.Empty, 0);
					}
				}
			}
		}
		private DispatcherTimer m_file_watch_timer = null!;

		/// <summary>Add a file ldraw source</summary>
		public Source AddFileSource(string filepath)
		{
			NotifyFileOpening(filepath);
			var src = View3d.LoadScriptFromFile(filepath);
			return Sources.Add2(new Source(this, src));
		}

		/// <summary>Return a generated name for a new scene UI</summary>
		public string GenerateSceneName()
		{
			for (; ; )
			{
				var name = $"{UITag.Scene}-{++m_scene_number}";
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
				var name = $"{UITag.Script}-{++m_script_number}";
				if (!Scripts.Any(x => string.Compare(x.ScriptName, name, true) == 0))
					return name;
			}
		}
		private int m_script_number;

		/// <summary>Create an empty script and return its filepath</summary>
		public string CreateNewScriptFile()
		{
			var script_name = GenerateScriptName();
			var filepath = Path_.CombinePath(TempScriptDirectory, $"{script_name}.ldr");
			File.AppendText(filepath).Dispose();
			return filepath;
		}

		/// <summary>Open a ldr script text file in a script window</summary>
		public ScriptUI? OpenInEditor(Source src)
		{
			if (src.FilePath.Length == 0)
				return null;

			// See if there is already a script with this source
			foreach (var script in Scripts)
			{
				if (script.Source.ContextId == src.ContextId)
					return script;
			}

			// Otherwise, create a new one
			return Scripts.Add2(new ScriptUI(src));
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
			Clear(scenes, [], 0, 0);
		}

		/// <summary>Clear instances from one or more scenes</summary>
		public void Clear(SceneUI scene) => Clear(new[] { scene });
		public void Clear(SceneUI scene, Guid context_id) => Clear(new[] { scene }, [context_id], 1, 0);
		public void Clear(IEnumerable<SceneUI> scenes, Guid context_id) => Clear(scenes, [context_id], 1, 0);
		public void Clear(IEnumerable<SceneUI> scenes, Guid[] context_ids, int include_count, int exclude_count)
		{
			// Remove objects from the scenes
			foreach (var scene in scenes)
			{
				var view = scene.SceneView;
				view.Scene.RemoveObjects(context_ids, include_count, exclude_count);
				view.Scene.Invalidate();
			}

			//// Delete unused objects
			//if (context_ids.Length != 0)
			//{
			//	View3d.DeleteUnused(context_ids, include_count, exclude_count);
			//}
		}

		// Add objects associated with 'id' to the scenes
		public void AddObjects(SceneUI scene, Guid context_id) => AddObjects(new[] { scene }, [context_id], 1, 0);
		public void AddObjects(IEnumerable<SceneUI> scenes, Guid context_id) => AddObjects(scenes, [context_id], 1, 0);
		public void AddObjects(IEnumerable<SceneUI> scenes, Guid[] context_ids, int include_count, int exclude_count)
		{
			// Add the objects from 'id' this scene.
			foreach (var scene in scenes)
			{
				var view = scene.SceneView;
				view.Scene.AddObjects(context_ids, include_count, exclude_count);

				// Auto range the view
				if (Settings.ResetOnLoad)
					view.AutoRange();
				else
					view.Invalidate();
			}
		}

		/// <summary>The file paths of existing temporary scripts</summary>
		public IEnumerable<FileSystemInfo> TemporaryScripts()
		{
			// Treat anything in the temporary script directory as a temporary script
			foreach (var file in Path_.EnumFileSystem(TempScriptDirectory, SearchOption.TopDirectoryOnly, exclude: FileAttributes.Directory))
			{
				// Filter out files that aren't ldr script
				var temp_script_pattern = $@"^{UITag.Script}[-].*\.ldr$";
				if (!Regex.IsMatch(file.FullName, temp_script_pattern))
					continue;

				yield return file;
			}
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
			"Supported Files", "*.ldr", "*.bdr", "*.p3d", "*.3ds", "*.stl", "*.csv",
			"Ldr Script", "*.ldr", "*.bdr",
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

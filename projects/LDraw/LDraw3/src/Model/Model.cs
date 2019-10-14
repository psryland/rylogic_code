using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using LDraw.UI;
using Microsoft.Win32;
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
			View3d = View3d.Create();
			Sync = SynchronizationContext.Current ?? throw new Exception("No synchronisation context available");
			StartupOptions = new StartupOptions(args);
			Settings = new SettingsData(StartupOptions.SettingsPath);
			Scenes = new ObservableCollection<SceneUI>();
			Scripts = new ObservableCollection<ScriptUI>();
		}
		public void Dispose()
		{
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
				void ReportError(object sender, MessageEventArgs arg)
				{
					// todo: errorlevel, File/Line?
					Log.Write(ELogLevel.Info, arg.Message);
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
		public SettingsData Settings { get; }

		/// <summary>The scene instances</summary>
		public ObservableCollection<SceneUI> Scenes { get; }

		/// <summary>The scene instances</summary>
		public ObservableCollection<ScriptUI> Scripts { get; }

		/// <summary>Notify of a file about to be opened</summary>
		public event EventHandler<ValueEventArgs<string>>? FileOpening;

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

		/// <summary>Return a generated name for a new scene UI</summary>
		public string GenerateSceneName()
		{
			var max = 0;
			foreach (var scene in Scenes.Where(x => IsGeneratedSceneName(x.SceneName)))
			{
				var idx_string = scene.SceneName.Substring(UITag.Scene.Length);
				var idx = int_.TryParse(idx_string) ?? 0;
				max = Math.Max(max, idx);
			}
			return max > 0 ? $"{UITag.Scene}{max + 1}" : UITag.Scene;
		}

		/// <summary>Return a generated name for a new script UI</summary>
		public string GenerateScriptName()
		{
			var max = 0;
			foreach (var script in Scripts.Where(x => IsGeneratedScriptName(x.ScriptName)))
			{
				var idx_string = script.ScriptName.Substring(UITag.Script.Length);
				var idx = int_.TryParse(idx_string) ?? 0;
				max = Math.Max(max, idx);
			}
			return max > 1 ? $"{UITag.Script}{max + 1}" : UITag.Script;
		}

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

		/// <summary>Clear the instances from one or more scenes</summary>
		public void Clear(IList<SceneUI> scenes, Guid[]? delete_ids, int include_count, int exclude_count)
		{
			// Remove objects from the scene
			foreach (var scene in scenes)
			{
				var view = scene.SceneView;
				view.Scene.RemoveObjects(Array.Empty<Guid>(), 0, 0);
			}

			// Delete unused objects
			if (delete_ids != null)
			{
				if (include_count == 0)
				{
					// Exclude the chart tools if this is a 'clear all' type of clear.
					Array.Resize(ref delete_ids, delete_ids.Length + 1);
					delete_ids[delete_ids.Length - 1] = ChartControl.ChartTools.Id;
					exclude_count++;
				}
				View3d.DeleteUnused(delete_ids, include_count, exclude_count);
			}
		}

		// Add objects associated with 'id' to the scenes
		public void AddObjects(IList<SceneUI> scenes, Guid id, bool additional)
		{
			// Remove other objects from the scene if this is not an additional add
			if (!additional)
				Clear(scenes, new[] { id }, 0, 1);

			// Add the objects from 'id' this scene.
			foreach (var scene in scenes)
			{
				var view = scene.SceneView;
				view.Scene.AddObjects(new[] { id }, 1, 0);

				// Auto range the view
				if (scene.AutoRange)
					view.AutoRange();
				else
					view.Invalidate();
			}
		}

		/// <summary>Add objects from a file to one or more scenes</summary>
		public void OpenFile(string? filepath = null, bool additional = false, IList<SceneUI>? scenes = null)
		{
			// Prompt for a filepath if none provided
			if (filepath == null || filepath.Length == 0)
			{
				var filter = Util.FileDialogFilter(
					"Supported Files", "*.ldr", "*.p3d", "*.3ds", "*.stl", "*.csv",
					"Ldr Script", "*.ldr",
					"Binary Model File", "*.p3d",
					"3D Studio Max Model File", "*.3ds",
					"STL CAD Model File", "*.stl",
					"Comma Separated Values", "*.csv",
					"All Files", "*.*");

				var dlg = new OpenFileDialog { Title = "Open Ldr Script file", Filter = filter };
				if (dlg.ShowDialog(App.Current.MainWindow) != true) return;
				filepath = dlg.FileName ?? throw new FileNotFoundException($"A invalid filepath was selected");
			}

			// If the file doesn't exist reject it
			if (!Path_.FileExists(filepath))
				throw new FileNotFoundException($"File '{filepath}' does not exist");

			// If no scene is provided, prompt for one if there are multiple choices
			scenes ??= ChooseScenes("Select the scene(s) to add objects to");
			if (scenes.Count == 0)
				return;

			// Notify of the file open (so it can be added to the recent file history)
			FileOpening?.Invoke(this, new ValueEventArgs<string>(filepath));

			// If the file has already been loaded in another scene, just add instances to 'scenes'
			var ctx_id = View3d.ContextIdFromFilepath(filepath);
			if (ctx_id != null)
			{
				AddObjects(scenes, ctx_id.Value, additional);
			}
			else
			{
				// Otherwise, load the source in a background thread
				var sync = SynchronizationContext.Current ?? throw new Exception("No synchronisation context");
				var include_paths = Settings.IncludePaths;
				ThreadPool.QueueUserWorkItem(x =>
				{
					// Load a source file and save the context id for that file
					var id = View3d.LoadScriptSource(filepath, true, include_paths);
					sync.Post(_ => AddObjects(scenes, id, additional), null);
				});
			}
		}

		/// <summary>Add objects from a script string to one or more scenes</summary>
		public void AddScript(string text, bool additional = false, IList<SceneUI>? scenes = null, Guid? context_id = null)
		{
			if (text == null || text.Length == 0)
				return;

			// If no scene is provided, prompt for one if there are multiple choices
			scenes ??= ChooseScenes("Select the scene(s) to add objects to");
			if (scenes.Count == 0)
				return;

			// Load the source in a background thread
			var sync = SynchronizationContext.Current ?? throw new Exception("No synchronisation context");
			var include_paths = Settings.IncludePaths;
			ThreadPool.QueueUserWorkItem(x =>
			{
				// Load a script and save the context id for that file
				var id = View3d.LoadScript(text, file:false, context_id, include_paths);
				sync.Post(_ => AddObjects(scenes, id, additional), null);
			});
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
		private IList<SceneUI> ChooseScenes(string prompt_text)
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
		private static class UITag
		{
			public const string Scene = "Scene";
			public const string Script = "Script";
		}
	}
}

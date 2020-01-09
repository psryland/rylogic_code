using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using Rylogic.Common;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace LDraw.UI
{
	public sealed partial class AssetUI :UserControl, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - An asset is a non-script geometry source such as p3d models etc.
		//  - This object is analogous to a 'ScriptUI', but for binary files.
		//  - AssetUI is a list item, not a fully dockable UI element.

		public AssetUI(Model model, string name, string filepath, Guid context_id)
		{
			InitializeComponent();
			ContextMenu = (ContextMenu)FindResource("AssetCMenu");
			Context = new Context(model, name, context_id);
			Filepath = filepath;

			Render = Command.Create(this, RenderInternal);
			RemoveObjects = Command.Create(this, RemoveObjectsInternal);
			CloseAsset = Command.Create(this, CloseAssetInternal);

			DataContext = this;
		}
		public void Dispose()
		{
			// Remove objects from all scenes
			Context.SelectedScenes = Array.Empty<SceneUI>();
			Model.Assets.Remove(this);

			Context = null!;
			GC.SuppressFinalize(this);
		}

		/// <summary>The context id and scenes that associated objects are added to</summary>
		public Context Context
		{
			get => m_context;
			private set
			{
				if (m_context == value) return;
				if (m_context != null)
				{
					m_context.PropertyChanged -= HandlePropertyChanged;
					Util.Dispose(ref m_context!);
				}
				m_context = value;
				if (m_context != null)
				{
					m_context.PropertyChanged += HandlePropertyChanged;
				}

				// Handlers
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
					case nameof(Context.Name):
						{
							NotifyPropertyChanged(nameof(AssetName));
							break;
						}
					}
				}
			}
		}
		private Context m_context = null!;

		/// <summary>App logic</summary>
		private Model Model => Context.Model;

		/// <summary>Name of the asset</summary>
		public string AssetName => Context.Name;

		/// <summary>Context id for objects created by this scene</summary>
		public Guid ContextId => Context.ContextId;

		/// <summary>The filepath for this script</summary>
		public string Filepath
		{
			get => m_filepath;
			private set
			{
				if (m_filepath == value) return;
				m_filepath = value;

				// Update the asset name
				Context.Name = Path_.FileName(m_filepath);
			}
		}
		private string m_filepath = null!;

		/// <summary>Update the objects associated with 'ContextId' within View3D's object store</summary>
		private void UpdateObjects()
		{
			var scenes = Context.SelectedScenes.ToArray();
			var include_paths = Model.Settings.IncludePaths;

			// Load the asset file in a background thread
			ThreadPool.QueueUserWorkItem(x =>
			{
				Model.View3d.LoadScript(Filepath, true, ContextId, include_paths, OnAdd);
				void OnAdd(Guid id, bool before)
				{
					if (before)
						Model.Clear(scenes, id);
					else
						Model.AddObjects(scenes, id);
				}
			});
		}

		/// <summary>Render the contents of this script file in the selected scene</summary>
		public Command Render { get; }
		private void RenderInternal()
		{
			var scenes = Context.SelectedScenes.ToArray();
			if (scenes.Length == 0)
				return;

			UpdateObjects();
		}

		/// <summary>Remove objects associated with this asset from the selected scenes</summary>
		public Command RemoveObjects { get; }
		private void RemoveObjectsInternal()
		{
			var scenes = Context.SelectedScenes.ToArray();
			if (scenes.Length == 0) return;
			Model.Clear(scenes, ContextId);
		}

		/// <summary>Close and remove this asset</summary>
		public Command CloseAsset { get; }
		private void CloseAssetInternal()
		{
			Dispose();
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

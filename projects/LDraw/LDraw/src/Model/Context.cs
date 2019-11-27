using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using LDraw.UI;
using Rylogic.Extn;

namespace LDraw
{
	public sealed class Context :IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - A 'Context' contains a context id and the scenes that objects,
		//    associated with that context id, are added to.

		public Context(Model model, string name, Guid context_id)
		{
			Name = name;
			Model = model;
			ContextId = context_id;
			AvailableScenes = new ListCollectionView(new List<SceneWrapper>());
			PopulateAvailableScenes();
		}
		public void Dispose()
		{
			// Remove objects from all scenes
			SelectedScenes = Array.Empty<SceneUI>();

			Model = null!;
			GC.SuppressFinalize(this);
		}

		/// <summary>An identifier for the context</summary>
		public string Name
		{
			get => m_name;
			set
			{
				if (m_name == value) return;
				m_name = value;
				NotifyPropertyChanged(nameof(Name));
			}
		}
		private string m_name = null!;

		/// <summary>Context id for objects created by this scene</summary>
		public Guid ContextId { get; }

		/// <summary>App logic</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Scenes.CollectionChanged -= HandleScenesCollectionChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					// Don't add this asset to m_model.Assets, that's the caller's choice.
					m_model.Scenes.CollectionChanged += HandleScenesCollectionChanged;
				}

				// Handlers
				void HandleScenesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					PopulateAvailableScenes();
				}
			}
		}
		private Model m_model = null!;

		/// <summary>The available scenes</summary>
		public ICollectionView AvailableScenes { get; }
		public void PopulateAvailableScenes()
		{
			// Refresh the scenes collection
			var scenes = (List<SceneWrapper>)AvailableScenes.SourceCollection;
			scenes.Sync(Model.Scenes.Select(x => new SceneWrapper(x, HandleSelected)));
			if (!SelectedScenes.Any() && scenes.Count != 0) scenes[0].Selected = true;
			AvailableScenes.Refresh();

			void HandleSelected(SceneUI scene, bool is_selected)
			{
				if (is_selected)
					Model.AddObjects(scene, ContextId);
				else
					Model.Clear(scene, ContextId);

				NotifyPropertyChanged(nameof(SelectedScenes));
				NotifyPropertyChanged(nameof(SelectedScenesDescription));
				NotifyPropertyChanged(nameof(CanRender));
			}
		}

		/// <summary>The scene that this context renders to</summary>
		public IEnumerable<SceneUI> SelectedScenes
		{
			get
			{
				var available = (List<SceneWrapper>)AvailableScenes.SourceCollection;
				return available.Where(x => x.SceneUI != null && x.Selected).Select(x => x.SceneUI!);
			}
			set
			{
				var set = value.ToHashSet(0);
				var available = (List<SceneWrapper>)AvailableScenes.SourceCollection;
				foreach (var scene in available)
					scene.Selected = set.Contains(scene.SceneUI!);
			}
		}

		/// <summary>A string description of the output scenes for this script</summary>
		public string SelectedScenesDescription
		{
			get
			{
				var desc = string.Join(",", SelectedScenes.Select(x => x.SceneName));
				return desc.Length != 0 ? desc : "None";
			}
		}

		/// <summary>True if there is a scene to render to</summary>
		public bool CanRender => SelectedScenes.Any();

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

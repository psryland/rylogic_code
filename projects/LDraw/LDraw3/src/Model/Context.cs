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
			scenes.Sync(Model.Scenes.Select(x => new SceneWrapper(x, this)));
			if (!SelectedScenes.Any() && scenes.Count != 0) scenes[0].Selected = true;
			AvailableScenes.Refresh();
		}

		/// <summary>The scene that this context renders to</summary>
		public IEnumerable<SceneUI> SelectedScenes
		{
			get
			{
				var available = (List<SceneWrapper>)AvailableScenes.SourceCollection;
				return available.Where(x => x.Selected).Select(x => x.SceneUI);
			}
			set
			{
				var set = value.ToHashSet(0);
				var available = (List<SceneWrapper>)AvailableScenes.SourceCollection;
				foreach (var scene in available)
					scene.Selected = set.Contains(scene.SceneUI);
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

		/// <summary>Binding wrapper for a scene</summary>
		private class SceneWrapper
		{
			// Notes:
			//  - This wrapper is needed because when UIElement objects are used as the items
			//    of a combo box it treats them as child controls, becoming their parent.

			private readonly Context m_owner;
			public SceneWrapper(SceneUI scene, Context owner)
			{
				SceneUI = scene;
				m_owner = owner;
			}

			/// <summary>The wrapped scene</summary>
			public SceneUI SceneUI { get; }

			/// <summary>The name of the wrapped scene</summary>
			public string SceneName => SceneUI.SceneName;

			/// <summary>True if the scene is selected</summary>
			public bool Selected
			{
				get => m_selected;
				set
				{
					if (m_selected == value) return;
					if (m_selected)
					{
						m_owner.Model.Clear(SceneUI, m_owner.ContextId);
					}
					m_selected = value;
					if (m_selected)
					{
						m_owner.Model.AddObjects(SceneUI, m_owner.ContextId);
					}
					m_owner.NotifyPropertyChanged(nameof(SelectedScenes));
					m_owner.NotifyPropertyChanged(nameof(SelectedScenesDescription));
					m_owner.NotifyPropertyChanged(nameof(CanRender));
				}
			}
			private bool m_selected;

			/// <summary></summary>
			public static implicit operator SceneUI?(SceneWrapper? x) => x?.SceneUI;

			/// <summary></summary>
			public override bool Equals(object obj)
			{
				return obj is SceneWrapper wrapper && ReferenceEquals(SceneUI, wrapper.SceneUI);
			}
			public override int GetHashCode()
			{
				return SceneUI.GetHashCode();
			}
		}
	}
}

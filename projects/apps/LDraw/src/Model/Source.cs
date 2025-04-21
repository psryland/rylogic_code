using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using LDraw.UI;

namespace LDraw
{
	public class Source : IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - Model for an LDraw source
		//  - View component is the 'SourceItemUI'

		public Source(Guid context_id, Model model)
		{
			ContextId = context_id;
			Model = model;
			SelectedScenes = model.Scenes.Count != 0 ? [model.Scenes[0]] : [];
		}
		public void Dispose()
		{
			// Remove objects from all scenes
		//	SelectedScenes = Array.Empty<SceneUI>();

			Model = null!;
			GC.SuppressFinalize(this);
		}

		/// <summary>Access to the app data</summary>
		public Model Model
		{
			get => m_model;
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.SourcesChanged -= HandleSourcesChanged;
					m_model.Scenes.CollectionChanged -= HandleScenesCollectionChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Scenes.CollectionChanged += HandleScenesCollectionChanged;
					m_model.SourcesChanged += HandleSourcesChanged;
				}

				void HandleSourcesChanged(object? sender, EventArgs args)
				{
					var info = Model.View3d.SourceInformation(ContextId);

					Name = info.Name;
					ObjectCount = info.ObjectCount;
				}
				void HandleScenesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					NotifyPropertyChanged(nameof(AvailableScenes));
				}
			}
		}
		private Model m_model = null!;

		/// <summary>The context id associated with the source</summary>
		public Guid ContextId { get; }

		/// <summary>The name of the source</summary>
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
		private string m_name = string.Empty;

		/// <summary>The number of objects in this source</summary>
		public int ObjectCount
		{
			get => m_object_count;
			set
			{
				if (m_object_count == value) return;
				m_object_count = value;
				NotifyPropertyChanged(nameof(ObjectCount));
			}
		}
		private int m_object_count = 0;
		
		/// <summary>True if there is a scene to render to</summary>
		public bool CanRender => SelectedScenes.Count != 0;

		/// <summary>The scenes to render this script in</summary>
		public ObservableCollection<SceneUI> SelectedScenes { get; }

		/// <summary>The available scenes to render this source in</summary>
		public ObservableCollection<SceneUI> AvailableScenes => Model.Scenes;

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}

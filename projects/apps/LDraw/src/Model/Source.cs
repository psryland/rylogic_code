using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using LDraw.UI;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace LDraw
{
	public class Source : IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - Model for an LDraw source
		//  - View component is the 'SourceItemUI'

		public Source(Model model, View3d.Source source)
		{
			Model = model;
			View3dSource = source;
			SelectedScenes = model.Scenes.Count != 0 ? [model.Scenes[0]] : [];
		}
		public void Dispose()
		{
			// Remove objects from all scenes
		//	SelectedScenes = Array.Empty<SceneUI>();

			Model = null!;
			View3dSource = null!;
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
					m_model.Scenes.CollectionChanged -= HandleScenesCollectionChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Scenes.CollectionChanged += HandleScenesCollectionChanged;
				}

				void HandleScenesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
				{
					NotifyPropertyChanged(nameof(AvailableScenes));
				}
			}
		}
		private Model m_model = null!;

		/// <summary>The native LDraw source</summary>
		private View3d.Source View3dSource
		{
			get => m_source;
			set
			{
				if (m_source == value) return;
				if (m_source != null)
				{
					m_source.PropertyChanged -= HandleSourcePropertyChanged;
					Util.Dispose(ref m_source!);
				}
				m_source = value;
				if (m_source != null)
				{
					m_source.PropertyChanged += HandleSourcePropertyChanged;
				}
			
				void HandleSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					if (e.PropertyName != null)
						NotifyPropertyChanged(e.PropertyName);
				}
			}
		}
		private View3d.Source m_source = null!;

		/// <summary>The context id associated with the source</summary>
		public Guid ContextId => View3dSource.ContextId;

		/// <summary>The name of the source</summary>
		public string Name
		{
			get => View3dSource.Name;
			set => View3dSource.Name = value;
		}

		/// <summary>The filepath associated with the source</summary>
		public string FilePath => View3dSource?.Info.FilePath ?? string.Empty;

		/// <summary>The number of objects in this source</summary>
		public int ObjectCount => View3dSource?.Info.ObjectCount ?? 0;

		/// <summary>True if there is a scene to render to</summary>
		public bool CanRender => SelectedScenes.Count != 0;

		/// <summary>True if this source has a file that can be edited</summary>
		public bool CanEdit => View3dSource?.Info is View3d.SourceInfo info && Path_.FileExists(info.FilePath) && info.TextFormat;

		/// <summary>The scenes to render this script in</summary>
		public ObservableCollection<SceneUI> SelectedScenes { get; }

		/// <summary>The available scenes to render this source in</summary>
		public ObservableCollection<SceneUI> AvailableScenes => Model.Scenes;

		/// <summary>Remove this source</summary>
		public void Remove()
		{
			View3dSource.Remove();
		}

		/// <summary>Reload the objects from this source</summary>
		public void Reload()
		{
			View3dSource.Reload();
		}

		/// <summary>Raised when the source has changed</summary>
		public event EventHandler SourceChanged
		{
			add { View3dSource.SourceChanged += value; }
			remove { View3dSource.SourceChanged -= value; }
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Extn;
using Rylogic.Gui.WPF;

namespace LDraw.UI
{
	public partial class SceneSelectorUI : UserControl, INotifyPropertyChanged
	{
		public SceneSelectorUI()
		{
			InitializeComponent();
			SelectableScenesView = new ListCollectionView(new List<SelectableScene>());

			// Don't bind DataContext, we need to inherit it
		}

		/// <summary>The selected scenes (as a single string)</summary>
		public string SelectedScenesDescription
		{
			get => SelectedScenes?.Any() ?? false ? string.Join(",", SelectedScenes.Select(x => x.SceneName)) : "None";
		}

		/// <summary>The selected scenes binding view</summary>
		public ICollectionView SelectableScenesView
		{
			get => m_selectable_scenes_view;
			private set
			{
				if (m_selectable_scenes_view == value) return;
				m_selectable_scenes_view = value;
				NotifyPropertyChanged(nameof(SelectableScenesView));
			}
		}
		private ICollectionView m_selectable_scenes_view = null!;

		/// <summary>The selected scenes on the owning component</summary>
		public IList<SceneUI> SelectedScenes
		{
			get => (IList<SceneUI>)GetValue(SelectedScenesProperty) ?? [];
			set => SetValue(SelectedScenesProperty, value);
		}
		private void SelectedScenes_Changed(IList<SceneUI> nue, IList<SceneUI> old)
		{
			if (old is INotifyCollectionChanged old_ncc)
				old_ncc.CollectionChanged -= HandleCollectionChanged;
			if (nue is INotifyCollectionChanged nue_ncc)
				nue_ncc.CollectionChanged += HandleCollectionChanged;

			HandleCollectionChanged(null, default!);
			NotifyPropertyChanged(nameof(SelectedScenes));

			void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
			{
				SelectableScenesView.Refresh();
				NotifyPropertyChanged(nameof(SelectedScenesDescription));
			}
		}
		public static readonly DependencyProperty SelectedScenesProperty = Gui_.DPRegister<SceneSelectorUI>(nameof(SelectedScenes), null, Gui_.EDPFlags.None);

		/// <summary>The scenes to choose from</summary>
		public IReadOnlyList<SceneUI> AvailableScenes
		{
			get => (IReadOnlyList<SceneUI>)GetValue(AvailableScenesProperty) ?? [];
			set => SetValue(AvailableScenesProperty, value);
		}
		private void AvailableScenes_Changed(IReadOnlyList<SceneUI> nue, IReadOnlyList<SceneUI> old)
		{
			if (old is INotifyCollectionChanged old_ncc)
				old_ncc.CollectionChanged -= HandleCollectionChanged;
			if (nue is INotifyCollectionChanged nue_ncc)
				nue_ncc.CollectionChanged += HandleCollectionChanged;

			HandleCollectionChanged(null, default!);
			NotifyPropertyChanged(nameof(AvailableScenes));

			void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
			{
				if (AvailableScenes is not null)
				{
					var selectable_scenes = (List<SelectableScene>)SelectableScenesView.SourceCollection;
					selectable_scenes.Assign(AvailableScenes.Select(x => new SelectableScene(x, this)));
					SelectableScenesView.Refresh();
				}
				NotifyPropertyChanged(nameof(SelectedScenesDescription));
			}
		}
		public static readonly DependencyProperty AvailableScenesProperty = Gui_.DPRegister<SceneSelectorUI>(nameof(AvailableScenes), null, Gui_.EDPFlags.None);
		
		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

		/// <summary></summary>
		private class SelectableScene(SceneUI scene, SceneSelectorUI me) : INotifyPropertyChanged
		{
			private readonly SceneUI m_scene = scene;
			private readonly SceneSelectorUI m_me = me;

			/// <summary></summary>
			public string Name => m_scene.SceneName;

			/// <summary></summary>
			public bool Selected
			{
				get => m_me.SelectedScenes.Contains(m_scene);
				set
				{
					if (value)
						m_me.SelectedScenes.AddIfUnique(m_scene);
					else
						m_me.SelectedScenes.Remove(m_scene);
					
					NotifyPropertyChanged(nameof(Selected));
					m_me.NotifyPropertyChanged(nameof(m_me.SelectedScenesDescription));
				}
			}

			/// <inheritdoc/>
			public event PropertyChangedEventHandler? PropertyChanged;
			private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gfx
{
	public sealed class ObjectManager : IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - This class provides the functionality for the object manager
		//  - UI frameworks need to provide binding wrappers

		public ObjectManager(View3d.Window window, IEnumerable<Guid> excluded)
		{
			m_window = null!;
			Window = window;
			Exclude = new HashSet<Guid>(excluded);
			Objects = new List<View3d.Object>();

			SyncObjectsWithScene();
		}
		public void Dispose()
		{
			Window = null!;
		}

		/// <summary>The scene that this manager is associated with</summary>
		public View3d.Window Window
		{
			get => m_window;
			private set
			{
				if (m_window == value) return;
				if (m_window != null)
				{
					m_window.OnSceneChanged -= HandleSceneChanged;
				}
				m_window = value;
				if (m_window != null)
				{
					m_window.OnSceneChanged += HandleSceneChanged;
				}

				// Handlers
				void HandleSceneChanged(object? sender, View3d.SceneChangedEventArgs args)
				{
					// Ignore if only excluded context ids have changed
					if (args.ContextIds.All(x => Exclude.Contains(x)))
						return;

					SyncObjectsWithScene();
				}
			}
		}
		private View3d.Window m_window;

		/// <summary>Context Ids of objects not to show in the object manager</summary>
		public HashSet<Guid> Exclude { get; }

		/// <summary>The collection of objects in this scene (top level tree nodes)</summary>
		public List<View3d.Object> Objects { get; }

		/// <summary>Update the 'Objects' collection to match the objects in the scene</summary>
		private void SyncObjectsWithScene()
		{
			// Read the objects from the scene
			var objects = new HashSet<View3d.Object>();
			Window.EnumObjects(obj => objects.Add(obj), Exclude.ToArray(), 0, Exclude.Count);
			Objects.Sync(objects);
			NotifyPropertyChanged(nameof(Objects));
		}

		/// <summary>True while the tree and grid are out of sync</summary>
		private bool SrcUpdating
		{
			get => m_src_updating;
			set
			{
				if (m_src_updating == value) return;
				m_src_updating = value;
				//Debug.Assert(m_src_updating || m_tree.RowCount == m_grid.RowCount);
			}
		}
		private bool m_src_updating;

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

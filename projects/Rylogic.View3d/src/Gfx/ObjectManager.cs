using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;

namespace Rylogic.Gfx
{
	public class ObjectManager : IDisposable
	{
		// Notes:
		//  - This class provides the functionality for the object manager
		//  - UI frameworks need to provide binding wrappers

		public ObjectManager(View3d.Window window, IEnumerable<Guid> excluded)
		{
			Window = window;
			Exclude = new HashSet<Guid>(excluded);
			Objects = new BindingListEx<View3d.Object>();

			SyncObjectsWithScene();
		}
		public void Dispose()
		{
			Window = null;
		}

		/// <summary>The scene that this manager is associated with</summary>
		public View3d.Window Window
		{
			get { return m_window; }
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
				void HandleSceneChanged(object sender, View3d.SceneChangedEventArgs args)
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
		public BindingListEx<View3d.Object> Objects { get; }

		/// <summary>Update the 'Objects' collection to match the objects in the scene</summary>
		private void SyncObjectsWithScene()
		{
			// Flag the source data as possibly out of sync between the tree and grid
			SrcUpdating = true;

			// Suspend tree and grid updates until the changes have been made to the 'Objects' collection
			//using (m_tree.SuspendRedraw(true))
			//using (m_grid.SuspendRedraw(true))
			using (Objects.SuspendEvents(reset_bindings_on_resume: true))
			{
				//m_tree.CurrentCell = null;
				//m_grid.CurrentCell = null;

				// Read the objects from the scene
				var objects = new HashSet<View3d.Object>();
				Window.EnumObjects(obj => objects.Add(obj), Exclude.ToArray(), 0, Exclude.Count);

				// Remove objects that are no longer in the scene
				Objects.RemoveIf(x => !objects.Contains(x));

				// Remove objects from 'objects' that are still in the scene
				Objects.ForEach(x => objects.Remove(x));

				// Add whatever is left
				objects.ForEach(x => Objects.Add(x));

				// Sort the objects alphabetically
				Objects.Sort(Cmp<View3d.Object>.From((l, r) => l.Name.CompareTo(r.Name)));
			}
			//Debug.Assert(m_tree.RowCount == Objects.Count);

			// After the objects collection has been changed, the tree should
			// update due to it's data binding. We need to update the grid as well.
			//UpdateRowCount();
		}

		/// <summary>True while the tree and grid are out of sync</summary>
		private bool SrcUpdating
		{
			get { return m_src_updating; }
			set
			{
				if (m_src_updating == value) return;
				m_src_updating = value;
				//Debug.Assert(m_src_updating || m_tree.RowCount == m_grid.RowCount);
			}
		}
		private bool m_src_updating;
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rylogic.Extn;

namespace Rylogic.Gfx
{
	public sealed partial class View3d
	{
		public sealed class ObjectManager :IDisposable, INotifyPropertyChanged
		{
			// Notes:
			//  - This class is the View-Model for the object manager.
			//  - UI frameworks need to provide binding wrappers
			public ObjectManager(Window window, IEnumerable<Guid> excluded)
			{
				Window = window;
				Exclude = [.. Enumerable.Concat(ExcludeCtxIds, excluded)];
				Objects = [];

				SyncObjectsWithScene();
			}
			public void Dispose()
			{
				Window = null!;
			}

			/// <summary>Context IDs of objects to always exclude</summary>
			public static HashSet<Guid> ExcludeCtxIds { get; } = [];

			/// <summary>The scene that this manager is associated with</summary>
			public Window Window
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
			private Window m_window = null!;

			/// <summary>Context Ids of objects not to show in the object manager</summary>
			public HashSet<Guid> Exclude { get; }

			/// <summary>The collection of objects in this scene (top level tree nodes)</summary>
			public List<Object> Objects { get; }

			/// <summary>Update the 'Objects' collection to match the objects in the scene</summary>
			private void SyncObjectsWithScene()
			{
				// Read the objects from the scene
				var objects = new HashSet<Object>();
				Window.EnumObjects(objects.Add, x => !Exclude.Contains(x));
				Objects.Sync(objects);
				NotifyPropertyChanged(nameof(Objects));
			}

			/// <inheritdoc/>
			public event PropertyChangedEventHandler? PropertyChanged;
			private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

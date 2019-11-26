using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dObjectManagerUI : Window, INotifyPropertyChanged
	{
		public View3dObjectManagerUI(Window owner, View3d.Window window, IEnumerable<Guid>? exclude = null)
		{
			InitializeComponent();
			Owner = owner;
			Icon = Owner?.Icon;

			PinState = new PinData(this, EPin.Centre);
			ObjectManager = new ObjectManager(window, exclude ?? Array.Empty<Guid>());
			Objects = new ListCollectionView(ObjectManager.Objects);

			ExpandAll = Command.Create(this, ExpandAllInternal);
			CollapseAll = Command.Create(this, CollapseAllInternal);
			ApplyFilter = Command.Create(this, ApplyFilterInternal);
			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			ObjectManager = null!;
			PinState = null;
			base.OnClosed(e);
		}

		/// <summary>The view model for the object manager behaviour</summary>
		public ObjectManager ObjectManager
		{
			get => m_object_manager;
			private set
			{
				if (m_object_manager == value) return;
				if (m_object_manager != null)
				{
					m_object_manager.PropertyChanged -= HandlePropertyChanged;
					Util.Dispose(ref m_object_manager!);
				}
				m_object_manager = value;
				if (m_object_manager != null)
				{
					m_object_manager.PropertyChanged += HandlePropertyChanged;
				}

				// Handler
				void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
					case nameof(Gfx.ObjectManager.Objects):
						{
							Objects.Refresh();
							NotifyPropertyChanged(nameof(Objects));
							break;
						}
					}
				}
			}
		}
		private ObjectManager m_object_manager = null!;

		/// <summary>Pinned window support</summary>
		private PinData? PinState
		{
			get => m_pin_state;
			set
			{
				if (m_pin_state == value) return;
				Util.Dispose(ref m_pin_state);
				m_pin_state = value;
			}
		}
		private PinData? m_pin_state;

		/// <summary>A view of the top-level objects in the scene</summary>
		public ICollectionView Objects { get; }

		/// <summary></summary>
		public Command ExpandAll { get; }
		private void ExpandAllInternal()
		{
		}

		/// <summary></summary>
		public Command CollapseAll { get; }
		private void CollapseAllInternal()
		{
		}

		/// <summary></summary>
		public Command ApplyFilter { get; }
		private void ApplyFilterInternal()
		{
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

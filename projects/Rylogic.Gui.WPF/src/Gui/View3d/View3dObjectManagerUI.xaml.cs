using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class View3dObjectManagerUI : Window
	{
		public View3dObjectManagerUI(View3dControl owner)
		{
			InitializeComponent();
			Owner = GetWindow(owner);

			View3dCtrl = owner;
			ObjectManager = new ObjectManager(owner.Window, new Guid[0] { });
			PinState = new PinData(this, EPin.Centre);

			Objects = new ListCollectionView(ObjectManager.Objects);
			DataContext = this;
		}
		protected override void OnClosed(EventArgs e)
		{
			ObjectManager = null;
			View3dCtrl = null;
			PinState = null;
			base.OnClosed(e);
		}

		/// <summary>The View3d Control that contains the 3d scene</summary>
		public View3dControl View3dCtrl
		{
			get { return m_view3d_ctrl; }
			private set
			{
				if (m_view3d_ctrl == value) return;
				if (m_view3d_ctrl != null)
				{
				}
				m_view3d_ctrl = value;
				if (m_view3d_ctrl != null)
				{
				}
			}
		}
		private View3dControl m_view3d_ctrl;

		/// <summary>The view model for the object manager behaviour</summary>
		public ObjectManager ObjectManager
		{
			get { return m_object_manager; }
			private set
			{
				if (m_object_manager == value) return;
				if (m_object_manager != null)
				{
					Util.Dispose(ref m_object_manager);
				}
				m_object_manager = value;
				if (m_object_manager != null)
				{

				}
			}
		}
		private ObjectManager m_object_manager;

		/// <summary>Pinned window support</summary>
		private PinData PinState
		{
			get { return m_pin_state; }
			set
			{
				if (m_pin_state == value) return;
				Util.Dispose(ref m_pin_state);
				m_pin_state = value;
			}
		}
		private PinData m_pin_state;

		/// <summary>A view of the top-level objects in the scene</summary>
		public ICollectionView Objects { get; }

		public Command ExpandAll { get; }

		public Command CollapseAll { get; }

	}
}

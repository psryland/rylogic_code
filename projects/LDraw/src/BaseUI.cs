using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace LDraw
{
	/// <summary>Base class for UI components</summary>
	public class BaseUI :UserControl ,IDockable
	{
		private BaseUI() {}
		public BaseUI(Model model, string name)
		{
			DockControl = new DockControl(this, name) { TabText = name };
			Model = model;
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			DockControl = null;
			base.Dispose(disposing);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				if (m_impl_dock_control != null)
				{
					m_impl_dock_control.DockContainerChanged -= HandleDockContainerChanged;
					Util.Dispose(ref m_impl_dock_control);
				}
				m_impl_dock_control = value;
				if (m_impl_dock_control != null)
				{
					m_impl_dock_control.DockContainerChanged += HandleDockContainerChanged;
				}
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Model.Settings; }
		}

		/// <summary>The app logic</summary>
		protected Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
		//			m_model.Window.OnRendering -= HandleSceneRendering;
				}
				SetModelCore(value);
				if (m_model != null)
				{
		//			m_model.Window.OnRendering += HandleSceneRendering;
				}
			}
		}
		private Model m_model;

		/// <summary>Set/Change the Model property</summary>
		protected virtual void SetModelCore(Model model)
		{
			m_model = model;
		}

		///// <summary>Add instances to the scene just prior to rendering</summary>
		//protected virtual void OnSceneRendering()
		//{ }
		//private void HandleSceneRendering(object sender, EventArgs e)
		//{
		//	OnSceneRendering();
		//}

		/// <summary>Invalidate this control and all children</summary>
		protected void Invalidate(object sender, EventArgs e)
		{
			Invalidate(true);
		}

		/// <summary>Raised when the dock container is assigned/changed</summary>
		protected virtual void OnDockContainerChanged(DockContainerChangedEventArgs args)
		{}
		private void HandleDockContainerChanged(object sender, DockContainerChangedEventArgs e)
		{
			OnDockContainerChanged(e);
		}

	}
}

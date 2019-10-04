using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

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
			[DebuggerStepThrough] get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				if (m_dock_control != null)
				{
					m_dock_control.DockContainerChanged -= HandleDockContainerChanged;
					m_dock_control.SavingLayout -= HandleSavingLayout;
					m_dock_control.Closed -= HandleClosed;
					Util.Dispose(ref m_dock_control);
				}
				m_dock_control = value;
				if (m_dock_control != null)
				{
					m_dock_control.Closed += HandleClosed;
					m_dock_control.SavingLayout += HandleSavingLayout;
					m_dock_control.DockContainerChanged += HandleDockContainerChanged;
				}

				// Handlers
				void HandleClosed(object sender, EventArgs e)
				{
					OnClosed();
				}
				void HandleSavingLayout(object sender, DockContainerSavingLayoutEventArgs args)
				{
					OnSavingLayout(args);
				}
				void HandleDockContainerChanged(object sender, DockContainerChangedEventArgs args)
				{
					OnDockContainerChanged(args);
				}
			}
		}
		private DockControl m_dock_control;

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
				}
				SetModelCore(value);
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model;

		/// <summary>Set/Change the Model property</summary>
		protected virtual void SetModelCore(Model model)
		{
			m_model = model;
		}

		/// <summary>Invalidate this control and all children</summary>
		protected void Invalidate(object sender, EventArgs e)
		{
			Invalidate(true);
		}

		/// <summary>Called when the dock container is assigned/changed</summary>
		protected virtual void OnDockContainerChanged(DockContainerChangedEventArgs args)
		{}

		/// <summary>Called when layout is saving for this UI</summary>
		protected virtual void OnSavingLayout(DockContainerSavingLayoutEventArgs args)
		{}

		/// <summary>Called when the tab for this UI is closed by the user</summary>
		protected virtual void OnClosed()
		{}
	}
}

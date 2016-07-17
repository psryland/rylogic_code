using System.ComponentModel;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace Tradee
{
	/// <summary>Base class for UI components</summary>
	public class BaseUI :UserControl ,IDockable
	{
		private BaseUI() {}
		public BaseUI(MainModel model, string name)
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
		[Browsable(false)] public DockControl DockControl
		{
			get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				if (m_impl_dock_control != null) Util.Dispose(ref m_impl_dock_control);
				m_impl_dock_control = value;
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>The parent UI</summary>
		protected MainModel Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				SetMainCore(value);
				if (m_model != null)
				{
				}
			}
		}
		protected void SetMainCore(MainModel model)
		{
			m_model = model;
		}
		private MainModel m_model;
	}
}

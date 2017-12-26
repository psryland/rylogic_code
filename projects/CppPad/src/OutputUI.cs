using System.ComponentModel;
using System.Windows.Forms;
using Rylogic.Utility;
using Rylogic.Gui;

namespace CppPad
{
	public class OutputUI :UserControl ,IDockable
	{
		public OutputUI(string tab_text)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "output") { TabText = tab_text };
			Edit = new ScintillaCtrl();
		}
		protected override void Dispose(bool disposing)
		{
			Edit = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)] public DockControl DockControl { get; private set; }

		/// <summary>The Scintilla control</summary>
		[Browsable(false)] public ScintillaCtrl Edit
		{
			get { return m_impl_edit; }
			set
			{
				if (m_impl_edit == value) return;
				if (m_impl_edit != null)
				{
					Controls.Remove(m_impl_edit);
					Util.Dispose(ref m_impl_edit);
				}
				m_impl_edit = value;
				if (m_impl_edit != null)
				{
					m_impl_edit.Dock = DockStyle.Fill;
					Controls.Add(m_impl_edit);
				}
			}
		}
		private ScintillaCtrl m_impl_edit;

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// OutputUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "OutputUI";
			this.Size = new System.Drawing.Size(488, 582);
			this.ResumeLayout(false);

		}
		#endregion
	}
}

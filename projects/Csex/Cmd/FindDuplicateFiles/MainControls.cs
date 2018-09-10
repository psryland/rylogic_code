using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

namespace Csex
{
	public class MainControls :UserControl ,IDockable
	{
		private readonly Model m_model;
		public MainControls(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "MainControls");
			m_model = model;
			
			m_btn_find_duplicates.Click += (s,a) =>
				{
					m_model.FindDuplicates();
				};
		}
		protected override void Dispose(bool disposing)
		{
			DockControl.Dispose();
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		public DockControl DockControl { get; private set; }
		private Button m_btn_find_duplicates;

		#region Component Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_btn_find_duplicates = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_btn_find_duplicates
			// 
			this.m_btn_find_duplicates.Location = new System.Drawing.Point(3, 3);
			this.m_btn_find_duplicates.Name = "m_btn_find_duplicates";
			this.m_btn_find_duplicates.Size = new System.Drawing.Size(75, 23);
			this.m_btn_find_duplicates.TabIndex = 0;
			this.m_btn_find_duplicates.Text = "Find Duplicates";
			this.m_btn_find_duplicates.UseVisualStyleBackColor = true;
			// 
			// MainControls
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btn_find_duplicates);
			this.Name = "MainControls";
			this.Size = new System.Drawing.Size(159, 232);
			this.ResumeLayout(false);

		}

		#endregion
	}
}

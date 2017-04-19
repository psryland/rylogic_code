using System.Drawing;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	public class JumpToUi :ToolForm
	{
		private NumericUpDown m_edit_address;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Label m_lbl_address;

		public JumpToUi(Form owner, long min, long max)
			:base(owner, EPin.Centre, Point.Empty, Size.Empty, false)
		{
			InitializeComponent();

			m_edit_address.Focus();
			m_edit_address.Minimum = min;
			m_edit_address.Maximum = max;
			m_edit_address.Select(0, m_edit_address.Text.Length);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The address field in the dialog</summary>
		public long Address
		{
			get { return (long)m_edit_address.Value; }
			set { m_edit_address.Value = value; }
		}

		#region Windows Form Designer generated code

		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_edit_address = new System.Windows.Forms.NumericUpDown();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_address = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_edit_address)).BeginInit();
			this.SuspendLayout();
			//
			// m_edit_address
			//
			this.m_edit_address.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_address.Location = new System.Drawing.Point(12, 21);
			this.m_edit_address.Name = "m_edit_address";
			this.m_edit_address.Size = new System.Drawing.Size(150, 20);
			this.m_edit_address.TabIndex = 0;
			this.m_edit_address.ThousandsSeparator = true;
			//
			// m_btn_ok
			//
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(12, 47);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "&Go To...";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			//
			// m_btn_cancel
			//
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(87, 47);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 2;
			this.m_btn_cancel.Text = "&Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			//
			// m_lbl_address
			//
			this.m_lbl_address.AutoSize = true;
			this.m_lbl_address.Location = new System.Drawing.Point(10, 5);
			this.m_lbl_address.Name = "m_lbl_address";
			this.m_lbl_address.Size = new System.Drawing.Size(97, 13);
			this.m_lbl_address.TabIndex = 3;
			this.m_lbl_address.Text = "Address to jump to:";
			//
			// JumpToUi
			//
			this.AcceptButton = this.m_btn_ok;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(174, 77);
			this.Controls.Add(this.m_lbl_address);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_edit_address);
			this.Name = "JumpToUi";
			this.Text = "Jump To...";
			((System.ComponentModel.ISupportInitialize)(this.m_edit_address)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
}

		#endregion
	}
}

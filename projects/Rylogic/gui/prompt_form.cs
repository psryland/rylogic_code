using System;
using System.IO;
using System.Windows.Forms;
using pr.extn;

namespace pr.gui
{
	/// <summary>A helper dialog for prompting for a single line of user input</summary>
	public class PromptForm :Form
	{
		#region UI Elements
		private TextBox m_edit;
		private Label m_lbl_info;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private ToolTip m_tt;
		#endregion

		public PromptForm()
		{
			InitializeComponent();

			InputType = EInputType.Anything;

			m_edit.KeyPress += (s,a)=>
			{
				bool valid;
				switch (InputType)
				{
				default: throw new ArgumentException("Unknown input type for prompt dialog");
				case EInputType.Anything:
					valid = true;
					break;
				case EInputType.Identifer:
					valid = a.KeyChar == '_' || char.IsLetter(a.KeyChar) || (Value.Length != 0 && char.IsDigit(a.KeyChar));
					break;
				case EInputType.Number:
					valid = char.IsDigit(a.KeyChar);
					break;
				case EInputType.Filename:
					valid = Path.GetInvalidFileNameChars().IndexOf(a.KeyChar) == -1;
					break;
				}
				a.Handled = !valid;
				if (!valid) m_tt.Show("Invalid character. Expecting "+InputType.ToString()+" characters", this);
			};
		}

		/// <summary>Limit user input to specific categories</summary>
		public EInputType InputType { get; set; }
		public enum EInputType
		{
			Anything,
			Identifer,
			Number,
			Filename,
		}

		/// <summary>The prompt dialog title</summary>
		public string Title
		{
			get { return Text; }
			set { Text = value; }
		}

		/// <summary>The text to display above the prompt edit box</summary>
		public string PromptText
		{
			get { return m_lbl_info.Text; }
			set { m_lbl_info.Text = value; }
		}

		/// <summary>The value entered in the edit box</summary>
		public string Value
		{
			get { return m_edit.Text; }
			set { m_edit.Text = value; }
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_edit = new System.Windows.Forms.TextBox();
			this.m_lbl_info = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// m_edit
			// 
			this.m_edit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit.Location = new System.Drawing.Point(12, 26);
			this.m_edit.Name = "m_edit";
			this.m_edit.Size = new System.Drawing.Size(271, 20);
			this.m_edit.TabIndex = 0;
			// 
			// m_lbl_info
			// 
			this.m_lbl_info.AutoSize = true;
			this.m_lbl_info.Location = new System.Drawing.Point(12, 10);
			this.m_lbl_info.Name = "m_lbl_info";
			this.m_lbl_info.Size = new System.Drawing.Size(118, 13);
			this.m_lbl_info.TabIndex = 1;
			this.m_lbl_info.Text = "Please choose a name:";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(127, 52);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 2;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(208, 52);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// toolTip1
			// 
			this.m_tt.ShowAlways = true;
			// 
			// PromptForm
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(295, 82);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_info);
			this.Controls.Add(this.m_edit);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "PromptForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Choose a Name";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}

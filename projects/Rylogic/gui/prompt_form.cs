using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.util;

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
			ValidateValue = null;

			m_edit.TextChanged += HandleValueChanged;
			m_edit.KeyPress += HandleKeyPress;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			// Set the initial size
			using (this.SuspendLayout(true))
			using (var gfx = CreateGraphics())
			{
				// Get the scaling due to DPI
				var cr = ClientRectangle;
				var scale_x = gfx.DpiX / 96f;
				var scale_y = gfx.DpiY / 96f;
				var x_10px = (int)(10 * scale_x);
				var y_10px = (int)(10 * scale_y);

				// Minimum client size
				var sz_client = RectangleToClient(RectangleToScreen(new Rectangle(Point.Empty, MinimumSize)));

				// Minimum size needed for buttons
				var btns = Controls.OfType<Button>().ToArray();
				var w_btns = btns.Sum(x => x.Width) + (btns.Length-1)*x_10px;

				// Minimum size needed for the text label
				var sz_info = m_lbl_info.PreferredSize;

				var w = Maths.Max(sz_client.Width, 2*x_10px + w_btns, 2*x_10px + sz_info.Width) ;
				var h = Maths.Max(sz_client.Height, y_10px + sz_info.Height + y_10px + m_edit.Height + y_10px + btns[0].Height + y_10px);
				ClientSize = new Size(w,h);
			}
			base.OnShown(e);
		}
		protected override void OnLayout(LayoutEventArgs levent)
		{
			using (this.SuspendLayout(false))
			using (var gfx = CreateGraphics())
			{
				// Get the scaling due to DPI
				var cr = ClientRectangle;
				var scale_x = gfx.DpiX / 96f;
				var scale_y = gfx.DpiY / 96f;
				var x_10px = (int)(10 * scale_x);
				var y_10px = (int)(10 * scale_y);

				// Info string
				m_lbl_info.Location = new Point(x_10px, y_10px);
				m_lbl_info.Size     = m_lbl_info.GetPreferredSize(new Size(cr.Width - 2*x_10px, 0));

				// Prompt field
				m_edit.Location = new Point(m_lbl_info.Left, m_lbl_info.Bottom + y_10px);
				m_edit.Size     = new Size(cr.Width - 2*x_10px, m_edit.Height);

				// Buttons
				m_btn_cancel.Location = new Point(cr.Right - x_10px - m_btn_cancel.Width, cr.Bottom - y_10px - m_btn_cancel.Height);
				m_btn_cancel.Size     = m_btn_cancel.PreferredSize;
				m_btn_ok.Location     = new Point(m_btn_cancel.Left - x_10px - m_btn_ok.Width, m_btn_cancel.Top);
				m_btn_ok.Size         = m_btn_ok.PreferredSize;

				// Any other buttons
				var x = cr.Left;
				var btns = Controls.OfType<Button>().Except(m_btn_cancel, m_btn_ok).ToArray();
				foreach (var btn in btns)
				{
					btn.Location  = new Point(x += x_10px, m_btn_cancel.Top);
					btn.Size      = btn.PreferredSize;
					x += btn.Width;
				}
			}
			base.OnLayout(levent);
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

		/// <summary>Access to the edit control used to set the value</summary>
		public TextBox ValueCtrl
		{
			get { return m_edit; }
		}

		/// <summary>A predicate for validating the input prompt</summary>
		public Func<string,bool> ValidateValue
		{
			get { return m_validate; }
			set
			{
				if (m_validate == value) return;
				m_validate = value;
				UpdateValueHintState();
			}
		}
		private Func<string,bool> m_validate;

		/// <summary>Update the hint state of the value text box</summary>
		public void UpdateValueHintState()
		{
			if (ValidateValue != null)
			{
				var valid = ValidateValue(m_edit.Text);
				m_edit.HintState(valid);
				m_btn_ok.Enabled = valid;
			}
			else
			{
				m_edit.HintState(TextBoxExtensions.EHintState.Uninitialised);
				m_btn_ok.Enabled = true;
			}
		}

		/// <summary>Validate key presses based on the input type</summary>
		private void HandleKeyPress(object sender, KeyPressEventArgs a)
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
			if (!valid)
				m_tt.Show("Invalid character. Expecting "+InputType.ToString()+" characters", this);
		}

		/// <summary>Validate the input value based on the 'ValidateValue' predicate</summary>
		private void HandleValueChanged(object sender, EventArgs e)
		{
			UpdateValueHintState();
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
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
			this.m_edit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
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
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
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
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(208, 52);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_tt
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
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "PromptForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Choose a Name";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}

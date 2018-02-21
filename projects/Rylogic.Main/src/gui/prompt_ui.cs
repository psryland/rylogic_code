using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui
{
	/// <summary>A helper dialog for prompting for a single line of user input</summary>
	public class PromptUI :Form
	{
		#region UI Elements
		private Label m_lbl_info;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private ComboBox m_cb_value;
		private ValueBox m_tb_value;
		private ToolTip m_tt;
		#endregion

		public PromptUI()
		{
			InitializeComponent();
			Mode = EMode.ValueInput;
			InputType = EInputType.Anything;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			// Get the input control
			var ctrl =
				Mode == EMode.ValueInput ? m_tb_value :
				Mode == EMode.OptionSelect ? m_cb_value :
				(Control)null;

			if (ctrl != null)
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

					var w = Math_.Max(sz_client.Width, 2*x_10px + w_btns, 2*x_10px + sz_info.Width) ;
					var h = Math_.Max(sz_client.Height, y_10px + sz_info.Height + y_10px + ctrl.Height + y_10px + btns[0].Height + y_10px);
					ClientSize = new Size(w,h);
				}

				ctrl.Focus();
			}
			base.OnShown(e);
		}
		protected override void OnLayout(LayoutEventArgs levent)
		{
			// Get the input control
			var ctrl =
				Mode == EMode.ValueInput ? m_tb_value :
				Mode == EMode.OptionSelect ? m_cb_value :
				(Control)null;

			if (ctrl != null)
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

					// Input control
					ctrl.Location = new Point(m_lbl_info.Left, m_lbl_info.Bottom + y_10px);
					ctrl.Size     = new Size(cr.Width - 2*x_10px, ctrl.Height);

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
			}
			base.OnLayout(levent);
		}

		/// <summary>The mode for the user interface</summary>
		public EMode Mode
		{
			get { return m_mode; }
			set
			{
				if (m_mode == value) return;
				m_mode = value;
				m_tb_value.Visible = m_mode == EMode.ValueInput;
				m_cb_value.Visible = m_mode == EMode.OptionSelect;
				Invalidate(invalidateChildren:true);
			}
		}
		private EMode m_mode;

		/// <summary>Limit user input to specific categories</summary>
		public EInputType InputType
		{
			get { return m_input_type; }
			set
			{
				if (m_input_type == value) return;
				m_input_type = value;
				m_tb_value.Invalidate();
			}
		}
		private EInputType m_input_type;

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

		/// <summary>The value entered in the value box or selected from the combo box</summary>
		public object Value
		{
			get
			{
				switch (Mode)
				{
				default: throw new Exception($"No value available in mode {Mode}");
				case EMode.ValueInput: return m_tb_value.Value;
				case EMode.OptionSelect: return m_cb_value.Value;
				}
			}
			set
			{
				switch (Mode)
				{
				default: throw new Exception($"Can not set a value in mode {Mode}");
				case EMode.ValueInput:
					m_tb_value.Value = value;
					m_tb_value.SelectAll();
					break;
				case EMode.OptionSelect:
					m_cb_value.Value = value;
					m_cb_value.SelectAll();
					break;
				}
			}
		}

		/// <summary>The data source for the options combo box</summary>
		public object Options
		{
			get { return m_cb_value.DataSource; }
			set { m_cb_value.DataSource = value; }
		}

		/// <summary>The property name of the option object to display</summary>
		public string DisplayProperty
		{
			get { return m_cb_value.DisplayProperty; }
			set { m_cb_value.DisplayProperty = value; }
		}

		/// <summary>Set the value type of the value box</summary>
		public Type ValueType
		{
			get { return m_tb_value.ValueType; }
			set { m_tb_value.ValueType = value; }
		}

		/// <summary>Access to the edit control used to set the value</summary>
		public TextBox ValueCtrl
		{
			get { return m_tb_value; }
		}

		/// <summary>Access to the options combo box control</summary>
		public ComboBox OptionsCtrl
		{
			get { return m_cb_value; }
		}

		/// <summary>A predicate for validating the input text in the value box</summary>
		public Func<string,bool> ValidateValue
		{
			get { return m_validate_value; }
			set { m_validate_value = value; }
		}
		private Func<string, bool> m_validate_value;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Value box
			m_tb_value.UseValidityColours = true;
			m_tb_value.ValueType = typeof(string);
			m_tb_value.KeyPress += (s,a) =>
			{
				a.Handled = !ValidCharacter(a.KeyChar);
				if (a.Handled)
					m_tt.Show($"Invalid character. Expecting {InputType} characters", this);
			};
			m_tb_value.ValidateText = (text) =>
			{
				return text.All(ValidCharacter) && (ValidateValue?.Invoke(text) ?? true);
			};
			m_tb_value.TextChanged += (s,a) =>
			{
				m_btn_ok.Enabled = m_tb_value.Valid;
			};
			m_tb_value.Value = string.Empty;

			// Combo box
			m_cb_value.UseValidityColours = true;
			m_cb_value.ValidateText = (text) =>
			{
				return ValidateValue?.Invoke(text) ?? true;
			};
			m_cb_value.SelectedIndexChanged += (s,a) =>
			{
				m_btn_ok.Enabled = m_cb_value.Valid;
			};
		}

		/// <summary>Returns true if 'ch' is a valid character for the current input mode</summary>
		private bool ValidCharacter(char ch)
		{
			bool valid;
			switch (InputType)
			{
			default:
				throw new ArgumentException($"Unknown input type: {InputType}");
			case EInputType.Anything:
				valid = true;
				break;
			case EInputType.Identifer:
				valid = ch == '_' || char.IsLetter(ch) || (Value.ToString().Length != 0 && char.IsDigit(ch));
				break;
			case EInputType.Number:
				valid = char.IsDigit(ch);
				break;
			case EInputType.Filename:
				valid = Path.GetInvalidFileNameChars().IndexOf(ch) == -1;
				break;
			}
			return valid;
		}

		/// <summary>The type of user interface to display</summary>
		public enum EMode
		{
			None,
			ValueInput,
			OptionSelect,
		}

		/// <summary>Modes for the value box input</summary>
		public enum EInputType
		{
			Anything,
			Identifer,
			Number,
			Filename,
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_lbl_info = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_cb_value = new Rylogic.Gui.ComboBox();
			this.m_tb_value = new Rylogic.Gui.ValueBox();
			this.SuspendLayout();
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
			// m_cb_options
			// 
			this.m_cb_value.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_value.BackColor = System.Drawing.Color.White;
			this.m_cb_value.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_value.BackColorValid = System.Drawing.Color.White;
			this.m_cb_value.CommitValueOnFocusLost = true;
			this.m_cb_value.DisplayProperty = null;
			this.m_cb_value.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_value.ForeColor = System.Drawing.Color.Gray;
			this.m_cb_value.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_value.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_value.FormattingEnabled = true;
			this.m_cb_value.Location = new System.Drawing.Point(12, 26);
			this.m_cb_value.Name = "m_cb_options";
			this.m_cb_value.PreserveSelectionThruFocusChange = false;
			this.m_cb_value.Size = new System.Drawing.Size(271, 21);
			this.m_cb_value.TabIndex = 4;
			this.m_cb_value.UseValidityColours = true;
			this.m_cb_value.Value = null;
			// 
			// m_tb_value
			// 
			this.m_tb_value.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_value.BackColor = System.Drawing.Color.White;
			this.m_tb_value.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_value.BackColorValid = System.Drawing.Color.White;
			this.m_tb_value.CommitValueOnFocusLost = true;
			this.m_tb_value.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_value.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_value.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_value.Location = new System.Drawing.Point(12, 26);
			this.m_tb_value.Name = "m_tb_value";
			this.m_tb_value.Size = new System.Drawing.Size(271, 20);
			this.m_tb_value.TabIndex = 5;
			this.m_tb_value.UseValidityColours = true;
			this.m_tb_value.Value = null;
			// 
			// PromptForm
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(295, 82);
			this.Controls.Add(this.m_tb_value);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_info);
			this.Controls.Add(this.m_cb_value);
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

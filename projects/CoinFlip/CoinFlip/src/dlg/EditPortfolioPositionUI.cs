using System;
using System.Diagnostics;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;
using ComboBox = Rylogic.Gui.WinForms.ComboBox;
using Util = Rylogic.Utility.Util;

namespace CoinFlip
{
	public class EditPortfolioPositionUI : ToolForm
	{
		/// <summary>The portfolio position we were handed</summary>
		private PortfolioData m_original;

		/// <summary>Helper for managing a set of combo boxes for selecting exchanges, pairs, and timeframes</summary>
		private ExchPairTimeFrameCombos m_ept_combos;

		#region UI Elements
		private TableLayoutPanel m_table0;
		private Panel m_panel_pair;
		private ComboBox m_cb_pair;
		private Label m_lbl_pair;
		private Panel m_panel_notes;
		private Panel m_panel_exchange;
		private ComboBox m_cb_exchange;
		private Label m_lbl_exchange;
		private Panel m_panel_price;
		private Label m_lbl_price;
		private ValueBox m_tb_price_q2b;
		private Panel m_panel_btns;
		private Button m_btn_cancel;
		private Button m_btn_ok;
		private ImageList m_il_buttons;
		private ToolTip m_tt;
		private Panel m_panel_volume;
		private Label m_lbl_volume;
		private ValueBox m_tb_volume;
		private TextBox m_tb_notes;
		#endregion

		public EditPortfolioPositionUI(Model model, PortfolioData pos)
			: base(model.UI, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;
			Model = model;

			m_original = pos;
			Position = new PortfolioData(pos);

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The portfolio position being edited</summary>
		public PortfolioData Position
		{
			[DebuggerStepThrough]
			get { return m_position; }
			private set
			{
				if (m_position == value) return;
				m_position = value;
				if (m_position != null)
					SetPosition();
			}
		}
		private PortfolioData m_position;

		/// <summary>App Model</summary>
		public Model Model
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary></summary>
		public Exchange Exchange
		{
			[DebuggerStepThrough]
			get { return Position.Exchange; }
		}

		/// <summary></summary>
		public TradePair Pair
		{
			[DebuggerStepThrough]
			get { return Position.Pair; }
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			m_ept_combos = new ExchPairTimeFrameCombos(Model, m_cb_exchange, m_cb_pair, null);
		}

		/// <summary>Update the portfolio position</summary>
		private void SetPosition(Exchange exch = null, Unit<decimal>? price = null, DateTimeOffset? timestamp = null)
		{
			exch = exch ?? Position.Exchange;
			price = price ?? Position.Price;
			timestamp = timestamp ?? Position.Timestamp;

		//	Position.Timestamp = timestamp;
		//	Position.Pair = Model.FindPairOnExchange(;
		}

		/// <summary>Commit the changes to the trade or create a new trade</summary>
		private void ApplyChanges()
		{
			m_btn_ok.Enabled = false;

		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditPortfolioPositionUI));
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel_volume = new System.Windows.Forms.Panel();
			this.m_lbl_volume = new System.Windows.Forms.Label();
			this.m_tb_volume = new Rylogic.Gui.WinForms.ValueBox();
			this.m_panel_pair = new System.Windows.Forms.Panel();
			this.m_cb_pair = new Rylogic.Gui.WinForms.ComboBox();
			this.m_lbl_pair = new System.Windows.Forms.Label();
			this.m_panel_notes = new System.Windows.Forms.Panel();
			this.m_tb_notes = new System.Windows.Forms.TextBox();
			this.m_panel_exchange = new System.Windows.Forms.Panel();
			this.m_cb_exchange = new Rylogic.Gui.WinForms.ComboBox();
			this.m_lbl_exchange = new System.Windows.Forms.Label();
			this.m_panel_price = new System.Windows.Forms.Panel();
			this.m_lbl_price = new System.Windows.Forms.Label();
			this.m_tb_price_q2b = new Rylogic.Gui.WinForms.ValueBox();
			this.m_panel_btns = new System.Windows.Forms.Panel();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_il_buttons = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_table0.SuspendLayout();
			this.m_panel_volume.SuspendLayout();
			this.m_panel_pair.SuspendLayout();
			this.m_panel_notes.SuspendLayout();
			this.m_panel_exchange.SuspendLayout();
			this.m_panel_price.SuspendLayout();
			this.m_panel_btns.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 1;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_panel_volume, 0, 3);
			this.m_table0.Controls.Add(this.m_panel_pair, 1, 1);
			this.m_table0.Controls.Add(this.m_panel_notes, 1, 3);
			this.m_table0.Controls.Add(this.m_panel_exchange, 0, 0);
			this.m_table0.Controls.Add(this.m_panel_price, 0, 2);
			this.m_table0.Controls.Add(this.m_panel_btns, 0, 5);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 6;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table0.Size = new System.Drawing.Size(292, 355);
			this.m_table0.TabIndex = 17;
			// 
			// m_panel_volume
			// 
			this.m_panel_volume.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_volume.Controls.Add(this.m_lbl_volume);
			this.m_panel_volume.Controls.Add(this.m_tb_volume);
			this.m_panel_volume.Location = new System.Drawing.Point(0, 124);
			this.m_panel_volume.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_volume.Name = "m_panel_volume";
			this.m_panel_volume.Size = new System.Drawing.Size(292, 42);
			this.m_panel_volume.TabIndex = 4;
			// 
			// m_lbl_volume
			// 
			this.m_lbl_volume.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_volume.AutoSize = true;
			this.m_lbl_volume.Location = new System.Drawing.Point(19, 15);
			this.m_lbl_volume.Name = "m_lbl_volume";
			this.m_lbl_volume.Size = new System.Drawing.Size(45, 13);
			this.m_lbl_volume.TabIndex = 3;
			this.m_lbl_volume.Text = "Volume:";
			// 
			// m_tb_volume
			// 
			this.m_tb_volume.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_volume.BackColor = System.Drawing.Color.White;
			this.m_tb_volume.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_volume.BackColorValid = System.Drawing.Color.White;
			this.m_tb_volume.CommitValueOnFocusLost = true;
			this.m_tb_volume.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_volume.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_volume.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_volume.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_volume.Location = new System.Drawing.Point(70, 10);
			this.m_tb_volume.Name = "m_tb_volume";
			this.m_tb_volume.Size = new System.Drawing.Size(216, 22);
			this.m_tb_volume.TabIndex = 0;
			this.m_tb_volume.UseValidityColours = true;
			this.m_tb_volume.Value = null;
			// 
			// m_panel_pair
			// 
			this.m_panel_pair.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel_pair, 2);
			this.m_panel_pair.Controls.Add(this.m_cb_pair);
			this.m_panel_pair.Controls.Add(this.m_lbl_pair);
			this.m_panel_pair.Location = new System.Drawing.Point(0, 40);
			this.m_panel_pair.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_pair.Name = "m_panel_pair";
			this.m_panel_pair.Size = new System.Drawing.Size(292, 42);
			this.m_panel_pair.TabIndex = 7;
			// 
			// m_cb_pair
			// 
			this.m_cb_pair.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_pair.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_pair.BackColorValid = System.Drawing.Color.White;
			this.m_cb_pair.CommitValueOnFocusLost = true;
			this.m_cb_pair.DisplayProperty = null;
			this.m_cb_pair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_pair.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_cb_pair.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_pair.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_pair.FormattingEnabled = true;
			this.m_cb_pair.Location = new System.Drawing.Point(70, 10);
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.PreserveSelectionThruFocusChange = false;
			this.m_cb_pair.Size = new System.Drawing.Size(216, 24);
			this.m_cb_pair.TabIndex = 0;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
			// 
			// m_lbl_pair
			// 
			this.m_lbl_pair.AutoSize = true;
			this.m_lbl_pair.Location = new System.Drawing.Point(30, 15);
			this.m_lbl_pair.Name = "m_lbl_pair";
			this.m_lbl_pair.Size = new System.Drawing.Size(28, 13);
			this.m_lbl_pair.TabIndex = 9;
			this.m_lbl_pair.Text = "Pair:";
			// 
			// m_panel_notes
			// 
			this.m_panel_notes.Controls.Add(this.m_tb_notes);
			this.m_panel_notes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel_notes.Location = new System.Drawing.Point(0, 166);
			this.m_panel_notes.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_notes.Name = "m_panel_notes";
			this.m_panel_notes.Size = new System.Drawing.Size(292, 120);
			this.m_panel_notes.TabIndex = 4;
			// 
			// m_tb_notes
			// 
			this.m_tb_notes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tb_notes.Location = new System.Drawing.Point(0, 0);
			this.m_tb_notes.Multiline = true;
			this.m_tb_notes.Name = "m_tb_notes";
			this.m_tb_notes.Size = new System.Drawing.Size(292, 120);
			this.m_tb_notes.TabIndex = 10;
			// 
			// m_panel_exchange
			// 
			this.m_panel_exchange.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel_exchange, 2);
			this.m_panel_exchange.Controls.Add(this.m_cb_exchange);
			this.m_panel_exchange.Controls.Add(this.m_lbl_exchange);
			this.m_panel_exchange.Location = new System.Drawing.Point(0, 0);
			this.m_panel_exchange.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_exchange.Name = "m_panel_exchange";
			this.m_panel_exchange.Size = new System.Drawing.Size(292, 40);
			this.m_panel_exchange.TabIndex = 0;
			// 
			// m_cb_exchange
			// 
			this.m_cb_exchange.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_exchange.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_exchange.BackColorValid = System.Drawing.Color.White;
			this.m_cb_exchange.CommitValueOnFocusLost = true;
			this.m_cb_exchange.DisplayProperty = null;
			this.m_cb_exchange.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_exchange.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_cb_exchange.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_exchange.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_exchange.FormattingEnabled = true;
			this.m_cb_exchange.Location = new System.Drawing.Point(70, 8);
			this.m_cb_exchange.Name = "m_cb_exchange";
			this.m_cb_exchange.PreserveSelectionThruFocusChange = false;
			this.m_cb_exchange.Size = new System.Drawing.Size(216, 24);
			this.m_cb_exchange.TabIndex = 0;
			this.m_cb_exchange.UseValidityColours = true;
			this.m_cb_exchange.Value = null;
			// 
			// m_lbl_exchange
			// 
			this.m_lbl_exchange.AutoSize = true;
			this.m_lbl_exchange.Location = new System.Drawing.Point(6, 13);
			this.m_lbl_exchange.Name = "m_lbl_exchange";
			this.m_lbl_exchange.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_exchange.TabIndex = 9;
			this.m_lbl_exchange.Text = "Exchange:";
			// 
			// m_panel_price
			// 
			this.m_panel_price.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_price.Controls.Add(this.m_lbl_price);
			this.m_panel_price.Controls.Add(this.m_tb_price_q2b);
			this.m_panel_price.Location = new System.Drawing.Point(0, 82);
			this.m_panel_price.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_price.Name = "m_panel_price";
			this.m_panel_price.Size = new System.Drawing.Size(292, 42);
			this.m_panel_price.TabIndex = 1;
			// 
			// m_lbl_price
			// 
			this.m_lbl_price.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_price.AutoSize = true;
			this.m_lbl_price.Location = new System.Drawing.Point(30, 15);
			this.m_lbl_price.Name = "m_lbl_price";
			this.m_lbl_price.Size = new System.Drawing.Size(34, 13);
			this.m_lbl_price.TabIndex = 3;
			this.m_lbl_price.Text = "Price:";
			// 
			// m_tb_price_q2b
			// 
			this.m_tb_price_q2b.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_price_q2b.BackColor = System.Drawing.Color.White;
			this.m_tb_price_q2b.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_price_q2b.BackColorValid = System.Drawing.Color.White;
			this.m_tb_price_q2b.CommitValueOnFocusLost = true;
			this.m_tb_price_q2b.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_price_q2b.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_price_q2b.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_price_q2b.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_price_q2b.Location = new System.Drawing.Point(70, 10);
			this.m_tb_price_q2b.Name = "m_tb_price_q2b";
			this.m_tb_price_q2b.Size = new System.Drawing.Size(216, 22);
			this.m_tb_price_q2b.TabIndex = 0;
			this.m_tb_price_q2b.UseValidityColours = true;
			this.m_tb_price_q2b.Value = null;
			// 
			// m_panel_btns
			// 
			this.m_panel_btns.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_table0.SetColumnSpan(this.m_panel_btns, 2);
			this.m_panel_btns.Controls.Add(this.m_btn_cancel);
			this.m_panel_btns.Controls.Add(this.m_btn_ok);
			this.m_panel_btns.Location = new System.Drawing.Point(0, 286);
			this.m_panel_btns.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_btns.Name = "m_panel_btns";
			this.m_panel_btns.Size = new System.Drawing.Size(292, 69);
			this.m_panel_btns.TabIndex = 6;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_cancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.m_btn_cancel.ImageKey = "check_reject.png";
			this.m_btn_cancel.ImageList = this.m_il_buttons;
			this.m_btn_cancel.Location = new System.Drawing.Point(147, 10);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
			this.m_btn_cancel.Size = new System.Drawing.Size(136, 49);
			this.m_btn_cancel.TabIndex = 1;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_il_buttons
			// 
			this.m_il_buttons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il_buttons.ImageStream")));
			this.m_il_buttons.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il_buttons.Images.SetKeyName(0, "check_accept.png");
			this.m_il_buttons.Images.SetKeyName(1, "check_reject.png");
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_ok.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.m_btn_ok.ImageKey = "check_accept.png";
			this.m_btn_ok.ImageList = this.m_il_buttons;
			this.m_btn_ok.Location = new System.Drawing.Point(22, 10);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
			this.m_btn_ok.Size = new System.Drawing.Size(119, 49);
			this.m_btn_ok.TabIndex = 0;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// EditPortfolioPositionUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(292, 355);
			this.Controls.Add(this.m_table0);
			this.MinimumSize = new System.Drawing.Size(282, 305);
			this.Name = "EditPortfolioPositionUI";
			this.Text = "Edit Position";
			this.m_table0.ResumeLayout(false);
			this.m_panel_volume.ResumeLayout(false);
			this.m_panel_volume.PerformLayout();
			this.m_panel_pair.ResumeLayout(false);
			this.m_panel_pair.PerformLayout();
			this.m_panel_notes.ResumeLayout(false);
			this.m_panel_notes.PerformLayout();
			this.m_panel_exchange.ResumeLayout(false);
			this.m_panel_exchange.PerformLayout();
			this.m_panel_price.ResumeLayout(false);
			this.m_panel_price.PerformLayout();
			this.m_panel_btns.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}

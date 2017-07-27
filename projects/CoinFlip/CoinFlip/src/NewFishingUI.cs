using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;
using ComboBox = pr.gui.ComboBox;

namespace CoinFlip
{
	public class NewFishingUI :Form
	{
		#region UI Elements
		private Label m_lbl_exch0;
		private ComboBox m_cb_exch0;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Label m_lbl_exch1;
		private ComboBox m_cb_exch1;
		private Label m_lbl_buy;
		private ComboBox m_cb_pair;
		private Label m_lbl_scale;
		private TextBox m_tb_balances0;
		private TextBox m_tb_balances1;
		private TableLayoutPanel m_table1;
		private ValueBox m_tb_scale;
		#endregion

		public NewFishingUI(Model model)
		{
			InitializeComponent();
			Model = model;

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			if (DialogResult == DialogResult.OK)
			{
				Fishing = new Fishing(Model,
					Exch0.Pairs.Values.First(x => x.Name == Pair),
					Exch1.Pairs.Values.First(x => x.Name == Pair),
					TradeScale);
			}
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				m_model = value;
			}
		}
		private Model m_model;

		/// <summary>The Fishing instance</summary>
		public Fishing Fishing
		{
			get { return m_fishing; }
			set
			{
				m_fishing = value;
				m_cb_exch0.SelectedItem = m_fishing.Exch0;
			}
		}
		private Fishing m_fishing;

		/// <summary>The reference exchange</summary>
		public Exchange Exch0
		{
			get { return (Exchange)m_cb_exch0.SelectedItem; }
		}

		/// <summary>The target exchange</summary>
		public Exchange Exch1
		{
			get { return (Exchange)m_cb_exch1.SelectedItem; }
		}

		/// <summary>The pair to trade</summary>
		public string Pair
		{
			get { return (string)m_cb_pair.SelectedItem; }
		}

		/// <summary>The scaling factor</summary>
		public decimal TradeScale
		{
			get { return (decimal)m_tb_scale.Value; }
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Reference exchange
			m_cb_exch0.DataSource = Model.Exchanges.Except(Model.CrossExchange).ToList();
			m_cb_exch0.SelectedIndexChanged += UpdateAvailablePairs;
			m_cb_exch0.SelectedIndexChanged += UpdateUI;

			// Target exchange
			m_cb_exch1.DataSource = Model.Exchanges.Except(Model.CrossExchange).ToList();
			m_cb_exch1.SelectedIndexChanged += UpdateAvailablePairs;
			m_cb_exch1.SelectedIndexChanged += UpdateUI;

			// Available pairs
			m_cb_pair.DataSource = null;
			m_cb_pair.SelectedIndexChanged += UpdateUI;
			UpdateAvailablePairs();

			// Trade scale
			m_tb_scale.ValueType = typeof(decimal);
			m_tb_scale.ValidateText = t => { var v = decimal_.TryParse(t); return v != null && v.Within(0m,1m); };
			m_tb_scale.Value = 1m;
			m_tb_scale.TextChanged += UpdateUI;
		}

		/// <summary></summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Update the balances
			if (Pair != null)
			{
				var coins = Pair.Split('/');
				m_tb_balances0.Text = Str.Build(
					$"{Exch0.Name}\r\n",
					$"   {Exch0.Balance.Get(coins[0])?.Available ?? 0m} {coins[0]}\r\n",
					$"   {Exch0.Balance.Get(coins[1])?.Available ?? 0m} {coins[1]}");
				m_tb_balances1.Text = Str.Build(
					$"{Exch1.Name}\r\n",
					$"   {Exch1.Balance.Get(coins[0])?.Available ?? 0m} {coins[0]}\r\n",
					$"   {Exch1.Balance.Get(coins[1])?.Available ?? 0m} {coins[1]}");
			}
			else
			{
				m_tb_balances0.Text = string.Empty;
				m_tb_balances1.Text = string.Empty;
			}

			// Enable the OK button
			m_btn_ok.Enabled =
				Exch0 != Exch1 &&
				Pair != null &&
				m_tb_scale.Valid;
		}

		/// <summary>Update the data source for the list of trading pairs</summary>
		private void UpdateAvailablePairs(object sender = null, EventArgs e = null)
		{
			var pairs0 = Exch0.Pairs.Values.ToHashSet(x => x.Name);
			var pairs1 = Exch1.Pairs.Values.ToHashSet(x => x.Name);
			using (m_cb_pair.PreserveSelectedItem())
				m_cb_pair.DataSource = pairs0.Intersect(pairs1).ToList();
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewFishingUI));
			this.m_lbl_exch0 = new System.Windows.Forms.Label();
			this.m_cb_exch0 = new pr.gui.ComboBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_exch1 = new System.Windows.Forms.Label();
			this.m_cb_exch1 = new pr.gui.ComboBox();
			this.m_lbl_buy = new System.Windows.Forms.Label();
			this.m_cb_pair = new pr.gui.ComboBox();
			this.m_lbl_scale = new System.Windows.Forms.Label();
			this.m_tb_scale = new pr.gui.ValueBox();
			this.m_tb_balances0 = new System.Windows.Forms.TextBox();
			this.m_tb_balances1 = new System.Windows.Forms.TextBox();
			this.m_table1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_table1.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_lbl_exch0
			// 
			this.m_lbl_exch0.AutoSize = true;
			this.m_lbl_exch0.Location = new System.Drawing.Point(12, 6);
			this.m_lbl_exch0.Name = "m_lbl_exch0";
			this.m_lbl_exch0.Size = new System.Drawing.Size(111, 13);
			this.m_lbl_exch0.TabIndex = 0;
			this.m_lbl_exch0.Text = "Reference Exchange:";
			// 
			// m_cb_exch0
			// 
			this.m_cb_exch0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_exch0.BackColor = System.Drawing.Color.White;
			this.m_cb_exch0.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_exch0.BackColorValid = System.Drawing.Color.White;
			this.m_cb_exch0.CommitValueOnFocusLost = true;
			this.m_cb_exch0.DisplayProperty = null;
			this.m_cb_exch0.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_exch0.ForeColor = System.Drawing.Color.Black;
			this.m_cb_exch0.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_exch0.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_exch0.FormattingEnabled = true;
			this.m_cb_exch0.Location = new System.Drawing.Point(24, 22);
			this.m_cb_exch0.Name = "m_cb_exch0";
			this.m_cb_exch0.PreserveSelectionThruFocusChange = false;
			this.m_cb_exch0.Size = new System.Drawing.Size(212, 21);
			this.m_cb_exch0.TabIndex = 0;
			this.m_cb_exch0.UseValidityColours = true;
			this.m_cb_exch0.Value = null;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(80, 218);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 6;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(161, 218);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 7;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_exch1
			// 
			this.m_lbl_exch1.AutoSize = true;
			this.m_lbl_exch1.Location = new System.Drawing.Point(12, 46);
			this.m_lbl_exch1.Name = "m_lbl_exch1";
			this.m_lbl_exch1.Size = new System.Drawing.Size(92, 13);
			this.m_lbl_exch1.TabIndex = 4;
			this.m_lbl_exch1.Text = "Target Exchange:";
			// 
			// m_cb_exch1
			// 
			this.m_cb_exch1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_exch1.BackColor = System.Drawing.Color.White;
			this.m_cb_exch1.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_exch1.BackColorValid = System.Drawing.Color.White;
			this.m_cb_exch1.CommitValueOnFocusLost = true;
			this.m_cb_exch1.DisplayProperty = null;
			this.m_cb_exch1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_exch1.ForeColor = System.Drawing.Color.Black;
			this.m_cb_exch1.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_exch1.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_exch1.FormattingEnabled = true;
			this.m_cb_exch1.Location = new System.Drawing.Point(24, 62);
			this.m_cb_exch1.Name = "m_cb_exch1";
			this.m_cb_exch1.PreserveSelectionThruFocusChange = false;
			this.m_cb_exch1.Size = new System.Drawing.Size(212, 21);
			this.m_cb_exch1.TabIndex = 1;
			this.m_cb_exch1.UseValidityColours = true;
			this.m_cb_exch1.Value = null;
			// 
			// m_lbl_buy
			// 
			this.m_lbl_buy.AutoSize = true;
			this.m_lbl_buy.Location = new System.Drawing.Point(21, 97);
			this.m_lbl_buy.Name = "m_lbl_buy";
			this.m_lbl_buy.Size = new System.Drawing.Size(59, 13);
			this.m_lbl_buy.TabIndex = 6;
			this.m_lbl_buy.Text = "Trade Pair:";
			// 
			// m_cb_pair
			// 
			this.m_cb_pair.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_pair.BackColor = System.Drawing.Color.White;
			this.m_cb_pair.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_pair.BackColorValid = System.Drawing.Color.White;
			this.m_cb_pair.CommitValueOnFocusLost = true;
			this.m_cb_pair.DisplayProperty = null;
			this.m_cb_pair.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_pair.ForeColor = System.Drawing.Color.Black;
			this.m_cb_pair.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_pair.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_pair.FormattingEnabled = true;
			this.m_cb_pair.Location = new System.Drawing.Point(86, 94);
			this.m_cb_pair.Name = "m_cb_pair";
			this.m_cb_pair.PreserveSelectionThruFocusChange = false;
			this.m_cb_pair.Size = new System.Drawing.Size(150, 21);
			this.m_cb_pair.TabIndex = 2;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
			// 
			// m_lbl_scale
			// 
			this.m_lbl_scale.AutoSize = true;
			this.m_lbl_scale.Location = new System.Drawing.Point(12, 124);
			this.m_lbl_scale.Name = "m_lbl_scale";
			this.m_lbl_scale.Size = new System.Drawing.Size(68, 13);
			this.m_lbl_scale.TabIndex = 10;
			this.m_lbl_scale.Text = "Trade Scale:";
			// 
			// m_tb_scale
			// 
			this.m_tb_scale.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_scale.BackColor = System.Drawing.Color.White;
			this.m_tb_scale.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_scale.BackColorValid = System.Drawing.Color.White;
			this.m_tb_scale.CommitValueOnFocusLost = true;
			this.m_tb_scale.ForeColor = System.Drawing.Color.Black;
			this.m_tb_scale.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_scale.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_scale.Location = new System.Drawing.Point(86, 121);
			this.m_tb_scale.Name = "m_tb_scale";
			this.m_tb_scale.Size = new System.Drawing.Size(150, 20);
			this.m_tb_scale.TabIndex = 4;
			this.m_tb_scale.UseValidityColours = true;
			this.m_tb_scale.Value = null;
			// 
			// m_tb_balances0
			// 
			this.m_tb_balances0.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_balances0.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tb_balances0.Location = new System.Drawing.Point(3, 3);
			this.m_tb_balances0.Multiline = true;
			this.m_tb_balances0.Name = "m_tb_balances0";
			this.m_tb_balances0.ReadOnly = true;
			this.m_tb_balances0.Size = new System.Drawing.Size(106, 59);
			this.m_tb_balances0.TabIndex = 11;
			this.m_tb_balances0.Text = "Balances";
			// 
			// m_tb_balances1
			// 
			this.m_tb_balances1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_balances1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tb_balances1.Location = new System.Drawing.Point(115, 3);
			this.m_tb_balances1.Multiline = true;
			this.m_tb_balances1.Name = "m_tb_balances1";
			this.m_tb_balances1.ReadOnly = true;
			this.m_tb_balances1.Size = new System.Drawing.Size(106, 59);
			this.m_tb_balances1.TabIndex = 12;
			this.m_tb_balances1.Text = "Balances";
			// 
			// m_table1
			// 
			this.m_table1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_table1.ColumnCount = 2;
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.Controls.Add(this.m_tb_balances0, 0, 0);
			this.m_table1.Controls.Add(this.m_tb_balances1, 1, 0);
			this.m_table1.Location = new System.Drawing.Point(12, 147);
			this.m_table1.Name = "m_table1";
			this.m_table1.RowCount = 1;
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.Size = new System.Drawing.Size(224, 65);
			this.m_table1.TabIndex = 13;
			// 
			// NewFishingUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(248, 253);
			this.Controls.Add(this.m_table1);
			this.Controls.Add(this.m_tb_scale);
			this.Controls.Add(this.m_lbl_scale);
			this.Controls.Add(this.m_cb_pair);
			this.Controls.Add(this.m_lbl_buy);
			this.Controls.Add(this.m_cb_exch1);
			this.Controls.Add(this.m_lbl_exch1);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_cb_exch0);
			this.Controls.Add(this.m_lbl_exch0);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(205, 240);
			this.Name = "NewFishingUI";
			this.Text = "New Fishing Instance";
			this.m_table1.ResumeLayout(false);
			this.m_table1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}

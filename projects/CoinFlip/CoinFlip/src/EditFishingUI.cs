using System;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;
using ComboBox = pr.gui.ComboBox;

namespace CoinFlip
{
	public class EditFishingUI :Form
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
		private ValueBox m_tb_price_offset_min;
		private Label m_lbl_price_offset;
		private ValueBox m_tb_price_offset_max;
		private Label m_lbl_volume_limit_base;
		private Label m_lbl_volume_limit_quote;
		private ValueBox m_tb_volume_limit_base;
		private ValueBox m_tb_volume_limit_quote;
		private ToolTip m_tt;
		private ValueBox m_tb_scale;
		#endregion

		public EditFishingUI(Model model)
			:this(model, new Settings.FishingData(string.Empty, string.Empty, string.Empty, 1m, 1m, 1m, new RangeF(0.002, 0.008)))
		{ }
		public EditFishingUI(Model model, Settings.FishingData fishing_data)
		{
			InitializeComponent();
			Model = model;
			m_fishing = new Settings.FishingData(fishing_data);

			SetupUI();
			UpdateUI();

			UpdateData();
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
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
		public Settings.FishingData FishingData
		{
			get { return m_fishing; }
			set
			{
				m_fishing = value;
				var pair = m_fishing.Pair;
				var exch0 = Model.Exchanges.FirstOrDefault(x => x.Name == m_fishing.Exch0);
				var exch1 = Model.Exchanges.FirstOrDefault(x => x.Name == m_fishing.Exch1);
				if (exch0 != null) m_cb_exch0.SelectedItem = exch0;
				if (exch1 != null) m_cb_exch1.SelectedItem = exch1;
				if (pair  != null) m_cb_pair.SelectedItem = pair;
				m_tb_scale.Value = m_fishing.Scale;
				m_tb_volume_limit_base.Value = m_fishing.VolumeLimitB;
				m_tb_volume_limit_quote.Value = m_fishing.VolumeLimitQ;
				m_tb_price_offset_min.Value = m_fishing.PriceOffset.Beg;
				m_tb_price_offset_max.Value = m_fishing.PriceOffset.End;
			}
		}
		private Settings.FishingData m_fishing;

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

		/// <summary>The maximum volume to trade of base currency</summary>
		public decimal VolumeLimitB
		{
			get { return (decimal)m_tb_volume_limit_base.Value; }
		}
		
		/// <summary>The maximum volume to trade of quote currency</summary>
		public decimal VolumeLimitQ
		{
			get { return (decimal)m_tb_volume_limit_quote.Value; }
		}

		/// <summary>Bait price offset</summary>
		public RangeF PriceOffset
		{
			get { return new RangeF((double)m_tb_price_offset_min.Value, (double)m_tb_price_offset_max.Value); }
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Reference exchange
			m_cb_exch0.ToolTip(m_tt, "The exchange used to determine the reference buy/sell prices");
			m_cb_exch0.DataSource = Model.Exchanges.Except(Model.CrossExchange).ToList();
			m_cb_exch0.SelectedItem = Model.Exchanges.FirstOrDefault(x => x.Name == FishingData.Exch0);
			m_cb_exch0.SelectedIndexChanged += HandleExchangeChanged;
			m_cb_exch0.SelectedIndexChanged += UpdateData;
			m_cb_exch0.SelectedIndexChanged += UpdateUI;

			// Target exchange
			m_cb_exch1.ToolTip(m_tt, "The exchange that fishing orders are placed on");
			m_cb_exch1.DataSource = Model.Exchanges.Except(Model.CrossExchange).ToList();
			m_cb_exch1.SelectedItem = Model.Exchanges.FirstOrDefault(x => x.Name == FishingData.Exch1);
			m_cb_exch1.SelectedIndexChanged += HandleExchangeChanged;
			m_cb_exch1.SelectedIndexChanged += UpdateData;
			m_cb_exch1.SelectedIndexChanged += UpdateUI;

			// Available pairs
			HandleExchangeChanged();
			m_cb_pair.ToolTip(m_tt, "The trading pair to fish with");
			m_cb_pair.SelectedItem = FishingData.Pair;
			m_cb_pair.SelectedIndexChanged += UpdateData;
			m_cb_pair.SelectedIndexChanged += UpdateUI;

			// Trade scale
			m_tb_scale.ToolTip(m_tt, "The volume traded equals the balance available times this scaling factor");
			m_tb_scale.ValueType = typeof(decimal);
			m_tb_scale.ValidateText = t => { var v = decimal_.TryParse(t); return v != null && v.Within(0m,1m); };
			m_tb_scale.Value = FishingData.Scale;
			m_tb_scale.ValueCommitted += UpdateData;
			m_tb_scale.TextChanged += UpdateUI;

			// Price offset min
			m_tb_price_offset_min.ToolTip(m_tt, "The lower bound on the distance from the reference price for the fishing trade price");
			m_tb_price_offset_min.ValueType = typeof(double);
			m_tb_price_offset_min.ValidateText = t => { var v = double_.TryParse(t); return v != null && v >= 0.0; };
			m_tb_price_offset_min.Value = FishingData.PriceOffset.Beg;
			m_tb_price_offset_min.ValueCommitted += UpdateData;
			m_tb_price_offset_min.TextChanged += UpdateUI;

			// Price offset max
			m_tb_price_offset_max.ToolTip(m_tt, "The upper bound on the distance from the reference price for the fishing trade price");
			m_tb_price_offset_max.ValueType = typeof(double);
			m_tb_price_offset_max.ValidateText = t => { var v = double_.TryParse(t); return v != null && v >= 0.0; };
			m_tb_price_offset_max.Value = FishingData.PriceOffset.End;
			m_tb_price_offset_max.ValueCommitted += UpdateData;
			m_tb_price_offset_max.TextChanged += UpdateUI;

			// Volume base max
			m_tb_volume_limit_base.ToolTip(m_tt, "The maximum amount of base currency to trade");
			m_tb_volume_limit_base.ValueType = typeof(decimal);
			m_tb_volume_limit_base.ValidateText = t => { var v = decimal_.TryParse(t); return v != null && v >= 0m; };
			m_tb_volume_limit_base.Value = FishingData.VolumeLimitB;
			m_tb_volume_limit_base.ValueCommitted += UpdateData;
			m_tb_volume_limit_base.TextChanged += UpdateUI;

			// Volume quote max
			m_tb_volume_limit_quote.ToolTip(m_tt, "The maximum amount of quote currency to trade");
			m_tb_volume_limit_quote.ValueType = typeof(decimal);
			m_tb_volume_limit_quote.ValidateText = t => { var v = decimal_.TryParse(t); return v != null && v >= 0m; };
			m_tb_volume_limit_quote.Value = FishingData.VolumeLimitQ;
			m_tb_volume_limit_quote.ValueCommitted += UpdateData;
			m_tb_volume_limit_quote.TextChanged += UpdateUI;
		}

		/// <summary></summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Update the balances
			if (Pair != null)
			{
				// Show balances
				var coins = Pair.Split('/');
				m_tb_balances0.Text = Str.Build(
					$"{Exch0.Name}\r\n",
					$"   {Exch0.Balance.Get(coins[0])?.Available ?? 0m} {coins[0]}\r\n",
					$"   {Exch0.Balance.Get(coins[1])?.Available ?? 0m} {coins[1]}");
				m_tb_balances1.Text = Str.Build(
					$"{Exch1.Name}\r\n",
					$"   {Exch1.Balance.Get(coins[0])?.Available ?? 0m} {coins[0]}\r\n",
					$"   {Exch1.Balance.Get(coins[1])?.Available ?? 0m} {coins[1]}");

				// Update Max volume labels
				m_lbl_volume_limit_base.Text = $"Max {coins[0]}:";
				m_lbl_volume_limit_quote.Text = $"Max {coins[1]}:";
			}
			else
			{
				m_tb_balances0.Text = string.Empty;
				m_tb_balances1.Text = string.Empty;
				m_lbl_volume_limit_base.Text  = $"Max Base:";
				m_lbl_volume_limit_quote.Text = $"Max Quote:";
			}

			// Enable the OK button
			m_btn_ok.Enabled =
				m_tb_scale.Valid &&
				m_tb_price_offset_min.Valid &&
				m_tb_price_offset_max.Valid &&
				m_tb_volume_limit_base.Valid &&
				m_tb_volume_limit_quote.Valid &&
				FishingData.Valid;
		}

		/// <summary>Repopulate the fishing data from the current control values</summary>
		private void UpdateData(object sender = null, EventArgs e = null)
		{
			FishingData.Pair         = Pair ?? string.Empty;
			FishingData.Exch0        = Exch0?.Name ?? string.Empty;
			FishingData.Exch1        = Exch1?.Name ?? string.Empty;
			FishingData.Scale        = TradeScale;
			FishingData.PriceOffset  = PriceOffset;
			FishingData.VolumeLimitB = VolumeLimitB;
			FishingData.VolumeLimitQ = VolumeLimitQ;
		}

		/// <summary>Update the data source for the list of trading pairs</summary>
		private void HandleExchangeChanged(object sender = null, EventArgs e = null)
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditFishingUI));
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
			this.m_tb_price_offset_min = new pr.gui.ValueBox();
			this.m_lbl_price_offset = new System.Windows.Forms.Label();
			this.m_tb_price_offset_max = new pr.gui.ValueBox();
			this.m_lbl_volume_limit_base = new System.Windows.Forms.Label();
			this.m_lbl_volume_limit_quote = new System.Windows.Forms.Label();
			this.m_tb_volume_limit_base = new pr.gui.ValueBox();
			this.m_tb_volume_limit_quote = new pr.gui.ValueBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
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
			this.m_cb_exch0.Size = new System.Drawing.Size(300, 21);
			this.m_cb_exch0.TabIndex = 0;
			this.m_cb_exch0.UseValidityColours = true;
			this.m_cb_exch0.Value = null;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(168, 326);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 8;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(249, 326);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 9;
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
			this.m_cb_exch1.Size = new System.Drawing.Size(300, 21);
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
			this.m_cb_pair.Size = new System.Drawing.Size(238, 21);
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
			this.m_tb_scale.TabIndex = 3;
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
			this.m_tb_balances0.Size = new System.Drawing.Size(150, 115);
			this.m_tb_balances0.TabIndex = 11;
			this.m_tb_balances0.Text = "Balances";
			// 
			// m_tb_balances1
			// 
			this.m_tb_balances1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_balances1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tb_balances1.Location = new System.Drawing.Point(159, 3);
			this.m_tb_balances1.Multiline = true;
			this.m_tb_balances1.Name = "m_tb_balances1";
			this.m_tb_balances1.ReadOnly = true;
			this.m_tb_balances1.Size = new System.Drawing.Size(150, 115);
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
			this.m_table1.Location = new System.Drawing.Point(12, 199);
			this.m_table1.Name = "m_table1";
			this.m_table1.RowCount = 1;
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.Size = new System.Drawing.Size(312, 121);
			this.m_table1.TabIndex = 13;
			// 
			// m_tb_price_offset_min
			// 
			this.m_tb_price_offset_min.BackColor = System.Drawing.Color.White;
			this.m_tb_price_offset_min.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_price_offset_min.BackColorValid = System.Drawing.Color.White;
			this.m_tb_price_offset_min.CommitValueOnFocusLost = true;
			this.m_tb_price_offset_min.ForeColor = System.Drawing.Color.Black;
			this.m_tb_price_offset_min.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_price_offset_min.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_price_offset_min.Location = new System.Drawing.Point(86, 147);
			this.m_tb_price_offset_min.Name = "m_tb_price_offset_min";
			this.m_tb_price_offset_min.Size = new System.Drawing.Size(75, 20);
			this.m_tb_price_offset_min.TabIndex = 4;
			this.m_tb_price_offset_min.UseValidityColours = true;
			this.m_tb_price_offset_min.Value = null;
			// 
			// m_lbl_price_offset
			// 
			this.m_lbl_price_offset.AutoSize = true;
			this.m_lbl_price_offset.Location = new System.Drawing.Point(12, 150);
			this.m_lbl_price_offset.Name = "m_lbl_price_offset";
			this.m_lbl_price_offset.Size = new System.Drawing.Size(65, 13);
			this.m_lbl_price_offset.TabIndex = 15;
			this.m_lbl_price_offset.Text = "Price Offset:";
			// 
			// m_tb_price_offset_max
			// 
			this.m_tb_price_offset_max.BackColor = System.Drawing.Color.White;
			this.m_tb_price_offset_max.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_price_offset_max.BackColorValid = System.Drawing.Color.White;
			this.m_tb_price_offset_max.CommitValueOnFocusLost = true;
			this.m_tb_price_offset_max.ForeColor = System.Drawing.Color.Black;
			this.m_tb_price_offset_max.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_price_offset_max.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_price_offset_max.Location = new System.Drawing.Point(167, 147);
			this.m_tb_price_offset_max.Name = "m_tb_price_offset_max";
			this.m_tb_price_offset_max.Size = new System.Drawing.Size(69, 20);
			this.m_tb_price_offset_max.TabIndex = 5;
			this.m_tb_price_offset_max.UseValidityColours = true;
			this.m_tb_price_offset_max.Value = null;
			// 
			// m_lbl_volume_limit_base
			// 
			this.m_lbl_volume_limit_base.AutoSize = true;
			this.m_lbl_volume_limit_base.Location = new System.Drawing.Point(20, 176);
			this.m_lbl_volume_limit_base.Name = "m_lbl_volume_limit_base";
			this.m_lbl_volume_limit_base.Size = new System.Drawing.Size(57, 13);
			this.m_lbl_volume_limit_base.TabIndex = 17;
			this.m_lbl_volume_limit_base.Text = "Base Max:";
			// 
			// m_lbl_volume_limit_quote
			// 
			this.m_lbl_volume_limit_quote.AutoSize = true;
			this.m_lbl_volume_limit_quote.Location = new System.Drawing.Point(167, 176);
			this.m_lbl_volume_limit_quote.Name = "m_lbl_volume_limit_quote";
			this.m_lbl_volume_limit_quote.Size = new System.Drawing.Size(62, 13);
			this.m_lbl_volume_limit_quote.TabIndex = 18;
			this.m_lbl_volume_limit_quote.Text = "Quote Max:";
			// 
			// m_tb_volume_limit_base
			// 
			this.m_tb_volume_limit_base.BackColor = System.Drawing.Color.White;
			this.m_tb_volume_limit_base.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_volume_limit_base.BackColorValid = System.Drawing.Color.White;
			this.m_tb_volume_limit_base.CommitValueOnFocusLost = true;
			this.m_tb_volume_limit_base.ForeColor = System.Drawing.Color.Black;
			this.m_tb_volume_limit_base.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_volume_limit_base.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_volume_limit_base.Location = new System.Drawing.Point(86, 173);
			this.m_tb_volume_limit_base.Name = "m_tb_volume_limit_base";
			this.m_tb_volume_limit_base.Size = new System.Drawing.Size(75, 20);
			this.m_tb_volume_limit_base.TabIndex = 6;
			this.m_tb_volume_limit_base.UseValidityColours = true;
			this.m_tb_volume_limit_base.Value = null;
			// 
			// m_tb_volume_limit_quote
			// 
			this.m_tb_volume_limit_quote.BackColor = System.Drawing.Color.White;
			this.m_tb_volume_limit_quote.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_volume_limit_quote.BackColorValid = System.Drawing.Color.White;
			this.m_tb_volume_limit_quote.CommitValueOnFocusLost = true;
			this.m_tb_volume_limit_quote.ForeColor = System.Drawing.Color.Black;
			this.m_tb_volume_limit_quote.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_volume_limit_quote.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_volume_limit_quote.Location = new System.Drawing.Point(235, 173);
			this.m_tb_volume_limit_quote.Name = "m_tb_volume_limit_quote";
			this.m_tb_volume_limit_quote.Size = new System.Drawing.Size(75, 20);
			this.m_tb_volume_limit_quote.TabIndex = 7;
			this.m_tb_volume_limit_quote.UseValidityColours = true;
			this.m_tb_volume_limit_quote.Value = null;
			// 
			// EditFishingUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(336, 361);
			this.Controls.Add(this.m_tb_volume_limit_quote);
			this.Controls.Add(this.m_tb_volume_limit_base);
			this.Controls.Add(this.m_lbl_volume_limit_quote);
			this.Controls.Add(this.m_lbl_volume_limit_base);
			this.Controls.Add(this.m_tb_price_offset_max);
			this.Controls.Add(this.m_tb_price_offset_min);
			this.Controls.Add(this.m_lbl_price_offset);
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
			this.MinimumSize = new System.Drawing.Size(340, 314);
			this.Name = "EditFishingUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Edit Fishing Instance";
			this.m_table1.ResumeLayout(false);
			this.m_table1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}

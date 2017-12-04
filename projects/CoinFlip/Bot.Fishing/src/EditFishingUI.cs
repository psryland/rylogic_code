using System;
using System.Linq;
using System.Windows.Forms;
using CoinFlip;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;
using ComboBox = pr.gui.ComboBox;

namespace Bot.Fishing
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
		private TextBox m_tb_balances0;
		private TextBox m_tb_balances1;
		private TableLayoutPanel m_table1;
		private ValueBox m_tb_price_offset;
		private Label m_lbl_price_offset;
		private Label m_lbl_volume_limit_base;
		private Label m_lbl_volume_limit_quote;
		private ValueBox m_tb_volume_limit_base;
		private ValueBox m_tb_volume_limit_quote;
		private ToolTip m_tt;
		private CheckBox m_chk_b2q;
		private CheckBox m_chk_q2b;
		private Label m_lbl_info;
		#endregion

		public EditFishingUI(FishFinder ff)
			:this(ff, new FishFinder.FishingData(string.Empty, string.Empty, string.Empty, 0.005m, ETradeDirection.Both), false)
		{ }
		public EditFishingUI(FishFinder ff, FishFinder.FishingData fishing_data, bool active)
		{
			InitializeComponent();
			Bot = ff;

			m_fishing = new FishFinder.FishingData(fishing_data);
			m_fishing_active = active;

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Bot = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The owning bot instance</summary>
		public FishFinder Bot
		{
			get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				m_bot = value;
			}
		}
		private FishFinder m_bot;

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return Bot.Model; }
		}

		/// <summary>The Fishing instance</summary>
		public FishFinder.FishingData FishingData
		{
			get { return m_fishing; }
			set
			{
				m_fishing = value;
				var pair  = TradePair.Parse(m_fishing.Pair);
				var exch0 = Model.Exchanges.FirstOrDefault(x => x.Name == m_fishing.Exch0);
				var exch1 = Model.Exchanges.FirstOrDefault(x => x.Name == m_fishing.Exch1);
				if (exch0 != null) m_cb_exch0.SelectedItem = exch0;
				if (exch1 != null) m_cb_exch1.SelectedItem = exch1;
				if (pair  != null) m_cb_pair.SelectedItem = pair.Name;
				m_chk_q2b.Checked = m_fishing.Direction.HasFlag(ETradeDirection.Q2B);
				m_chk_b2q.Checked = m_fishing.Direction.HasFlag(ETradeDirection.B2Q);
				m_tb_price_offset.Value = m_fishing.PriceOffset;
				if (pair != null) m_tb_volume_limit_base.Value = Model.Coins[pair.Base].AutoTradingLimit;
				if (pair != null) m_tb_volume_limit_quote.Value = Model.Coins[pair.Quote].AutoTradingLimit;
			}
		}
		private FishFinder.FishingData m_fishing;
		private bool m_fishing_active;

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
		public decimal PriceOffset
		{
			get { return (decimal)m_tb_price_offset.Value; }
		}

		/// <summary>The trade direction</summary>
		public ETradeDirection TradeDirection
		{
			get
			{
				return
					(m_chk_q2b.Checked ? ETradeDirection.Q2B : 0) |
					(m_chk_b2q.Checked ? ETradeDirection.B2Q : 0);
			}
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Reference exchange
			m_cb_exch0.ToolTip(m_tt, "The exchange used to determine the reference buy/sell prices");
			m_cb_exch0.DataSource = Model.TradingExchanges.ToList();
			m_cb_exch0.SelectedItem = Model.Exchanges.FirstOrDefault(x => x.Name == FishingData.Exch0);
			m_cb_exch0.SelectedIndexChanged += HandleExchangeChanged;
			m_cb_exch0.SelectedIndexChanged += UpdateUI;

			// Target exchange
			m_cb_exch1.ToolTip(m_tt, "The exchange that fishing orders are placed on");
			m_cb_exch1.DataSource = Model.TradingExchanges.ToList();
			m_cb_exch1.SelectedItem = Model.Exchanges.FirstOrDefault(x => x.Name == FishingData.Exch1);
			m_cb_exch1.SelectedIndexChanged += HandleExchangeChanged;
			m_cb_exch1.SelectedIndexChanged += UpdateUI;

			HandleExchangeChanged();

			// Available pairs
			m_cb_pair.ToolTip(m_tt, "The trading pair to fish with");
			m_cb_pair.SelectedItem = FishingData.Pair;
			m_cb_pair.SelectedIndexChanged += UpdateUI;
			m_cb_pair.SelectedIndexChanged += HandlePairChanged;

			HandlePairChanged();

			// Price offset min
			m_tb_price_offset.ToolTip(m_tt, "The minimum distance from the reference price for the bait trade price");
			m_tb_price_offset.ValueType = typeof(decimal);
			m_tb_price_offset.ValidateText = t => { var v = decimal_.TryParse(t); return v != null && v > 0m; };
			m_tb_price_offset.Value = FishingData.PriceOffset;
			m_tb_price_offset.ValueChanged += UpdateUI;
			m_tb_price_offset.ValueCommitted += (s, a) =>
			{
				FishingData.PriceOffset = PriceOffset;
			};

			// Volume base max
			m_tb_volume_limit_base.ToolTip(m_tt, "The maximum amount of base currency to trade");
			m_tb_volume_limit_base.ValueType = typeof(decimal);
			m_tb_volume_limit_base.ValidateText = t => { var v = decimal_.TryParse(t); return v != null && v >= 0m; };
			m_tb_volume_limit_base.ValueChanged += UpdateUI;
			m_tb_volume_limit_base.ValueCommitted += (s, a) =>
			{
				if (!Pair.HasValue()) return;
				var pair = TradePair.Parse(Pair);
				Model.Coins[pair.Base].AutoTradingLimit = VolumeLimitB;
			};

			// Volume quote max
			m_tb_volume_limit_quote.ToolTip(m_tt, "The maximum amount of quote currency to trade");
			m_tb_volume_limit_quote.ValueType = typeof(decimal);
			m_tb_volume_limit_quote.ValidateText = t => { var v = decimal_.TryParse(t); return v != null && v >= 0m; };
			m_tb_volume_limit_quote.ValueChanged += UpdateUI;
			m_tb_volume_limit_quote.ValueCommitted += (s, a) =>
			{
				if (!Pair.HasValue()) return;
				var pair = TradePair.Parse(Pair);
				Model.Coins[pair.Quote].AutoTradingLimit = VolumeLimitQ;
			};

			// Trade direction
			m_chk_b2q.ToolTip(m_tt, "Trade Base currency to Quote currency on the target exchange");
			m_chk_b2q.Checked = FishingData.Direction.HasFlag(ETradeDirection.B2Q);
			m_chk_b2q.CheckedChanged += UpdateUI;
			m_chk_b2q.CheckedChanged += (s, a) =>
			{
				FishingData.Direction = TradeDirection;
			};

			m_chk_q2b.ToolTip(m_tt, "Trade Quote currency to Base currency on the target exchange");
			m_chk_q2b.Checked = FishingData.Direction.HasFlag(ETradeDirection.Q2B);
			m_chk_q2b.CheckedChanged += UpdateUI;
			m_chk_q2b.CheckedChanged += (s, a) =>
			{
				FishingData.Direction = TradeDirection;
			};
		}

		/// <summary></summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			var min_price_offset = 0m;

			// Update the balances
			if (Pair != null)
			{
				// Show balances
				var pair = TradePair.Parse(Pair);
				m_tb_balances0.Text = Str.Build(
					$"{Exch0.Name}\r\n",
					$"   {Exch0.Balance.Get(pair.Base)[Bot.Fund]?.Available ?? 0m} {pair.Base}\r\n",
					$"   {Exch0.Balance.Get(pair.Quote)[Bot.Fund]?.Available ?? 0m} {pair.Quote}");
				m_tb_balances1.Text = Str.Build(
					$"{Exch1.Name}\r\n",
					$"   {Exch1.Balance.Get(pair.Base)[Bot.Fund]?.Available ?? 0m} {pair.Base}\r\n",
					$"   {Exch1.Balance.Get(pair.Quote)[Bot.Fund]?.Available ?? 0m} {pair.Quote}");

				// Update Max volume limits
				m_lbl_volume_limit_base.Text = $"Max {pair.Base}:";
				m_lbl_volume_limit_quote.Text = $"Max {pair.Quote}:";

				// Update trade direction
				m_chk_b2q.Text = $"{pair.Base} → {pair.Quote}";
				m_chk_q2b.Text = $"{pair.Quote} → {pair.Base}";

				var pair0 = Exch0.Pairs[Pair];
				var pair1 = Exch1.Pairs[Pair];
				min_price_offset = pair0.Fee + pair1.Fee;
			}
			else
			{
				m_tb_balances0.Text = string.Empty;
				m_tb_balances1.Text = string.Empty;
				m_lbl_volume_limit_base.Text  = $"Max Base:";
				m_lbl_volume_limit_quote.Text = $"Max Quote:";
				m_chk_b2q.Text = $"Base → Quote";
				m_chk_q2b.Text = $"Quote → Base";
			}

			// Disable UI elements that can't be changed while active
			m_cb_exch0.Enabled = !m_fishing_active;
			m_cb_exch1.Enabled = !m_fishing_active;
			m_cb_pair.Enabled = !m_fishing_active;
			m_chk_b2q.Enabled = !m_fishing_active;
			m_chk_q2b.Enabled = !m_fishing_active;

			// Enable the OK button
			m_btn_ok.Enabled =
				m_tb_price_offset.Valid &&
				m_tb_volume_limit_base.Valid &&
				m_tb_volume_limit_quote.Valid &&
				FishingData.Valid &&
				VolumeLimitB >= 0 &&
				VolumeLimitQ >= 0 &&
				PriceOffset >= min_price_offset;
			m_lbl_info.Text = Str.Build(
				!m_tb_price_offset.Valid       ? "Price offset is invalid\r\n" : string.Empty,
				!m_tb_volume_limit_base.Valid  ? "Base volume limit is invalid\r\n" : string.Empty,
				!m_tb_volume_limit_quote.Valid ? "Quote volume limit is invalid\r\n" : string.Empty,
				!FishingData.Valid             ? FishingData.ReasonInvalid : string.Empty,
				VolumeLimitB < 0               ? "Volume limit (base) is invalid\r\n"  : string.Empty,
				VolumeLimitQ < 0               ? "Volume limit (quote) is invalid\r\n" : string.Empty,
				PriceOffset < min_price_offset ? $"Price offset is less than the sum of transaction fees ({min_price_offset})\r\n" : string.Empty);
		}

		/// <summary>Handle the exchanges being changed</summary>
		private void HandleExchangeChanged(object sender = null, EventArgs e = null)
		{
			FishingData.Exch0 = Exch0?.Name ?? string.Empty;
			FishingData.Exch1 = Exch1?.Name ?? string.Empty;

			// Update the list of available pairs
			var pairs0 = Exch0.Pairs.Values.ToHashSet(x => x.Name);
			var pairs1 = Exch1.Pairs.Values.ToHashSet(x => x.Name);
			using (m_cb_pair.PreserveSelectedItem())
				m_cb_pair.DataSource = pairs0.Intersect(pairs1).ToList();
		}

		/// <summary>Handle the pair being changed</summary>
		private void HandlePairChanged(object sender = null, EventArgs e = null)
		{
			FishingData.Pair = Pair ?? string.Empty;

			// Update the auto trade limits in the UI
			if (Pair.HasValue())
			{
				var pair = TradePair.Parse(Pair);
				m_tb_volume_limit_base.SetValue(Model.Coins[pair.Base].AutoTradingLimit, notify: false);
				m_tb_volume_limit_quote.SetValue(Model.Coins[pair.Quote].AutoTradingLimit, notify: false);
			}
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
			this.m_tb_balances0 = new System.Windows.Forms.TextBox();
			this.m_tb_balances1 = new System.Windows.Forms.TextBox();
			this.m_table1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_tb_price_offset = new pr.gui.ValueBox();
			this.m_lbl_price_offset = new System.Windows.Forms.Label();
			this.m_lbl_volume_limit_base = new System.Windows.Forms.Label();
			this.m_lbl_volume_limit_quote = new System.Windows.Forms.Label();
			this.m_tb_volume_limit_base = new pr.gui.ValueBox();
			this.m_tb_volume_limit_quote = new pr.gui.ValueBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_chk_b2q = new System.Windows.Forms.CheckBox();
			this.m_chk_q2b = new System.Windows.Forms.CheckBox();
			this.m_lbl_info = new System.Windows.Forms.Label();
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
			this.m_cb_exch0.Size = new System.Drawing.Size(294, 21);
			this.m_cb_exch0.TabIndex = 0;
			this.m_cb_exch0.UseValidityColours = true;
			this.m_cb_exch0.Value = null;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(162, 329);
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
			this.m_btn_cancel.Location = new System.Drawing.Point(243, 329);
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
			this.m_cb_exch1.Size = new System.Drawing.Size(294, 21);
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
			this.m_cb_pair.Size = new System.Drawing.Size(232, 21);
			this.m_cb_pair.TabIndex = 2;
			this.m_cb_pair.UseValidityColours = true;
			this.m_cb_pair.Value = null;
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
			this.m_tb_balances0.Size = new System.Drawing.Size(147, 106);
			this.m_tb_balances0.TabIndex = 11;
			this.m_tb_balances0.Text = "Balances";
			// 
			// m_tb_balances1
			// 
			this.m_tb_balances1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_balances1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tb_balances1.Location = new System.Drawing.Point(156, 3);
			this.m_tb_balances1.Multiline = true;
			this.m_tb_balances1.Name = "m_tb_balances1";
			this.m_tb_balances1.ReadOnly = true;
			this.m_tb_balances1.Size = new System.Drawing.Size(147, 106);
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
			this.m_table1.Location = new System.Drawing.Point(12, 211);
			this.m_table1.Name = "m_table1";
			this.m_table1.RowCount = 1;
			this.m_table1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table1.Size = new System.Drawing.Size(306, 112);
			this.m_table1.TabIndex = 13;
			// 
			// m_tb_price_offset
			// 
			this.m_tb_price_offset.BackColor = System.Drawing.Color.White;
			this.m_tb_price_offset.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_price_offset.BackColorValid = System.Drawing.Color.White;
			this.m_tb_price_offset.CommitValueOnFocusLost = true;
			this.m_tb_price_offset.ForeColor = System.Drawing.Color.Black;
			this.m_tb_price_offset.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_price_offset.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_price_offset.Location = new System.Drawing.Point(86, 164);
			this.m_tb_price_offset.Name = "m_tb_price_offset";
			this.m_tb_price_offset.Size = new System.Drawing.Size(73, 20);
			this.m_tb_price_offset.TabIndex = 4;
			this.m_tb_price_offset.UseValidityColours = true;
			this.m_tb_price_offset.Value = null;
			// 
			// m_lbl_price_offset
			// 
			this.m_lbl_price_offset.AutoSize = true;
			this.m_lbl_price_offset.Location = new System.Drawing.Point(15, 167);
			this.m_lbl_price_offset.Name = "m_lbl_price_offset";
			this.m_lbl_price_offset.Size = new System.Drawing.Size(65, 13);
			this.m_lbl_price_offset.TabIndex = 15;
			this.m_lbl_price_offset.Text = "Price Offset:";
			// 
			// m_lbl_volume_limit_base
			// 
			this.m_lbl_volume_limit_base.AutoSize = true;
			this.m_lbl_volume_limit_base.Location = new System.Drawing.Point(171, 156);
			this.m_lbl_volume_limit_base.Name = "m_lbl_volume_limit_base";
			this.m_lbl_volume_limit_base.Size = new System.Drawing.Size(57, 13);
			this.m_lbl_volume_limit_base.TabIndex = 17;
			this.m_lbl_volume_limit_base.Text = "Base Max:";
			// 
			// m_lbl_volume_limit_quote
			// 
			this.m_lbl_volume_limit_quote.AutoSize = true;
			this.m_lbl_volume_limit_quote.Location = new System.Drawing.Point(166, 182);
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
			this.m_tb_volume_limit_base.Location = new System.Drawing.Point(230, 153);
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
			this.m_tb_volume_limit_quote.Location = new System.Drawing.Point(230, 179);
			this.m_tb_volume_limit_quote.Name = "m_tb_volume_limit_quote";
			this.m_tb_volume_limit_quote.Size = new System.Drawing.Size(75, 20);
			this.m_tb_volume_limit_quote.TabIndex = 7;
			this.m_tb_volume_limit_quote.UseValidityColours = true;
			this.m_tb_volume_limit_quote.Value = null;
			// 
			// m_chk_b2q
			// 
			this.m_chk_b2q.AutoSize = true;
			this.m_chk_b2q.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_chk_b2q.Location = new System.Drawing.Point(65, 127);
			this.m_chk_b2q.Name = "m_chk_b2q";
			this.m_chk_b2q.Size = new System.Drawing.Size(94, 17);
			this.m_chk_b2q.TabIndex = 19;
			this.m_chk_b2q.Text = "Base to Quote";
			this.m_chk_b2q.UseVisualStyleBackColor = true;
			// 
			// m_chk_q2b
			// 
			this.m_chk_q2b.AutoSize = true;
			this.m_chk_q2b.Location = new System.Drawing.Point(169, 127);
			this.m_chk_q2b.Name = "m_chk_q2b";
			this.m_chk_q2b.Size = new System.Drawing.Size(94, 17);
			this.m_chk_q2b.TabIndex = 20;
			this.m_chk_q2b.Text = "Quote to Base";
			this.m_chk_q2b.UseVisualStyleBackColor = true;
			// 
			// m_lbl_info
			// 
			this.m_lbl_info.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_info.Location = new System.Drawing.Point(12, 329);
			this.m_lbl_info.Name = "m_lbl_info";
			this.m_lbl_info.Size = new System.Drawing.Size(147, 26);
			this.m_lbl_info.TabIndex = 21;
			this.m_lbl_info.Text = "Info";
			// 
			// EditFishingUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(330, 364);
			this.Controls.Add(this.m_lbl_info);
			this.Controls.Add(this.m_chk_q2b);
			this.Controls.Add(this.m_chk_b2q);
			this.Controls.Add(this.m_tb_volume_limit_quote);
			this.Controls.Add(this.m_tb_volume_limit_base);
			this.Controls.Add(this.m_lbl_volume_limit_quote);
			this.Controls.Add(this.m_lbl_volume_limit_base);
			this.Controls.Add(this.m_tb_price_offset);
			this.Controls.Add(this.m_lbl_price_offset);
			this.Controls.Add(this.m_table1);
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

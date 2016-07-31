using System;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;
using Tradee;

namespace TestClient
{
	public class MainUI :Form
	{
		#region UI Elements
		private CheckBox m_chk_send_data;
		private Timer m_timer;
		#endregion

		private long m_now;
		private long m_one;
		private Random m_rng;
		private Candle m_candle;
		private pr.gui.ComboBox m_cb_timeframe;
		private double m_price;

		public MainUI()
		{
			InitializeComponent();
			StartPosition = FormStartPosition.Manual;
			Location = new System.Drawing.Point(2000,500);

			m_now       = new DateTime(2000,1,1,0,0,0).Ticks;
			m_rng       = new Random(0);
			m_candle    = new Candle(m_now, 0, 0, 0, 0, 0);
			m_price     = 1.0;
			SymbolCode      = "PSRRLZ";

			Tradee = new TradeeProxy(DispatchMsg);

			m_cb_timeframe.DataSource = Enum<ETimeFrame>.ValuesArray;

			m_chk_send_data.CheckedChanged += (s,a) =>
			{
				m_timer.Enabled = m_chk_send_data.Checked;
				if (!m_chk_send_data.Checked)
					return;

				// Get the value of one time frame unit in ticks
				var tf = (ETimeFrame)m_cb_timeframe.SelectedItem;
				m_one = Misc.TimeFrameToTicks(1.0, tf);

				// Send fake historic data
				SendHistoricData();
			};

			m_timer.Tick += HandleTick;
		}
		protected override void Dispose(bool disposing)
		{
			Tradee = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The connection to the Tradee application</summary>
		private TradeePipe Tradee
		{
			get { return m_tradee; }
			set
			{
				if (m_tradee == value) return;
				Util.Dispose(ref m_tradee);
				m_tradee = value;
			}
		}
		private TradeePipe m_tradee;

		private string SymbolCode { get; set; }

		/// <summary>Handle messages sent from 'Tradee'</summary>
		public void DispatchMsg(object msg)
		{
			switch (msg.GetType().Name)
			{
			case nameof(OutMsg.TestMsg):
				{
					var m = (OutMsg.TestMsg)msg;
					MsgBox.Show(null, m.Text, "Test Msg Received");
					Tradee.Post(new InMsg.TestMsg { Text = "Who dat dere?" });
					break;
				}
			}
		}

		/// <summary>Send fake historic data</summary>
		private void SendHistoricData()
		{
			const int Count = 20;
			var tf = (ETimeFrame)m_cb_timeframe.SelectedItem;
			var candles = new Candles(
				Enumerable.Range(0, Count).Select(i => m_now - (Count - i) * m_one).ToArray(),
				Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(0.5, 1.5)).ToArray(),
				Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(1.5, 2.0)).ToArray(),
				Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(0.0, 0.5)).ToArray(),
				Enumerable.Range(0, Count).Select(i => m_rng.NextDouble(0.5, 1.5)).ToArray(),
				Enumerable.Range(0, Count).Select(i => (double)(int)m_rng.NextDouble(1, 100)).ToArray());
			Tradee.Post(new InMsg.CandleData(SymbolCode, ETimeFrame.Min1, candles));
		}

		/// <summary>Generate fake data on timer ticks</summary>
		private void HandleTick(object sender, EventArgs args)
		{
			m_price += m_rng.NextDouble(-0.01,+0.01);
			var spread = m_rng.NextDouble(0.001, 0.01);
			var tf = (ETimeFrame)m_cb_timeframe.SelectedItem;

			// Fake price data
			if (++m_candle.Volume == 11)
			{
				m_now += m_one;

				m_candle.Timestamp = m_now;
				m_candle.Open      = m_price;
				m_candle.High      = m_price;
				m_candle.Low       = m_price;
				m_candle.Close     = m_price;
				m_candle.Volume    = 1;
			}
			m_candle.High    = Math.Max(m_candle.High, m_price);
			m_candle.Low     = Math.Min(m_candle.Low, m_price);
			m_candle.Close   = m_price;

			Tradee.Post(new InMsg.CandleData(SymbolCode, tf, m_candle));

			// Fake spread
			var sym_data = new PriceData(m_price + spread, m_price, 10000, 0.0001, 1.0, 1000, 1000, 100000000);
			var data = new InMsg.SymbolData(SymbolCode, sym_data);
			Tradee.Post(data);
		}
		
		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_chk_send_data = new System.Windows.Forms.CheckBox();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.m_cb_timeframe = new pr.gui.ComboBox();
			this.SuspendLayout();
			// 
			// m_chk_send_data
			// 
			this.m_chk_send_data.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_send_data.AutoSize = true;
			this.m_chk_send_data.Location = new System.Drawing.Point(139, 10);
			this.m_chk_send_data.Name = "m_chk_send_data";
			this.m_chk_send_data.Size = new System.Drawing.Size(68, 23);
			this.m_chk_send_data.TabIndex = 0;
			this.m_chk_send_data.Text = "Send Data";
			this.m_chk_send_data.UseVisualStyleBackColor = true;
			// 
			// m_cb_timeframe
			// 
			this.m_cb_timeframe.DisplayProperty = null;
			this.m_cb_timeframe.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_timeframe.FormattingEnabled = true;
			this.m_cb_timeframe.Location = new System.Drawing.Point(12, 12);
			this.m_cb_timeframe.Name = "m_cb_timeframe";
			this.m_cb_timeframe.PreserveSelectionThruFocusChange = false;
			this.m_cb_timeframe.Size = new System.Drawing.Size(121, 21);
			this.m_cb_timeframe.TabIndex = 1;
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(219, 47);
			this.Controls.Add(this.m_cb_timeframe);
			this.Controls.Add(this.m_chk_send_data);
			this.Name = "MainUI";
			this.Text = "Test Client";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}

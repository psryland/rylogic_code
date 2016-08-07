using System;
using System.Collections.Generic;
using System.Threading;
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
		private pr.gui.ListBox m_lb;
		private CheckBox m_chk_send_data;
		#endregion

		private List<Transmitter> m_transmitters;
		private ManualResetEvent m_send;
		private ManualResetEvent m_quit;

		public MainUI()
		{
			InitializeComponent();
			StartPosition = FormStartPosition.Manual;
			Location = new System.Drawing.Point(2000,500);
			Tradee = new TradeeProxy(DispatchMsg);
			m_send = new ManualResetEvent(false);
			m_quit = new ManualResetEvent(false);
			m_transmitters = new List<Transmitter>();
			m_transmitters.Add(new Transmitter("Pauls", this, m_send, m_quit));
			m_transmitters.Add(new Transmitter("noise", this, m_send, m_quit));

			m_lb.DataSource = m_transmitters;
			m_chk_send_data.CheckedChanged += (s,a) =>
			{
				if (m_chk_send_data.Checked)
					m_send.Set();
				else
					m_send.Reset();
			};
		}
		protected override void Dispose(bool disposing)
		{
			Util.DisposeAll(m_transmitters);

			if (m_quit != null) { m_quit.Dispose(); m_quit = null; }
			if (m_send != null) { m_send.Dispose(); m_send = null; }
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
				if (m_tradee != null)
				{
					m_tradee.Posted -= UpdateUI;
					Util.Dispose(ref m_tradee);
				}
				m_tradee = value;
				if (m_tradee != null)
				{
					m_tradee.Posted += UpdateUI;
					Post(new InMsg.HelloTradee("Hi From TestClient"));
				}
			}
		}
		private TradeePipe m_tradee;

		/// <summary>Send a message to Tradee</summary>
		public void Post<T>(T msg, Action on_complete = null) where T :ITradeeMsg
		{
			if (InvokeRequired)
			{
				this.BeginInvoke(() => Post(msg, on_complete));
				return;
			}
			if (Tradee.Post(msg) && on_complete != null)
				on_complete();
		}

		/// <summary>Handle messages sent from 'Tradee'</summary>
		private void DispatchMsg(object msg)
		{
			switch (msg.GetType().Name)
			{
			case nameof(OutMsg.HelloClient):
				{
					var m = (OutMsg.HelloClient)msg;
					MsgBox.Show(null, m.Text, "Test Msg Received");
					Tradee.Post(new InMsg.HelloTradee("Who dat dere?"));
					break;
				}
			}
		}

		/// <summary></summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			Invalidate(true);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_chk_send_data = new System.Windows.Forms.CheckBox();
			this.m_lb = new pr.gui.ListBox();
			this.SuspendLayout();
			// 
			// m_chk_send_data
			// 
			this.m_chk_send_data.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_send_data.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_send_data.AutoSize = true;
			this.m_chk_send_data.Location = new System.Drawing.Point(139, 126);
			this.m_chk_send_data.Name = "m_chk_send_data";
			this.m_chk_send_data.Size = new System.Drawing.Size(68, 23);
			this.m_chk_send_data.TabIndex = 0;
			this.m_chk_send_data.Text = "Send Data";
			this.m_chk_send_data.UseVisualStyleBackColor = true;
			// 
			// m_lb
			// 
			this.m_lb.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lb.FormattingEnabled = true;
			this.m_lb.IntegralHeight = false;
			this.m_lb.Location = new System.Drawing.Point(12, 12);
			this.m_lb.Name = "m_lb";
			this.m_lb.Size = new System.Drawing.Size(195, 112);
			this.m_lb.TabIndex = 1;
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(219, 157);
			this.Controls.Add(this.m_lb);
			this.Controls.Add(this.m_chk_send_data);
			this.Name = "MainUI";
			this.Text = "Test Client";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}

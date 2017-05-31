using System;
using System.Windows.Forms;
using pr.util;

namespace CoinFlip
{
	public class MainUI :Form
	{
		#region UI Elements
		private CheckBox m_chk_run;
		#endregion

		public MainUI()
		{
			InitializeComponent();
			Model = new Model(this);
			Heart = new Timer();

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Heart = null;
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
				Util.Dispose(ref m_model);
				m_model = value;
			}
		}
		private Model m_model;

		/// <summary>Heart beat</summary>
		public Timer Heart
		{
			get { return m_heart; }
			private set
			{
				if (m_heart == value) return;
				if (m_heart != null)
				{
					m_heart.Tick -= HeartBeat;
					Util.Dispose(ref m_heart);
				}
				m_heart = value;
				if (m_heart != null)
				{
					m_heart.Tick += HeartBeat;
					m_heart.Interval = 500;
				}
			}
		}
		private Timer m_heart;

		/// <summary>Run the app</summary>
		private void HeartBeat(object sender, EventArgs e)
		{
			Model.Step();
		}

		/// <summary>Wire up the UI</summary>
		private void SetupUI()
		{
			m_chk_run.CheckedChanged += (s,a) =>
			{
				Heart.Enabled = m_chk_run.Checked;
				m_chk_run.Text = m_chk_run.Checked ? "Stop" : "Start";
			};
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_chk_run = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// m_chk_run
			// 
			this.m_chk_run.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_run.Location = new System.Drawing.Point(63, 20);
			this.m_chk_run.Name = "m_chk_run";
			this.m_chk_run.Size = new System.Drawing.Size(96, 44);
			this.m_chk_run.TabIndex = 1;
			this.m_chk_run.Text = "Start";
			this.m_chk_run.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_chk_run.UseVisualStyleBackColor = true;
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(232, 86);
			this.Controls.Add(this.m_chk_run);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MainUI";
			this.Text = "Coin Flip";
			this.ResumeLayout(false);

		}
		#endregion
	}

}

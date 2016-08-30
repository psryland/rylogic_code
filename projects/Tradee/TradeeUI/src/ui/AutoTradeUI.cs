using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace Tradee
{
	public class AutoTradeUI :BaseUI
	{
		#region UI Elements
		private CheckBox m_chk_enable;
		private ToolTip m_tt;
		#endregion

		public AutoTradeUI(MainModel model)
			:base(model, "Auto Trade")
		{
			InitializeComponent();
			DockControl.DefaultDockLocation = new DockContainer.DockLocation(new[] { EDockSite.Left });

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			AutoTrade = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			m_chk_enable.CheckedChanged += (s,a) =>
			{
				// Todo, auto trade all favourite instruments
				if (m_chk_enable.Checked)
				{
					var instr = Model.Favourites.FirstOrDefault();
					AutoTrade = new AutoTrade(instr);
				}
				else
				{
					AutoTrade = null;
				}
			};
		}

		/// <summary>Auto trader</summary>
		public AutoTrade AutoTrade
		{
			get { return m_auto_trade; }
			private set
			{
				if (m_auto_trade == value) return;
				Util.Dispose(ref m_auto_trade);
				m_auto_trade = value;
			}
		}
		private AutoTrade m_auto_trade;

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_chk_enable = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// m_chk_enable
			// 
			this.m_chk_enable.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_enable.AutoSize = true;
			this.m_chk_enable.Location = new System.Drawing.Point(3, 3);
			this.m_chk_enable.Name = "m_chk_enable";
			this.m_chk_enable.Size = new System.Drawing.Size(106, 23);
			this.m_chk_enable.TabIndex = 0;
			this.m_chk_enable.Text = "Enable Auto Trade";
			this.m_chk_enable.UseVisualStyleBackColor = true;
			// 
			// AutoTradeUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoScroll = true;
			this.Controls.Add(this.m_chk_enable);
			this.MinimumSize = new System.Drawing.Size(177, 0);
			this.Name = "AutoTradeUI";
			this.Size = new System.Drawing.Size(238, 356);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}

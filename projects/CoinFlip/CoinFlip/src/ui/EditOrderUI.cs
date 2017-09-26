using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class EditOrderUI :ToolForm
	{
		public EditOrderUI(Control parent)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			HideOnClose = false;
			Icon = (parent as Form)?.Icon;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// EditOrderUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(487, 517);
			this.Name = "EditOrderUI";
			this.Text = "Edit Order";
			this.ResumeLayout(false);

		}
		#endregion
	}
}

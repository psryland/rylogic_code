namespace TestCS
{
	partial class FormTestApp
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.testsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tests_view3d = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_test_colour_wheel = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_hint_balloon = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.testsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(178, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_exit});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(92, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// testsToolStripMenuItem
			// 
			this.testsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tests_view3d,
            this.m_menu_test_colour_wheel,
            this.m_menu_hint_balloon});
			this.testsToolStripMenuItem.Name = "testsToolStripMenuItem";
			this.testsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
			this.testsToolStripMenuItem.Text = "&Tests";
			// 
			// m_menu_tests_view3d
			// 
			this.m_menu_tests_view3d.Name = "m_menu_tests_view3d";
			this.m_menu_tests_view3d.Size = new System.Drawing.Size(152, 22);
			this.m_menu_tests_view3d.Text = "&View3d";
			// 
			// m_menu_test_colour_wheel
			// 
			this.m_menu_test_colour_wheel.Name = "m_menu_test_colour_wheel";
			this.m_menu_test_colour_wheel.Size = new System.Drawing.Size(152, 22);
			this.m_menu_test_colour_wheel.Text = "&ColourWheel";
			// 
			// m_menu_hint_balloon
			// 
			this.m_menu_hint_balloon.Name = "m_menu_hint_balloon";
			this.m_menu_hint_balloon.Size = new System.Drawing.Size(152, 22);
			this.m_menu_hint_balloon.Text = "HintBalloon";
			// 
			// FormTestApp
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(178, 164);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "FormTestApp";
			this.Text = "TestApp";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_exit;
		private System.Windows.Forms.ToolStripMenuItem testsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem m_menu_tests_view3d;
		private System.Windows.Forms.ToolStripMenuItem m_menu_test_colour_wheel;
		private System.Windows.Forms.ToolStripMenuItem m_menu_hint_balloon;
	}
}


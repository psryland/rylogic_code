using System.Windows.Forms;

namespace TestCS
{
	public class FormTestApp : Form
	{
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_exit;
		private ToolStripMenuItem m_menu_tests;
		private ToolStripMenuItem m_menu_tests_view3d;
		private ToolStripMenuItem m_menu_test_colour_wheel;
		private ToolStripMenuItem m_menu_hint_balloon;
		private ToolStripMenuItem m_menu_tool_form;
		private ToolStripMenuItem m_menu_tree_grid;
		private ToolStripMenuItem m_menu_checked_listbox;
		private ToolStripMenuItem m_menu_diagram_control;
		private ToolStripMenuItem m_menu_graph_control;

		public FormTestApp()
		{
			InitializeComponent();

			m_menu_file_exit.Click += (s,a) =>
				{
					Close();
				};

			m_menu_tests_view3d.Click += (s,a) =>
				{
					new FormView3d().Show(this);
				};

			m_menu_test_colour_wheel.Click += (s,a) =>
				{
					new FormColourWheel().Show(this);
				};

			m_menu_hint_balloon.Click += (s,a) =>
				{
					new FormHintBalloon().Show(this);
				};

			m_menu_graph_control.Click += (s,a) =>
				{
					new GraphControlUI().Show(this);
				};

			m_menu_diagram_control.Click += (s,a) =>
				{
					new DiagramControlUI().Show(this);
				};

			m_menu_tool_form.Click += (s,a) =>
				{
					new ToolFormUI(this).Show(this);
				};

			m_menu_tree_grid.Click += (s,a) =>
				{
					new TreeGridUI().Show(this);
				};

			m_menu_checked_listbox.Click += (s,a) =>
				{
					new CheckedListBoxUI().Show(this);
				};
		}

		#region Windows Form Designer generated code

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

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tests = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tests_view3d = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_test_colour_wheel = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_hint_balloon = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_graph_control = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tool_form = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_tree_grid = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_checked_listbox = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_diagram_control = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_menu
			// 
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file,
            this.m_menu_tests});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(178, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "m_menu";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(92, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_menu_tests
			// 
			this.m_menu_tests.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_tests_view3d,
            this.m_menu_test_colour_wheel,
            this.m_menu_hint_balloon,
            this.m_menu_graph_control,
            this.m_menu_diagram_control,
            this.m_menu_tool_form,
            this.m_menu_tree_grid,
            this.m_menu_checked_listbox});
			this.m_menu_tests.Name = "m_menu_tests";
			this.m_menu_tests.Size = new System.Drawing.Size(46, 20);
			this.m_menu_tests.Text = "&Tests";
			// 
			// m_menu_tests_view3d
			// 
			this.m_menu_tests_view3d.Name = "m_menu_tests_view3d";
			this.m_menu_tests_view3d.Size = new System.Drawing.Size(159, 22);
			this.m_menu_tests_view3d.Text = "&View3d";
			// 
			// m_menu_test_colour_wheel
			// 
			this.m_menu_test_colour_wheel.Name = "m_menu_test_colour_wheel";
			this.m_menu_test_colour_wheel.Size = new System.Drawing.Size(159, 22);
			this.m_menu_test_colour_wheel.Text = "&ColourWheel";
			// 
			// m_menu_hint_balloon
			// 
			this.m_menu_hint_balloon.Name = "m_menu_hint_balloon";
			this.m_menu_hint_balloon.Size = new System.Drawing.Size(159, 22);
			this.m_menu_hint_balloon.Text = "HintBalloon";
			// 
			// m_menu_graph_control
			// 
			this.m_menu_graph_control.Name = "m_menu_graph_control";
			this.m_menu_graph_control.Size = new System.Drawing.Size(159, 22);
			this.m_menu_graph_control.Text = "&GraphControl";
			// 
			// m_menu_tool_form
			// 
			this.m_menu_tool_form.Name = "m_menu_tool_form";
			this.m_menu_tool_form.Size = new System.Drawing.Size(159, 22);
			this.m_menu_tool_form.Text = "&Tool Form";
			// 
			// m_menu_tree_grid
			// 
			this.m_menu_tree_grid.Name = "m_menu_tree_grid";
			this.m_menu_tree_grid.Size = new System.Drawing.Size(159, 22);
			this.m_menu_tree_grid.Text = "&Tree Grid";
			// 
			// m_menu_checked_listbox
			// 
			this.m_menu_checked_listbox.Name = "m_menu_checked_listbox";
			this.m_menu_checked_listbox.Size = new System.Drawing.Size(159, 22);
			this.m_menu_checked_listbox.Text = "CheckedListBox";
			// 
			// m_menu_diagram_control
			// 
			this.m_menu_diagram_control.Name = "m_menu_diagram_control";
			this.m_menu_diagram_control.Size = new System.Drawing.Size(159, 22);
			this.m_menu_diagram_control.Text = "&DiagramControl";
			// 
			// FormTestApp
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(178, 164);
			this.Controls.Add(this.m_menu);
			this.MainMenuStrip = this.m_menu;
			this.Name = "FormTestApp";
			this.Text = "TestApp";
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}

namespace imager
{
	partial class DirectoryManager
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DirectoryManager));
			this.m_edit_path = new System.Windows.Forms.TextBox();
			this.m_lbl_path = new System.Windows.Forms.Label();
			this.m_btn_browse = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_list = new System.Windows.Forms.DataGridView();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_add = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_list)).BeginInit();
			this.SuspendLayout();
			// 
			// m_edit_path
			// 
			this.m_edit_path.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_path.Location = new System.Drawing.Point(51, 340);
			this.m_edit_path.Name = "m_edit_path";
			this.m_edit_path.Size = new System.Drawing.Size(291, 20);
			this.m_edit_path.TabIndex = 1;
			// 
			// m_lbl_path
			// 
			this.m_lbl_path.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_path.AutoSize = true;
			this.m_lbl_path.Location = new System.Drawing.Point(13, 343);
			this.m_lbl_path.Name = "m_lbl_path";
			this.m_lbl_path.Size = new System.Drawing.Size(32, 13);
			this.m_lbl_path.TabIndex = 2;
			this.m_lbl_path.Text = "Path:";
			// 
			// m_btn_browse
			// 
			this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse.Location = new System.Drawing.Point(348, 338);
			this.m_btn_browse.Name = "m_btn_browse";
			this.m_btn_browse.Size = new System.Drawing.Size(35, 23);
			this.m_btn_browse.TabIndex = 3;
			this.m_btn_browse.Text = "...";
			this.m_btn_browse.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(302, 367);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(81, 23);
			this.m_btn_ok.TabIndex = 6;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(389, 367);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 8;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_list
			// 
			this.m_list.AllowUserToAddRows = false;
			this.m_list.AllowUserToResizeRows = false;
			this.m_list.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_list.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_list.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_list.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_list.Location = new System.Drawing.Point(12, 12);
			this.m_list.Name = "m_list";
			this.m_list.RowHeadersWidth = 22;
			this.m_list.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_list.Size = new System.Drawing.Size(452, 322);
			this.m_list.TabIndex = 11;
			// 
			// m_image_list
			// 
			this.m_image_list.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.m_image_list.ImageSize = new System.Drawing.Size(16, 16);
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// m_btn_add
			// 
			this.m_btn_add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add.Location = new System.Drawing.Point(389, 338);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(75, 23);
			this.m_btn_add.TabIndex = 12;
			this.m_btn_add.Text = "Add";
			this.m_btn_add.UseVisualStyleBackColor = true;
			// 
			// DirectoryManager
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(476, 398);
			this.Controls.Add(this.m_btn_add);
			this.Controls.Add(this.m_list);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_browse);
			this.Controls.Add(this.m_lbl_path);
			this.Controls.Add(this.m_edit_path);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "DirectoryManager";
			this.Text = "Manage Folders";
			((System.ComponentModel.ISupportInitialize)(this.m_list)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_edit_path;
		private System.Windows.Forms.Label m_lbl_path;
		private System.Windows.Forms.Button m_btn_browse;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.DataGridView m_list;
		private System.Windows.Forms.ImageList m_image_list;
		private System.Windows.Forms.Button m_btn_add;
	}
}
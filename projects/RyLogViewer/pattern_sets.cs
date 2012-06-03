using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.gui;
using pr.util;
using pr.extn;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	public class PatternSets<T> :UserControl where T:Pattern
	{
		public class Set
		{
			public string Name                   { get; set; }
			public string Filepath               { get; set; }
			public Set()                         { Name = ""; Filepath = ""; }
			public Set(string filepath)          { Name = Path.GetFileName(filepath); Filepath = filepath; }
			public XElement ToXml(XElement node) { node.Add(new XElement("name" ,Name), new XElement("filepath" ,Filepath)); return node; }
		}

		private const string PatternSetXmlTag = "patternset";
		private const string PatternSetExtn = @"patternset";
		private const string PatternSetFilter = @"Pattern Set Files (*.patternset)|*.patternset|All files (*.*)|*.*";

		private List<Set> m_sets;
		private List<T> m_current_set;
		private Label m_lbl_pattern_set;
		private Button m_btn_add_set;
		private Button m_btn_del_set;
		private ImageList m_image_list;
		private ToolTip m_tt;
		private Button m_btn_load_set;
		private ComboBox m_combo_recent_sets;
		
		/// <summary>Get/Set the available pattern sets</summary>
		public List<Set> Sets
		{
			get { return m_sets; }
			set
			{
				m_sets = value;
				UpdateUI();
			}
		}

		/// <summary>Get/Set the reference to the current pattern set</summary>
		public List<T> CurrentSet
		{
			get { return m_current_set; }
			set
			{
				m_current_set = value;
				UpdateUI();
			}
		}
		
		// The designer requires a parameterless constructor
		public PatternSets()
		{
			InitializeComponent();
		
			// Available sets combo
			m_combo_recent_sets.SelectedIndexChanged += SelectSet;
			m_combo_recent_sets.DisplayMember = Util<Set>.MemberName(x=>x.Name);
			
			// Save the current patterns list as a set
			m_btn_add_set.Click += (s,a)=>
				{
					PromptForm pf = new PromptForm();
					if (pf.ShowDialog(this) != DialogResult.OK) return;
					
					try
					{
						// Export the current set as xml
						XDocument doc = new XDocument(new XElement("root"));
						foreach (var p in CurrentSet)
							p.ToXml(doc.Root);
					
						// Write the current set to a file in the app path
						string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
						string filepath = Path.Combine(path, pf.Value);
						doc.Save(filepath);
						
						// Add an entry to the pattern set collection
						Sets.Insert(0, new Set(filepath));
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, "Add Pattern Set Failed", "Could not create a pattern set from the current patterns. Error: " + ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				};

			// Load an existing 'Set' into the collection
			m_btn_load_set.Click += (s,a)=>
				{
					// Read a pattern set file name and add it to the combo
					OpenFileDialog fd = new OpenFileDialog{Filter = PatternSetFilter, Multiselect = false};
					if (fd.ShowDialog(this) != DialogResult.OK) return;
					
					// Ensure it's not a duplicate
					foreach (var set in Sets)
						if (string.Compare(set.Filepath, fd.FileName, true) == 0)
							return;
					
					// Insert at the front
					Sets.Insert(0, new Set(fd.FileName));
				};
			
			// Delete the currently selected 'Set' from the collection
			m_btn_del_set.Click += (s,a)=>
				{
					
					DialogResult res = MessageBox.Show(this, "Confirm Delete", "Delete pattern set '"+""+"'?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
					if (res != DialogResult.Yes) return;
					Sets.RemoveAt(m_combo_recent_sets.SelectedIndex);
					UpdateUI();
				};
			
			// ToolTips
			m_tt.SetToolTip(m_combo_recent_sets, "Recent pattern sets. Select to load a pattern set");
			m_tt.SetToolTip(m_btn_add_set, "Add the current list of patterns as a pattern set");
			m_tt.SetToolTip(m_btn_load_set, "Load a pattern set from file");
			m_tt.SetToolTip(m_btn_del_set, "Delete a pattern set from the dropdown list");
		}
		
		/// <summary>Helper for saving</summary>
		private void AddPatterns()
		{
		}

		private void SelectSet(object sender, EventArgs eventArgs)
		{
			// Pop up a menu with options to merge/replace
			ContextMenuStrip menu = new ContextMenuStrip();
			menu.Items.Add("Replace patterns", null, (ss,aa)=>{ m_current_set.Clear(); AddPatterns(); });
			menu.Items.Add("Merge patterns", null, (ss,aa)=>{ AddPatterns(); });
			menu.Show(MousePosition);
		}
			
		private void UpdateUI()
		{
			m_btn_del_set.Enabled = m_combo_recent_sets.SelectedIndex > 0;
			
			// Update the contents of the combo
			int selected = m_combo_recent_sets.SelectedIndex; // Preserve the selected
			m_combo_recent_sets.Items.Clear();
			m_combo_recent_sets.Items.Add(new Set{Name = "(Select Pattern Set)"});
			foreach (var set in Sets)
				m_combo_recent_sets.Items.Add(set);
			m_combo_recent_sets.SelectedIndex = selected;
		}
		
		/// <summary>Generate a list of pattern sets from xml</summary>
		public static List<Set> Import(string pattern_sets)
		{
			List<Set> sets = new List<Set>();
			XDocument doc = XDocument.Parse(pattern_sets);
			if (doc.Root != null)
			{
				foreach (var set in doc.Root.Elements(PatternSetXmlTag))
					sets.Add(new Set(set.Value));
			}
			return sets;
		}
		
		/// <summary>Serialise the pattern sets to xml</summary>
		public static string Export(List<Set> pattern_sets)
		{
			XDocument doc = new XDocument(new XElement("root"));
			if (doc.Root == null) return "";
			
			foreach (var set in pattern_sets)
				doc.Root.Add(set.ToXml(new XElement(PatternSetXmlTag)));
			
			return doc.ToString(SaveOptions.None);
		}

		#region Component Designer generated code
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternSets<T>));
			this.m_lbl_pattern_set = new System.Windows.Forms.Label();
			this.m_btn_add_set = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_del_set = new System.Windows.Forms.Button();
			this.m_combo_recent_sets = new System.Windows.Forms.ComboBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_btn_load_set = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_lbl_pattern_set
			// 
			this.m_lbl_pattern_set.AutoSize = true;
			this.m_lbl_pattern_set.Location = new System.Drawing.Point(3, 12);
			this.m_lbl_pattern_set.Name = "m_lbl_pattern_set";
			this.m_lbl_pattern_set.Size = new System.Drawing.Size(68, 13);
			this.m_lbl_pattern_set.TabIndex = 5;
			this.m_lbl_pattern_set.Text = "Pattern Sets:";
			this.m_lbl_pattern_set.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_btn_add_set
			// 
			this.m_btn_add_set.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add_set.AutoSize = true;
			this.m_btn_add_set.ImageIndex = 2;
			this.m_btn_add_set.ImageList = this.m_image_list;
			this.m_btn_add_set.Location = new System.Drawing.Point(238, 2);
			this.m_btn_add_set.Name = "m_btn_add_set";
			this.m_btn_add_set.Size = new System.Drawing.Size(45, 30);
			this.m_btn_add_set.TabIndex = 4;
			this.m_btn_add_set.UseVisualStyleBackColor = true;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "edit_add.png");
			this.m_image_list.Images.SetKeyName(1, "fileclose.png");
			this.m_image_list.Images.SetKeyName(2, "bookmark_add.png");
			// 
			// m_btn_del_set
			// 
			this.m_btn_del_set.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_del_set.AutoSize = true;
			this.m_btn_del_set.ImageIndex = 1;
			this.m_btn_del_set.ImageList = this.m_image_list;
			this.m_btn_del_set.Location = new System.Drawing.Point(325, 3);
			this.m_btn_del_set.Name = "m_btn_del_set";
			this.m_btn_del_set.Size = new System.Drawing.Size(45, 30);
			this.m_btn_del_set.TabIndex = 3;
			this.m_btn_del_set.UseVisualStyleBackColor = true;
			// 
			// m_combo_recent_sets
			// 
			this.m_combo_recent_sets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_recent_sets.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.m_combo_recent_sets.FormattingEnabled = true;
			this.m_combo_recent_sets.Location = new System.Drawing.Point(77, 8);
			this.m_combo_recent_sets.Name = "m_combo_recent_sets";
			this.m_combo_recent_sets.Size = new System.Drawing.Size(155, 21);
			this.m_combo_recent_sets.TabIndex = 6;
			// 
			// m_btn_load_set
			// 
			this.m_btn_load_set.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_load_set.AutoSize = true;
			this.m_btn_load_set.ImageIndex = 0;
			this.m_btn_load_set.ImageList = this.m_image_list;
			this.m_btn_load_set.Location = new System.Drawing.Point(283, 3);
			this.m_btn_load_set.Name = "m_btn_load_set";
			this.m_btn_load_set.Size = new System.Drawing.Size(42, 30);
			this.m_btn_load_set.TabIndex = 7;
			this.m_btn_load_set.UseVisualStyleBackColor = true;
			// 
			// PatternSets
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.Controls.Add(this.m_btn_load_set);
			this.Controls.Add(this.m_combo_recent_sets);
			this.Controls.Add(this.m_lbl_pattern_set);
			this.Controls.Add(this.m_btn_add_set);
			this.Controls.Add(this.m_btn_del_set);
			this.Name = "PatternSets";
			this.Size = new System.Drawing.Size(373, 36);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
	
}

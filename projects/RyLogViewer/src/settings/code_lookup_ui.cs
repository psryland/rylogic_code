using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using RyLogViewer.Properties;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public class CodeLookupUI :Form
	{
		public class Pair
		{
			public string Key   { get; set; }
			public string Value { get; set; }
			public Pair() {}
			public Pair(string key, string value) { Key = key; Value = value; }
		}
		
		private readonly BindingSource m_src;
		private readonly ToolTip       m_tt;
		
		/// <summary>The code lookup table</summary>
		public List<Pair> Values { get; private set; }
		
		public CodeLookupUI(Dictionary<string, string> values)
		{
			InitializeComponent();
			Values = values.Select(x => new Pair(x.Key, x.Value)).ToList();
			m_src  = new BindingSource{DataSource = Values, AllowNew = true, Sort = "Key"};
			m_tt   = new ToolTip();
			
			// Lookup table
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn {Name = "Code"  ,HeaderText = "Code"  ,DataPropertyName = "Key"   ,FillWeight = 1});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn {Name = "Value" ,HeaderText = "Value" ,DataPropertyName = "Value" ,FillWeight = 2});
			m_grid.DataError += (s,a) => a.Cancel = true;
			m_grid.DataSource = m_src;
			
			// Export
			m_btn_export.ToolTip(m_tt, "Export the code/value list to file");
			m_btn_export.Click += (s,a) => ExportCodeLookupList();
			
			// Import
			m_btn_import.ToolTip(m_tt, "Import code/value pairs from file");
			m_btn_import.Click += (s,a) => ImportCodeLookupList();
		}
		
		/// <summary>Export the current code lookup list to a file</summary>
		private void ExportCodeLookupList()
		{
			// Prompt for the file to save
			var dg = new SaveFileDialog {Title = "Export code lookup list", Filter = Resources.XmlOrCsvFileFilter};
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			
			try
			{
				string extn = Path.GetExtension(dg.FileName);
				extn = extn != null ? extn.Trim(new[]{'.'}) : "";
				
				// If a csv file is being saved, write out csv
				if (string.CompareOrdinal(extn, "csv") == 0)
				{
					// Create a CSV object
					var csv = new CSVData{AutoSize = true};
					csv.Reserve(Values.Count,2);
					foreach (var c in Values)
					{
						CSVData.Row row = new CSVData.Row();
						row.Add(c.Key);
						row.Add(c.Value);
						csv.Add(row);
					}
					csv.Save(dg.FileName);
				}
				// Otherwise write out xml
				else
				{
					XDocument doc = new XDocument(new XElement(XmlTag.Root));
					if (doc.Root == null) throw new Exception("Failed to create root xml node");
					var codes = new XElement(XmlTag.CodeValues);
					foreach (var v in Values)
						codes.Add(new XElement(XmlTag.CodeValue,
							new XElement(XmlTag.Code , v.Key),
							new XElement(XmlTag.Value, v.Value)
							));
					doc.Root.Add(codes);
					doc.Save(dg.FileName, SaveOptions.None);
				}
			}
			catch (Exception ex)
			{
				Misc.ShowErrorMessage(this, ex, "Export failed.", "Export Failed");
			}
		}

		/// <summary>Import the code lookup list from a file</summary>
		private void ImportCodeLookupList()
		{
			// If there's an existing list, ask before clearing it
			if (Values.Count != 0)
			{
				var res = MessageBox.Show(this, "Replace the current list with code/value pairs loaded from file?", "Confirm Replace Codes", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if (res != DialogResult.OK) return;
			}
			
			// Prompt for the file to load
			var dg = new OpenFileDialog {Title = "Import code lookup list", Filter = Resources.XmlOrCsvFileFilter};
			if (dg.ShowDialog(this) != DialogResult.OK) return;
			
			try
			{
				string extn = Path.GetExtension(dg.FileName);
				extn = extn != null ? extn.Trim(new[]{'.'}) : "";
				
				// Load into a temp list
				var values = new List<Pair>();
				bool partial_import = false;
					
				// Load from csv
				if (string.CompareOrdinal(extn, "csv") == 0)
				{
					var csv = CSVData.Load(dg.FileName);
					foreach (var row in csv.Rows)
					{
						partial_import |= row.Count != 0 && row.Count != 2;
						if (row.Count < 2) continue;
						values.Add(new Pair(row[0], row[1]));
					}
				}
				// Load from xml
				else
				{
					XDocument doc = XDocument.Load(dg.FileName);
					if (doc.Root == null) throw new Exception("XML file invalid, no root node found");
					var codes = doc.Root.Element(XmlTag.CodeValues);
					if (codes == null) throw new Exception("XML file invalid, no 'codevalues' element found");
					foreach (var code in codes.Elements(XmlTag.CodeValue))
					{
						var cnode = code.Element(XmlTag.Code);
						var vnode = code.Element(XmlTag.Value);
						if (cnode != null && vnode != null)
							values.Add(new Pair(cnode.Value, vnode.Value));
						else
							partial_import = true;
					}
				}
				
				// If errors where found during the import, ask the user if they still want to continue
				if (partial_import)
				{
					var res = MessageBox.Show(this, "Some imported data was ignored because it was invalid.\r\nContinue with import?", "Partial Import", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
					if (res != DialogResult.OK) return;
				}
				
				// If successful, replace the existing list
				Values.Clear();
				Values.AddRange(values);
				m_src.ResetBindings(false);
			}
			catch (Exception ex)
			{
				Misc.ShowErrorMessage(this, ex, "Import failed.", "Import Failed");
			}
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
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_grid = new DataGridView();
			this.m_btn_import = new System.Windows.Forms.Button();
			this.m_btn_export = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(257, 167);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 22;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Location = new System.Drawing.Point(5, 5);
			this.m_grid.Name = "m_grid";
			this.m_grid.RowHeadersWidth = 28;
			this.m_grid.RowTemplate.Height = 18;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(335, 153);
			this.m_grid.TabIndex = 24;
			// 
			// m_btn_import
			// 
			this.m_btn_import.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_import.Location = new System.Drawing.Point(93, 167);
			this.m_btn_import.Name = "m_btn_import";
			this.m_btn_import.Size = new System.Drawing.Size(75, 23);
			this.m_btn_import.TabIndex = 25;
			this.m_btn_import.Text = "&Import";
			this.m_btn_import.UseVisualStyleBackColor = true;
			// 
			// m_btn_export
			// 
			this.m_btn_export.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_export.Location = new System.Drawing.Point(12, 167);
			this.m_btn_export.Name = "m_btn_export";
			this.m_btn_export.Size = new System.Drawing.Size(75, 23);
			this.m_btn_export.TabIndex = 26;
			this.m_btn_export.Text = "&Export";
			this.m_btn_export.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(176, 167);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 29;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// CodeLookupUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(344, 202);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_export);
			this.Controls.Add(this.m_btn_import);
			this.Controls.Add(this.m_grid);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(360, 122);
			this.Name = "CodeLookupUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Configure Code Lookup";
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Button m_btn_ok;
		private DataGridView m_grid;
		private Button m_btn_import;
		private Button m_btn_export;
		private Button m_btn_cancel;
	}
}

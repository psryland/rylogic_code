using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.gui;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	internal abstract partial class PatternSetUi :UserControl
	{
		[Serializable]
		public class Set
		{
			public string Name { get; set; }
			public string Filepath { get; set; }
			public Set() { Name = ""; Filepath = ""; }
			public Set(string filepath) { Name = Path.GetFileName(filepath); Filepath = filepath; }
			public XElement ToXml(XElement node) { node.Add(new XElement("name" ,Name), new XElement("filepath" ,Filepath)); return node; }
		}

		protected const string PatternSetXmlTag = "patternset";
		protected const string PatternSetExtn = @"patternset";
		protected const string PatternSetFilter = @"Pattern Set Files (*.patternset)|*.patternset|All files (*.*)|*.*";
		private readonly ToolTip m_tt;
		protected readonly List<Set> m_sets;
		protected Settings m_settings;

		protected PatternSetUi()
		{
			InitializeComponent();
			m_tt = new ToolTip();
			m_sets = new List<Set>();
			UpdateUI();
			
			// Combo
			m_combo_sets.DisplayMember = Util<Set>.MemberName(x=>x.Name);
			m_combo_sets.SelectedIndex = 0;
			
			// Save the current list of patterns as a pattern set
			m_btn_save.Click += (s,a)=>
				{
					// Ask for a name for the set
					SaveFileDialog fd = new SaveFileDialog{Filter = PatternSetFilter, CreatePrompt = false, OverwritePrompt = true};
					if (fd.ShowDialog(this) != DialogResult.OK) return;
			
					try
					{
						// Export the current patterns as xml
						XDocument doc = new XDocument(new XElement("root"));
						if (doc.Root == null) throw new ApplicationException("failed to add root xml element");
						CurrentSetToXml(doc.Root);
					
						// Write the current set to a file in the app path
						doc.Save(fd.FileName);
						
						// Add an entry to the pattern set collection
						Set set = new Set(fd.FileName);
						m_sets.RemoveAll(ps => ps.Name == set.Name);
						m_sets.Insert(0, set);
						m_settings.Save();
						UpdateUI();
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, "Could not create a pattern set from the current patterns. Error: " + ex.Message, "Add Pattern Set Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				};

			// ToolTips
			m_tt.SetToolTip(m_combo_sets ,"Recent pattern sets. Select to load or merge a pattern set");
			m_tt.SetToolTip(m_btn_save   ,"Save the current list of patterns as a pattern set");
			m_tt.SetToolTip(m_btn_load   ,"Load a pattern set from file");
			m_tt.SetToolTip(m_btn_del    ,"Delete the current pattern set from the dropdown list");
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected abstract void CurrentSetToXml(XElement parent);
		
		/// <summary>Update UI elements based on current state</summary>
		private void UpdateUI()
		{
			m_btn_del.Enabled = m_combo_sets.SelectedIndex > 0;
			
			m_combo_sets.Items.Clear();
			m_combo_sets.Items.Add(new Set{Name = "(Select Pattern Set)"});
			foreach (var set in m_sets)
				m_combo_sets.Items.Add(set);
		}
		
		/// <summary>Generate a list of pattern sets from xml</summary>
		protected void Import(string pattern_sets)
		{
			m_sets.Clear();
			XDocument doc = XDocument.Parse(pattern_sets);
			if (doc.Root != null)
			{
				foreach (var set in doc.Root.Elements(PatternSetXmlTag))
					m_sets.Add(new Set(set.Value));
			}
		}
		
		/// <summary>Serialise the pattern sets to xml</summary>
		protected string Export()
		{
			XDocument doc = new XDocument(new XElement("root"));
			if (doc.Root == null) return "";
			
			foreach (var set in m_sets)
				doc.Root.Add(set.ToXml(new XElement(PatternSetXmlTag)));
			
			return doc.ToString(SaveOptions.None);
		}
	}

	// Specific instances for each pattern type
	internal class PatternSetHL :PatternSetUi
	{
		/// <summary>A reference to the current set of highlight patterns</summary>
		public List<Highlight> CurrentSet { get; private set; }
		
		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<Highlight> highlights)
		{
			m_settings = settings;
			m_settings.SettingsSaving += (s,a)=>{ m_settings.HighlightPatternSets = Export(); };
			CurrentSet = highlights;
			Import(settings.HighlightPatternSets);
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.Highlight)));
		}
	}
	internal class PatternSetFT :PatternSetUi
	{
		/// <summary>A reference to the current set of filter patterns</summary>
		public List<Filter> CurrentSet { get; private set; }

		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<Filter> filters)
		{
			m_settings = settings;
			m_settings.SettingsSaving += (s,a)=>{ m_settings.FilterPatternSets = Export(); };
			CurrentSet = filters;
			Import(settings.FilterPatternSets);
		}
		
		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.Highlight)));
		}
	}
}

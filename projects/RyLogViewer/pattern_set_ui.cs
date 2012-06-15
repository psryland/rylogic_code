using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.extn;
using pr.util;
using RyLogViewer.Properties;

namespace RyLogViewer
{
	internal abstract partial class PatternSetUi :UserControl
	{
		[Serializable] public class Set
		{
			public string Filepath { get; set; }
			public string Name { get { return Path.GetFileNameWithoutExtension(Filepath); } }
			public Set()                { Filepath = ""; }
			public Set(string filepath) { Filepath = filepath; }
			public Set(XElement node)
			{
				// ReSharper disable PossibleNullReferenceException
				Filepath = node.Element(XmlTag.Filepath).Value;
				// ReSharper restore PossibleNullReferenceException
			}
			public XElement ToXml(XElement node)
			{
				node.Add
				(
					new XElement(XmlTag.Filepath ,Filepath)
				);
				return node;
			}
		}

		protected const string PatternSetXmlTag = "patternset";
		private readonly ToolTip m_tt;
		protected readonly List<Set> m_sets;
		protected Settings m_settings;

		/// <summary>Raised when the list of current patterns is changed</summary>
		public event EventHandler CurrentSetChanged;
		protected void RaiseCurrentSetChanged()
		{
			if (CurrentSetChanged != null)
				CurrentSetChanged(this, EventArgs.Empty);
		}
		
		protected PatternSetUi()
		{
			InitializeComponent();
			m_tt = new ToolTip();
			m_sets = new List<Set>();
			UpdateUI();
			
			// Combo
			m_combo_sets.ToolTip(m_tt, "Select a pattern set from this list to merge with or replace the existing patterns");
			m_combo_sets.DisplayMember = Util<Set>.MemberName(x=>x.Name);
			m_combo_sets.SelectedIndex = 0;
			m_combo_sets.SelectedIndexChanged += (s,a)=>
				{
					if (m_combo_sets.SelectedIndex <= 0) return;
					Set set = (Set)m_combo_sets.SelectedItem;
					
					// Pop up a menu with options to merge/replace
					ContextMenuStrip menu = new ContextMenuStrip();
					menu.Items.Add("Replace patterns"  , null, (ss,aa)=>{ ClearPatterns(); AddPatternSet(set); });
					menu.Items.Add("Merge patterns"    , null, (ss,aa)=>{ AddPatternSet(set); });
					menu.Items.Add(new ToolStripSeparator());
					menu.Items.Add("Delete Pattern Set", null, (ss,aa)=>{ DeletePatternSet(set); });
					menu.Show(MousePosition);
				};
			
			// Save the current list of patterns as a pattern set
			m_btn_save.ToolTip(m_tt, "Save the current list of patterns as a pattern set");
			m_btn_save.Click += (s,a)=>
				{
					// Ask for a name for the set
					SaveFileDialog fd = new SaveFileDialog{Filter = PatternSetFilter, CreatePrompt = false, OverwritePrompt = true};
					if (fd.ShowDialog(this) != DialogResult.OK) return;
			
					try
					{
						// Export the current patterns as xml
						XDocument doc = new XDocument(new XElement(XmlTag.Root));
						if (doc.Root == null) throw new ApplicationException("failed to add root xml element");
						CurrentSetToXml(doc.Root);
					
						// Write the current set to a file in the app path
						doc.Save(fd.FileName);
						
						// Add an entry to the pattern set collection
						Set set = new Set(fd.FileName);
						m_sets.RemoveAll(ps => ps.Name == set.Name);
						m_sets.Insert(0, set);
						UpdateUI();
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, string.Format(Resources.CreatePatternSetFailedMsg, ex.Message), Resources.CreatePatternSetFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				};
			
			// Load a pattern set from file
			m_btn_load.ToolTip(m_tt, "Load a pattern set from file");
			m_btn_load.Click += (s,a)=>
				{
					// Ask for the file location
					OpenFileDialog fd = new OpenFileDialog{Filter = PatternSetFilter, CheckFileExists = true};
					if (fd.ShowDialog(this) != DialogResult.OK) return;
					m_sets.Add(new Set(fd.FileName));
				};
		}

		/// <summary>Return the pattern set filter</summary>
		protected abstract string PatternSetFilter { get; }

		/// <summary>Add the patterns in 'set' to the current list</summary>
		private void AddPatternSet(Set set)
		{
			try
			{
				XDocument doc = XDocument.Load(set.Filepath);
				if (doc.Root == null) throw new InvalidDataException("root xml node not found");
				MergePatterns(doc.Root);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, string.Format(Resources.LoadPatternSetFailedMsg, ex.Message), Resources.LoadPatternSetFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		
		/// <summary>Remove a pattern set from the list</summary>
		private void DeletePatternSet(Set set)
		{
			DialogResult res = MessageBox.Show(this, string.Format(Resources.DeletePatternSetFileX, set.Filepath), Resources.ConfirmDelete, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			if (res != DialogResult.Yes) return;
			try
			{
				File.Delete(set.Filepath);
				m_sets.Remove(set);
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, string.Format(Resources.CouldNotDeletePatternSetFileX, set.Filepath ,ex.Message), Resources.DeleteFailed, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			m_combo_sets.SelectedIndex = 0;
			UpdateUI();
		}
		
		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected abstract void CurrentSetToXml(XElement parent);
		
		/// <summary>Clear the current set of patterns</summary>
		protected abstract void ClearPatterns();
		
		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected abstract void MergePatterns(XElement node); 
		
		/// <summary>Update UI elements based on current state</summary>
		protected void UpdateUI()
		{
			m_combo_sets.Items.Clear();
			m_combo_sets.Items.Add(new Set(@"x:\(Select Pattern Set).tmp"));
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
					try { m_sets.Add(new Set(set)); } catch {} // Ignore those that fail
			}
		}
		
		/// <summary>Serialise the pattern sets to xml</summary>
		protected string Export()
		{
			XDocument doc = new XDocument(new XElement(XmlTag.Root));
			if (doc.Root == null) return "";
			
			foreach (var set in m_sets)
				doc.Root.Add(set.ToXml(new XElement(PatternSetXmlTag)));
			
			return doc.ToString(SaveOptions.None);
		}
	}

	
	/// <summary>Highlight specific instance of the pattern set control</summary>
	internal class PatternSetHL :PatternSetUi
	{
		private const string HLPatternSetExtn   = @"rylog_highlights";
		private const string HLPatternSetFilter = @"Highlight Pattern Set Files (*."+HLPatternSetExtn+")|*."+HLPatternSetExtn+"|All files (*.*)|*.*";
		
		/// <summary>A reference to the current set of highlight patterns</summary>
		public List<Highlight> CurrentSet { get; private set; }
		
		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<Highlight> highlights)
		{
			m_settings = settings;
			m_settings.SettingsSaving += (s,a)=>{ m_settings.HighlightPatternSets = Export(); };
			Import(settings.HighlightPatternSets);
			CurrentSet = highlights;
			UpdateUI();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return HLPatternSetFilter; }
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.Highlight)));
		}
		
		/// <summary>Clear the current set of patterns</summary>
		protected override void ClearPatterns()
		{
			if (CurrentSet.Count == 0) return;
			CurrentSet.Clear();
			RaiseCurrentSetChanged();
		}
		
		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected override void MergePatterns(XElement node)
		{
			bool some_added = false;
			foreach (XElement n in node.Elements(XmlTag.Highlight))
				try { some_added |= CurrentSet.AddIfUnique(new Highlight(n)); } catch {} // Ignore those that fail
			if (some_added) RaiseCurrentSetChanged();
		}
	}
	
	
	/// <summary>Filter specific instance of the pattern set control</summary>
	internal class PatternSetFT :PatternSetUi
	{
		protected const string FTPatternSetExtn   = @"rylog_filters";
		protected const string FTPatternSetFilter = @"Filter Pattern Set Files (*."+FTPatternSetExtn+")|*."+FTPatternSetExtn+"|All files (*.*)|*.*";
		
		/// <summary>A reference to the current set of filter patterns</summary>
		public List<Filter> CurrentSet { get; private set; }

		/// <summary>Initialise the control</summary>
		internal void Init(Settings settings, List<Filter> filters)
		{
			m_settings = settings;
			m_settings.SettingsSaving += (s,a)=>{ m_settings.FilterPatternSets = Export(); };
			Import(settings.FilterPatternSets);
			CurrentSet = filters;
			UpdateUI();
		}

		/// <summary>Return the pattern set filter</summary>
		protected override string PatternSetFilter
		{
			get { return FTPatternSetFilter; }
		}

		/// <summary>Save the current list of patterns as an xml child of 'parent'</summary>
		protected override void CurrentSetToXml(XElement parent)
		{
			foreach (var p in CurrentSet)
				parent.Add(p.ToXml(new XElement(XmlTag.Filter)));
		}
		
		/// <summary>Clear the current set of patterns</summary>
		protected override void ClearPatterns()
		{
			if (CurrentSet.Count == 0) return;
			CurrentSet.Clear();
			RaiseCurrentSetChanged();
		}
		
		/// <summary>Add the patterns in 'node' to the current list</summary>
		protected override void MergePatterns(XElement node)
		{
			bool some_added = false;
			foreach (XElement n in node.Elements(XmlTag.Highlight))
				try { some_added |= CurrentSet.AddIfUnique(new Filter(n)); } catch {} // Ignore those that fail
			if (some_added) RaiseCurrentSetChanged();
		}
	}
}

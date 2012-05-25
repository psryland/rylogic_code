//***************************************************
// RecentFiles
//  Copyright © Rylogic Ltd 2009
//***************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace pr.gui
{
	// Usage:
	//	Create a Properties.Settings instance
	//	Create a pr.gui.RecentFiles instance
	//	Add a menu item 'm_menu_file_recent'
	// e.g.
	//	private readonly Settings m_app_settings;
	//	private readonly RecentFiles m_recent_files;
	// Constructer:
	//	m_recent_files = new RecentFiles(m_menu_file_recent, delegate (string file) { LoadFile(file); });
	//	m_recent_files.Import(m_app_settings.RecentFiles);
	// FileOpen/Save/SaveAs:
	//	m_recent_files.Add(filename);
	//	m_app_settings.RecentFiles = m_recent_files.Export();
	//	m_app_settings.Save();
	// Shutdown:
	//	m_app_settings.RecentFiles = m_recent_files.Export();
	//	m_app_settings.Save();
	public class RecentFiles
	{
		private readonly List<string> m_files = new List<string>();
		private ToolStripMenuItem m_menu = null;
		private OnClickHandler m_on_click = null;

		public delegate void OnClickHandler(string file);

		public RecentFiles() { MaxCount = 10; }
		public RecentFiles(ToolStripMenuItem menu, OnClickHandler on_click) :this() { SetTargetMenu(menu, on_click); }
		
		/// <summary>Get/Set the limit for how many files appear in the recent files list</summary>
		public int MaxCount {get;set;}

		/// <summary>Access to the list of recent files</summary>
		public List<string> Files
		{
			get { return m_files; }
		}

		/// <summary>Set the menu we're populating with the recent files</summary>
		public void SetTargetMenu(ToolStripMenuItem menu, OnClickHandler on_click)
		{
			m_menu = menu;
			m_on_click = on_click;
		}

		/// <summary>Reset the recent files list</summary>
		public void Clear()
		{
			m_files.Clear();
			if (m_menu != null) m_menu.DropDownItems.Clear();
		}
		
		/// <summary>Add a file to the recent files list</summary>
		public void Add(string file, bool update_menu)
		{
			file.ToLower();
			m_files.Remove(file);
			m_files.Insert(0, file);
			if (m_files.Count > MaxCount) m_files.RemoveAt(m_files.Count - 1);
			if (update_menu) UpdateMenu();
		}
		public void Add(string file)
		{
			Add(file, true);
		}

		/// <summary>Update the recent files menu</summary>
		public void UpdateMenu()
		{
			if (m_menu == null || m_on_click == null) return;
			m_menu.DropDownItems.Clear();
			foreach (string s in m_files)
				m_menu.DropDownItems.Add(new ToolStripMenuItem(s, null, delegate (object sender, EventArgs e)
				{
					ToolStripMenuItem menu = (ToolStripMenuItem)sender;
					Add(menu.Text, false);
					m_on_click(menu.Text);
				}));
		}

		/// <summary>Export recent files to a single string</summary>
		public string Export()
		{
			StringBuilder str = new StringBuilder();
			foreach (string s in m_files) str.Append(s).Append(",");
			if (str.Length != 0) str.Remove(str.Length - 1, 1);
			return str.ToString();
		}

		/// <summary>Import recent files from a single string</summary>
		public void Import(string str)
		{
			foreach (string s in str.Split(','))
				if (!string.IsNullOrEmpty(s))
					Add(s, false);
			
			m_files.Reverse();
			UpdateMenu();
		}
	}
}

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.util;

namespace pr
{
	public sealed partial class Grapher :Form
	{
		public struct Block
		{
			public DataGridView m_grid;
			public Range m_range;
			public int m_column;
			public string Description { get { return m_grid.Columns[m_column].HeaderText + " : [" + m_range.Begin + "->" + m_range.End + "]"; } }
		}
		private readonly RecentFiles  m_recent_files; // Recent files
		private readonly List<Plot>   m_plot;         // Collection of graph windows
		private readonly List<Series> m_data;         // Collection of graph data
		private readonly FileWatch    m_watcher;      // Watch the source file

		public Grapher()
		{
			DoubleBuffered = true;
			InitializeComponent();
			m_recent_files = new RecentFiles(m_menu_file_recent_files, LoadFile);
			m_recent_files.Import(Settings.RecentFiles);
			m_plot = new List<Plot>();
			m_data = new List<Series>();
			m_watcher = new FileWatch();

			m_menu_file_open.Click += delegate { LoadFile(); };
			m_menu_exit.Click += delegate { Close(); };
			m_menu_add_plot.Click += delegate { AddPlot(); };
			m_menu_data_add_series.Click += delegate { AddSeries(); };
			m_menu_data_show_series_list.Click += delegate { ShowSeriesList(); };

			// Persist the window position
			StartPosition   = FormStartPosition.Manual;
			Bounds          = Settings.WindowPosition;
			LocationChanged += delegate { if (WindowState != FormWindowState.Minimized) {Settings.WindowPosition = Bounds; Settings.Save();} };
			ResizeEnd       += delegate { Settings.WindowPosition = Bounds; Settings.Save(); };

			// Check for changed files
			GotFocus += delegate { m_watcher.CheckForChangedFiles(); };

			m_grid.AutoGenerateColumns = false;
			m_grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			m_grid.KeyDown += DataGridView_Extensions.Delete;
			m_grid.KeyDown += DataGridView_Extensions.PasteGrow;
			m_grid.KeyDown += DataGridView_Extensions.CutCopyPasteReplace;
			m_grid.RowPrePaint += RowPrePaint;
			m_grid.ColumnAdded += OnColumnAdded;
			m_grid.MouseClick += OnClick;
			m_grid.ColumnHeaderMouseClick += OnColumnHeaderClicked;
			
			Load += delegate 
			{
				m_grid.ColumnCount = 2;
				m_grid.RowCount = 10;
				m_grid.Columns[0].Width = 40;
				m_grid.Columns[0].HeaderText = "Index";
				m_grid.ClearSelection();

				// Add a default plot
				Plot p = new Plot();
				p.Graph.SetLabels("Plot 0", "", "");
				m_plot.Add(p);
				UpdateGraphMenu();
			};
		}

		// Add a new plot window
		private void AddPlot()
		{
			AddPlot d = new AddPlot("Plot "+m_plot.Count, "", "");
			if (d.ShowDialog() != DialogResult.OK) return;
			m_plot.Add(d.NewPlot());
			UpdateGraphMenu();
		}

		// Update the graphs menu to the current list
		private void UpdateGraphMenu()
		{
			m_menu_graphs.DropDownItems.Clear();
			m_menu_graphs.DropDownItems.Add(m_menu_add_plot);
			m_menu_graphs.DropDownItems.Add(m_menu_file_separator1);
			foreach (Plot p in m_plot)
			{
				Plot local_p = p;
				ToolStripMenuItem m = new ToolStripMenuItem{Text=p.Graph.Title};
				m.Click += delegate { local_p.Show(); };
				m_menu_graphs.DropDownItems.Add(m);
			}
		}

		// Add a series to the series collection based on a region selected in the grid
		private void AddSeries()
		{
			// Check there's at least one plot window to use
			if (m_plot.Count == 0)
			{
				MessageBox.Show(this, "Please create a plot window first", "No Plot Windows", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// Find the selected regions of the grid
			List<Block> blocks = FindContiguousBlocks();
			if (blocks.Count == 0) return;

			// Prompt for more info about the series to add
			AddSeries d = new AddSeries(m_plot, blocks);
			if (d.ShowDialog() != DialogResult.OK) return;

			// Create the series
			Series s = d.NewSeries(m_grid);
			m_data.Add(s);

			// Add it to the selected plot
			d.Plot.Graph.Data.Add(s.GraphSeries);
			d.Plot.Graph.FindDefaultRange();
			if (d.Plot.Graph.Data.Count == 1)
			{
				d.Plot.Graph.ResetToDefaultRange();
				Point location = Location;
				location.Offset(Size.Width, 0);
				d.Plot.Location = location;
			}
			d.Plot.Graph.Refresh();
			d.Plot.Show();
		}

		// Display a list of the current series
		private void ShowSeriesList()
		{
			Form f = new Form{Text="Series:", Size=new Size(200, 400), FormBorderStyle=FormBorderStyle.SizableToolWindow};
			ListBox list = new ListBox();
			foreach (Series s in m_data) list.Items.Add(s.GraphSeries.Name);
			//list.KeyDown += delegate (object i, KeyEventArgs e)
			//{
			//    if (e.KeyCode != Keys.Delete) return;
			//    foreach (if (list.SelectedItems

			//};
			f.Controls.Add(list);
			f.ShowDialog();
			// on delete, search all plots for the deleted series and remove it
		}

		// Search the selected cells for rectangular regions with two or more selected cells
		private List<Block> FindContiguousBlocks()
		{
			List<Block> blocks = new List<Block>();
	
			foreach (DataGridViewCell cell in m_grid.SelectedCells)
			{
				if (cell.Tag != null) continue;

				// Find the range of selected cells
				Block block = new Block{m_grid = m_grid};
				block.m_column = cell.ColumnIndex;
				block.m_range = new Range{Begin=cell.RowIndex,End=cell.RowIndex+1};
				for (int i = cell.RowIndex-1; i >= 0               && m_grid[cell.ColumnIndex, i].Selected; --i) { block.m_range.Begin--;	m_grid[cell.ColumnIndex, i].Tag = 1; }
				for (int i = cell.RowIndex+1; i <  m_grid.RowCount && m_grid[cell.ColumnIndex, i].Selected; ++i) { block.m_range.End++;	m_grid[cell.ColumnIndex, i].Tag = 1; }
				if (block.m_range.Count <= 1) continue;

				blocks.Add(block);
			}

			// Reset the tags
			foreach (DataGridViewCell cell in m_grid.SelectedCells)
				cell.Tag = null;

			// Sort by column then starting row
			blocks.Sort(delegate (Block lhs, Block rhs)
			{
				return
					lhs.m_column < rhs.m_column ? -1 :
					lhs.m_column > rhs.m_column ?  1 :
					lhs.m_range.Begin < rhs.m_range.Begin ? -1 :
					lhs.m_range.Begin > rhs.m_range.Begin ?  1 : 0;
			});

			return blocks;
		}

		// Load a source data file
		private void LoadFile()
		{
			OpenFileDialog fd = new OpenFileDialog();
			fd.Filter = "CSV File|*.csv";
			if (fd.ShowDialog() != DialogResult.OK) return;
			LoadFile(fd.FileName);
		}

		// Load a source data file
		private void LoadFile(string filename)
		{
			m_recent_files.Add(filename);
			Settings.RecentFiles = m_recent_files.Export();
			Settings.Save();

			m_watcher.RemoveAll();
			m_watcher.Add(filename, OnSourceFileChanged, 0, null);

			CSVData csv = CSVData.Load(filename);
			SetData(csv);
		}

		// Handle the source file changing externally
		private bool OnSourceFileChanged(string filepath, object ctx)
		{
			if (Settings.AutoReload)
			{
				DialogResult res = MessageBox.Show(this, "File '"+filepath+"' has changed. Reload file and regenerate plots?", "Source file changed", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (res == DialogResult.No) return true;
			}
		
			//// Check each series and expand/contract ranges
			//foreach (Series s in m_data)
			//{
			//    s.
			//}
			return true;
		}

		// Populate the grid from csv data
		private void SetData(CSVData csv)
		{
			// Find the max dimensions of the csv data and set the grid size
			int sizex = 0, sizey = csv.RowCount;
			foreach (var x in csv.Rows) if (x.Count > sizex) sizex = x.Count;

			m_grid.SuspendLayout();
			m_grid.Columns.Clear();
			m_grid.ColumnCount = sizex + 1;
			m_grid.Rows.Clear();
			m_grid.RowCount = sizey;
			
			for (int j = 0; j != csv.RowCount; ++j)
			{
				for (int i = 0; i != csv.Rows[j].Count; ++i)
				{
					m_grid[i+1, j].Value = csv[j, i];
				}
			}
			
			m_grid.Columns[0].Width = 40;
			m_grid.ClearSelection();
			m_grid.ResumeLayout();
		}

		// Set the row index
		private void RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
		{
			DataGridView grid = (DataGridView)sender;
			grid[0, e.RowIndex].Value = e.RowIndex.ToString();
		}

		// A column has been added
		private void OnColumnAdded(object i, DataGridViewColumnEventArgs e)
		{
			e.Column.SortMode = DataGridViewColumnSortMode.NotSortable;
			if (e.Column.Index == 0)
			{
				e.Column.HeaderText = "Index";
				e.Column.Width = 40;
			}
			else
			{
				e.Column.HeaderText = char.ToString((char)(65 + e.Column.Index - 1));
			}
		}

		// Called when the user clicks a column in the grid.
		void OnColumnHeaderClicked(object sender, DataGridViewCellMouseEventArgs e)
		{
			for (int i = 0; i != m_grid.RowCount; ++i)
				m_grid[e.ColumnIndex, i].Selected = true;
		}

		// On mouse grid within the grid
		private void OnClick(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right) return;
			
			// Create a context menu
			ContextMenu menu = new ContextMenu();
			{
				MenuItem item = new MenuItem{Text="Add Series", Enabled=m_grid.SelectedCells.Count >= 2};
				item.Click += delegate { AddSeries(); };
				menu.MenuItems.Add(item);
			}
			menu.Show(m_grid, e.Location, LeftRightAlignment.Left);
		}
	}
}

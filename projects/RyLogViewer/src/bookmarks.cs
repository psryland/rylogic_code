using System.Windows.Forms;
using pr.common;
using pr.extn;

namespace RyLogViewer
{
	/// <summary>A saved location within the file</summary>
	public class Bookmark
	{
		public Range  Range    { get; set; }
		public string Text     { get; set; }
		public long   Position { get { return Range.Begin; } }
		
		public Bookmark(Range range, string text)
		{
			Range = range;
			Text  = text;
		}
	}

	public partial class Main
	{
		private readonly BindingSource m_bookmarks;
		
		/// <summary>Show the bookmarks dialog</summary>
		private void ShowBookmarksDialog()
		{
			// Display the bookmarks window
			m_bookmarks_ui.Display();
		}

		/// <summary>Add or remove a bookmark at 'row_index'</summary>
		private void ToggleBookmark(int row_index)
		{
			if (row_index < 0 || row_index >= m_line_index.Count) return;
			var line = m_line_index[row_index];
			
			// Find a bookmark whose Position is contained in the range of row 'row_index'
			int count = m_bookmarks.List.RemoveIf<Bookmark>(x => line.Contains(x.Range.Begin));
			
			// If no bookmarks where removed, add 'row_index' as a bookmark
			if (count == 0)
				m_bookmarks.Add(new Bookmark(line, ReadLine(row_index).RowText));
		}

		/// <summary>Scroll to the next bookmark</summary>
		private void NextBookmark()
		{
			if (m_bookmarks.Count == 0) return;
			if (m_bookmarks.Position + 1 == m_bookmarks.Count)
				m_bookmarks.MoveFirst();
			else
				m_bookmarks.MoveNext();
		}

		/// <summary>Scroll to the previous bookmark</summary>
		private void PrevBookmark()
		{
			if (m_bookmarks.Count == 0) return;
			if (m_bookmarks.Position == 0)
				m_bookmarks.MoveLast();
			else
				m_bookmarks.MovePrevious();
		}

		private void SelectBookmark(Bookmark bookmark)
		{
			SelectRowByAddr(bookmark.Position);
		}
	}
}

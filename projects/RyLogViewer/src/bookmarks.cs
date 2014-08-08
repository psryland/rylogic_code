using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace RyLogViewer
{
	/// <summary>A saved location within the file</summary>
	public class Bookmark
	{
		/// <summary>The byte range of the bookmarked line</summary>
		public Range Range { get; set; }

		/// <summary>The text of the bookmarked line (used by reflection in the bookmarks ui)</summary>
		public string Text { get; set; }

		/// <summary>The begin address of the bookmarked line</summary>
		public long Position { get { return Range.Begin; } }

		public Bookmark(Range range, string text)
		{
			Range = range;
			Text  = text;
		}
	}

	public partial class Main
	{
		private readonly BindingList<Bookmark> m_bookmarks;
		private readonly BindingSource         m_bs_bookmarks;
		private readonly EventBatcher          m_batch_refresh_bkmks;

		/// <summary>Setup the app's bookmark support</summary>
		private void SetupBookmarks()
		{
			m_bs_bookmarks.PositionChanged += (s,a) => SelectBookmark(m_bs_bookmarks.Position);
			m_bookmarks_ui.NextBookmark    += NextBookmark;
			m_bookmarks_ui.PrevBookmark    += PrevBookmark;
			m_batch_refresh_bkmks.Action   += RefreshBookmarks;
		}

		/// <summary>Show the bookmarks dialog</summary>
		private void ShowBookmarksDialog()
		{
			// Display the bookmarks window
			m_bookmarks_ui.Display();
		}

		/// <summary>Add or remove a bookmark at 'row_index'</summary>
		private void SetBookmark(int row_index, Bit.EState bookmarked)
		{
			if (row_index < 0 || row_index >= m_line_index.Count) return;
			var line = m_line_index[row_index];

			SetBookmark(line, bookmarked);

			// Restore the selected row
			SelectedRowIndex = row_index;
		}

		/// <summary>Sets or clears the bookmark at file address 'addr'</summary>
		private void SetBookmark(Range line, Bit.EState bookmarked)
		{
			// Look for the bookmark
			var idx = m_bookmarks.BinarySearch(b => b.Position.CompareTo(line.Begin));
			if (idx >= 0 && bookmarked != Bit.EState.Set)
			{
				// Bookmark was found, remove it
				m_bookmarks.RemoveAt(idx);
			}
			else if (idx < 0 && bookmarked != Bit.EState.Clear)
			{
				// Bookmark not found, insert it
				m_bookmarks.Insert(~idx, new Bookmark(line, ReadLine(line).RowText));
			}
			m_batch_refresh_bkmks.Signal();
		}

		/// <summary>Scroll to the next bookmark after the current selected row</summary>
		private void NextBookmark()
		{
			if (m_bookmarks.Count == 0) return;

			var row_index = SelectedRowIndex;
			var line = row_index >= 0 && row_index < m_line_index.Count ? m_line_index[row_index] : Range.Zero;

			// Look for the first bookmark after line.Begin
			var idx = m_bookmarks.BinarySearch(b => b.Position.CompareTo(line.Begin + 1));
			if (idx < 0) idx = ~idx;
			if (idx == m_bookmarks.Count) idx = 0;
			SelectBookmark(idx);
		}

		/// <summary>Scroll to the previous bookmark before the current selected row</summary>
		private void PrevBookmark()
		{
			if (m_bookmarks.Count == 0) return;

			var row_index = SelectedRowIndex;
			var line = row_index >= 0 && row_index < m_line_index.Count ? m_line_index[row_index] : Range.Zero;

			// Look for the first bookmark after line.Begin
			var idx = m_bookmarks.BinarySearch(b => b.Position.CompareTo(line.Begin));
			if (idx < 0) idx = ~idx;
			if (idx < 1) idx = m_bookmarks.Count;
			SelectBookmark(idx - 1);
		}

		/// <summary>Move to the bookmark with index 'idx'</summary>
		private void SelectBookmark(int idx)
		{
			if (idx < 0 || idx >= m_bookmarks.Count) return;
			SelectRowByAddr(m_bookmarks[idx].Position);
			m_bs_bookmarks.Position = idx;
		}

		/// <summary>Removes all bookmarks and updates the UI</summary>
		private void ClearAllBookmarks()
		{
			m_bookmarks.Clear();
			m_batch_refresh_bkmks.Signal();
		}

		/// <summary>Updates the UI after the bookmarks have changed</summary>
		private void RefreshBookmarks()
		{
			m_bs_bookmarks.ResetBindings(false);
			UpdateUI();
		}
	}
}

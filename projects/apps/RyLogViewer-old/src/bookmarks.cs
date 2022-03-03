using System;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Util = Rylogic.Utility.Util;

namespace RyLogViewer
{
	/// <summary>A saved location within the file</summary>
	public class Bookmark
	{
		public Bookmark(RangeI range, string text)
		{
			Range = range;
			Text  = text;
		}

		/// <summary>The byte range of the bookmarked line</summary>
		public RangeI Range { get; set; }

		/// <summary>The text of the bookmarked line (used by reflection in the bookmarks UI)</summary>
		public string Text { get; set; }

		/// <summary>The begin address of the bookmarked line</summary>
		public long Position
		{
			get { return Range.Beg; }
		}
	}

	public partial class Main
	{
		/// <summary>The bookmarks dialog</summary>
		private BookmarksUI BookmarksUI
		{
			get { return m_bookmark_ui; }
			set
			{
				if (m_bookmark_ui == value) return;
				if (m_bookmark_ui != null)
				{
					Util.Dispose(ref m_batch_refresh_bkmks);
					Util.Dispose(ref m_bookmark_ui);
				}
				m_bookmark_ui = value;
				if (m_bookmark_ui != null)
				{
					m_batch_refresh_bkmks = new EventBatcher(TimeSpan.FromMilliseconds(100));
				}
			}
		}
		private BookmarksUI m_bookmark_ui;
		private EventBatcher m_batch_refresh_bkmks;

		/// <summary>The collection of bookmarks</summary>
		private BindingSource<Bookmark> Bookmarks
		{
			get { return m_bookmarks; }
			set
			{
				if (m_bookmarks == value) return;
				if (m_bookmarks != null)
				{
					m_bookmarks.PositionChanged -= HandleBookmarkPositionChanged;
				}
				m_bookmarks = value;
				if (m_bookmarks != null)
				{
					m_bookmarks.PositionChanged += HandleBookmarkPositionChanged;
				}

				// Handlers
				void HandleBookmarkPositionChanged(object sender, PositionChgEventArgs e)
				{
					SelectBookmark(Bookmarks.Position);
				}
			}
		}
		private BindingSource<Bookmark> m_bookmarks;

		/// <summary>Set up the app's bookmark support</summary>
		private void SetupBookmarks()
		{
			BookmarksUI.NextBookmark    += NextBookmark;
			BookmarksUI.PrevBookmark    += PrevBookmark;
			m_batch_refresh_bkmks.Action   += RefreshBookmarks;
		}

		/// <summary>Show the bookmarks dialog</summary>
		private void ShowBookmarksDialog()
		{
			// Display the bookmarks window
			BookmarksUI.Show();
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
		private void SetBookmark(RangeI line, Bit.EState bookmarked)
		{
			// Look for the bookmark
			var idx = Bookmarks.BinarySearch(b => b.Position.CompareTo(line.Beg));
			if (idx >= 0 && bookmarked != Bit.EState.Set)
			{
				// Bookmark was found, remove it
				Bookmarks.RemoveAt(idx);
			}
			else if (idx < 0 && bookmarked != Bit.EState.Clear)
			{
				// Bookmark not found, insert it
				Bookmarks.Insert(~idx, new Bookmark(line, ReadLine(line).RowText));
			}
			m_batch_refresh_bkmks.Signal();
		}

		/// <summary>Scroll to the next bookmark after the current selected row</summary>
		private void NextBookmark()
		{
			if (Bookmarks.Count == 0) return;

			var row_index = SelectedRowIndex;
			var line = row_index >= 0 && row_index < m_line_index.Count ? m_line_index[row_index] : RangeI.Zero;

			// Look for the first bookmark after line.Begin
			var idx = Bookmarks.BinarySearch(b => b.Position.CompareTo(line.Beg + 1));
			if (idx < 0) idx = ~idx;
			if (idx == Bookmarks.Count) idx = 0;
			SelectBookmark(idx);
		}

		/// <summary>Scroll to the previous bookmark before the current selected row</summary>
		private void PrevBookmark()
		{
			if (Bookmarks.Count == 0) return;

			var row_index = SelectedRowIndex;
			var line = row_index >= 0 && row_index < m_line_index.Count ? m_line_index[row_index] : RangeI.Zero;

			// Look for the first bookmark after line.Begin
			var idx = Bookmarks.BinarySearch(b => b.Position.CompareTo(line.Beg));
			if (idx < 0) idx = ~idx;
			if (idx < 1) idx = Bookmarks.Count;
			SelectBookmark(idx - 1);
		}

		/// <summary>Move to the bookmark with index 'idx'</summary>
		private void SelectBookmark(int idx)
		{
			if (idx < 0 || idx >= Bookmarks.Count) return;
			SelectRowByAddr(Bookmarks[idx].Position);
			Bookmarks.Position = idx;
		}

		/// <summary>Removes all bookmarks and updates the UI</summary>
		private void ClearAllBookmarks()
		{
			Bookmarks.Clear();
			m_batch_refresh_bkmks.Signal();
		}

		/// <summary>Updates the UI after the bookmarks have changed</summary>
		private void RefreshBookmarks()
		{
			Bookmarks.ResetBindings(false);
			UpdateUI();
		}
	}
}

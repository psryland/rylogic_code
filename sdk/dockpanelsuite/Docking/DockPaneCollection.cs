using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WeifenLuo.WinFormsUI.Docking
{
    public class DockPaneCollection : ReadOnlyCollection<DockPane>
    {
        internal DockPaneCollection()
            : base(new List<DockPane>())
        {
        }

        internal int Add(DockPane pane)
        {
            if (Items.Contains(pane))
                return Items.IndexOf(pane);

            Items.Add(pane);
            return Count - 1;
        }

        internal void AddAt(DockPane pane, int index)
        {
            if (index < 0 || index > Items.Count - 1)
                return;
            
            if (Contains(pane))
                return;

            Items.Insert(index, pane);
        }

        internal void Dispose()
        {
			// pr 25/9/14 - replaced this code because it was throwing an index out of range exception
			// It looks like closing a window can cause another windows to be removed.
			while (this.Count != 0)
				this[0].Close();

			//for (int i=Count - 1; i>=0; i--)
			//	this[i].Close();
        }

        internal void Remove(DockPane pane)
        {
            Items.Remove(pane);
        }
    }
}

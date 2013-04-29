/*
	Copyright (c) 2011 - 2012 Trogu Antonio Davide

	This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

module dgui.tabcontrol;

import std.utf: toUTFz;
import dgui.core.controls.subclassedcontrol;
import dgui.core.interfaces.ilayoutcontrol;
import dgui.layout.panel;
import dgui.imagelist;

private struct TcItem
{
	TCITEMHEADERW Header;
	TabPage Page;
}

enum TabAlignment
{
	TOP    = 0,
	LEFT   = TCS_VERTICAL,
	RIGHT  = TCS_VERTICAL | TCS_RIGHT,
	BOTTOM = TCS_BOTTOM,
}

class TabPage: Panel
{
	private int _imgIndex;
	private TabControl _owner;
	
	protected void initTabPage()
	{
		//Does Nothing
	}

	@property public final int index()
	{
		if(this._owner && this._owner.created && this._owner.tabPages)
		{
			int i = 0;
			
			foreach(TabPage tp; this._owner.tabPages)
			{
				if(tp is this)
				{
					return i;
				}

				i++;
			}
		}

		return -1;
	}

	@property package void tabControl(TabControl tc)
	{
		this._owner = tc;
	}

	@property public final TabControl tabControl()
	{
		return this._owner;
	}

	alias @property Control.text text;

	@property public override void text(string txt)
	{
		super.text = txt;

		if(this._owner && this._owner.created)
		{
			TcItem tci = void;

			tci.Header.mask = TCIF_TEXT;
			tci.Header.pszText = toUTFz!(wchar*)(txt);

			this._owner.sendMessage(TCM_SETITEMW, this.index, cast(LPARAM)&tci);
			this.redraw();
		}
	}

	@property public final int imageIndex()
	{
		return this._imgIndex;
	}

	@property public final void imageIndex(int idx)
	{
		this._imgIndex = idx;

		if(this._owner && this._owner.created)
		{
			TcItem tci = void;

			tci.Header.mask = TCIF_IMAGE;
			tci.Header.iImage = idx;

			this._owner.sendMessage(TCM_SETITEMW, this.index, cast(LPARAM)&tci);
		}
	}
	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		this.setExStyle(WS_EX_STATICEDGE, true);
		
		super.createControlParams(ccp);
	}
	
	protected override void onHandleCreated(EventArgs e)
	{
		this.initTabPage();
		
		super.onHandleCreated(e);
	}
}

alias CancelEventArgs!(TabPage) CancelTabPageEventArgs;

class TabControl: SubclassedControl, ILayoutControl
{
	public Event!(Control, CancelTabPageEventArgs) tabPageChanging;
	public Event!(Control, EventArgs) tagPageChanged;
	
	private Collection!(TabPage) _tabPages;
	private TabAlignment _ta = TabAlignment.TOP;
	private ImageList _imgList;
	private int _selIndex = 0; //By Default: select the first TagPage (if exists)

	public final T addPage(T: TabPage = TabPage)(string t, int imgIndex = -1)
	{
		if(!this._tabPages)
		{
			this._tabPages = new Collection!(TabPage);
		}
		
		T tp = new T();
		tp.text = t;
		tp.imageIndex = imgIndex;
		tp.visible = false;
		tp.tabControl = this;
		tp.parent = this;

		this._tabPages.add(tp);
		
		if(this.created)
		{
			this.createTabPage(tp);
		}

		return tp;		
	}

	public final void removePage(int idx)
	{
		if(this.created)
		{
			this.removeTabPage(idx);
		}
		
		this._tabPages.removeAt(idx);
	}

	@property public final TabPage[] tabPages()
	{
		if(this._tabPages)
		{
			return this._tabPages.get();
		}
		
		return null;
	}
	
	@property public final TabPage selectedPage()
	{
		if(this._tabPages)
		{
			return this._tabPages[this._selIndex];
		}

		return null;
	}

	@property public final void selectedPage(TabPage stp)
	{		
		this.selectedIndex = stp.index;
	}

	@property public final int selectedIndex()
	{
		return this._selIndex;
	}

	@property public final void selectedIndex(int idx)
	{
		if(this._tabPages)
		{
			TabPage sp = this.selectedPage;   //Old TabPage
			TabPage tp = this._tabPages[idx]; //New TabPage	

			if(sp && sp !is tp)
			{
				this._selIndex = idx;
				tp.visible = true;  //Show new TabPage
				sp.visible = false; //Hide old TabPage
			}
			else if(sp is tp) // Same TabPage, make visibile
			{
				/*
				 * By default, TabPages are created not visible
				 */
				
				tp.visible = true;
			}

			if(this.created)
			{
				this.updateLayout();
			}			
		}
	}

	@property public final ImageList imageList()
	{
		return this._imgList;
	}

	@property public final void imageList(ImageList imgList)
	{
		this._imgList = imgList;

		if(this.created)
		{
			this.sendMessage(TCM_SETIMAGELIST, 0, cast(LPARAM)this._imgList.handle);
		}
	}

	@property public final TabAlignment alignment()
	{
		return this._ta;
	}

	@property public final void alignment(TabAlignment ta)
	{
		this.setStyle(this._ta, false);
		this.setStyle(ta, true);
		
		this._ta = ta;
	}

	private void doTabPages()
	{
		if(this._tabPages)
		{
			foreach(int i, TabPage tp; this._tabPages)
			{
				this.createTabPage(tp, false);
				
				if(i == this._selIndex)
				{
					tp.visible = true;
					this.updateLayout();
				}
			}
			
			this.selectedIndex = this._selIndex;
		}
	}	
	
	public void updateLayout()
	{
		TabPage selPage = this.selectedPage;
		
		if(selPage)
		{
			TabControl tc = selPage.tabControl;				
			Rect adjRect, r = Rect(NullPoint, tc.clientSize);
		
			tc.sendMessage(TCM_ADJUSTRECT, false, cast(LPARAM)&adjRect.rect);

			r.left += adjRect.left;
			r.top += adjRect.top;
			r.right += r.left + adjRect.width;
			r.bottom += r.top + adjRect.height;
		
			selPage.bounds = r; //selPage docks its child componentsS
		}
	}

	private void createTabPage(TabPage tp, bool adding = true)
	{
		TcItem tci;
		tci.Header.mask = TCIF_IMAGE | TCIF_TEXT | TCIF_PARAM;		
		tci.Header.iImage = tp.imageIndex;
		tci.Header.pszText = toUTFz!(wchar*)(tp.text);
		tci.Page = tp;
		
		tp.sendMessage(DGUI_CREATEONLY, 0, 0); //Calls Control.create()
		
		int idx = tp.index;
		this.sendMessage(TCM_INSERTITEMW, idx, cast(LPARAM)&tci);

		if(adding) //Adding mode: select the last TabPage
		{
			this.sendMessage(TCM_SETCURSEL, idx, 0);
			this.selectedIndex = idx;
		}
	}

	private void removeTabPage(int idx)
	{		
		if(this._tabPages)
		{
			if(idx == this._selIndex)
			{
				this.selectedIndex = idx > 0 ? idx - 1 : 0;
			}

			if(this.created)
			{
				this.sendMessage(TCM_DELETEITEM, idx, 0);
				this.sendMessage(TCM_SETCURSEL, this._selIndex, 0); //Set the new tab's index
			}

			TabPage tp = this._tabPages[idx];
			tp.dispose();
		}
	}
	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		this.setStyle(WS_CLIPCHILDREN | WS_CLIPSIBLINGS, true);
		this.setExStyle(WS_EX_CONTROLPARENT, true);
		
		ccp.SuperclassName = WC_TABCONTROL;
		ccp.ClassName = WC_DTABCONTROL;
		
		super.createControlParams(ccp);
	}

	protected override void onHandleCreated(EventArgs e)
	{
		if(this._imgList)
		{
			this.sendMessage(TCM_SETIMAGELIST, 0, cast(LPARAM)this._imgList.handle);
		}

		this.doTabPages();
		super.onHandleCreated(e);
	}

	protected override void onReflectedMessage(ref Message m)
	{
		if(m.Msg == WM_NOTIFY)
		{	
			NMHDR* pNotify = cast(NMHDR*)m.lParam;

			switch(pNotify.code)
			{
				case TCN_SELCHANGING:
					scope CancelTabPageEventArgs e = new CancelTabPageEventArgs(this.selectedPage);
					this.onTabPageChanging(e);
					m.Result = e.cancel;
					break;

				case TCN_SELCHANGE:
					this.selectedIndex = this.sendMessage(TCM_GETCURSEL, 0, 0);
					this.onTabPageChanged(EventArgs.empty);
					break;
				
				default:
					break;
			}
		}
		
		super.onReflectedMessage(m);
	}
	
	protected override void show()
	{
		super.show();
		this.updateLayout();
	}
	
	protected override void onResize(EventArgs e)
	{
		this.updateLayout();
		super.onResize(e);
	}

	protected void onTabPageChanging(CancelTabPageEventArgs e)
	{
		this.tabPageChanging(this, e);
	}

	protected void onTabPageChanged(EventArgs e)
	{
		this.tagPageChanged(this, e);
	}
}
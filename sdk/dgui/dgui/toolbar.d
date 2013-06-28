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

module dgui.toolbar;

import dgui.core.controls.subclassedcontrol;
import dgui.core.utils;
public import dgui.imagelist;

enum ToolButtonStyle: ubyte
{
	BUTTON = TBSTYLE_BUTTON,
	SEPARATOR = TBSTYLE_SEP,
	DROPDOWN = TBSTYLE_DROPDOWN,
}

class ToolButton
{
	public Event!(ToolButton, EventArgs) click;
	
	private ToolBar _owner;
	private ContextMenu _ctxMenu;
	private ToolButtonStyle _tbs;
	private int _imgIndex;
	private bool _enabled;
	
	package this(ToolBar tb, ToolButtonStyle tbs, int imgIndex, bool enabled)
	{
		this._owner = tb;
		this._tbs = tbs;
		this._imgIndex = imgIndex;
		this._enabled = enabled;
	}

	@property public final int index()
	{
		if(this._owner && this._owner.created && this._owner.buttons)
		{
			int i = 0;

			foreach(ToolButton tbtn; this._owner.buttons)
			{
				if(tbtn is this)
				{
					return i;
				}
				
				i++;
			}
		}
		
		return -1;
	}

	@property public final ToolButtonStyle style()
	{
		return this._tbs;
	}

	@property public final void style(ToolButtonStyle tbs)
	{
		this._tbs = tbs;

		if(this._owner && this._owner.created)
		{
			 TBBUTTONINFOW tbinfo = void;

			 tbinfo.cbSize = TBBUTTONINFOW.sizeof;
			 tbinfo.dwMask = TBIF_BYINDEX | TBIF_STYLE;
			 tbinfo.fsStyle = tbs;

			 this._owner.sendMessage(TB_SETBUTTONINFOW, this.index, cast(LPARAM)&tbinfo);
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
			 TBBUTTONINFOW tbinfo = void;

			 tbinfo.cbSize = TBBUTTONINFOW.sizeof;
			 tbinfo.dwMask = TBIF_BYINDEX | TBIF_IMAGE;
			 tbinfo.iImage = idx;

			 this._owner.sendMessage(TB_SETBUTTONINFOW, this.index, cast(LPARAM)&tbinfo);
		}
	}

	@property public final bool enabled()
	{
		return this._enabled;
	}

	@property public final void enabled(bool b)
	{
		this._enabled = b;

		if(this._owner && this._owner.created)
		{
			 TBBUTTONINFOW tbinfo = void;

			 tbinfo.cbSize = TBBUTTONINFOW.sizeof;
			 tbinfo.dwMask = TBIF_BYINDEX | TBIF_STATE;
			 this._owner.sendMessage(TB_GETBUTTONINFOW, this.index, cast(LPARAM)&tbinfo); //Ricavo i dati completi.

			 b ? (tbinfo.fsState |= TBSTATE_ENABLED) : (tbinfo.fsState &= ~TBSTATE_ENABLED);
			 this._owner.sendMessage(TB_SETBUTTONINFOW, this.index, cast(LPARAM)&tbinfo);
		}
	}

	@property public ContextMenu contextMenu()
	{
		return this._ctxMenu;
	}

	@property public void contextMenu(ContextMenu cm)
	{
		this._ctxMenu = cm;
	}

	@property public final ToolBar toolBar()
	{
		return this._owner;
	}

	package void onToolBarButtonClick(EventArgs e)
	{
		this.click(this, e);
	}
}

class ToolBar: SubclassedControl
{
	private Collection!(ToolButton) _buttons;
	private ImageList _imgList;

	@property public final ImageList imageList()
	{
		return this._imgList;
	}

	@property public final void imageList(ImageList imgList)
	{
		this._imgList = imgList;

		if(this.created)
		{
			this.sendMessage(TB_SETIMAGELIST, 0, cast(LPARAM)this._imgList.handle);
		}
	}

	public final ToolButton addDropdownButton(int imgIndex, ContextMenu ctxMenu, bool en = true)
	{
		if(!this._buttons)
		{
			this._buttons = new Collection!(ToolButton)();
		}
		
		ToolButton tb = new ToolButton(this, ToolButtonStyle.DROPDOWN, imgIndex, en);
		tb.contextMenu = ctxMenu;
		this._buttons.add(tb);

		if(this.created)
		{
			ToolBar.addItem(tb);
		}

		return tb;		
	}

	public final ToolButton addButton(int imgIndex, bool en = true)
	{
		if(!this._buttons)
		{
			this._buttons = new Collection!(ToolButton)();
		}
		
		ToolButton tb = new ToolButton(this, ToolButtonStyle.BUTTON, imgIndex, en);
		this._buttons.add(tb);

		if(this.created)
		{
			ToolBar.addItem(tb);
		}

		return tb;
	}

	public final void addSeparator()
	{
		if(!this._buttons)
		{
			this._buttons = new Collection!(ToolButton)();
		}
		
		ToolButton tb = new ToolButton(this, ToolButtonStyle.SEPARATOR, -1, true);
		this._buttons.add(tb);

		if(this.created)
		{
			ToolBar.addItem(tb);
		}
	}

	public final void removeButton(int idx)
	{
		this._buttons.removeAt(idx);
		
		if(this.created)
		{
			this.sendMessage(TB_DELETEBUTTON, idx, 0);
		}
	}

	@property public final ToolButton[] buttons()
	{
		if(this._buttons)
		{
			return this._buttons.get();
		}
		
		return null;
	}

	private void forceToolbarSize()
	{
		uint sz = this.sendMessage(TB_GETBUTTONSIZE, 0, 0);

		this.size = Size(LOWORD(sz), HIWORD(sz));
	}

	private static void addItem(ToolButton tb)
	{
		TBBUTTON tbtn;

		switch(tb.style)
		{
			case ToolButtonStyle.BUTTON, ToolButtonStyle.DROPDOWN:
				tbtn.iBitmap = tb.imageIndex;
				tbtn.fsStyle = cast(ubyte)tb.style;
				tbtn.fsState = cast(ubyte)(tb.enabled ? TBSTATE_ENABLED : 0);
				tbtn.dwData = winCast!(uint)(tb);
				break;

			case ToolButtonStyle.SEPARATOR:
				tbtn.fsStyle = cast(ubyte)tb.style;
				break;

			default:
				assert(false, "Unknown ToolButton Style");
		}

		if(tb.toolBar._dock is DockStyle.LEFT || tb.toolBar._dock is DockStyle.RIGHT)
		{
			tbtn.fsState |= TBSTATE_WRAP;
		}

		tb.toolBar.sendMessage(TB_INSERTBUTTONW, tb.index, cast(LPARAM)&tbtn);		
	}

	protected override void createControlParams(ref CreateControlParams ccp)
	{		
		ccp.SuperclassName = WC_TOOLBAR;
		ccp.ClassName = WC_DTOOLBAR;
		this.setStyle(TBSTYLE_FLAT | CCS_NODIVIDER | CCS_NOPARENTALIGN, true);

		if(this._dock is DockStyle.NONE)
		{
			this._dock = DockStyle.TOP;
		}

		if(this._dock is DockStyle.LEFT || this._dock is DockStyle.RIGHT)
		{
			this.setStyle(CCS_VERT, true);
		}
		
		super.createControlParams(ccp);
	}

	protected override void onHandleCreated(EventArgs e)
	{
		this.sendMessage(TB_BUTTONSTRUCTSIZE, TBBUTTON.sizeof, 0);
		int exStyle = this.sendMessage(TB_GETEXTENDEDSTYLE, 0, 0);
		this.sendMessage(TB_SETEXTENDEDSTYLE, 0, exStyle | TBSTYLE_EX_DRAWDDARROWS);
		this.forceToolbarSize(); // HACK: Forza il ridimensionamento della barra strumenti.

		if(this._imgList)
		{
			this.sendMessage(TB_SETIMAGELIST, 0, cast(LPARAM)this._imgList.handle);
		}

		if(this._buttons)
		{
			foreach(ToolButton tb; this._buttons)
			{
				ToolBar.addItem(tb);
			}
		}
		
		super.onHandleCreated(e);
	}

	protected override void onReflectedMessage(ref Message m)
	{
		switch(m.Msg)
		{
			case WM_NOTIFY:
			{
				NMHDR* pNmhdr = cast(NMHDR*)m.lParam;

				switch(pNmhdr.code)
				{
					case NM_CLICK:
					{
						NMMOUSE* pNMouse = cast(NMMOUSE*)m.lParam;
						ToolButton tBtn = winCast!(ToolButton)(pNMouse.dwItemData);

						if(tBtn)
						{
							tBtn.onToolBarButtonClick(EventArgs.empty); //FIXME!
						}
					}
					break;

					case TBN_DROPDOWN:
					{
						NMTOOLBARW* pNmToolbar = cast(NMTOOLBARW*)m.lParam;

						Point pt = Cursor.position;
						convertPoint(pt, null, this);
						int idx = this.sendMessage(TB_HITTEST, 0, cast(LPARAM)&pt.point);

						if(idx != -1)
						{
							ToolButton tbtn = this._buttons[idx];

							if(tbtn && tbtn.contextMenu)
							{
								tbtn.contextMenu.popupMenu(this._handle, Cursor.position);
							}
						}
					}
					break;
					
					default:
						break;
				}
			}
			break;
			
			default:
				break;
		}
		
		super.onReflectedMessage(m);
	}

	protected override void wndProc(ref Message m)
	{
		if(m.Msg == WM_WINDOWPOSCHANGING)
		{
			/*
			 * HACK: Forza il ridimensionamento della barra strumenti.
			 */
			
			WINDOWPOS* pWindowPos = cast(WINDOWPOS*)m.lParam;
			uint sz = this.sendMessage(TB_GETBUTTONSIZE, 0, 0);

			switch(this._dock)
			{
				case DockStyle.TOP, DockStyle.BOTTOM:
					pWindowPos.cy = HIWORD(sz);
					break;
				
				case DockStyle.LEFT, DockStyle.RIGHT:
					pWindowPos.cx = LOWORD(sz);
					break;

				default:
					break;
			}
		}

		super.wndProc(m);
	}
}
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

module dgui.core.menu.abstractmenu;

import std.utf: toUTFz;
public import dgui.core.winapi;
public import dgui.core.collection;
public import dgui.imagelist;
public import dgui.core.events.eventargs;
public import dgui.core.events.event;
import dgui.canvas;
import dgui.core.interfaces.idisposable;
import dgui.core.handle;
import dgui.core.utils;
import dgui.core.wincomp;

enum: uint
{
	MIIM_STRING 		= 64,
	MIIM_FTYPE  		= 256,
	
	MIM_MAXHEIGHT       = 1,
	MIM_BACKGROUND      = 2,
	MIM_HELPID          = 4,
	MIM_MENUDATA        = 8,
	MIM_STYLE           = 16,
	MIM_APPLYTOSUBMENUS = 0x80000000L,

	MNS_NOCHECK    		= 0x80000000,
	MNS_MODELESS    	= 0x40000000,
	MNS_DRAGDROP    	= 0x20000000,
	MNS_AUTODISMISS 	= 0x10000000,
	MNS_NOTIFYBYPOS 	= 0x08000000,
	MNS_CHECKORBMP  	= 0x04000000,
}

enum MenuBits: ubyte
{
	ENABLED    = 1,
	CHECKED    = 2,
}

enum MenuStyle: ubyte
{
	NORMAL	  = 1,
	SEPARATOR = 2,
}

abstract class Menu: Handle!(HMENU), IDisposable
{
	public Event!(Menu, EventArgs) popup;
	
	private Collection!(MenuItem) _items;
	protected Menu _parent;
	
	public ~this()
	{
		this.dispose();
	}

	public abstract void create();
	
	public void dispose()
	{
		//From MSDN: DestroyMenu is recursive, it will destroy the menu and its submenus.
		if(this.created)
		{
			DestroyMenu(this._handle);
		}
	}
	
	@property public final MenuItem[] items()
	{
		if(this._items)
		{
			return this._items.get();
		}
		
		return null;
	}	
	
	@property public Menu parent()
	{
		return this._parent;
	}	
	
	public final MenuItem addItem(string t)
	{
		return this.addItem(t, -1, true);
	}
	
	public final MenuItem addItem(string t, bool e)
	{
		return this.addItem(t, -1, e);
	}
		
	public final MenuItem addItem(string t, int imgIdx)
	{
		return this.addItem(t, imgIdx, true);
	}
	
	public final MenuItem addItem(string t, int imgIdx, bool e)
	{
		if(!this._items)
		{
			this._items = new Collection!(MenuItem)();
		}
		
		MenuItem mi = new MenuItem(this, MenuStyle.NORMAL, t, e);
		mi.imageIndex = imgIdx;
		
		this._items.add(mi);

		if(this.created)
		{
			mi.create();
		}
		
		return mi;
	}

	public final MenuItem addSeparator()
	{
		if(!this._items)
		{
			this._items = new Collection!(MenuItem)();
		}
		
		MenuItem mi = new MenuItem(this, MenuStyle.SEPARATOR, null, true);
		this._items.add(mi);

		if(this.created)
		{
			mi.create();
		}
		
		return mi;
	}
	
	public final void removeItem(int idx)
	{
		if(this._items)
		{
			this._items.removeAt(idx);
		}
		
		if(this.created)
		{
			DeleteMenu(this._handle, idx, MF_BYPOSITION);
		}
	}
	
	public void onPopup(EventArgs e)
	{
		this.popup(this, e);
	}
}

class RootMenu: Menu
{
	protected Collection!(HBITMAP) _bitmaps;
	protected ImageList _imgList;
	
	public override void dispose()
	{
		if(this._bitmaps)
		{
			foreach(HBITMAP hBitmap; this._bitmaps)
			{
				DeleteObject(hBitmap);
			}
		}		
		
		if(this._imgList)
		{
			this._imgList.dispose();
		}
		
		super.dispose();
	}
	
	@property package Collection!(HBITMAP) bitmaps()
	{
		return this._bitmaps;
	}
	
	@property public ImageList imageList()
	{
		return this._imgList;
	}
	
	@property public void imageList(ImageList imgList)
	{
		this._imgList = imgList;
		
		if(!this._bitmaps)
		{
			this._bitmaps = new Collection!(HBITMAP)();
		}
	}
	
	public override void create()
	{
		MENUINFO mi;

		mi.cbSize = MENUINFO.sizeof;
		mi.fMask  = MIM_MENUDATA | MIM_APPLYTOSUBMENUS | MIM_STYLE;
		mi.dwStyle = MNS_NOTIFYBYPOS | MNS_CHECKORBMP;
		mi.dwMenuData = winCast!(uint)(this);
	
		SetMenuInfo(this._handle, &mi);

		if(this._items)
		{
			foreach(MenuItem mi; this._items)
			{
				mi.create();
			}
		}
	}
}

class MenuItem: Menu
{
	public Event!(MenuItem, EventArgs) click;
	
	private MenuStyle _style = MenuStyle.NORMAL;
	private MenuBits _mBits = MenuBits.ENABLED;
	private int _imgIndex = -1;
	private int _index = -1;
	private string _text;

	protected this(Menu parent, MenuStyle mt, string t, bool e)
	{
		this._parent = parent;
		this._style = mt;
		this._text = t;
		
		if(!e)
		{
			this._mBits &= ~MenuBits.ENABLED;
		}
	}

	public void performClick()
	{
		this.onClick(EventArgs.empty);
	}
	
	private static void createMenuItem(MenuItem mi, HMENU hPopupMenu)
	{
		MENUITEMINFOW minfo;

		minfo.cbSize = MENUITEMINFOW.sizeof;
		minfo.fMask = MIIM_FTYPE;
		minfo.dwItemData = winCast!(uint)(mi);
		
		switch(mi.style)
		{
			case MenuStyle.NORMAL:
			{
				WindowsVersion ver = getWindowsVersion();
				
				minfo.fMask |= MIIM_DATA | MIIM_STRING | MIIM_STATE;
				minfo.fState = (mi.enabled ? MFS_ENABLED : MFS_DISABLED) | (mi.checked ? MFS_CHECKED : 0);
				minfo.dwTypeData = toUTFz!(wchar*)(mi.text);

				RootMenu root = mi.rootMenu;
				
				if(root.imageList && mi.imageIndex != -1)
				{
					minfo.fMask |= MIIM_BITMAP;
					
					if(ver > WindowsVersion.WINDOWS_XP) // Is Vista or 7
					{
						HBITMAP hBitmap = iconToBitmapPARGB32(root.imageList.images[mi.imageIndex].handle);
						root.bitmaps.add(hBitmap);
						
						minfo.hbmpItem = hBitmap;
					}
					else // Is 2000 or XP
					{
						minfo.hbmpItem = HBMMENU_CALLBACK;
					}
				}
			}
			break;
			
			case MenuStyle.SEPARATOR:
				minfo.fType = MFT_SEPARATOR;
				break;
			
			default:
				break;
		}
		
		if(mi._items)
		{
			HMENU hChildMenu = CreatePopupMenu();
			minfo.fMask |= MIIM_SUBMENU;
			minfo.hSubMenu = hChildMenu;
			
			foreach(MenuItem smi; mi._items)
			{
				MenuItem.createMenuItem(smi, hChildMenu);
			}
		}
		
		InsertMenuItemW(hPopupMenu ? hPopupMenu : mi._parent.handle, -1, TRUE, &minfo);
	}
	
	@property public final int index()
	{
		if(this._parent)
		{
			int i = 0;
			
			foreach(MenuItem mi; this._parent.items)
			{
				if(mi is this)
				{
					return i;
				}

				i++;
			}
		}

		return -1;
	}	
	
	@property public final MenuStyle style()
	{
		return this._style;
	}
	
	@property public RootMenu rootMenu()
	{
		Menu p = this._parent;
		
		while(p.parent)
		{
			p = p.parent;
		}
		
		return cast(RootMenu)p;
	}
	
	@property public int imageIndex()
	{
		return this._imgIndex;
	}
	
	@property public void imageIndex(int imgIdx)
	{
		this._imgIndex = imgIdx;

		if(this._parent && this._parent.created)
		{
			RootMenu root = this.rootMenu;
			
			int idx = this.index;
			HBITMAP hBitmap = iconToBitmapPARGB32(root.imageList.images[imgIdx].handle);
			root.bitmaps.add(hBitmap);
			
			MENUITEMINFOW minfo;

			minfo.cbSize = MENUITEMINFOW.sizeof;
			minfo.fMask = MIIM_BITMAP;
			minfo.hbmpItem = hBitmap;
			
			SetMenuItemInfoW(this._parent.handle, idx, true, &minfo);
		}
	}

	@property public final bool enabled()
	{
		return cast(bool)(this._mBits & MenuBits.ENABLED);
	}
	
	@property public final void enabled(bool b)
	{
		this._mBits |= MenuBits.ENABLED;

		if(this._parent && this._parent.created)
		{
			int idx = this.index;

			MENUITEMINFOW minfo;

			minfo.cbSize = MENUITEMINFOW.sizeof;
			minfo.fMask = MIIM_STATE;
			minfo.fState = b ? MFS_ENABLED : MFS_DISABLED;

			SetMenuItemInfoW(this._parent.handle, idx, true, &minfo);
		}
	}
	
	@property public final string text()
	{
		return this._text;
	}

	@property public final void text(string s)
	{
		this._text = s;

		if(this._parent && this._parent.created)
		{
			int idx = this.index;

			MENUITEMINFOW minfo;

			minfo.cbSize = MENUITEMINFOW.sizeof;
			minfo.fMask = MIIM_STRING;
			minfo.dwTypeData = toUTFz!(wchar*)(s);

			SetMenuItemInfoW(this._parent.handle, idx, true, &minfo);
		}
	}	
	
	@property public final bool checked()
	{
		return cast(bool)(this._mBits & MenuBits.CHECKED);
	}

	@property public final void checked(bool b)
	{
		this._mBits |= MenuBits.CHECKED;

		if(this._parent && this._parent.created)
		{
			int idx = this.index;

			MENUITEMINFOW minfo;

			minfo.cbSize = MENUITEMINFOW.sizeof;
			minfo.fMask = MIIM_STATE;
			
			if(b)
			{
				minfo.fState |= MFS_CHECKED;
			}
			else
			{	
				minfo.fState &= ~MFS_CHECKED;
			}

			SetMenuItemInfoW(this._parent.handle, idx, true, &minfo);
		}
	}
	
	protected override void create()
	{		
		MenuItem.createMenuItem(this, null);
	}

	protected void onClick(EventArgs e)
	{
		this.click(this, e);
	}
}
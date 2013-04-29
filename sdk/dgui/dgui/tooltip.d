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

module dgui.tooltip;

import dgui.core.controls.subclassedcontrol;

enum ToolTipIcons
{
	NONE	= TTI_NONE,
	INFO 	= TTI_INFO,
	WARNING = TTI_WARNING,
	ERROR   = TTI_ERROR,
}

class ToolTip: SubclassedControl
{
	private ToolTipIcons _ttIcon = ToolTipIcons.NONE;
	private bool _creating = false;
	private Control _ctrl;
	private string _title;
	
	public override void dispose()
	{
		if(this._ctrl)
		{
			this.removeTool();
		}
		
		super.dispose();
	}
	
	private void addTool(Control c)
	{
		this._ctrl = c;
		
		TOOLINFOW ti;
		
		ti.cbSize = TOOLINFOW.sizeof;
		ti.uFlags = TTF_SUBCLASS | TTF_IDISHWND;
		ti.lpszText = cast(wchar*)LPSTR_TEXTCALLBACKW;
		ti.hwnd = c.parent ? c.parent.handle : c.handle;
		ti.uId = cast(uint)c.handle;
	
		this.sendMessage(TTM_ADDTOOLW, 0, cast(LPARAM)&ti);
	}
	
	private void removeTool()
	{
		TOOLINFOW ti;
		
		ti.cbSize = TOOLINFOW.sizeof;
		ti.hwnd = this._ctrl.parent ? this._ctrl.parent.handle : this._ctrl.handle;
		ti.uId = cast(uint)this._ctrl.handle;
	
		this.sendMessage(TTM_DELTOOLW, 0, cast(LPARAM)&ti);
		this._ctrl = null;
	}
	
	@property public void icon(ToolTipIcons tti)
	{
		this._ttIcon = tti;
		
		if(this.created)
		{
			this.sendMessage(TTM_SETTITLEW, this._ttIcon, cast(LPARAM)toUTFz!(wchar*)(this._title));
		}
	}
	
	@property public string title()
	{
		return this._title;
	}
	
	@property public void title(string s)
	{
		this._title = s;
		
		if(this.created)
		{
			this.sendMessage(TTM_SETTITLEW, this._ttIcon, cast(LPARAM)toUTFz!(wchar*)(this._title));
		}
	}
	
	@property public void baloonTip(bool b)
	{
		this.setStyle(TTS_BALLOON, b);
	}
	
	@property public void closeButton(bool b)
	{
		this.setStyle(TTS_CLOSE, b);
	}
	
	@property public void alwaysTip(bool b)
	{
		this.setStyle(TTS_ALWAYSTIP, b);
	}
	
	@property public override void parent(Control c)
	{
		if(this._creating)
		{
			super.parent = c;
		}
		else
		{
			throwException!(DGuiException)("A ToolTip cannot have a parent");
		}
	}
	
	public final void activate(Control c)
	{
		if(!this.created)
		{
			this._creating = true;
			this.parent = c.parent ? c.parent : c;
			this.show();
			this._creating = false;
		}
		
		if(this._ctrl !is c)
		{
			if(this._ctrl)
			{
				this.removeTool();
			}
			
			this.addTool(c);
		}
		
		this.sendMessage(TTM_ACTIVATE, true, 0);
	}
	
	public override void show()
	{
		if(this._creating)
		{
			super.show();		
		}
		else
		{
			throwException!(DGuiException)("Cannot create a ToolTip directly");
		}
	}
	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		ccp.SuperclassName = WC_TOOLTIP;
		ccp.ClassName = WC_DTOOLTIP;
		ccp.DefaultBackColor = SystemColors.colorInfoBk;
		ccp.DefaultForeColor = SystemColors.colorInfoText;
		
		this.setStyle(WS_POPUP | TTS_NOPREFIX, true);
		this.setExStyle(WS_EX_TOPMOST | WS_EX_TOOLWINDOW, true);
		
		/* According To MSDN:
		    The window procedure for the tooltip control automatically sets the size, position, and visibility of the control. 
		    The height of the tooltip window is based on the height of the font currently selected into the device context 
		    for the tooltip control. 
		    The width varies based on the length of the string currently in the tooltip window. */
		this.bounds = Rect(CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT);
		
		ToolTip.setBit(this._cBits, ControlBits.CANNOT_ADD_CHILD | ControlBits.USE_CACHED_TEXT, true);
		super.createControlParams(ccp);
	}
	
	protected override void onReflectedMessage(ref Message m)
	{
		if(m.Msg == WM_NOTIFY)
		{
			NMHDR* pNotify = cast(NMHDR*)m.lParam;
			
			if(pNotify.code == TTN_GETDISPINFOW)
			{
				NMTTDISPINFOW* pDispInfo = cast(NMTTDISPINFOW*)pNotify;
				pDispInfo.lpszText = toUTFz!(wchar*)(this.text);
			}
		}
		
		super.onReflectedMessage(m);
	}
	
	protected override void onHandleCreated(EventArgs e)
	{
		if(this._ttIcon !is ToolTipIcons.NONE || this._title.length)
		{
			this.sendMessage(TTM_SETTITLEW, this._ttIcon, cast(LPARAM)toUTFz!(wchar*)(this._title));
		}
		
		SetWindowPos(this._handle, cast(HWND)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
		super.onHandleCreated(e);
	}
}
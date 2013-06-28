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

module dgui.form;

public import dgui.core.dialogs.dialogresult;
public import dgui.menubar;
private import dgui.core.utils;
import dgui.layout.layoutcontrol;
import dgui.core.events.eventargs;

alias CancelEventArgs!(Form) CancelFormEventArgs;

enum FormBits: ulong
{
	NONE 		 	= 0,
	MODAL_COMPLETED = 1,
}

enum FormBorderStyle: ubyte
{
	NONE 				= 0, 
	MANUAL 				= 1, // Internal Use
	FIXED_SINGLE 		= 2,
	FIXED_3D 			= 4,
	FIXED_DIALOG		= 8,
	SIZEABLE 			= 16, 
	FIXED_TOOLWINDOW 	= 32,
	SIZEABLE_TOOLWINDOW = 64,
}

enum FormStartPosition: ubyte
{
	MANUAL 			 = 0,
	CENTER_PARENT	 = 1,
	CENTER_SCREEN	 = 2,
	DEFAULT_LOCATION = 4,
}

class Form: LayoutControl
{
	private FormBits _fBits = FormBits.NONE;
	private FormStartPosition _startPosition = FormStartPosition.MANUAL;
	private FormBorderStyle _formBorder = FormBorderStyle.SIZEABLE;
	private DialogResult _dlgResult = DialogResult.CANCEL;
	private HWND _hActiveWnd;
	private Icon _formIcon;
	private MenuBar _menu;

	public Event!(Control, EventArgs) close;
	public Event!(Control, CancelFormEventArgs) closing;
	
	public this()
	{
		this.setStyle(WS_SYSMENU | WS_MAXIMIZEBOX | WS_MINIMIZEBOX, true);
	}
	
	@property public final void formBorderStyle(FormBorderStyle fbs)
	{
		if(this.created)
		{
			uint style = 0, exStyle = 0;

			makeFormBorderStyle(this._formBorder, style, exStyle); // Vecchio Stile.
			this.setStyle(style, false);
			this.setExStyle(exStyle, false);

			style = 0;
			exStyle = 0;

			makeFormBorderStyle(fbs, style, exStyle); // Nuovo Stile.
			this.setStyle(style, true);
			this.setExStyle(exStyle, true);
		}
		
		this._formBorder = fbs;
	}
	
	@property public final void controlBox(bool b)
	{
		this.setStyle(WS_SYSMENU, b);
	}

	@property public final void maximizeBox(bool b)
	{
		this.setStyle(WS_MAXIMIZEBOX, b);
	}

	@property public final void minimizeBox(bool b)
	{
		this.setStyle(WS_MINIMIZEBOX, b);
	}

	@property public final void showInTaskbar(bool b)
	{
		this.setExStyle(WS_EX_APPWINDOW, b);
	}

	@property public final MenuBar menu()
	{
		return this._menu;
	}

	@property public final void menu(MenuBar mb)
	{
		if(this.created)
		{
			if(this._menu)
			{
				this._menu.dispose();
			}

			mb.create();
			SetMenu(this._handle, mb.handle);
		}

		this._menu = mb;
	}

	@property public final Icon icon()
	{
		return this._formIcon;
	}

	@property public final void icon(Icon ico)
	{
		if(this.created)
		{
			if(this._formIcon)
			{
				this._formIcon.dispose();
			}
			
			this.sendMessage(WM_SETICON, ICON_BIG, cast(LPARAM)ico.handle);
			this.sendMessage(WM_SETICON, ICON_SMALL, cast(LPARAM)ico.handle);
		}

		this._formIcon = ico;
	}
	
	@property public final void topMost(bool b)
	{
		this.setExStyle(WS_EX_TOPMOST, b);
	}

	@property public final void startPosition(FormStartPosition fsp)
	{
		this._startPosition = fsp;
	}
	
	private void doEvents()
	{
		MSG m = void;

		while(GetMessageW(&m, null, 0, 0))
		{
			if(Form.hasBit(this._cBits, ControlBits.MODAL_CONTROL) && Form.hasBit(this._fBits, FormBits.MODAL_COMPLETED))
			{
				break;
			}
			else if(!IsDialogMessageW(this._handle, &m))
			{
				TranslateMessage(&m);
				DispatchMessageW(&m);
			}
		}
	}
	
	public override void show()
	{
		super.show();
		
		this.doEvents();
	}
	
	public final DialogResult showDialog()
	{
		Form.setBit(this._cBits, ControlBits.MODAL_CONTROL, true);
		this._hActiveWnd = GetActiveWindow();
		EnableWindow(this._hActiveWnd, false);
		
		this.show();
		return this._dlgResult;
	}

	private final void doFormStartPosition()
	{
		if((this._startPosition is FormStartPosition.CENTER_PARENT && !this.parent) || 
			this._startPosition is FormStartPosition.CENTER_SCREEN)
		{
			Rect wa = Screen.workArea;
			Rect b = this._bounds;
			
			this._bounds.position = Point((wa.width - b.width) / 2, 
										  (wa.height - b.height) / 2);
		}
		else if(this._startPosition is FormStartPosition.CENTER_PARENT)
		{
			Rect pr = this.parent.bounds;
			Rect b = this._bounds;

			this._bounds.position = Point(pr.left + (pr.width - b.width) / 2, 
										  pr.top + (pr.height - b.height) / 2);
		}
		else if(this._startPosition is FormStartPosition.DEFAULT_LOCATION)
		{
			this._bounds.position = Point(CW_USEDEFAULT, CW_USEDEFAULT);
		}
	}

	private static void makeFormBorderStyle(FormBorderStyle fbs, ref uint style, ref uint exStyle)
	{
		switch(fbs)
		{
			case FormBorderStyle.FIXED_3D:
				style &= ~(WS_BORDER | WS_THICKFRAME | WS_DLGFRAME);
				exStyle &= ~(WS_EX_TOOLWINDOW | WS_EX_STATICEDGE);
				
				style |= WS_CAPTION;
				exStyle |= WS_EX_CLIENTEDGE | WS_EX_WINDOWEDGE | WS_EX_DLGMODALFRAME;
				break;
			
			case FormBorderStyle.FIXED_DIALOG:
				style &= ~(WS_BORDER | WS_THICKFRAME);
				exStyle &= ~(WS_EX_TOOLWINDOW | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
			
				style |= WS_CAPTION | WS_DLGFRAME;
				exStyle |= WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE;
				break;
			
			case FormBorderStyle.FIXED_SINGLE:
				style &= ~(WS_THICKFRAME | WS_DLGFRAME);
				exStyle &= ~(WS_EX_TOOLWINDOW | WS_EX_CLIENTEDGE | WS_EX_WINDOWEDGE | WS_EX_STATICEDGE);
			
				style |= WS_CAPTION | WS_BORDER;
				exStyle |= WS_EX_WINDOWEDGE | WS_EX_DLGMODALFRAME;
				break;
			
			case FormBorderStyle.FIXED_TOOLWINDOW:
				style &= ~(WS_BORDER | WS_THICKFRAME | WS_DLGFRAME);
				exStyle &= ~(WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
			
				style |= WS_CAPTION;
				exStyle |= WS_EX_TOOLWINDOW | WS_EX_WINDOWEDGE | WS_EX_DLGMODALFRAME;
				break;
			
			case FormBorderStyle.SIZEABLE:
				style &= ~(WS_BORDER | WS_DLGFRAME);
				exStyle &= ~(WS_EX_TOOLWINDOW | WS_EX_CLIENTEDGE | WS_EX_DLGMODALFRAME | WS_EX_STATICEDGE);
			
				style |= WS_CAPTION | WS_THICKFRAME;
				exStyle |= WS_EX_WINDOWEDGE;
				break;
			
			case FormBorderStyle.SIZEABLE_TOOLWINDOW:
				style &= ~(WS_BORDER | WS_DLGFRAME);
				exStyle &= ~(WS_EX_CLIENTEDGE | WS_EX_DLGMODALFRAME | WS_EX_STATICEDGE);

				style |= WS_THICKFRAME | WS_CAPTION;
				exStyle |= WS_EX_TOOLWINDOW | WS_EX_WINDOWEDGE;
				break;
			
			case FormBorderStyle.NONE:
				style &= ~(WS_BORDER | WS_THICKFRAME | WS_CAPTION | WS_DLGFRAME);
				exStyle &= ~(WS_EX_TOOLWINDOW | WS_EX_CLIENTEDGE | WS_EX_DLGMODALFRAME | WS_EX_STATICEDGE | WS_EX_WINDOWEDGE);
				break;

			default:
				assert(0, "Unknown Form Border Style");
				//break;
		}		
	}
	
	protected override void onDGuiMessage(ref Message m)
	{
		switch(m.Msg)
		{
			case DGUI_SETDIALOGRESULT:
			{
				this._dlgResult = cast(DialogResult)m.wParam;
				
				Form.setBit(this._fBits, FormBits.MODAL_COMPLETED, true);
				ShowWindow(this._handle, SW_HIDE); // Hide this window (it waits to be destroyed)
				EnableWindow(this._hActiveWnd, true);
				SetActiveWindow(this._hActiveWnd); // Restore the previous active window
			}
			break;
			
			default:
				break;
		}
		
		super.onDGuiMessage(m);
	}
	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		uint style = 0, exStyle = 0;
		makeFormBorderStyle(this._formBorder, style, exStyle);
		
		this.setStyle(style, true);
		this.setExStyle(exStyle, true);
		ccp.ClassName = WC_FORM;
		ccp.DefaultCursor = SystemCursors.arrow;
		
		this.doFormStartPosition();
		super.createControlParams(ccp);
	}

	protected override void onHandleCreated(EventArgs e)
	{
		if(this._menu)
		{
			this._menu.create();
			SetMenu(this._handle, this._menu.handle);
			DrawMenuBar(this._handle);
		}
		
		if(this._formIcon)
		{	
			Message m = Message(this._handle, WM_SETICON, ICON_BIG, cast(LPARAM)this._formIcon.handle);
			this.originalWndProc(m);
			
			m.Msg = ICON_SMALL;
			this.originalWndProc(m);
		}

		super.onHandleCreated(e);
	}

	protected override void wndProc(ref Message m)
	{
		switch(m.Msg)
		{
			case WM_CLOSE:
			{
				scope CancelFormEventArgs e = new CancelFormEventArgs(this);
				this.onClosing(e);

				if(!e.cancel)
				{
					this.onClose(EventArgs.empty);

					if(Form.hasBit(this._cBits, ControlBits.MODAL_CONTROL))
					{
						EnableWindow(this._hActiveWnd, true);
						SetActiveWindow(this._hActiveWnd);
					}
					
					super.wndProc(m);
				}

				m.Result = 0;
			}
			break;

			default:
				super.wndProc(m);
				break;
		}
	}

	protected void onClosing(CancelFormEventArgs e)
	{
		this.closing(this, e);
	}

	protected void onClose(EventArgs e)
	{
		this.close(this, e);
	}
}
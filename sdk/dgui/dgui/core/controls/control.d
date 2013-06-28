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

module dgui.core.controls.control;

public import dgui.core.interfaces.idisposable;
public import dgui.core.events.controlcodeeventargs;
public import dgui.core.events.scrolleventargs;
public import dgui.core.events.mouseeventargs;
public import dgui.core.events.painteventargs;
public import dgui.core.events.keyeventargs;
public import dgui.core.events.event;
public import dgui.core.windowclass;
public import dgui.core.message;
public import dgui.core.charset;
public import dgui.core.winapi;
public import dgui.core.exception;
public import dgui.core.geometry;
public import dgui.core.collection;
public import dgui.core.handle;
public import dgui.core.utils;
public import dgui.core.tag;
public import dgui.contextmenu;
public import dgui.canvas;

enum DockStyle: ubyte
{
	NONE 	= 0,
	LEFT 	= 1,
	TOP 	= 2,
	RIGHT 	= 4,
	BOTTOM 	= 8,
	FILL 	= 16,
}

enum PositionSpecified
{
	POSITION = 0,
	SIZE     = 1,
	ALL      = 2,
}

enum ControlBits: ulong
{
	NONE          		= 0,
	ERASED        		= 1,
	MOUSE_ENTER   		= 2,
	CAN_NOTIFY   		= 4,
	MODAL_CONTROL 		= 8,   // For Modal Dialogs
	DOUBLE_BUFFERED		= 16,  // Use DGui's double buffered routine to draw components (be careful with this one!)
	OWN_CLICK_MSG 	    = 32,  // Does the component Handles click itself?
	CANNOT_ADD_CHILD	= 64,  // The child window will not be added to the parent's child controls' list
	USE_CACHED_TEXT		= 128, // Does not send WM_SETTEXT / WM_GETTEXT messages, but it uses it's internal variable only.
}

enum BorderStyle: ubyte
{
	NONE 		 = 0,
	MANUAL 		 = 1, // Internal Use
	FIXED_SINGLE = 2,
	FIXED_3D	 = 4,
}

struct CreateControlParams
{
	string ClassName;
	string SuperclassName; //Used in Superlassing
	Color DefaultBackColor;
	Color DefaultForeColor;
	Cursor DefaultCursor;
	ClassStyles ClassStyle;
}

abstract class Control: Handle!(HWND), IDisposable
{
	private ContextMenu _menu;
	private Control _parent;
	private ContextMenu _ctxMenu;
	private Font _defaultFont;
	private Cursor _defaultCursor;
	private HBRUSH _foreBrush;
	private HBRUSH _backBrush;
	private uint _extendedStyle = 0;
	private uint _style = WS_VISIBLE;
	protected string _text;
	protected Rect _bounds;
	protected Color _foreColor;
	protected Color _backColor;
	protected DockStyle _dock = DockStyle.NONE;
	protected ControlBits _cBits = ControlBits.CAN_NOTIFY;

	public Event!(Control, PaintEventArgs) paint;
	public Event!(Control, EventArgs) focusChanged;
	public Event!(Control, KeyCharEventArgs) keyChar;
	public Event!(Control, ControlCodeEventArgs) controlCode;
	public Event!(Control, KeyEventArgs) keyDown;
	public Event!(Control, KeyEventArgs) keyUp;
	public Event!(Control, MouseEventArgs) doubleClick;
	public Event!(Control, MouseEventArgs) mouseKeyDown;
	public Event!(Control, MouseEventArgs) mouseKeyUp;
	public Event!(Control, MouseEventArgs) mouseMove;
	public Event!(Control, MouseEventArgs) mouseEnter;
	public Event!(Control, MouseEventArgs) mouseLeave;
	public Event!(Control, EventArgs) visibleChanged;
	public Event!(Control, EventArgs) handleCreated;
	public Event!(Control, EventArgs) resize;
	public Event!(Control, EventArgs) click;

    mixin TagProperty; // Insert tag() property in Control

	public this()
	{
		
	}

	public ~this()
	{
		this.dispose();
	}

	public void dispose()
	{
		if(this._backBrush)
		{
			DeleteObject(this._backBrush);
		}

		if(this._foreBrush)
		{
			DeleteObject(this._foreBrush);
		}

		if(this._handle)
		{
			/* From MSDN: Destroys the specified window. 
			   The function sends WM_DESTROY and WM_NCDESTROY messages to the window 
			   to deactivate it and remove the keyboard focus from it. 
			   The function also destroys the window's menu, flushes the thread message queue, 
			   destroys timers, removes clipboard ownership, and breaks the clipboard viewer chain 
			   (if the window is at the top of the viewer chain). If the specified window is a parent 
			   or owner window, DestroyWindow automatically destroys the associated child or owned 
			   windows when it destroys the parent or owner window. The function first destroys child 
			   or owned windows, and then it destroys the parent or owner window
			*/
			
			DestroyWindow(this._handle);
		}
		
		this._handle = null;
	}
	

	public static void convertRect(ref Rect rect, Control from, Control to)
	{
		MapWindowPoints(from ? from.handle : null, to ? to.handle : null, cast(POINT*)&rect.rect, 2);
	}
	
	public static void convertPoint(ref Point pt, Control from, Control to)
	{
		MapWindowPoints(from ? from.handle : null, to ? to.handle : null, &pt.point, 1);
	}
	
	public static void convertSize(ref Size sz, Control from, Control to)
	{
		MapWindowPoints(from ? from.handle : null, to ? to.handle : null, cast(POINT*)&sz.size, 1);
	}		
	
	@property public final Rect bounds()
	{
		return this._bounds;
 	}

	@property public void bounds(Rect rect)
	{
		this._bounds = rect;
		
		if(this.created)
		{
			this.setWindowPos(rect.left, rect.top, rect.width, rect.height);
		}
	}

	@property public final BorderStyle borderStyle()
	{
		if(this.getExStyle() & WS_EX_CLIENTEDGE)
		{
			return BorderStyle.FIXED_3D;
		}
		else if(this.getStyle() & WS_BORDER)
		{
			return BorderStyle.FIXED_SINGLE;
		}

		return BorderStyle.NONE;
	}

	@property public final void borderStyle(BorderStyle bs)
	{		
		switch(bs)
		{
			case BorderStyle.FIXED_3D:
				this.setStyle(WS_BORDER, false);
				this.setExStyle(WS_EX_CLIENTEDGE, true);
				break;

			case BorderStyle.FIXED_SINGLE:
				this.setStyle(WS_BORDER, true);
				this.setExStyle(WS_EX_CLIENTEDGE, false);
				break;
			
			case BorderStyle.NONE:
				this.setStyle(WS_BORDER, false);
				this.setExStyle(WS_EX_CLIENTEDGE, false);
				break;

			default:
				assert(0, "Unknown Border Style");
				//break;
		}
	}

	@property public final Control parent()
	{
		return this._parent;
	}

	@property public void parent(Control c)
	{
		this._parent = c;
		
		if(!Control.hasBit(this._cBits, ControlBits.CANNOT_ADD_CHILD))
		{
			c.sendMessage(DGUI_ADDCHILDCONTROL, winCast!(WPARAM)(this), 0);
		}
	}

	@property public final Control topLevelControl()
	{
		Control topCtrl = this;

		while(topCtrl.parent)
		{
			topCtrl = topCtrl.parent;
		}

		return topCtrl;
	}

	public final Canvas createCanvas()
	{
		return Canvas.fromHDC(GetDC(this._handle));
	}

	public final void focus()
	{
		if(this.created)
		{
			SetFocus(this._handle);
		}
	}
	
	@property public bool focused()
	{
		if(this.created)
		{
			return GetFocus() == this._handle;
		}
		
		return false;
	}

	@property public final Color backColor()
	{
		return this._backColor;
	}

	@property public final void backColor(Color c)
	{
		if(this._backBrush)
		{
			DeleteObject(this._backBrush);
		}
		
		this._backColor = c;
		this._backBrush = CreateSolidBrush(c.colorref);
		
		if(this.created)
		{
			this.invalidate();
		}
	}

	@property public final Color foreColor()
	{
		return this._foreColor;
	}

	@property public final void foreColor(Color c)
	{
		if(this._foreBrush)
		{
			DeleteObject(this._foreBrush);
		}
		
		this._foreColor = c;
		this._foreBrush = CreateSolidBrush(c.colorref);
		
		if(this.created)
		{
			this.invalidate();
		}
	}

	@property public final bool scrollBars()
	{
		return cast(bool)(this.getStyle() & (WS_VSCROLL | WS_HSCROLL));
	}

	@property public final void scrollBars(bool b)
	{
		this.setStyle(WS_VSCROLL | WS_HSCROLL, true);
	}

	@property public string text()
	{
		if(this.created && !Control.hasBit(this._cBits, ControlBits.USE_CACHED_TEXT))
		{
			return getWindowText(this._handle);
		}
		
		return this._text;
	}

	@property public void text(string s) //Overwritten in TabPage
	{
		this._text = s;

		if(this.created && !Control.hasBit(this._cBits, ControlBits.USE_CACHED_TEXT))
		{
			Control.setBit(this._cBits, ControlBits.CAN_NOTIFY, false); //Do not trigger TextChanged Event
			setWindowText(this._handle, s);
			Control.setBit(this._cBits, ControlBits.CAN_NOTIFY, true);
		}
	}

	@property public final Font font()
	{	
		if(!this._defaultFont)
		{
			/* Font is not set, use Windows Font */
			this._defaultFont = SystemFonts.windowsFont;
		}
		
		return this._defaultFont;
	}

	@property public final void font(Font f)
	{		
		if(this.created)
		{
			if(this._defaultFont)
			{
				this._defaultFont.dispose();
			}
			
			this.sendMessage(WM_SETFONT, cast(WPARAM)f.handle, true);
		}

		this._defaultFont = f;
	}

	@property public final Point position()
	{
		return this.bounds.position;
	}

	@property public final void position(Point pt)
	{
		this._bounds.position = pt;

		if(this.created)
		{
			this.setPosition(pt.x, pt.y);
		}
	}

	@property public final Size size()
	{
		return this._bounds.size;
 	}

	@property public final void size(Size sz)
	{
		this._bounds.size = sz;

		if(this.created)
		{
			this.setSize(sz.width, sz.height);
		}
	}
	
	@property public final Size clientSize()
	{
		if(this.created)
		{
			Rect r = void;
			
			GetClientRect(this._handle, &r.rect);
			return r.size;
		}
		
		return this.size;
	}

	@property public final ContextMenu contextMenu()
	{
		return this._ctxMenu;
	}

	@property public final void contextMenu(ContextMenu cm)
	{
		if(this._ctxMenu !is cm)
		{
			if(this._ctxMenu)
			{
				this._ctxMenu.dispose();
			}
			
			this._ctxMenu = cm;
		}
	}	

	@property public final int width()
	{
		return this._bounds.width;
	}

	@property public final void width(int w)
	{
		this._bounds.width = w;

		if(this.created)
		{
			this.setSize(w, this._bounds.height);
		}
	}

	@property public final int height()
	{
		return this._bounds.height;
	}

	@property public final void height(int h)
	{
		this._bounds.height = h;

		if(this.created)
		{
			this.setSize(this._bounds.width, h);
		}
	}

	@property public final DockStyle dock()
	{
		return this._dock;
	}

	@property public final void dock(DockStyle ds)
	{
		this._dock = ds;
	}

	@property public final Cursor cursor()
	{
		if(this.created)
		{
			return Cursor.fromHCURSOR(cast(HCURSOR)GetClassLongW(this._handle, GCL_HCURSOR), false);
		}

		return this._defaultCursor;
	}

	@property public final void cursor(Cursor c)
	{
		if(this._defaultCursor)
		{
			this._defaultCursor.dispose();
		}
		
		this._defaultCursor = c;
		
		if(this.created)
		{
			this.sendMessage(WM_SETCURSOR, cast(WPARAM)this._handle, 0);
		}
	}

	@property public final bool visible()
	{
		return cast(bool)(this.getStyle() & WS_VISIBLE);
	}

	@property public final void visible(bool b)
	{
		b ? this.show() : this.hide();
	}

	@property public final bool enabled()
	{
		return !(this.getStyle() & WS_DISABLED);
	}

	@property public final void enabled(bool b)
	{
		if(this.created)
		{
			EnableWindow(this._handle, b);
		}
		else
		{
			this.setStyle(WS_DISABLED, !b);
		}
	}

	public void show()
	{ 
		if(this.created)
		{
			SetWindowPos(this._handle, null, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
			
			if(this._parent)
			{
				this._parent.sendMessage(DGUI_DOLAYOUT, 0, 0);
			}
		}
		else
		{
			this.setStyle(WS_VISIBLE, true);
			this.create(); //The component is not created, create it now
		}
	}

	public final void hide()
	{
		if(this.created)
		{
			SetWindowPos(this._handle, null, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOMOVE | SWP_NOSIZE | SWP_HIDEWINDOW);
		}
		else
		{
			this.setStyle(WS_VISIBLE, false);
		}
	}

	public final void redraw()
	{
		SetWindowPos(this._handle, null, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);
	}

	public final void invalidate()
	{
		RedrawWindow(this._handle, null, null, RDW_ERASE | RDW_INVALIDATE | RDW_UPDATENOW);
	}
	
	public final void sendMessage(ref Message m)
	{
		/*
		 * SendMessage() emulation: it allows to send messages even if the control is not created,
		 * it is useful in order to send custom messages to components.
		 */
		
		if(m.Msg >= DGUI_BASE) /* DGui's Custom Message Handling */
		{
			this.onDGuiMessage(m);
		}
		else /* Window Procedure Message Handling */
		{
			//Control.setBit(this._cBits, ControlBits.CAN_NOTIFY, false);
			this.wndProc(m);
			//Control.setBit(this._cBits, ControlBits.CAN_NOTIFY, true);
		}
	}
	
	public final uint sendMessage(uint msg, WPARAM wParam, LPARAM lParam)
	{
		Message m = Message(this._handle, msg, wParam, lParam);
		this.sendMessage(m);
		
		return cast(uint)m.Result;
	}
	
	extern(Windows) protected static LRESULT msgRouter(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam) nothrow
	{		
		try
		{
			if(msg == WM_NCCREATE)
			{
				/*
				 * TRICK: Id == hWnd
				 * ---
				 * Inizializzazione Componente
				 */
		
				CREATESTRUCTW* pCreateStruct = cast(CREATESTRUCTW*)lParam;
				LPARAM param = cast(LPARAM)pCreateStruct.lpCreateParams;
				SetWindowLongW(hWnd, GWL_USERDATA, cast(int)param);
				SetWindowLongW(hWnd, GWL_ID, cast(uint)hWnd);

				Control theThis = winCast!(Control)(param);
				theThis._handle = hWnd;	//Assign handle.
			}

			Control theThis = winCast!(Control)(GetWindowLongW(hWnd, GWL_USERDATA));
			Message m = Message(hWnd, msg, wParam, lParam);

			if(theThis)
			{
				theThis.wndProc(m);
			}
			else
			{
				Control.defWindowProc(m);
			}
			return m.Result;
		}
		catch (Exception)
		{
			assert(false, "msgRouter failed");
			return E_FAIL;
		}
	}

	private void onMenuCommand(WPARAM wParam, LPARAM lParam)
	{
		MENUITEMINFOW minfo;
		
		minfo.cbSize = MENUITEMINFOW.sizeof;
		minfo.fMask = MIIM_DATA;
		
		if(GetMenuItemInfoW(cast(HMENU)lParam, cast(UINT)wParam, TRUE, &minfo))
		{
			MenuItem sender = winCast!(MenuItem)(minfo.dwItemData);
			sender.performClick();
		}
	}

	private void create()
	{
		CreateControlParams ccp;
		ccp.DefaultBackColor = SystemColors.colorBtnFace;
		ccp.DefaultForeColor = SystemColors.colorBtnText;

		this.createControlParams(ccp);

		this._backBrush = CreateSolidBrush(ccp.DefaultBackColor.colorref);
		this._foreBrush = CreateSolidBrush(ccp.DefaultForeColor.colorref);

		if(ccp.DefaultCursor)
		{
			this._defaultCursor = ccp.DefaultCursor;
		}

		if(!this._defaultFont)
		{
			this._defaultFont = SystemFonts.windowsFont;
		}
		
		if(!this._backColor.valid) // Invalid Color
		{
			this.backColor = ccp.DefaultBackColor;
		}
		
		if(!this._foreColor.valid) // Invalid Color
		{
			this.foreColor = ccp.DefaultForeColor;
		}

		HWND hParent = null;

		if(Control.hasBit(this._cBits, ControlBits.MODAL_CONTROL)) //Is Modal ?
		{
			hParent = GetActiveWindow();
			this.setStyle(WS_CHILD, false);
			this.setStyle(WS_POPUP, true);
		}
		else if(this._parent)
		{
			hParent = this._parent.handle;
			
			/* As MSDN says:
			    WS_POPUP: The windows is a pop-up window. *** This style cannot be used with the WS_CHILD style. *** */
			
			if(!(this.getStyle() & WS_POPUP)) //The windows doesn't have WS_POPUP style, set WS_CHILD style.
			{
				this.setStyle(WS_CHILD, true);
			}
			
			this.setStyle(WS_CLIPSIBLINGS, true);
		}

		createWindowEx(this.getExStyle(),
					   ccp.ClassName,
					   this._text, 
					   this.getStyle(),
					   this._bounds.x,
					   this._bounds.y,
					   this._bounds.width,
					   this._bounds.height,
					   hParent,
					   winCast!(void*)(this));
		
		if(!this._handle)
		{
			throwException!(Win32Exception)("Control Creation failed: (ClassName: '%s', Text: '%s')", 
											ccp.ClassName, this._text);
		}
		
		UpdateWindow(this._handle);
		
		if(this._parent)
		{
			this._parent.sendMessage(DGUI_CHILDCONTROLCREATED, winCast!(WPARAM)(this), 0); //Notify the parent window
		}
	}
	
	private void setPosition(int x, int y)
	{
		this.setWindowPos(x, y, 0, 0, PositionSpecified.POSITION);
	}
	
	private void setSize(int w, int h)
	{
		this.setWindowPos(0, 0, w, h, PositionSpecified.SIZE);
	}
	
	private void setWindowPos(int x, int y, int w, int h, PositionSpecified ps = PositionSpecified.ALL)
	{
		uint wpf = SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOMOVE | SWP_NOSIZE;
		
		if(ps !is PositionSpecified.ALL)
		{
			if(ps is PositionSpecified.POSITION)
			{
				wpf &= ~SWP_NOMOVE;
			}
			else //if(ps is PositionSpecified.SIZE)
			{
				wpf &= ~SWP_NOSIZE;
			}
		}
		else
		{
			wpf &= ~(SWP_NOMOVE | SWP_NOSIZE);
		}
		
		SetWindowPos(this._handle, null, x, y, w, h, wpf); //Bounds updated in WM_WINDOWPOSCHANGED
	}
	
	private void drawMenuItemImage(DRAWITEMSTRUCT* pDrawItem)
	{
		MenuItem mi = winCast!(MenuItem)(pDrawItem.itemData);
		
		if(mi)
		{
			scope Canvas c = Canvas.fromHDC(pDrawItem.hDC, false); //HDC *Not* Owned by Canvas Object
			int icoSize = GetSystemMetrics(SM_CYMENU);
			c.drawImage(mi.rootMenu.imageList.images[mi.imageIndex], Rect(0, 0, icoSize, icoSize));
		}
	}	
	
	protected final uint getStyle()
	{
		if(this.created)
		{
			return GetWindowLongW(this._handle, GWL_STYLE);
		}

		return this._style;
	}

	protected final void setStyle(uint cstyle, bool set)
	{
		if(this.created)
		{
			uint style = this.getStyle();
			set ? (style |= cstyle) : (style &= ~cstyle);

			SetWindowLongW(this._handle, GWL_STYLE, style);
			this.redraw();
			this._style = style;
		}
		else
		{
			set ? (this._style |= cstyle) : (this._style &= ~cstyle);
		}
	}

	protected static final void setBit(ref ulong rBits, ulong rBit, bool set)
	{
		set ? (rBits |= rBit) : (rBits &= ~rBit);
	}
	
	protected static final bool hasBit(ref ulong rBits, ulong rBit)
	{
		return cast(bool)(rBits & rBit);
	}

	protected final uint getExStyle()
	{
		if(this.created)
		{
			return GetWindowLongW(this._handle, GWL_EXSTYLE);
		}

		return this._extendedStyle;
	}

	protected final void setExStyle(uint cstyle, bool set)
	{
		if(this.created)
		{
			uint exStyle = this.getExStyle();
			set ? (exStyle |= cstyle) : (exStyle &= ~cstyle);
		
			SetWindowLongW(this._handle, GWL_EXSTYLE, exStyle);
			this.redraw();
			this._extendedStyle = exStyle;
		}
		else
		{
			set ? (this._extendedStyle |= cstyle) : (this._extendedStyle &= ~cstyle);
		}
	}
	
	protected void createControlParams(ref CreateControlParams ccp)
	{
		ClassStyles cstyle = ccp.ClassStyle | ClassStyles.DBLCLKS;
		
		WindowClass.register(ccp.ClassName, cstyle, ccp.DefaultCursor, &Control.msgRouter);
	}
	
	protected uint originalWndProc(ref Message m)
	{
		return Control.defWindowProc(m);
	}

	protected static uint defWindowProc(ref Message m)
	{
		if(IsWindowUnicode(m.hWnd))
		{
			m.Result = DefWindowProcW(m.hWnd, m.Msg, m.wParam, m.lParam);
		}
		else
		{
			m.Result = DefWindowProcA(m.hWnd, m.Msg, m.wParam, m.lParam);
		}
		
		return cast(uint)(m.Result);
	}

	protected void onDGuiMessage(ref Message m)
	{
		switch(m.Msg)
		{
			case DGUI_REFLECTMESSAGE:
				Message rm = *(cast(Message*)m.wParam);
				this.onReflectedMessage(rm);
				*(cast(Message*)m.wParam) = rm; //Copy the result, so the parent can return result.
				//m.Result = rm.Result; // No result here! 
				break;
			
			case DGUI_CREATEONLY:
			{
				if(!this.created)
				{
					this.create();
				}
			}
			break;
			
			default:
				m.Result = 0;
				break;
		}
	}
	
	protected void onReflectedMessage(ref Message m)
	{
		switch(m.Msg)
		{
			case WM_CTLCOLOREDIT, WM_CTLCOLORBTN:
				SetBkColor(cast(HDC)m.wParam, this.backColor.colorref);
				SetTextColor(cast(HDC)m.wParam, this.foreColor.colorref);
				m.Result = cast(LRESULT)this._backBrush;
				break;
			
			case WM_MEASUREITEM:
			{
				MEASUREITEMSTRUCT* pMeasureItem = cast(MEASUREITEMSTRUCT*)m.lParam;
				
				if(pMeasureItem.CtlType == ODT_MENU)
				{
					MenuItem mi = winCast!(MenuItem)(pMeasureItem.itemData);
				
					if(mi)
					{
						if(mi.parent.handle == GetMenu(this._handle))// Check if parent of 'mi' is the menu bar
						{
							FontMetrics fm = this.font.metrics;
							
							int icoSize = GetSystemMetrics(SM_CYMENU);
							pMeasureItem.itemWidth = icoSize + fm.MaxCharWidth;
						}
						else
						{
							pMeasureItem.itemWidth = 10;
						}
					}
				}				
			}
			break;
			
			case WM_DRAWITEM:
			{
				DRAWITEMSTRUCT* pDrawItem = cast(DRAWITEMSTRUCT*)m.lParam;
			
				if(pDrawItem.CtlType == ODT_MENU)
				{
					this.drawMenuItemImage(pDrawItem);
				}
			}
			break;
			
			default:
				//Control.defWindowProc(m);
				break;
		}
	}

	protected void onClick(EventArgs e)
	{
		this.click(this, e);
	}

	protected void onKeyUp(KeyEventArgs e)
	{
		this.keyUp(this, e);
	}

	protected void onKeyDown(KeyEventArgs e)
	{
		this.keyDown(this, e);
	}

	protected void onKeyChar(KeyCharEventArgs e)
	{
		this.keyChar(this, e);
	}

	protected void onPaint(PaintEventArgs e)
	{
		this.paint(this, e);
	}

	protected void onHandleCreated(EventArgs e)
	{
		this.handleCreated(this, e);
	}

	protected void onResize(EventArgs e)
	{
		this.resize(this, e);
	}
	
	protected void onVisibleChanged(EventArgs e)
	{
		this.visibleChanged(this, e);
	}

	protected void onMouseKeyDown(MouseEventArgs e)
	{
		this.mouseKeyDown(this, e);
	}

	protected void onMouseKeyUp(MouseEventArgs e)
	{
		this.mouseKeyUp(this, e);
	}

	protected void onDoubleClick(MouseEventArgs e)
	{
		this.doubleClick(this, e);
	}

	protected void onMouseMove(MouseEventArgs e)
	{
		this.mouseMove(this, e);
	}

	protected void onMouseEnter(MouseEventArgs e)
	{
		this.mouseEnter(this, e);
	}

	protected void onMouseLeave(MouseEventArgs e)
	{
		this.mouseLeave(this, e);
	}
	
	protected void onFocusChanged(EventArgs e)
	{
		this.focusChanged(this, e);
	}
	
	protected void onControlCode(ControlCodeEventArgs e)
	{
		this.controlCode(this, e);
	}
	
	protected void wndProc(ref Message m)
	{
		switch(m.Msg)
		{
			case WM_ERASEBKGND:
				m.Result = 0; // Do nothing here, handle it in WM_PAINT
				break;
			
			case WM_PAINT:
			{
				HDC hdc;
				Rect clipRect;
				PAINTSTRUCT ps;

				if(!m.wParam)
				{
					hdc = BeginPaint(this._handle, &ps);
					clipRect = Rect.fromRECT(&ps.rcPaint); //Clip Rectangle
				}
				else // Assume WPARAM as HDC
				{
					hdc = cast(HDC)m.wParam;
					GetUpdateRect(this._handle, &clipRect.rect, false);
				}
				
				FillRect(hdc, &clipRect.rect, this._backBrush); //Fill with background color;
				
				scope Canvas c = Canvas.fromHDC(hdc, false);
				scope PaintEventArgs e = new PaintEventArgs(c, clipRect);
				this.onPaint(e);
				
				if(!m.wParam)
				{
					EndPaint(this._handle, &ps);
				}
				
				m.Result = 0;
			}
			break;
			
			case WM_CREATE: // Aggiornamento Font, rimuove FIXED SYS
			{				
				this.sendMessage(WM_SETFONT, cast(WPARAM)this._defaultFont.handle, true);

				if(this._ctxMenu)
				{
					HMENU hDefaultMenu = GetMenu(this._handle);

					if(hDefaultMenu)
					{
						DestroyMenu(hDefaultMenu); //Destroy default menu (if exists)
					}
					
					this._ctxMenu.create();
				}
				
				this.onHandleCreated(EventArgs.empty);
				m.Result = 0; //Continue..
			}
			break;
			
			case WM_WINDOWPOSCHANGED:
			{
				WINDOWPOS* pWndPos = cast(WINDOWPOS*)m.lParam;

				if(!(pWndPos.flags & SWP_NOMOVE) || !(pWndPos.flags & SWP_NOSIZE))
				{
					/* Note: 'pWndPos' has NonClient coordinates */
					
					if(!(pWndPos.flags & SWP_NOMOVE))
					{
						this._bounds.x = pWndPos.x;
						this._bounds.y = pWndPos.y;
					}
					
					if(!(pWndPos.flags & SWP_NOSIZE))
					{
						this._bounds.width = pWndPos.cx;
						this._bounds.height = pWndPos.cy;
					}
					
					if(!(pWndPos.flags & SWP_NOSIZE))
					{
						this.onResize(EventArgs.empty);
					}
				}
				else if(pWndPos.flags & SWP_SHOWWINDOW || pWndPos.flags & SWP_HIDEWINDOW)
				{
					if(pWndPos.flags & SWP_SHOWWINDOW && this._parent)
					{
						this._parent.sendMessage(DGUI_DOLAYOUT, 0, 0);
					}

					this.onVisibleChanged(EventArgs.empty);
				}
				
				this.originalWndProc(m); //Send WM_SIZE too
			}
			break;

			case WM_KEYDOWN:
			{				
				scope KeyEventArgs e = new KeyEventArgs(cast(Keys)m.wParam);
				this.onKeyDown(e);				

				if(e.handled)
				{
					this.originalWndProc(m);
				}
				else
				{
					m.Result = 0;
				}
			}
			break;

			case WM_KEYUP:
			{
				scope KeyEventArgs e = new KeyEventArgs(cast(Keys)m.wParam);
				this.onKeyUp(e);

				if(e.handled)
				{
					this.originalWndProc(m);
				}
				else
				{
					m.Result = 0;
				}
			}
			break;

			case WM_CHAR:
			{				
				scope KeyCharEventArgs e = new KeyCharEventArgs(cast(Keys)m.wParam, cast(char)m.wParam);
				this.onKeyChar(e);

				if(e.handled)
				{
					this.originalWndProc(m);
				}
				else
				{
					m.Result = 0;
				}
			}
			break;
			
			case WM_MOUSELEAVE:
			{
				Control.setBit(this._cBits, ControlBits.MOUSE_ENTER, false);
				
				scope MouseEventArgs e = new MouseEventArgs(Point(LOWORD(m.lParam), HIWORD(m.lParam)), cast(MouseKeys)m.wParam);
				this.onMouseLeave(e);

				this.originalWndProc(m);
			}
			break;
			
			case WM_MOUSEMOVE:
			{
				scope MouseEventArgs e = new MouseEventArgs(Point(LOWORD(m.lParam), HIWORD(m.lParam)), cast(MouseKeys)m.wParam);
				this.onMouseMove(e);

				if(!Control.hasBit(this._cBits, ControlBits.MOUSE_ENTER))
				{
					Control.setBit(this._cBits, ControlBits.MOUSE_ENTER, true);
					
					TRACKMOUSEEVENT tme;

					tme.cbSize = TRACKMOUSEEVENT.sizeof;
					tme.dwFlags = TME_LEAVE;
					tme.hwndTrack = this._handle;

					TrackMouseEvent(&tme);

					this.onMouseEnter(e);
				}

				this.originalWndProc(m);
			}
			break;
			
			case WM_LBUTTONDOWN, WM_MBUTTONDOWN, WM_RBUTTONDOWN:
			{
				scope MouseEventArgs e = new MouseEventArgs(Point(LOWORD(m.lParam), HIWORD(m.lParam)), cast(MouseKeys)m.wParam);
				this.onMouseKeyDown(e);

				this.originalWndProc(m);
			}
			break;

			case WM_LBUTTONUP, WM_MBUTTONUP, WM_RBUTTONUP:
			{
				MouseKeys mk = MouseKeys.NONE;

				if(GetAsyncKeyState(MK_LBUTTON))
				{
					mk |= MouseKeys.LEFT;
				}

				if(GetAsyncKeyState(MK_MBUTTON))
				{
					mk |= MouseKeys.MIDDLE;
				}

				if(GetAsyncKeyState(MK_RBUTTON))
				{
					mk |= MouseKeys.RIGHT;
				}
				
				Point p = Point(LOWORD(m.lParam), HIWORD(m.lParam));				
				scope MouseEventArgs e = new MouseEventArgs(p, mk);
				this.onMouseKeyUp(e);
				
				Control.convertPoint(p, this, null);

				if(m.Msg == WM_LBUTTONUP && !Control.hasBit(this._cBits, ControlBits.OWN_CLICK_MSG) && WindowFromPoint(p.point) == this._handle)
				{
					this.onClick(EventArgs.empty);
				}
				
				this.originalWndProc(m);
			}
			break;

			case WM_LBUTTONDBLCLK, WM_MBUTTONDBLCLK, WM_RBUTTONDBLCLK:
			{				
				scope MouseEventArgs e = new MouseEventArgs(Point(LOWORD(m.lParam), HIWORD(m.lParam)), cast(MouseKeys)m.wParam);
				this.onDoubleClick(e);

				this.originalWndProc(m);
			}
			break;
			
			case WM_SETCURSOR:
			{
				if(cast(HWND)m.wParam == this._handle && this._defaultCursor && cast(LONG)this._defaultCursor.handle != GetClassLongW(this._handle, GCL_HCURSOR))
				{
					SetClassLongW(this._handle, GCL_HCURSOR, cast(LONG)this._defaultCursor.handle);
				}

				this.originalWndProc(m); //Continue cursor selection
			}
			break;
		
			case WM_MENUCOMMAND:
				this.onMenuCommand(m.wParam, m.lParam);
				break;

			case WM_CONTEXTMENU:
			{
				if(this._ctxMenu)
				{
					this._ctxMenu.popupMenu(this._handle, Cursor.position);
				}

				this.originalWndProc(m);				
			}
			break;

			case WM_SETFOCUS, WM_KILLFOCUS:
			{
				this.onFocusChanged(EventArgs.empty);
				this.originalWndProc(m);
			}
			break;
			
			case WM_GETDLGCODE:
			{
				scope ControlCodeEventArgs e = new ControlCodeEventArgs();
				this.onControlCode(e);
				
				if(e.controlCode is ControlCode.IGNORE)
				{
					this.originalWndProc(m);
				}
				else
				{
					m.Result = e.controlCode;
				}
			}
			break;
			
			case WM_INITMENU:
			{
				if(this._ctxMenu)
				{
					this._ctxMenu.onPopup(EventArgs.empty);
				}
				
				m.Result = 0;
			}
			break;

			default:
				this.originalWndProc(m);
				break;
		}
	}
}
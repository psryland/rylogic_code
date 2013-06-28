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

module dgui.core.controls.subclassedcontrol;

public import dgui.core.controls.reflectedcontrol;

abstract class SubclassedControl: ReflectedControl
{
	private WNDPROC _oldWndProc; // Original Window Procedure
	
	protected override void createControlParams(ref CreateControlParams ccp)
	{		
		this._oldWndProc = WindowClass.superclass(ccp.SuperclassName, ccp.ClassName, &SubclassedControl.msgRouter);
	}
	
	protected override uint originalWndProc(ref Message m)
	{
		if(IsWindowUnicode(this._handle))
		{
			m.Result = CallWindowProcW(this._oldWndProc, this._handle, m.Msg, m.wParam, m.lParam);
		}
		else
		{
			m.Result = CallWindowProcA(this._oldWndProc, this._handle, m.Msg, m.wParam, m.lParam);
		}
		
		return cast(uint)m.Result;
	}

	protected override void wndProc(ref Message m)
	{
		switch(m.Msg)
		{
			case WM_ERASEBKGND:
			{
				if(SubclassedControl.hasBit(this._cBits, ControlBits.DOUBLE_BUFFERED))
				{
					Rect r = void;
					GetUpdateRect(this._handle, &r.rect, false);
					
					scope Canvas orgCanvas = Canvas.fromHDC(cast(HDC)m.wParam, false); //Don't delete it, it's a DC from WM_ERASEBKGND or WM_PAINT
					scope Canvas memCanvas = orgCanvas.createInMemory(); // Off Screen Canvas
					
					Message rm = m;
					
					rm.Msg = WM_ERASEBKGND;
					rm.wParam = cast(WPARAM)memCanvas.handle;
					this.originalWndProc(rm);
					
					rm.Msg = WM_PAINT;
					//rm.wParam = cast(WPARAM)memCanvas.handle;
					this.originalWndProc(rm);
						
					scope PaintEventArgs e = new PaintEventArgs(memCanvas, r);
					this.onPaint(e);
					
					memCanvas.copyTo(orgCanvas, r, r.position);
					SubclassedControl.setBit(this._cBits, ControlBits.ERASED, true);
					m.Result = 0;
				}
				else
				{
					this.originalWndProc(m);
				}
			}
			break;
			
			case WM_PAINT:
			{
				if(SubclassedControl.hasBit(this._cBits, ControlBits.DOUBLE_BUFFERED) && SubclassedControl.hasBit(this._cBits, ControlBits.ERASED))
				{
					SubclassedControl.setBit(this._cBits, ControlBits.ERASED, false);
					m.Result = 0;
				}
				else
				{
					/* *** Not double buffered *** */
					Rect r = void;
					GetUpdateRect(this._handle, &r.rect, false); //Keep drawing area
					this.originalWndProc(m);
					
					scope Canvas c = Canvas.fromHDC(m.wParam ? cast(HDC)m.wParam : GetDC(this._handle), m.wParam ? false : true);
					HRGN hRgn = CreateRectRgnIndirect(&r.rect);
					SelectClipRgn(c.handle, hRgn);
					DeleteObject(hRgn);

					SetBkColor(c.handle, this.backColor.colorref);
					SetTextColor(c.handle, this.foreColor.colorref);
					
					scope PaintEventArgs e = new PaintEventArgs(c, r);
					this.onPaint(e);
				}
			}
			break;

			case WM_CREATE:
				this.originalWndProc(m);
				super.wndProc(m);
				break;

			default:
				super.wndProc(m);
				break;
		}
	}
}
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

module dgui.core.controls.scrollablecontrol;

public import dgui.core.controls.reflectedcontrol;
public import dgui.core.events.mouseeventargs;
public import dgui.core.events.scrolleventargs;

abstract class ScrollableControl: ReflectedControl
{
	public Event!(Control, ScrollEventArgs) scroll;
	public Event!(Control, MouseWheelEventArgs) mouseWheel;
	
	protected final void scrollWindow(ScrollWindowDirection swd, int amount)
	{
		this.scrollWindow(swd, amount, NullRect);
	}	
	
	protected final void scrollWindow(ScrollWindowDirection swd, int amount, Rect rectScroll)
	{
		if(this.created)
		{
			switch(swd)
			{
				case ScrollWindowDirection.LEFT:
					ScrollWindowEx(this._handle, amount, 0, null, rectScroll == NullRect ? null : &rectScroll.rect, null, null, SW_INVALIDATE);
					break;
				
				case ScrollWindowDirection.UP:
					ScrollWindowEx(this._handle, 0, amount, null, rectScroll == NullRect ? null : &rectScroll.rect, null, null, SW_INVALIDATE);
					break;
				
				case ScrollWindowDirection.RIGHT:
					ScrollWindowEx(this._handle, -amount, 0, null, rectScroll == NullRect ? null : &rectScroll.rect, null, null, SW_INVALIDATE);
					break;
				
				case ScrollWindowDirection.DOWN:
					ScrollWindowEx(this._handle, 0, -amount, null, rectScroll == NullRect ? null : &rectScroll.rect, null, null, SW_INVALIDATE);
					break;
				
				default:
					break;
			}
		}
	}	
	
	protected void onMouseWheel(MouseWheelEventArgs e)
	{
		this.mouseWheel(this, e);
	}

	protected void onScroll(ScrollEventArgs e)
	{
		this.scroll(this, e);
	}
	
	protected override void wndProc(ref Message m)
	{
		switch(m.Msg)
		{
			case WM_MOUSEWHEEL:
			{
				short delta = GetWheelDelta(m.wParam);
				scope MouseWheelEventArgs e = new MouseWheelEventArgs(Point(LOWORD(m.lParam), HIWORD(m.lParam)), 
																      cast(MouseKeys)m.wParam, delta > 0 ? MouseWheel.UP : MouseWheel.DOWN);
				this.onMouseWheel(e);
				this.originalWndProc(m);
			}
			break;
			
			case WM_VSCROLL, WM_HSCROLL:
			{
				ScrollDirection sd = m.Msg == WM_VSCROLL ? ScrollDirection.VERTICAL : ScrollDirection.HORIZONTAL;
				ScrollMode sm = cast(ScrollMode)m.wParam;

				scope ScrollEventArgs e = new ScrollEventArgs(sd, sm);
				this.onScroll(e);

				this.originalWndProc(m);
			}
			break;
			
			default:
				break;
		}
		
		super.wndProc(m);
	}
}
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

module dgui.layout.layoutcontrol;

import dgui.core.interfaces.ilayoutcontrol;
public import dgui.core.controls.containercontrol;

class ResizeManager: Handle!(HDWP), IDisposable
{
	public this(int c)
	{
		if(c > 1)
		{
			this._handle = BeginDeferWindowPos(c);
		}
	}
	
	public ~this()
	{
		this.dispose();
	}
	
	public void dispose()
	{
		if(this._handle)
		{
			EndDeferWindowPos(this._handle);
		}
	}
	
	public void setPosition(Control ctrl, Point pt)
	{
		this.setPosition(ctrl, pt.x, pt.y);
	}
	
	public void setPosition(Control ctrl, int x, int y)
	{
		this.resizeControl(ctrl, x, y, 0, 0, PositionSpecified.POSITION);
	}
	
	public void setSize(Control ctrl, Size sz)
	{
		this.setSize(ctrl, sz.width, sz.height);
	}	
	
	public void setSize(Control ctrl, int w, int h)
	{
		this.resizeControl(ctrl, 0, 0, w, h, PositionSpecified.SIZE);
	}
	
	public void resizeControl(Control ctrl, Rect r, PositionSpecified ps = PositionSpecified.ALL)
	{
		this.resizeControl(ctrl, r.x, r.y, r.width, r.height, ps);
	}

	public void resizeControl(Control ctrl, int x, int y, int w, int h, PositionSpecified ps = PositionSpecified.ALL)
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
		
		if(this._handle)
		{
			this._handle = DeferWindowPos(this._handle, ctrl.handle, null, x, y, w, h, wpf);
		}
		else
		{
			SetWindowPos(ctrl.handle, null, x, y, w, h, wpf); //Bounds updated in WM_WINDOWPOSCHANGED
		}
	}	
}

abstract class LayoutControl: ContainerControl, ILayoutControl
{
	public override void show()
	{
		super.show();
		this.updateLayout();
	}
	
	public void updateLayout()
	{
		if(this._childControls && this.created && this.visible)
		{
			scope ResizeManager rm = new ResizeManager(this._childControls.length);
			Rect da = Rect(NullPoint, this.clientSize);
			
			foreach(Control c; this._childControls)
			{				
				if(da.empty)
				{
					rm.dispose();
					break;
				}
				
				if(c.dock !is DockStyle.NONE && c.visible && c.created)
				{
					switch(c.dock)
					{
						case DockStyle.LEFT:
							//c.bounds = Rect(da.left, da.top, c.width, da.height);
							rm.resizeControl(c, da.left, da.top, c.width, da.height);
							da.left += c.width;
							break;
						
						case DockStyle.TOP:
							//c.bounds = Rect(da.left, da.top, da.width, c.height);
							rm.resizeControl(c, da.left, da.top, da.width, c.height);
							da.top += c.height;
							break;

						case DockStyle.RIGHT:
							//c.bounds = Rect(da.right - c.width, da.top, c.width, da.height);
							rm.resizeControl(c, da.right - c.width, da.top, c.width, da.height);
							da.right -= c.width;
							break;

						case DockStyle.BOTTOM:
							//c.bounds = Rect(c, da.left, da.bottom - c.height, da.width, c.height);
							rm.resizeControl(c, da.left, da.bottom - c.height, da.width, c.height);
							da.bottom -= c.height;
							break;
						
						case DockStyle.FILL:
							//c.bounds = da;
							rm.resizeControl(c, da);
							da.size = NullSize;
							break;
						
						default:
							rm.dispose();
							assert(false, "Unknown DockStyle");
							//break;
					}
				}
			}
		}
	}	
	
	protected override void onDGuiMessage(ref Message m)
	{
		switch(m.Msg)
		{
			case DGUI_DOLAYOUT:
				this.updateLayout();
				break;
			
			case DGUI_CHILDCONTROLCREATED:
			{
				Control c = winCast!(Control)(m.wParam);
				
				if(c.dock !is DockStyle.NONE && c.visible)
				{
					this.updateLayout();
				}
			}
			break;
			
			default:
				break;
		}
		
		super.onDGuiMessage(m);
	}
	
	protected override void onHandleCreated(EventArgs e)
	{
		super.onHandleCreated(e);
		
		this.updateLayout();
	}
	
	protected override void onResize(EventArgs e)
	{
		this.updateLayout();
		
		InvalidateRect(this._handle, null, true);
		UpdateWindow(this._handle);
		super.onResize(e);
	}
}
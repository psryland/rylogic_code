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

module dgui.core.controls.ownerdrawcontrol;

public import dgui.core.controls.subclassedcontrol;
public import dgui.core.events.eventargs;

enum OwnerDrawMode: ubyte
{
	NORMAL = 0,
	FIXED = 1,
	VARIABLE = 2,
}

enum DrawItemState: uint
{
	DEFAULT  = ODS_DEFAULT,
	CHECKED  = ODS_CHECKED,
	DISABLED = ODS_DISABLED,
	FOCUSED  = ODS_FOCUS,
	GRAYED   = ODS_GRAYED,
	SELECTED = ODS_SELECTED,
}

class MeasureItemEventArgs: EventArgs
{
	private int _width;
	private int _height;
	private int _index;
	private Canvas _canvas;
	
	public this(Canvas c, int width, int height, int index)
	{
		this._canvas = c;
		this._width = width;
		this._height = height;
		this._index = index;
	}

	@property public Canvas canvas()
	{
		return this._canvas;
	}

	@property public int width()
	{
		return this._width;
	}

	@property public void width(int w)
	{
		this._width = w;
	}

	@property public int height()
	{
		return this._height;
	}

	@property public void height(int h)
	{
		this._height = h;
	}

	@property public int index()
	{
		return this._index;
	}
}

class DrawItemEventArgs: EventArgs
{
	private DrawItemState _state;
	private Color _foreColor;
	private Color _backColor;
	private Canvas _canvas;
	private Rect _itemRect;
	private int _index;

	public this(Canvas c, DrawItemState state, Rect itemRect, Color foreColor, Color backColor, int index)
	{
		this._canvas = c;
		this._state = state;
		this._itemRect = itemRect;
		this._foreColor = foreColor;
		this._backColor = backColor;
		this._index = index;
	}

	@property public Canvas canvas()
	{
		return this._canvas;
	}

	@property public DrawItemState itemState()
	{
		return this._state;
	}

	@property public Rect itemRect()
	{
		return this._itemRect;
	}

	@property public Color foreColor()
	{
		return this._foreColor;
	}

	@property public Color backColor()
	{
		return this._backColor;
	}

	public void drawBackground()
	{
		scope SolidBrush brush = new SolidBrush(this._backColor);
		this._canvas.fillRectangle(brush, this._itemRect);
	}

	public void drawFocusRect()
	{
		if(this._state & DrawItemState.FOCUSED)
		{
			DrawFocusRect(this._canvas.handle, &this._itemRect.rect);
		}
	}

	@property public int index()
	{
		return this._index;
	}
}

abstract class OwnerDrawControl: SubclassedControl
{
	public Event!(Control, MeasureItemEventArgs) measureItem;
	public Event!(Control, DrawItemEventArgs) drawItem;

	protected OwnerDrawMode _drawMode = OwnerDrawMode.NORMAL;

	@property public OwnerDrawMode drawMode()
	{
		return this._drawMode;
	}

	@property public void drawMode(OwnerDrawMode dm)
	{
		this._drawMode = dm;
	}

	protected void onMeasureItem(MeasureItemEventArgs e)
	{
		this.measureItem(this, e);
	}

	protected void onDrawItem(DrawItemEventArgs e)
	{
		this.drawItem(this, e);
	}

	protected override void onReflectedMessage(ref Message m)
	{
		switch(m.Msg)
		{
			case WM_MEASUREITEM:
			{
				MEASUREITEMSTRUCT* pMeasureItem = cast(MEASUREITEMSTRUCT*)m.lParam;
				HDC hdc = GetDC(this._handle);
				SetBkColor(hdc, this.backColor.colorref);
				SetTextColor(hdc, this.foreColor.colorref);
					
				scope Canvas c = Canvas.fromHDC(hdc);
				scope MeasureItemEventArgs e = new MeasureItemEventArgs(c, pMeasureItem.itemWidth, pMeasureItem.itemHeight, 
																		   pMeasureItem.itemID);
																		   
				this.onMeasureItem(e);

				if(e.width)
				{
					pMeasureItem.itemWidth = e.width;
				}

				if(e.height)
				{
					pMeasureItem.itemHeight = e.height;
				}

				ReleaseDC(this._handle, null);
			}
			break;

			case WM_DRAWITEM:
			{
				DRAWITEMSTRUCT* pDrawItem = cast(DRAWITEMSTRUCT*)m.lParam;
				Rect r = Rect.fromRECT(&pDrawItem.rcItem);

				Color fc, bc;

				if(pDrawItem.itemState & ODS_SELECTED)
				{
					fc = SystemColors.colorHighLightText;
					bc = SystemColors.colorHighLight;
				}
				else
				{
					fc = this.foreColor;
					bc = this.backColor;
				}

				scope Canvas c = Canvas.fromHDC(pDrawItem.hDC);
				scope DrawItemEventArgs e = new DrawItemEventArgs(c, cast(DrawItemState)pDrawItem.itemState, 
																  r, fc, bc, pDrawItem.itemID);

				this.onDrawItem(e);
			}
			break;
			
			default:
				break;
		}

		super.onReflectedMessage(m);
	}
}
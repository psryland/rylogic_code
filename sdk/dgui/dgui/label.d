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

module dgui.label;

import std.string;
import dgui.core.controls.control;

enum LabelDrawMode: ubyte
{
	NORMAL = 0,
	OWNER_DRAW = 1,
}

class Label: Control
{
	private LabelDrawMode _drawMode = LabelDrawMode.NORMAL;
	private TextAlignment _textAlign = TextAlignment.MIDDLE | TextAlignment.LEFT;

	alias @property Control.text text;
	private bool _multiLine = false;
	
	@property public override void text(string s)
	{
		super.text = s;
		
		this._multiLine = false;
		
		foreach(char ch; s)
		{
			if(ch == '\n' || ch == '\r')
			{
				this._multiLine = true;
				break;
			}
		}
		
		if(this.created)
		{
			this.invalidate();
		}
	}
	
	@property public final LabelDrawMode drawMode()
	{
		return this._drawMode;
	}

	@property public final void drawMode(LabelDrawMode ldm)
	{
		this._drawMode = ldm;
	}

	@property public final TextAlignment alignment()
	{
		return this._textAlign;
	}

	@property public final void alignment(TextAlignment ta)
	{
		this._textAlign = ta;
		
		if(this.created)
		{
			this.invalidate();
		}
	}

	protected override void createControlParams(ref CreateControlParams ccp)
	{
		ccp.ClassName = WC_DLABEL;
		ccp.ClassStyle = ClassStyles.HREDRAW | ClassStyles.VREDRAW;

		super.createControlParams(ccp);
	}

	protected override void onPaint(PaintEventArgs e)
	{
		super.onPaint(e);

		if(this._drawMode is LabelDrawMode.NORMAL)
		{
			Canvas c = e.canvas;
			Rect r = Rect(NullPoint, this.clientSize);
			
			scope TextFormat tf = new TextFormat(this._multiLine ? TextFormatFlags.WORD_BREAK : TextFormatFlags.SINGLE_LINE);
			tf.alignment = this._textAlign;

			scope SolidBrush sb = new SolidBrush(this.backColor);
			c.fillRectangle(sb, r);
			c.drawText(this.text, r, this.foreColor, this.font, tf);
		}
	}
}
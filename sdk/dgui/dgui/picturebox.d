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

module dgui.picturebox;

import dgui.core.controls.control;
import dgui.canvas;

enum SizeMode
{
	NORMAL = 0,
	AUTO_SIZE = 1,
}

class PictureBox: Control
{
	private SizeMode _sm = SizeMode.NORMAL;
	private Image _img;

	public override void dispose() 
	{
		if(this._img)
		{
			this._img.dispose();
			this._img = null;
		}

		super.dispose();
	}

	alias @property Control.bounds bounds;

	@property public override void bounds(Rect r)
	{
		if(this._img && this._sm is SizeMode.AUTO_SIZE)
		{
			// Ignora 'r.size' e usa la dimensione dell'immagine
			Size sz = r.size;
			super.bounds = Rect(r.x, r.y, sz.width, sz.height);
			
		}
		else
		{
			super.bounds = r;
		}
	}

	@property public final SizeMode sizeMode()
	{
		return this._sm;
	}

	@property public final void sizeMode(SizeMode sm)
	{
		this._sm = sm;

		if(this.created)
		{
			this.redraw();
		}
	}

	@property public final Image image()
	{
		return this._img;
	}

	@property public final void image(Image img)
	{
		if(this._img)
		{
			this._img.dispose(); // Destroy the previous image
		}
		
		this._img = img;

		if(this.created)
		{
			this.redraw();
		}
	}

	protected override void createControlParams(ref CreateControlParams ccp)
	{
		ccp.ClassName  = WC_DPICTUREBOX;
		ccp.DefaultCursor = SystemCursors.arrow;
		ccp.ClassStyle = ClassStyles.PARENTDC;
		
		super.createControlParams(ccp);
	}

	protected override void onPaint(PaintEventArgs e)
	{
		if(this._img)
		{
			Canvas c = e.canvas;

			switch(this._sm)
			{
				case SizeMode.AUTO_SIZE:
					c.drawImage(this._img, Rect(NullPoint, this.size));
					break;
				
				default:
					c.drawImage(this._img, 0, 0);
					break;
			}
		}

		super.onPaint(e);
	}
}

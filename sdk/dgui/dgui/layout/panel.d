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

module dgui.layout.panel;

public import dgui.layout.layoutcontrol;

class Panel: LayoutControl
{
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		ccp.ClassName = WC_DPANEL;
		ccp.DefaultCursor = SystemCursors.arrow;
		
		super.createControlParams(ccp);
	}
}

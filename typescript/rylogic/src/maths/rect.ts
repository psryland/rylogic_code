import * as Math_ from "./maths";
import * as Vec2_ from "./v2";
import Rect = Math_.Rect;
import Size = Math_.Size;
import Vec2 = Math_.Vec2;

class RectangleImpl implements Rect
{
	constructor()
	{
		this.x = 0;
		this.y = 0;
		this.w = 0;
		this.h = 0;
	}

	public x: number;
	public y: number;
	public w: number;
	public h: number;

	get l(): number
	{
		return this.x;
	}
	set l(v: number)
	{
		this.x = v;
	}

	get t(): number
	{
		return this.y;
	}
	set t(v: number)
	{
		this.y = v;
	}

	get r(): number
	{
		return this.x + this.w;
	}
	set r(v: number)
	{
		this.w = v - this.x;
	}

	get b(): number
	{
		return this.y + this.h;
	}
	set b(v: number)
	{
		this.h = v - this.y;
	}

	get centre(): Vec2
	{
		return [this.x + this.w * 0.5, this.y + this.h * 0.5];
	}
	set centre(v: Vec2)
	{
		this.x = v[0] - this.w * 0.5;
		this.y = v[1] - this.h * 0.5;
	}

	get size(): Size
	{
		return [this.w, this.h];
	}
	set size(s: Size)
	{
		this.w = s[0];
		this.h = s[1];
	}

	get tl(): Vec2
	{
		return [this.l, this.t];
	}
	set tl(v: Vec2)
	{
		this.l = v[0];
		this.t = v[1];
	}

	get tr(): Vec2
	{
		return [this.r, this.t];
	}
	set tr(v: Vec2)
	{
		this.r = v[0];
		this.t = v[1];
	}

	get bl(): Vec2
	{
		return [this.l, this.b];
	}
	set bl(v: Vec2)
	{
		this.l = v[0];
		this.b = v[1];
	}

	get br(): Vec2
	{
		return [this.r, this.b];
	}
	set br(v: Vec2)
	{
		this.r = v[0];
		this.b = v[1];
	}
}

/**
 * Create a zero rectangle
 */
export function create(x?: number, y?: number, w?: number, h?: number): Rect
{
	let out = new RectangleImpl();
	if (x === undefined) x = 0;
	if (y === undefined) y = 0;
	if (w === undefined) w = 0;
	if (h === undefined) h = 0;
	return set(out, x, y, w, h);
}

/**
 * Create a new copy of 'rect'
 * @param rect The source rect to copy
 * @param out Where to write the result
 * @returns The clone of 'rect'
 */
export function clone(rect: Rect, out?: Rect): Rect
{
	out = out || create();
	if (rect !== out)
	{
		out.x = rect.x;
		out.y = rect.y;
		out.w = rect.w;
		out.h = rect.h;
	}
	return out;
}

/**
 * Create from left, top, right, bottom
 * @param l 
 * @param t 
 * @param r 
 * @param b 
 */
export function ltrb(l: number, t: number, r: number, b: number): Rect
{
	let out = create();
	return set(out, l, t, r - l, b - t);
}

/**
 * Return a bounding rect about the given points
 * @param points The points to bound with the returned rectangle
 */
export function bound(...points: Vec2[]): Rect
{
	var br = create(0, 0, -1, -1);
	for (let i = 0; i != points.length; ++i)
		EncompassPoint(br, points[i], br);
	return br;
}

/**
 * Set the components of a rectangle
 * @param rect 
 * @param x 
 * @param y 
 * @param w 
 * @param h 
 */
export function set(rect: Rect, x: number, y: number, w: number, h: number): Rect
{
	rect.x = x;
	rect.y = y;
	rect.w = w;
	rect.h = h;
	return rect;
}

/**
 * Returns true of 'point' is within [[x,y], [x+w,y+h])
 * @param rect 
 * @param point
 */
export function Contains(rect: Rect, point: Vec2): boolean
{
	let contained =
		point[0] >= rect.l && point[0] < rect.r &&
		point[1] >= rect.t && point[1] < rect.b;
	return contained;
}

/**
 * Return 'point' scaled by the transform that maps 'rect' to the square (bottom left:-1,-1)->(top right:1,1) 
 * Inverse of 'ScalePoint'
 * @param rect
 * @param point
 * @param xsign Set to +1 if the rect origin is on the left, -1 if on the right
 * @param ysign Set to +1 if the rect origin is at the top, -1 if at the bottom
 */
export function NormalisePoint(rect: Rect, point: Vec2, xsign: number, ysign: number): Vec2
{
	return Vec2_.create(
		(xsign || +1) * (2 * (point[0] - rect.x) / rect.w - 1),
		(ysign || +1) * (2 * (point[1] - rect.y) / rect.h - 1));
}

/**
 * Scales a normalised 'point' by the transform that maps the square (bottom left:-1,-1)->(top right:1,1) to 'rect'
 * Inverse of 'NormalisedPoint'
 * @param rect
 * @param point
 * @param xsign Set to +1 if the rect origin is on the left, -1 if on the right
 * @param ysign Set to +1 if the rect origin is at the top, -1 if at the bottom
 */
export function ScalePoint(rect: Rect, point: Vec2, xsign: number, ysign: number): Vec2
{
	return Vec2_.create(
		rect.x + rect.w * (1 + (xsign || +1) * point[0]) / 2,
		rect.y + rect.h * (1 + (ysign || +1) * point[1]) / 2);
}

/**
 * Expand 'rect' to include 'point'
 * @param rect The initial rectangle. (Start with Rect.Invalid when finding a bounding rect)
 * @param point The point to encompass with 'rect'
 * @param out Where to write the result
 */
export function EncompassPoint(rect: Rect, point: Vec2, out?: Rect): Rect
{
	out = out || create();
	out.x = rect.w >= 0 ? Math.min(rect.x, point[0]) : point[0];
	out.y = rect.h >= 0 ? Math.min(rect.y, point[1]) : point[1];
	out.w = rect.w >= 0 ? Math.max(rect.w, point[0] - rect.l, rect.r - point[0]) : 0;
	out.h = rect.h >= 0 ? Math.max(rect.h, point[1] - rect.t, rect.b - point[1]) : 0;
	return out;
}

/**
 * Expand 'rect' to include 'rhs'
 * @param rect The initial rectangle. (Start with Rect.Invalid when finding a bounding rect)
 * @param rhs The rectangle to encompass with 'rect'
 * @param out Where to write the result
 */
export function EncompassRect(rect: Rect, rhs: Rect, out?: Rect): Rect
{
	out = out || create();
	out.x = rect.w >= 0 ? Math.min(rect.x, rhs.x) : rhs.x;
	out.y = rect.h >= 0 ? Math.min(rect.y, rhs.y) : rhs.y;
	out.w = rect.w >= 0 ? Math.max(rect.w, rhs.r - rect.l, rect.r - rhs.l) : 0;
	out.h = rect.h >= 0 ? Math.max(rect.h, rhs.b - rect.t, rect.b - rhs.t) : 0;
	return out;
}

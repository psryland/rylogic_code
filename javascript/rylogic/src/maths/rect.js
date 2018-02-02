/**
 * @module Rect
 */

import * as Maths from "./maths";

let FVec = Float32Array;
const Invalid = make(0,0,-1,-1);

/**
 * Create a zero rectangle
 * @returns {x,y,w,h}
 */
export function create()
{
	return new class
	{
		constructor()
		{
			this.x = 0;
			this.y = 0;
			this.w = 0;
			this.h = 0;
		}
		get l() { return this.x; }
		get t() { return this.y; }
		get r() { return this.x + this.w; }
		get b() { return this.y + this.h; }
		set l(v) { this.x = v; }
		set t(v) { this.y = v; }
		set r(v) { this.w = v - this.x; }
		set b(v) { this.h = v - this.y; }
		get centre() { return [this.x + this.w*0.5, this.y + this.h*0.5]; }
		set centre(v) { this.x = v[0] - this.w*0.5; this.y = v[1] - this.h*0.5; }
	}
}

/**
 * Create a rectangle from a point and size
 * @param {Number} x
 * @param {Number} y
 * @param {Number} w
 * @param {Number} h
 * @returns {Rect}
 */
export function make(x,y,w,h)
{
	let out = create();
	return set(out, x, y, w, h);
}

/**
 * Create from left, top, right, bottom
 * @param {Nummber} l 
 * @param {Nummber} t 
 * @param {Nummber} r 
 * @param {Nummber} b 
 * @returns {Rect}
 */
export function ltrb(l,t,r,b)
{
	let out = create();
	return set(out, l, t, r - l, b - t);
}

/**
 * Return a bounding rect about the given points
 * @param {[v2]} points 
 */
export function bound(...points)
{
	var br = clone(Invalid);
	for (let i = 0; i != points.length; ++i)
		Encompass(br, points[i], br);
	return br;
}

/**
 * Set the components of a rectangle
 * @param {Rect} rect 
 * @param {Number} x 
 * @param {Number} y 
 * @param {Number} w 
 * @param {Number} h 
 * @returns {Rect}
 */
export function set(rect, x, y, w, h)
{
	rect.x = x;
	rect.y = y;
	rect.w = w;
	rect.h = h;
	return rect;
}

/**
 * Create a new copy of 'rect'
 * @param {Rect} rect the source rect to copy
 * @param {Rect} out (optional) the rect to write to
 * @returns {Rect} the clone of 'rect'
 */
export function clone(rect, out)
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
 * Returns true of 'point' is within [[x,y], [x+w,y+h])
 * @param {Rect} rect 
 * @param {[x,y]} point
 * @returns {boolean}
 */
export function Contains(rect, point)
{
	let contained =
		point[0] >= rect.l && point[0] < rect.r &&
		point[1] >= rect.t && point[1] < rect.b;
	return contained;
}

/**
 * Return 'point' scaled by the transform that maps 'rect' to the square (bottom left:-1,-1)->(top right:1,1) 
 * Inverse of 'ScalePoint'
 * @param {Rect} rect
 * @param {[x,y]} point
 * @param {Number} xsign Set to +1 if the rect origin is on the left, -1 if on the right
 * @param {Number} ysign Set to +1 if the rect origin is at the top, -1 if at the bottom
 * @returns {[x,y]}
 */
export function NormalisePoint(rect, point, xsign, ysign)
{
	return [
		(xsign || +1) * (2 * (point[0] - rect.x) / rect.w - 1),
		(ysign || +1) * (2 * (point[1] - rect.y) / rect.h - 1)];
}

/**
 * Scales a normalised 'point' by the transform that maps the square (bottom left:-1,-1)->(top right:1,1) to 'rect'
 * Inverse of 'NormalisedPoint'
 * @param {Rect} rect
 * @param {[x,y]} point
 * @param {Number} xsign Set to +1 if the rect origin is on the left, -1 if on the right
 * @param {Number} ysign Set to +1 if the rect origin is at the top, -1 if at the bottom
 * @returns {[x,y]}
 */
export function ScalePoint(rect, point, xsign, ysign)
{
	return [
		rect.x + rect.w * (1 + (xsign || +1) * point[0]) / 2,
		rect.y + rect.h * (1 + (ysign || +1) * point[1]) / 2];
}

/**
 * Expand 'rect' to include 'point'
 * @param {Rect} rect The initial rectangle. (Start with Rect.Invalid when finding a bounding rect)
 * @param {v2} point The point to encompass with 'rect'
 * @param {Rect} out (optional) where to write the expanded rect to
 * @returns {Rect}
 */
export function Encompass(rect, point, out)
{
	out = out || create();
	out.x = rect.w >= 0 ? Math.min(rect.x, point[0]) : point[0];
	out.y = rect.h >= 0 ? Math.min(rect.y, point[1]) : point[1];
	out.w = rect.w >= 0 ? Math.max(rect.w, point[0] - rect.l, rect.r - point[0]) : 0;
	out.h = rect.h >= 0 ? Math.max(rect.h, point[1] - rect.t, rect.b - point[1]) : 0;
	return out;
}
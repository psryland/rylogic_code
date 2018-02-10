/**
 * @module BBox
 */

import * as Maths from "./maths";
import * as v4 from "./v4";
import * as m4x4 from "./m4x4";
import * as Rect from "./rect";

/**
 * Constants
 */
const Invalid = create();

/**
 * Create a new, invalid, bounding box
 * @returns {centre,radius}
 */
export function create()
{
	return new class
	{
		constructor()
		{
			this.centre = v4.make(0,0,0,1);
			this.radius = v4.make(-1,-1,-1,0);
		}

		/**
		 * Return true if this bounding box represents a point or volume
		 * @returns {boolean}
		 */
		get is_valid()
		{
			return this.radius[0] >= 0 && this.radius[1] >= 0 && this.radius[2] >= 0;
		}

		/**
		 * Returns true if this bounding box is a degenerate point
		 * @returns {boolean}
		 */
		get is_point()
		{
			return v4.Eql(this.radius, v4.Zero);
		}

		/**
		 * Return the lower bound
		 * @returns {v4}
		 */
		get lower()
		{
			return v4.Sub(this.centre, this.radius);
		}

		/**
		 * Return the upper bound
		 * @returns {v4}
		 */
		get upper()
		{
			return v4.Add(this.centre, this.radius);
		}

		/**
		 * Return the size of this bounding box [x,y,z,0]
		 * @returns {v4}
		 */
		get size()
		{
			return v4.MulS(this.radius, 2);
		}

		/**
		 * Gets the squared length of the diagonal of the bounding box
		 * * @returns {Number}
		 */
		get diametre_sq()
		{
			return 4 * v4.LengthSq(this.radius);
		}

		/**
		 * Gets the length of the diagonal of the bounding box
		 * @returns {Number}
		 */
		get diametre()
		{
			return Math.sqrt(this.diametre_sq);
		}

		/**
		 * Gets the volume of the bounding box
		 * @returns {Number}
		 */
		get volume()
		{
			let sz = this.size;
			return sz[0] * sz[1] * sz[2];
		}
	}
}

/**
 * Create a new copy of 'bbox'
 * @param {BBox} bbox the source bounding box to copy
 * @param {BBox} out (optional) the bounding box to write to
 * @returns {BBox} The clone of 'bbox'
 */
export function clone(bbox, out)
{
	out = out || create();
	if (bbox !== out)
	{
		v4.clone(bbox.centre, out.centre);
		v4.clone(bbox.radius, out.radius);
	}
	return out;
}

/**
 * Returns a corner point of the bounding box.
 * @param {BBox} bbox The bounding box to return the corner from
 * @param {Number} corner A 3-bit mask of the corner to return: 000 = -x,-y,-z, 111 = +x,+y,+z
 * @returns {v4}
 */
export function GetCorner(bbox, corner)
{
	if (corner >= 8) throw new Error("Invalid corner index");
	let x = ((corner >> 0) & 0x1) * 2 - 1;
	let y = ((corner >> 1) & 0x1) * 2 - 1;
	let z = ((corner >> 2) & 0x1) * 2 - 1;
	let c = v4.make(
		bbox.centre[0] + x*bbox.radius[0],
		bbox.centre[1] + y*bbox.radius[1],
		bbox.centre[2] + z*bbox.radius[2],
		1);
	return c;
}

/**
 * Returns true if 'point' is within this bounding box (within tolerance).
 * @param {BBox} bbox The bounding box to check for surrounding 'point'
 * @param {point} point The point to test for being within 'bbox'
 * @param {Number} tol (optional) Tolerance
 * @returns {boolean}
 */
export function IsPointWithin(bbox, point, tol)
{
	tol = tol || 0;
	let within =
		Math.abs(point[0] - bbox.centre[0]) <= bbox.radius[0] + tol &&
		Math.abs(point[1] - bbox.centre[1]) <= bbox.radius[1] + tol &&
		Math.abs(point[2] - bbox.centre[2]) <= bbox.radius[2] + tol;
	return within;
}

/**
 * Returns true if 'bbox' is within this bounding box (within 'tol'erance)
 * @param {BBox} bbox The bounding box to check for surrounding 'rhs'
 * @param {BBox} rhs The point to test for being within 'bbox'
 * @param {Number} tol (optional) Tolerance
 * @returns {boolean}
 */
export function IsBBoxWithin(bbox, rhs, tol)
{
	tol = tol || 0;
	let within = 
		Math.abs(rhs.centre[0] - bbox.centre[0]) <= (bbox.radius[0] - rhs.radius[0] + tol) &&
		Math.abs(rhs.centre[1] - bbox.centre[1]) <= (bbox.radius[1] - rhs.radius[1] + tol) &&
		Math.abs(rhs.centre[2] - bbox.centre[2]) <= (bbox.radius[2] - rhs.radius[2] + tol);
	return within;
}

/**
 * Expands the bounding box to include 'point'
 * @param {BBox} bbox The bounding box to grow
 * @param {v4} point The point to encompass in 'bbox'
 * @param {BBox} out (optional). The bounding box to write the result to
 * @returns {BBox}
 */
export function EncompassPoint(bbox, point, out)
{
	clone(bbox, out);
	for (let i = 0; i != 3; ++i)
	{
		if (out.radius[i] < 0)
		{
			out.centre[i] = point[i];
			out.radius[i] = 0;
		}
		else
		{
			let signed_dist = point[i] - out.centre[i];
			let length      = Math.abs(signed_dist);
			if (length > out.radius[i])
			{
				let new_radius = (length + out.radius[i]) / 2;
				out.centre[i] += signed_dist * (new_radius - out.radius[i]) / length;
				out.radius[i] = new_radius;
			}
		}
	}
	return out;
}

/**
 * Expands the bounding box to include 'rhs'.
 * @param {BBox} bbox The bounding box to grow
 * @param {BBox} rhs The bounding box to encompass in 'bbox'
 * @param {BBox} out (optional). The bounding box to write the result to
 * @returns {BBox}
 */
export function EncompassBBox(bbox, rhs, out)
{
	if (!rhs.is_valid) throw new Error("Encompassing an invalid bounding box");
	clone(bbox, out);
	EncompassPoint(out, v4.Add(rhs.centre, rhs.radius), out);
	EncompassPoint(out, v4.Sub(rhs.centre, rhs.radius), out);
	return out;
}

/**
 * Returns true if 'rhs' intersects with this bounding box
 * @returns {boolean}
 * @param {BBox} lhs The bounding box to intersect
 * @param {BBox} rhs The other bounding box to intersect
 * @returns {boolean}
 */
export function IsIntersection(lhs, rhs)
{
	let intersection =
		Math.abs(lhs.centre[0] - rhs.centre[0]) <= (lhs.radius[0] + rhs.radius[0]) &&
		Math.abs(lhs.centre[1] - rhs.centre[1]) <= (lhs.radius[1] + rhs.radius[1]) &&
		Math.abs(lhs.centre[2] - rhs.centre[2]) <= (lhs.radius[2] + rhs.radius[2]);
	return intersection;
}

/**
 * Create a RectangleF from the X,Y axes of a bounding box
 * @param {BBox} bbox
 * @returns {Rect}
 */
export function ToRectXY(bbox)
{
	let mn = bbox.min;
	let sz = bbox.size;
	return Rect.make(mn[0], mn[1], sz[0], sz[1]);
}

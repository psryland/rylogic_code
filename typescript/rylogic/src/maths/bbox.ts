import * as Math_ from "./maths";
import * as Rect_ from "./rect";
import * as Vec4_ from "./v4";
import Vec4 = Math_.Vec4;
import Rect = Math_.Rect;
import BBox = Math_.BBox;

class BBoxImpl implements BBox
{
	constructor(centre: Vec4, radius: Vec4)
	{
		this.centre = centre;
		this.radius = radius;
	}

	/** The centre of the bounding box */
	public centre: Vec4;

	/** The radius of the bounding box */
	public radius: Vec4;

	/** Return true if this bounding box represents a point or volume */
	get is_valid(): boolean
	{
		return this.radius[0] >= 0 && this.radius[1] >= 0 && this.radius[2] >= 0;
	}

	/** Returns true if this bounding box is a degenerate point */
	get is_point(): boolean
	{
		return Vec4_.Eql(this.radius, Vec4_.Zero);
	}

	/** Return the lower bound */
	get lower(): Vec4
	{
		return Vec4_.Sub(this.centre, this.radius);
	}

	/** Return the upper bound */
	get upper(): Vec4
	{
		return Vec4_.Add(this.centre, this.radius);
	}

	/** Return the size of this bounding box [x,y,z,0] */
	get size(): Vec4
	{
		return Vec4_.MulS(this.radius, 2);
	}

	/** Gets the squared length of the diagonal of the bounding box */
	get diametre_sq(): number
	{
		return 4 * Vec4_.LengthSq(this.radius);
	}

	/** Gets the length of the diagonal of the bounding box */
	get diametre(): number
	{
		return Math.sqrt(this.diametre_sq);
	}

	/** Gets the volume of the bounding box */
	get volume(): number
	{
		let sz = this.size;
		return sz[0] * sz[1] * sz[2];
	}
}

/**
 * Create a new, invalid, bounding box
 * @param centre The position of the centre of the bounding box
 * @param radius The radius of the bounding box in each axis direction
 * @returns The new bounding box
 */
export function create(centre?: Vec4, radius?: Vec4): BBox
{
	if (centre === undefined) centre = Vec4_.create(+0, +0, +0, 1);
	if (radius === undefined) radius = Vec4_.create(-1, -1, -1, 0);
	let bbox = new BBoxImpl(centre, radius);
	return bbox;
}

/**
 * Create a new copy of 'bbox'
 * @param bbox The source bounding box to copy
 * @param out Where to write the result
 * @returns The clone of 'bbox'
 */
export function clone(bbox: BBox, out?: BBox): BBox
{
	out = out || create();
	if (bbox !== out)
	{
		Vec4_.clone(bbox.centre, out.centre);
		Vec4_.clone(bbox.radius, out.radius);
	}
	return out;
}

/**
 * Return a bounding box about the given points
 * @param points The points to bound with the returned bounding box
 */
export function bound(...points: Vec4[]): BBox
{
	var bb = create();
	for (let i = 0; i != points.length; ++i)
		EncompassPoint(bb, points[i], bb);
	return bb;
}

/**
 * Returns a corner point of the bounding box.
 * @param bbox The bounding box to return the corner from
 * @param corner A 3-bit mask of the corner to return: 000 = -x,-y,-z, 111 = +x,+y,+z
 */
export function GetCorner(bbox: BBox, corner: number): Vec4
{
	if (corner >= 8) throw new Error("Invalid corner index");
	let x = ((corner >> 0) & 0x1) * 2 - 1;
	let y = ((corner >> 1) & 0x1) * 2 - 1;
	let z = ((corner >> 2) & 0x1) * 2 - 1;
	let c = Vec4_.create(
		bbox.centre[0] + x * bbox.radius[0],
		bbox.centre[1] + y * bbox.radius[1],
		bbox.centre[2] + z * bbox.radius[2],
		1);
	return c;
}

/**
 * Returns true if 'point' is within this bounding box (within tolerance).
 * @param bbox The bounding box to check for surrounding 'point'
 * @param point The point to test for being within 'bbox'
 * @param tol Tolerance for testing "within"
 */
export function IsPointWithin(bbox: BBox, point: Vec4, tol?: number): boolean
{
	tol = tol || 0;
	let within =
		Math.abs(point[0] - bbox.centre[0]) <= bbox.radius[0] + tol &&
		Math.abs(point[1] - bbox.centre[1]) <= bbox.radius[1] + tol &&
		Math.abs(point[2] - bbox.centre[2]) <= bbox.radius[2] + tol;
	return within;
}

/**
 * Returns true if 'rhs' is within 'lhs' (within 'tol'erance)
 * @param lhs The bounding box to check for surrounding 'rhs'
 * @param rhs The bounding box to test for being within 'bbox'
 * @param tol Tolerance for testing "within"
 */
export function IsBBoxWithin(lhs: BBox, rhs: BBox, tol?: number): boolean
{
	tol = tol || 0;
	let within =
		Math.abs(rhs.centre[0] - lhs.centre[0]) <= (lhs.radius[0] - rhs.radius[0] + tol) &&
		Math.abs(rhs.centre[1] - lhs.centre[1]) <= (lhs.radius[1] - rhs.radius[1] + tol) &&
		Math.abs(rhs.centre[2] - lhs.centre[2]) <= (lhs.radius[2] - rhs.radius[2] + tol);
	return within;
}

/**
 * Expands the bounding box to include 'point'
 * @param bbox The bounding box to grow
 * @param point The point to encompass in 'bbox'
 * @param out Where to write the result
 */
export function EncompassPoint(bbox: BBox, point: Vec4, out?: BBox): BBox
{
	out = clone(bbox, out);
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
			let length = Math.abs(signed_dist);
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
 * @param bbox The bounding box to grow
 * @param rhs The bounding box to encompass in 'bbox'
 * @param out Where to write the result
 */
export function EncompassBBox(bbox: BBox, rhs: BBox, out?: BBox): BBox
{
	if (!rhs.is_valid)
		throw new Error("Encompassing an invalid bounding box");

	out = clone(bbox, out);
	EncompassPoint(out, Vec4_.Add(rhs.centre, rhs.radius), out);
	EncompassPoint(out, Vec4_.Sub(rhs.centre, rhs.radius), out);
	return out;
}

/**
 * Returns true if 'rhs' intersects with this bounding box
 * @param lhs The bounding box to intersect
 * @param rhs The other bounding box to intersect
 */
export function IsIntersection(lhs: BBox, rhs: BBox): boolean
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
export function ToRectXY(bbox: BBox): Rect
{
	let mn = bbox.lower;
	let sz = bbox.size;
	return Rect_.create(mn[0], mn[1], sz[0], sz[1]);
}

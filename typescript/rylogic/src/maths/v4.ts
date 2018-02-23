import * as Math_ from "./maths";
import Vec4 = Math_.Vec4;

/**
 * Constants
 */
export const Zero = create();
export const XAxis = create(1, 0, 0, 0);
export const YAxis = create(0, 1, 0, 0);
export const ZAxis = create(0, 0, 1, 0);
export const WAxis = create(0, 0, 0, 1);
export const Origin = create(0, 0, 0, 1);

/**
 * Create a 4-vector. If only 'x' is given, then the returned vector is equivalent to v2.make(x,x)
 */
export function create(x?: number, y?: number, z?: number, w?: number): Vec4
{
	var out: Vec4 = new Float32Array(4);
	if (x === undefined) x = 0;
	if (y === undefined) y = x;
	if (z === undefined) z = x;
	if (w === undefined) w = x;
	return set(out, x, y, z, w);
}

/**
 * Assign the values of a 4-vector
 * @param vec The vector to be assigned
 * @param x
 * @param y
 * @param z
 * @param w
 */
export function set(vec: Vec4, x: number, y: number, z: number, w: number): Vec4
{
	vec[0] = x;
	vec[1] = y;
	vec[2] = z;
	vec[3] = w;
	return vec;
}

/**
 * Create a new copy of 'vec'
 * @param vec The source vector to copy
 * @param out Where to write the result
 * @returns The clone of 'vec'
 */
export function clone(vec: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	if (vec !== out)
	{
		out[0] = vec[0];
		out[1] = vec[1];
		out[2] = vec[2];
		out[3] = vec[3];
	}
	return out;
}

/**
 * Exact equality of two 4-vectors
 * @param a
 * @param b
 */
export function Eql(a: Vec4, b: Vec4): boolean
{
	let eql =
		a[0] == b[0] &&
		a[1] == b[1] &&
		a[2] == b[2] &&
		a[3] == b[3];
	return eql;
}

/**
 * Approximate equality of two 4-vectors
 * @param a
 * @param b
 */
export function FEql(a: Vec4, b: Vec4): boolean
{
	let feql =
		Math_.FEql(a[0], b[0]) &&
		Math_.FEql(a[1], b[1]) &&
		Math_.FEql(a[2], b[2]) &&
		Math_.FEql(a[3], b[3]);
	return feql;
}

/**
 * Returns true if 'vec' contains any 'NaN's
 * @param vec 
 */
export function IsNaN(vec: Vec4): boolean
{
	let is_nan =
		isNaN(vec[0]) ||
		isNaN(vec[1]) ||
		isNaN(vec[2]) ||
		isNaN(vec[3]);
	return is_nan;
}

/**
 * Return the negation of 'vec'
 * @param vec The vector to negate
 * @param out Where to write the result
 * @returns The negative of 'vec'
 */
export function Neg(vec: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	out[0] = -vec[0];
	out[1] = -vec[1];
	out[2] = -vec[2];
	out[3] = -vec[3];
	return out;
}

/**
 * Return 'vec' with the w component set to 1
 * @param vec 
 */
export function SetW1(vec: Vec4): Vec4
{
	vec[3] = 1;
	return vec;
}

/**
 * Absolute value of a 4-vector (component-wise)
 * @param vec The vector to find the absolute value of
 * @param out Where to write the result
 */
export function Abs(vec: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	out[0] = Math.abs(vec[0]);
	out[1] = Math.abs(vec[1]);
	out[2] = Math.abs(vec[2]);
	out[3] = Math.abs(vec[3]);
	return out;
}

/**
 * Clamp the components of 'vec' to the inclusive range given by [min,max]
 * @param vec The vector to be clamped
 * @param min The inclusive minimum value
 * @param max The inclusive maximum value
 * @param out Where to write the result
 */
export function Clamp(vec: Vec4, min: Vec4, max: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	out[0] = Math_.Clamp(vec[0], min[0], max[0]);
	out[1] = Math_.Clamp(vec[1], min[1], max[1]);
	out[2] = Math_.Clamp(vec[2], min[2], max[2]);
	out[3] = Math_.Clamp(vec[3], min[3], max[3]);
	return out;
}

/**
 * Compute the squared length of a 4-vector
 * @param vec The vector to compute the squared length of.
 * @returns the squared length of the vector
 */
export function LengthSq(vec: Vec4): number
{
	return vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2] + vec[3] * vec[3];
}

/**
 * Compute the length of a 4-vector
 * @param vec The vector to find the length of
 * @returns The length of the vector
 */
export function Length(vec: Vec4): number
{
	return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a 4-vector to length = 1
 * @param vec The vector to normalise
 * @param out Where to write the result
 * @param opts optional parameters: {def = return value if 'vec' has length = 0}
 * @returns The vector with components normalised
 */
export function Normalise(vec: Vec4, out?: Vec4, opts?: { def: Vec4 }): Vec4
{
	out = out || create();
	let len = Length(vec);
	if (len == 0)
	{
		if (opts) out = opts.def;
		else throw new Error("Cannot normalise a zero vector");
	}
	out[0] = vec[0] / len;
	out[1] = vec[1] / len;
	out[2] = vec[2] / len;
	out[3] = vec[3] / len;
	return out;
}

/**
 * Return the dot product of two 4-vectors.
 * @param a
 * @param b
 */
export function Dot(a: Vec4, b: Vec4): number
{
	return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
}

/**
 * Cross product: a x b
 * @param a
 * @param b
 * @param out Where to write the result
 */
export function Cross(a: Vec4, b: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	set(out, a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0], 0);
	return out;
}

/**
 * The triple product a x b . c
 * @param a
 * @param b
 * @param v
 */
export function Triple(a: Vec4, b: Vec4, c: Vec4): number
{
	return Dot(a, Cross(b, c));
}

/**
 * Return 'a + b'
 * @param a
 * @param b
 * @param out Where to write the result
 */
export function Add(a: Vec4, b: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	set(out, a[0] + b[0], a[1] + b[1], a[2] + b[2], a[3] + b[3]);
	return out;
}

/**
 * Add an array of vectors
 * @param arr The array of 4-vectors to sum.
 * @returns The sum of the given vectors
 */
export function AddN(...arr: Vec4[]): Vec4
{
	let sum = create();
	for (let i = 0; i != arr.length; ++i)
		Add(arr[i], sum, sum);
	return sum;
}

/**
 * Return 'a - b'
 * @param a
 * @param b
 * @param out Where to write the result
 */
export function Sub(a: Vec4, b: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	set(out, a[0] - b[0], a[1] - b[1], a[2] - b[2], a[3] - b[3]);
	return out;
}

/**
 * Multiply a vector by a scalar
 * @param a The vector to multiply
 * @param b the number to scale by
 * @param out Where to write the result
 */
export function MulS(a: Vec4, b: number, out?: Vec4): Vec4
{
	out = out || create();
	out[0] = a[0] * b;
	out[1] = a[1] * b;
	out[2] = a[2] * b;
	out[3] = a[3] * b;
	return out;
}

/**
 * Multiply a vector by another vector (component-wise)
 * @param a The vector to multiply
 * @param b The vector to scale by
 * @param out Where to write the result
 */
export function MulV(a: Vec4, b: Vec4, out?: Vec4): Vec4
{
	out = out || create();
	out[0] = a[0] * b[0];
	out[1] = a[1] * b[1];
	out[2] = a[2] * b[2];
	out[3] = a[3] * b[3];
	return out;
}

/**
 * Returns true if 'a' and 'b' parallel
 * @param a
 * @param b
 */
export function Parallel(a: Vec4, b: Vec4): boolean
{
	return LengthSq(Cross(a, b)) < Math_.TinySq;
}

/**
 * Returns a vector guaranteed not parallel to 'v'
 * @param v The vector to not be parallel to
 */
export function CreateNotParallelTo(v: Vec4): Vec4
{
	let x_aligned = Math.abs(v[0]) > Math.abs(v[1]) && Math.abs(v[0]) > Math.abs(v[2]);
	let out = create(+!x_aligned, 0, +x_aligned, v[3]);
	return out;
}

/**
 * Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular.
 * @param vec The vector to be perpendicular to.
 * @param previous The previously used perpendicular, for returning consistent results.
 */
export function Perpendicular(vec: Vec4, previous?: Vec4): Vec4
{
	if (LengthSq(vec) < Math_.Tiny)
		throw new Error("Cannot make a perpendicular to a zero vector");

	let out = create();

	// If 'previous' is parallel to 'vec', choose a new perpendicular (includes previous == zero)
	if (!previous || Parallel(vec, previous))
	{
		Cross(vec, CreateNotParallelTo(vec), out);
		MulS(out, Length(vec) / Length(out), out);
	}
	else
	{
		// If 'previous' is still perpendicular, keep it
		if (Math_.FEql(Dot(vec, previous), 0))
			return previous;

		// Otherwise, make a perpendicular that is close to 'previous'
		Cross(vec, previous, out);
		Cross(out, vec, out);
		Normalise(out, out);
	}
	return out;
}

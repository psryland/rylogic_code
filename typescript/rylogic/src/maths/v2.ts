import * as Math_ from "./maths";
import Vec2 = Math_.Vec2;

/**
* Constants
*/
export const Zero = create();
export const XAxis = create(1, 0);
export const YAxis = create(0, 1);

/**
* Create a 2-vector containing zeros.
* If only 'x' is given, then the returned vector is equivalent to v2.make(x,x)
*/
export function create(x?:number, y?:number): Vec2
{
	var out:Vec2 = new Float32Array(2);
	if (x === undefined) x = 0;
	if (y === undefined) y = x;
	return set(out, x, y);
}

/**
* Assign the values of a 2-vector
* @param  vec The vector to be assigned
* @param x
* @param y
* @returns 'vec'
*/
export function set(vec: Vec2, x: number, y: number): Vec2
{
	vec[0] = x;
	vec[1] = y;
	return vec;
}

/**
* Create a new copy of 'vec'
* @param vec The source vector to copy
* @param out Where to write the result
* @returns The clone of 'vec'
*/
export function clone(vec: Vec2, out?: Vec2): Vec2
{
	out = out || create();
	if (vec !== out)
	{
		out[0] = vec[0];
		out[1] = vec[1];
	}
	return out;
}

/**
* Exact equality of two 2-vectors
* @param a
* @param b
*/
export function Eql(a: Vec2, b: Vec2): boolean
{
	let eql =
		a[0] == b[0] &&
		a[1] == b[1];
	return eql;
}

/**
* Approximate equality of two 2-vectors
* @param a
* @param b
*/
export function FEql(a: Vec2, b: Vec2): boolean
{
	let feql =
		Math_.FEql(a[0], b[0]) &&
		Math_.FEql(a[1], b[1]);
	return feql;
}

/**
* Returns true if 'vec' contains any 'NaN's
* @param vec
*/
export function IsNaN(vec: Vec2): boolean
{
	let is_nan =
		isNaN(vec[0]) ||
		isNaN(vec[1]);
	return is_nan;
}

/**
* Return the negation of 'vec'
* @param vec The vector to negate
* @param out Where to write the result
* @returns The negative of 'vec'
*/
export function Neg(vec: Vec2, out?: Vec2): Vec2
{
	out = out || create();
	out[0] = -vec[0];
	out[1] = -vec[1];
	return out;
}

/**
* Absolute value of a 2-vector (component-wise)
* @param vec The vector to find the absolute value of
* @param out Where to write the result
*/
export function Abs(vec: Vec2, out?: Vec2): Vec2
{
	out = out || create();
	out[0] = Math.abs(vec[0]);
	out[1] = Math.abs(vec[1]);
	return out;
}

/**
* Clamp the components of 'vec' to the inclusive range given by [min,max]
* @param vec The vector to be clamped
* @param min The inclusive minimum value
* @param max The inclusive maximum value
* @param out Where to write the result
*/
export function Clamp(vec: Vec2, min: Vec2, max: Vec2, out?: Vec2): Vec2
{
	out = out || create();
	out[0] = Math_.Clamp(vec[0], min[0], max[0]);
	out[1] = Math_.Clamp(vec[1], min[1], max[1]);
	return out;
}

/**
* Compute the squared length of a 2-vector
* @param vec The vector to compute the squared length of
* @returns The squared length of the vector
*/
export function LengthSq(vec: Vec2): number
{
	return vec[0] * vec[0] + vec[1] * vec[1];
}

/**
* Compute the length of a 2-vector
* @param vec The vector to find the length of
* @returns The length of the vector
*/
export function Length(vec: Vec2): number
{
	return Math.sqrt(LengthSq(vec));
}

/**
* Normalise a 2-vector to length = 1
* @param vec the vector to normalise
* @param out Where to write the result
* @param opts optional parameters: {def = return value if 'vec' has length zero}
* @returns The vector with components normalised, (or opts.def if 'vec' is length zero)
*/
export function Normalise(vec: Vec2, out?: Vec2, opts?: { def: Vec2 }): Vec2
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
	return out;
}

/**
* Return the dot product of two 2-vectors
* @param a
* @param b
*/
export function Dot(a: Vec2, b: Vec2): number
{
	return a[0] * b[0] + a[1] * b[1];
}

/**
* Cross product: a x b
* (Equivalent to dot(rotate90cw(a), b))
* @param a
* @param b
*/
export function Cross(a: Vec2, b: Vec2): number
{
	return a[1] * b[0] - a[0] * b[1];
}

/**
* Return 'a + b'
* @param a
* @param b
* @param out Where to write the result
*/
export function Add(a: Vec2, b: Vec2, out?: Vec2): Vec2
{
	out = out || create();
	set(out, a[0] + b[0], a[1] + b[1]);
	return out;
}

/**
* Add an array of vectors
* @param arr The array of 2-vectors to add
* @returns The sum of the given vectors
*/
export function AddN(...arr: Vec2[]): Vec2
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
export function Sub(a: Vec2, b: Vec2, out?: Vec2): Vec2
{
	out = out || create();
	set(out, a[0] - b[0], a[1] - b[1]);
	return out;
}

/**
* Multiply a vector by a scalar
* @param a The vector to multiply
* @param {Number} b the number to scale by
* @param out Where to write the result
*/
export function MulS(a: Vec2, b: number, out?: Vec2): Vec2
{
	out = out || create();
	out[0] = a[0] * b;
	out[1] = a[1] * b;
	return out;
}

/**
* Multiply a vector by another vector (component-wise)
* @param a The vector to multiply
* @param b The vector to scale by
* @param out Where to write the result
*/
export function MulV(a: Vec2, b: Vec2, out?: Vec2): Vec2
{
	out = out || create();
	out[0] = a[0] * b[0];
	out[1] = a[1] * b[1];
	return out;
}

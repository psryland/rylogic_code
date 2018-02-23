import * as Math_ from "./maths";
import Size = Math_.Size;

/** Create a zero size */
export function create(w?: number, h?: number): Size
{
	let size:Size = new Float32Array(2);
	if (w === undefined) w = 0;
	if (h === undefined) h = 0;
	return set(size, w, h);
}

/**
 * Set the components of a size
 * @param size
 * @param w 
 * @param h 
 */
export function set(size: Size, w: number, h: number): Size
{
	size[0] = w;
	size[1] = h;
	return size;
}

/**
 * Create a new copy of 'size'
 * @param size The source size to copy
 * @param out Where to write the result
 * @returns The clone of 'size'
 */
export function clone(size: Size, out?: Size): Size
{
	out = out || create();
	if (size !== out)
	{
		out[0] = size[0];
		out[1] = size[1];
	}
	return out;
}

/**
 * @module bbox
 */

import * as Maths from "./maths";
import * as v4 from "./v4";
import * as m4x4 from "./m4x4";

/**
 * Create a new, invalid, bounding box
 */
export function create()
{
	let out = {centre: new FVec([0,0,0,1]), radius: new FVec([-1,-1,-1,0])};
	return out;
}

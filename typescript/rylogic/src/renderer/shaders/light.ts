import * as Math_ from "../../maths/maths";
import * as Vec4_ from "../../maths/v4";
import * as Rdr_ from "../renderer";
import Vec4 = Math_.Vec4;

/** A light source */
export interface ILight
{
	/** The type of light source */
	lighting_type: Rdr_.ELight;

	/** The position of the light source (ignored for directional lights) */
	position: Vec4;

	/** The direction of the light source (ignored for point lights) */
	direction: Vec4;

	/** The ambient light colour */
	ambient: Vec4;

	/** The diffuse light colour */
	diffuse: Vec4;

	/** The specular light colour */
	specular: Vec4;

	/** The specular power */
	specpwr: number;
}

/**
 * Create a light source.
 * @param lighting_type The type of light source
 * @param pos The position of the light (in world space, for positional lights only)
 * @param dir The direction the light is pointing (in world space, for directional lights only)
 * @param ambient The ambient light colour
 * @param diffuse The diffuse light colour
 * @param specular The specular light colour
 * @param specpwr The specular power
 */
export function Create(lighting_type: Rdr_.ELight, pos: Vec4, dir: Vec4, ambient ?: Vec4, diffuse ?: Vec4, specular ?: Vec4, specpwr ?: number): ILight
{
	let light: ILight =
		{
			lighting_type: lighting_type,
			position: pos,
			direction: Vec4_.Normalise(dir),
			ambient: ambient || [1, 0, 0, 0],
			diffuse: diffuse || [1, 0.5, 0.5, 0.5],
			specular: specular || [1, 0, 0, 0],
			specpwr: specpwr || 0,
		};

	return light;
}

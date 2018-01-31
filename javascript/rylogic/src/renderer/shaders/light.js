import * as v4 from "../../maths/v4";

class Light
{
	constructor(lighting_type, pos, dir, ambient, diffuse, specular, specpwr)
	{
		this.lighting_type = lighting_type;
		this.position = pos;
		this.direction = v4.Normalise(dir);
		this.ambient = ambient || [0, 0, 0];
		this.diffuse = diffuse || [0.5, 0.5, 0.5];
		this.specular = specular || [0, 0, 0];
		this.specpwr = specpwr || 0;
	}
}

/**
 * Create a light source.
 * @param {ELight} lighting_type 
 * @param {v4} pos the position of the light (in world space)
 * @param {v4} dir the direction the light is pointing (in world space)
 * @param {[Number]} ambient a 3 element array containing R,G,B for the ambient light
 * @param {[Number]} diffuse a 3 element array containing R,G,B for the diffuse light
 * @param {[Number]} specular a 3 element array containing R,G,B for the specular light
 * @param {Number} specpwr the specular power
 * @returns {Light}
 */
export function Create(lighting_type, pos, dir, ambient, diffuse, specular, specpwr)
{
	return new Light(lighting_type, pos, dir, ambient, diffuse, specular, specpwr);
}

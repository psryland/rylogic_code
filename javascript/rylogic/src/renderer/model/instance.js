import * as m4x4 from "../../maths/m4x4";
import * as Rdr from "../renderer";

/**
 * Create an instance of a model
 * @param {string} name The name to associate with the instance
 * @param {*} model The model that this is an instance of
 * @param {*} o2w (optional) The initial object to world transform for the instance
 * @param {*} flags (optional) Rendering flags
 */
export function Create(name, model, o2w, flags)
{
	let inst =
	{
		name: name,
		model: model,
		o2w: o2w || m4x4.create(),
		flags: flags || Rdr.EFlags.None
	};
	return inst;
}
import * as Math_ from "../../maths/maths";
import * as M4x4_ from "../../maths/m4x4";
import * as Rdr_ from "../renderer";
import M4x4 = Math_.M4x4;
import Vec4 = Math_.Vec4;

export interface IInstance
{
	/** The name associated with this instance (mainly for debugging) */
	name: string;

	/** The model that this is an instance of */
	model: Rdr_.Model.IModel;

	/** The object to world transform for the instance */
	o2w: M4x4;

	/** Instance specific flags */
	flags: Rdr_.EFlags;

	/** Instance colour tint */
	tint?: Vec4;
}

/**
 * Create an instance of a model
 * @param name The name to associate with the instance
 * @param model The model that this is an instance of
 * @param o2w The initial object to world transform for the instance
 * @param flags Rendering flags
 */
export function Create(name: string, model: Rdr_.Model.IModel, o2w?: M4x4, flags?: Rdr_.EFlags, tint?: Vec4): IInstance
{
	let inst =
		{
			name: name,
			model: model,
			o2w: o2w || M4x4_.create(),
			flags: flags || Rdr_.EFlags.None,
			tint: tint,
		};
	return inst;
}

/**
 * @module Unittests
 */

"#if UNITTESTS"

import * as Maths from "../maths/maths";

export const EqlN = Maths.EqlN;

 /**
  * Basic assert function
  */
export function Assert(condition)
{
	if (condition) return;
	let err = new Error("Unittest failure: ");
	alert(err.message);
	throw err;
}

{
	Assert(EqlN([], []));
	Assert(EqlN([1,2,3], [1,2,3]));
	Assert(EqlN(["1","2","three"], ["1","2","three"]));
	Assert(!EqlN([], [1]));
	Assert(!EqlN([1,2,3], [1,4,3]));
	Assert(!EqlN(["1","2","three"], ["1","three","2"]));
}

"#endif"

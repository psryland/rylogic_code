/** Generate a RFC4122 version 4 GUID */
export function Uuidv4(): string
{
	function b(a?:any):string
	{
		return a
			?
			(                                                                // if the place holder was passed, return a random number from 0 to 15
				a ^                                                          // unless b is 8,
				(crypto.getRandomValues(new Uint8Array(1)) as Uint8Array)[0] // in which case
				% 16                                                         // a random number from
				>> a / 4                                                     // 8 to 11
			).toString(16)                                                   // in hexadecimal
			:
			( // or otherwise a concatenated string:
				"" +
				+1e7 +    // 10000000 +
				-1e3 +    // -1000 +
				-4e3 +    // -4000 +
				-8e3 +    // -80000000 +
				-1e11     // -100000000000,
			).replace(    // replacing
				/[018]/g, // zeroes, ones, and eights with
				b         // random hex digits
			)
	}
	return b();
	//("" + 1e7 + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, c => (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16));
}






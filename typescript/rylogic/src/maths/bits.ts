
/** Returns true if 'n' is a exact power of two */
export function IsPowerOfTwo(n: number): boolean
{
	return Math.floor(n) == n && ((n - 1) & n) == 0;
}

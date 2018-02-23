
/**
 * Swap elements 'i' and 'j' in an array
 * @param arr The array containing the elements to swap
 * @param i The index of an element to swap
 * @param j The index of an element to swap
 */
export function Swap<T>(arr: T[], i: number, j: number): void
{
	let tmp = arr[i];
	arr[i] = arr[j];
	arr[j] = tmp;
}

/**
 * Return the first element to satisfy 'pred' or null
 * @param arr
 */
export function FirstOrDefault<T>(arr: T[], pred?: (_: T) => boolean): T | null
{
	let i = 0;
	pred = pred || (_ => true);
	for (; i != arr.length && !pred(arr[i]); ++i) { }
	return i != arr.length ? arr[i] : null;
}

/**
 * Move elements in 'arr' that don't satisfy 'pred' to the end of the array.
 * @param arr The array to move elements in
 * @param pred A function that returns true for elements to be moved
 * @param stable True if the order of 'arr' must be preserved. Note: the front half of the array is always stable, only the moved elements may not be stable
 * @returns Returns the index of the first element not satisfying 'pred'
 */
export function Partition<T>(arr: T[], pred: (x: T) => boolean, stable: boolean): number
{
	let first = 0, last = arr.length;
	stable = stable !== undefined && stable;

	// Skip in-place elements at beginning
	for (; first != last && pred(arr[first]); ++first) { }
	if (first == last)
		return first;

	// Skip in-place elements at the end
	for (; last != first && !pred(arr[last - 1]); --last) { }
	if (first == last)
		return first;

	if (stable)
	{
		// Store out of place elements in a temporary buffer while moving others forward
		let buffer = [arr[first]];
		for (let next = first; ++next != last;)
		{
			if (pred(arr[next]))
			{
				arr[first] = arr[next];
				++first;
			}
			else
			{
				buffer.push(arr[next]);
			}
		}

		// Copy the temporary buffer elements back into 'arr' at 'first'
		for (let next = first; next != last; ++next)
			arr[next] = <T>buffer.shift();

		return first;
	}
	else
	{
		// Swap out of place elements
		for (let next = first; ++next != last;)
		{
			if (pred(arr[next]))
			{
				// out of place, swap and loop
				Swap(arr, first, next);
				++first;
			}
		}
		return first;
	}
}

/**
 * Erase an element from an array
 * @param arr The array to search in
 * @param element The element to erase
 * @param start The index to start searching from
 * @returns The index position of where the element was erased from
 */
export function Erase<T>(arr: T[], element: T, start?: number): number
{
	let idx = arr.indexOf(element, start);
	if (idx != -1) arr.splice(idx, 1);
	return idx;
}

/**
 * Remove elements from an array that satisfy 'pred'
 * @param arr The array to erase elements from
 * @param pred The predicate function for determining the elements to erase
 * @returns Returns the number of elements removed
 */
export function EraseIf<T>(arr: T[], pred: (_: T) => boolean): number
{
	let len = Partition(arr, function(x) { return !pred(x); }, false);
	arr.length = len;
	return len;
}

/**
 * Binary search for an element using a predicate function.
 * Returns the index of the element if found or the 2s-complement of the first index for which cmp(element) > 0.
 * @param arr The array to search
 * @param cmp Comparison function, return <0 if T is less than the target, >0 if greater, or ==0 if equal
 * @param insert_position True to always return positive indices (i.e. when searching to find insert position)
 * @returns The index of the element if found or the 2s-complement of the first element larger than the one searched for.
 */
export function BinarySearch<T>(arr: T[], cmp: (_: T) => number, find_insert_position: boolean = false): number
{
	var idx = ~0;
	if (arr.length != 0)
	{
		for (let b = 0, e = arr.length; ;)
		{
			let m = b + ((e - b) >> 1); // prevent overflow
			let c = cmp(arr[m]);       // <0 means arr[m] is less than the target element
			if (c == 0) { idx = m; break; }
			if (c < 0) { if (m == b) { idx = ~e; break; } b = m; continue; }
			if (c > 0) { if (m == b) { idx = ~b; break; } e = m; }
		}
	}
	if (find_insert_position && idx < 0) idx = ~idx;
	return idx;
}

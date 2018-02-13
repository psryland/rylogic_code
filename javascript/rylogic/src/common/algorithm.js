/**
 * @module algorithm
 */

/**
 * Swap elements 'i' and 'j' in an array
 * @param {Array} arr The array containing the elements to swap
 * @param {Number} i The index of an element to swap
 * @param {Number} j The index of an element to swap
 */
export function Swap(arr, i, j)
{
	let tmp = arr[i];
	arr[i] = arr[j];
	arr[j] = tmp;
}

/**
 * Move elements in 'arr' that don't satisfy 'pred' to the end of the array.
 * @param {Array} arr The array to move elements in
 * @param {Function} pred A function that returns true for elements to be moved
 * @param {boolean} stable True if the order of 'arr' must be preserved. Note: the front half of the array is always stable, only the moved elements may not be stable
 * @returns {Number} Returns the index of the first element not satisfying 'pred'
 */
export function Partition(arr, pred, stable)
{
	let first = 0, last = arr.length;
	stable = stable !== undefined && stable;

	// Skip in-place elements at beginning
	for (; first != last && pred(arr[first]); ++first) {}
	if (first == last)
		return first;

	// Skip in-place elements at the end
	for (;last != first && !pred(arr[last-1]); --last) {}
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
			arr[next] = buffer.shift();

		return first;
	}
	else
	{
		// Swap out of place elements
		for (let next = first; ++next != last; )
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
 * @param {Array} arr The array to search in
 * @param {} element The element to erase
 * @param {Number} start (optional) The index to start searching from
 * @returns {Number} The index position of where the element was erased from
 */
export function Erase(arr, element, start)
{
	let idx = arr.indexOf(element, start);
	if (idx != -1) arr.splice(idx, 1);
	return idx;
}

/**
 * Remove elements from an array that satisfy 'pred'
 * @param {Array} arr 
 * @param {Function} pred 
 * @param {boolean} stable 
 * @returns Returns the number of elements removed
 */
export function EraseIf(arr, pred, stable)
{
	let len = Partition(arr, function(x) { return !pred(x); }, false);
	arr.length = len;
	return len;
}

/**
 * Binary search for an element using only a predicate function.
 * Returns the index of the element if found or the 2s-complement of the first.
 * @param {Array} arr The array to search
 * @param {Function} cmp Comparison function, return <0 if T is less than the target, >0 if greater, or ==0 if equal
 * @param {boolean} insert_position (optional, default = false) True to always return positive indices (i.e. when searching to find insert position)
 * @returns {Number} The index of the element if found or the 2s-complement of the first element larger than the one searched for.
 */
export function BinarySearch(arr, cmp, find_insert_position)
{
	var idx = ~0;
	if (arr.length != 0)
	{
		for (let b = 0, e = arr.length;;)
		{
			let m = b + ((e - b) >> 1); // prevent overflow
			let c = cmp(arr[m]);       // <0 means arr[m] is less than the target element
			if (c == 0) { idx = m; break; }
			if (c <  0) { if (m == b) { idx = ~e; break; } b = m; continue; }
			if (c >  0) { if (m == b) { idx = ~b; break; } e = m; }
		}
	}
	if (find_insert_position !== undefined && find_insert_position && idx < 0) idx = ~idx;
	return idx;
}

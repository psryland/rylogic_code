//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System.Collections;
using System.Collections.Generic;

namespace Rylogic.Container
{
	public readonly struct ListSpan<ListT, T> :IReadOnlyList<T>
		where ListT : IList<T>
	{
		// Note:
		//  - Used to return a subrange within a list, without creating a copy.
		//  - Readonly because adding/removing from the list would invalidate
		//    other ListSpans

		public ListSpan(ListT list, int start, int length)
		{
			List = list;
			Start = start;
			Length = length;
		}

		/// <summary>The list that this is a sub range within</summary>
		public ListT List { get; }

		/// <summary>Subrange start</summary>
		public int Start { get; }

		/// <summary>Subrange length</summary>
		public int Length { get; }

		/// <summary>Element access (relative to 'Start')</summary>
		public T this[int index] => List[Start + index];

		/// <inheritdoc/>
		int IReadOnlyCollection<T>.Count => Length;

		/// <summary>Enumerate</summary>
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = Start, len = Length; len-- != 0; ++i)
				yield return List[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}


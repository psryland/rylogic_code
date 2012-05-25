#region Copyright (c) 2003 - 2008, Andreas Mueller
/////////////////////////////////////////////////////////////////////////////////////////
// 
// Copyright (c) 2003 - 2008, Andreas Mueller.
// All rights reserved. This program and the accompanying materials
// are made available under the terms of the Eclipse Public License v1.0
// which accompanies this distribution, and is available at
// http://www.eclipse.org/legal/epl-v10.html
//
// Contributors:
//    Andreas Mueller - initial API and implementation
//
// 
// This software is derived from software bearing the following
// restrictions:
// 
// Copyright (c) 1994
// Hewlett-Packard Company
// 
// Permission to use, copy, modify, distribute and sell this software
// and its documentation for any purpose is hereby granted without fee,
// provided that the above copyright notice appear in all copies and
// that both that copyright notice and this permission notice appear
// in supporting documentation.  Hewlett-Packard Company makes no
// representations about the suitability of this software for any
// purpose.  It is provided "as is" without express or implied warranty.
// 
// 
// Copyright (c) 1996,1997
// Silicon Graphics Computer Systems, Inc.
// 
// Permission to use, copy, modify, distribute and sell this software
// and its documentation for any purpose is hereby granted without fee,
// provided that the above copyright notice appear in all copies and
// that both that copyright notice and this permission notice appear
// in supporting documentation.  Silicon Graphics makes no
// representations about the suitability of this software for any
// purpose.  It is provided "as is" without express or implied warranty.
// 
// 
// (C) Copyright Nicolai M. Josuttis 1999.
// Permission to copy, use, modify, sell and distribute this software
// is granted provided this copyright notice appears in all copies.
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
// 
/////////////////////////////////////////////////////////////////////////////////////////
#endregion


using System;
using NStl.Iterators;
using NStl.SyntaxHelper;
using NStl.Linq;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// StableSort is much like <see cref="Sort{T}(IRandomAccessIterator{T},IRandomAccessIterator{T},IBinaryFunction{T,T,bool})"/>:
        /// it sorts the elements in [first, last) into ascending order, 
        /// meaning that if i and j are any two valid iterators in [first, last) 
        /// such that i precedes j, then j.Value is not less than i.Value. 
        /// StableSort differs from Sort in two ways. First, StableSort 
        /// uses an algorithm that has different run-time complexity than Sort. 
        /// Second, as the name suggests, StableSort is stable: 
        /// it preserves the relative ordering of equivalent elements.
        /// </para>
        /// <para>
        /// Note that two elements may be equivalent without being equal. 
        /// One standard example is sorting a sequence of names by last name: 
        /// if two people have the same last name but different first names, 
        /// then they are equivalent but not equal. This is why StableSort is sometimes 
        /// useful: if you are sorting a sequence of records that have several 
        /// different fields, then you may want to sort it by one field without 
        /// completely destroying the ordering that you previously obtained from 
        /// sorting it by a different field. You might, for example, sort by 
        /// first name and then do a stable sort by last name.
        /// </para>
        /// <para>
        /// StableSort is an adaptive algorithm: it attempts to allocate a 
        /// temporary memory buffer, and its run-time complexity depends 
        /// on how much memory is available. Worst-case behavior (if no auxiliary memory is available) 
        /// is N (log N)^2 comparisons, where N is last - first, and best 
        /// case (if a large enough auxiliary memory buffer is available) is N (log N).
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing to the
        /// first element in the range to be sorted.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// one past the final element in the range to be sorted.
        /// </param>
        /// <param name="less">
        /// A functor object that is used to determine if one object is less than another.
        /// </param>
        /// <remarks>
        /// StableSort uses the merge sort algorithm; see section 5.2.4 of Knuth. 
        /// (D. E. Knuth, The Art of Computer Programming. Volume 3: Sorting and 
        /// Searching. Addison-Wesley, 1975.)
        /// </remarks>
        public static void
            StableSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> less)
        {
            T[] buffer = CreateBuffer(first, last);
            if (buffer.Length == 0)
                InplaceStableSort(first, last, less);
            else
                StableSortAdaptive(first, last, buffer.Begin(), buffer.Length, less);
        }
        /// <summary>
        /// <para>
        /// StableSort is much like <see cref="Sort{T}(IRandomAccessIterator{T},IRandomAccessIterator{T})"/>:
        /// it sorts the elements in [first, last) into ascending order, 
        /// meaning that if i and j are any two valid iterators in [first, last) 
        /// such that i precedes j, then j.Value is not less than i.Value. 
        /// StableSort differs from Sort in two ways. First, StableSort 
        /// uses an algorithm that has different run-time complexity than Sort. 
        /// Second, as the name suggests, StableSort is stable: 
        /// it preserves the relative ordering of equivalent elements.
        /// </para>
        /// <para>
        /// Note that two elements may be equivalent without being equal. 
        /// One standard example is sorting a sequence of names by last name: 
        /// if two people have the same last name but different first names, 
        /// then they are equivalent but not equal. This is why StableSort is sometimes 
        /// useful: if you are sorting a sequence of records that have several 
        /// different fields, then you may want to sort it by one field without 
        /// completely destroying the ordering that you previously obtained from 
        /// sorting it by a different field. You might, for example, sort by 
        /// first name and then do a stable sort by last name.
        /// </para>
        /// <para>
        /// StableSort is an adaptive algorithm: it attempts to allocate a 
        /// temporary memory buffer, and its run-time complexity depends 
        /// on how much memory is available. Worst-case behavior (if no auxiliary memory is available) 
        /// is N (log N)^2 comparisons, where N is last - first, and best 
        /// case (if a large enough auxiliary memory buffer is available) is N (log N).
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing to the
        /// first element in the range to be sorted.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// one past the final element in the range to be sorted.
        /// </param>
        /// <remarks>
        /// StableSort uses the merge sort algorithm; see section 5.2.4 of Knuth. 
        /// (D. E. Knuth, The Art of Computer Programming. Volume 3: Sorting and 
        /// Searching. Addison-Wesley, 1975.)
        /// </remarks>
        public static void
            StableSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last) where T : IComparable<T>
        {
            StableSort(first, last, Compare.Less<T>());
        }
        private static void
            InplaceStableSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last,
                                  IBinaryFunction<T, T, bool> comparison)
        {
            if (last.Diff(first) < 15)
            {// this is faster for smaller ranges
                InsertionSort(first, last, comparison);
                return;
            }
            IRandomAccessIterator<T> middle = first.Add((last.Diff(first)) / 2);
            InplaceStableSort(first, middle, comparison);
            InplaceStableSort(middle, last, comparison);
            MergeWithoutBuffer(first, middle, last, middle.Diff(first), last.Diff(middle), comparison);
        }
        private static void
            StableSortAdaptive<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IRandomAccessIterator<T> buffer,
                                   int bufferSize, IBinaryFunction<T, T, bool> comparison)
        {
            int length = ((last.Diff(first)) + 1) / 2;
            IRandomAccessIterator<T> middle = first.Add(length);
            if (length > bufferSize)
            {
                StableSortAdaptive(first, middle, buffer, bufferSize, comparison);
                StableSortAdaptive(middle, last, buffer, bufferSize, comparison);
            }
            else
            {
                MergeSortWithBuffer(first, middle, buffer, comparison);
                MergeSortWithBuffer(middle, last, buffer, comparison);
            }
            MergeAdaptive(first, middle, last, middle.Diff(first),
                             last.Diff(middle), buffer, bufferSize, comparison);
        }
    }
}

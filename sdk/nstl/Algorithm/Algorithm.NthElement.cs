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

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// NthElement is similar to <see cref="PartialSort{T}(IRandomAccessIterator{T},IRandomAccessIterator{T},IRandomAccessIterator{T},IBinaryFunction{T,T,bool})"/>,
        /// in that it partially orders a range of elements: it arranges the range [first, last) 
        /// such that the element pointed to by the iterator nth is the same as the element 
        /// that would be in that position if the entire range [first, last) had been sorted. 
        /// Additionally, none of the elements in the range [nth, last) is less 
        /// than any of the elements in the range [first, nth).
        /// </para>
        /// <para>
        /// The complexity is linear, on average last - first. Note that 
        /// this is significantly less than the run-time complexity of PartialSort.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing to
        /// the first element of the range.
        /// </param>
        /// <param name="nth">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation specifying the
        /// position of the nth element.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing one
        /// past the final element of the range.
        /// </param>
        /// <param name="less">
        /// A functor that is used to determine if one element is less than the other.
        /// </param>
        /// <remarks>
        /// The way in which this differs from PartialSort is that neither the 
        /// range [first, nth) nor the range [nth, last) is be sorted: it is simply 
        /// guaranteed that none of the elements in [nth, last) is less than 
        /// any of the elements in [first, nth). In that sense, NthElement is 
        /// more similar to partition than to sort. NthElement does less work 
        /// than PartialSort, so, reasonably enough, it is faster. That's 
        /// the main reason to use NthElement instead of PartialSort.
        /// </remarks>
        public static void
            NthElement<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> nth,
                          IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> less)
        {
            while (last.Diff(first) > 3)
            {
                IRandomAccessIterator<T> cut =
                    UnguardedPartition(first, last,
                        Median(
                            first.Value,
                            (first.Add((last.Diff(first)) / 2)).Value,
                            (last.Add(-1)).Value, less
                                       ),
                         less);
                if (cut.Less(nth) || Equals(cut, nth))
                    first = (IRandomAccessIterator<T>)cut.Clone();
                else
                    last = (IRandomAccessIterator<T>)cut.Clone();
            }
            InsertionSort(first, last, less);
        }
        /// <summary>
        /// <para>
        /// NthElement is similar to <see cref="PartialSort{T}(IRandomAccessIterator{T},IRandomAccessIterator{T},IRandomAccessIterator{T})"/>,
        /// in that it partially orders a range of elements: it arranges the range [first, last) 
        /// such that the element pointed to by the iterator nth is the same as the element 
        /// that would be in that position if the entire range [first, last) had been sorted. 
        /// Additionally, none of the elements in the range [nth, last) is less 
        /// than any of the elements in the range [first, nth).
        /// </para>
        /// <para>
        /// The complexity is linear, on average last - first. Note that 
        /// this is significantly less than the run-time complexity of PartialSort.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing to
        /// the first element of the range.
        /// </param>
        /// <param name="nth">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation specifying the
        /// position of the nth element.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing one
        /// past the final element of the range.
        /// </param>
        /// <remarks>
        /// The way in which this differs from PartialSort is that neither the 
        /// range [first, nth) nor the range [nth, last) is be sorted: it is simply 
        /// guaranteed that none of the elements in [nth, last) is less than 
        /// any of the elements in [first, nth). In that sense, NthElement is 
        /// more similar to partition than to sort. NthElement does less work 
        /// than PartialSort, so, reasonably enough, it is faster. That's 
        /// the main reason to use NthElement instead of PartialSort.
        /// </remarks>
        public static void
            NthElement<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> nth,
                           IRandomAccessIterator<T> last) where T : IComparable<T>
        {
            NthElement(first, nth, last, Compare.Less<T>());
        }
    }
}

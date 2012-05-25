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
        /// Sort sorts the elements in [first, last) into ascending order. 
        /// </para>
        /// <para>
        /// The complexity is O(N log(N))(both average and worst-case).
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref=" IRandomAccessIterator{T}"/> implementation pointing 
        /// to the first element of the range to be sorted.
        /// </param>
        /// <param name="last">
        /// An <see cref=" IRandomAccessIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be sorted.
        /// </param>
        /// <param name="less">
        /// A functor that is used to determine if one value is less than another.
        /// </param>
        /// <remarks>
        /// <para>
        /// Sort is not guaranteed to be stable. That is, suppose that i.Value and j.Value
        /// are equivalent: neither one is less than the other. It is not guaranteed 
        /// that the relative order of these two elements will be preserved by Sort.
        /// </para>
        /// <para>
        /// Also using build in sort methods, e.g. <see cref="System.Collections.Generic.List{T}.Sort()"/> 
        /// of containers is usually faster.
        /// </para>
        /// </remarks>
        public static void
            Sort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> less)
        {
            if (!Equals(first, last))
            {
                IntrosortLoop(first, last, __lg(last.Diff(first)) * 2, less);
                FinalInsertionSort(first, last, less);
            }
        }
        /// <summary>
        /// <para>
        /// Sort sorts the elements in [first, last) into ascending order. 
        /// </para>
        /// <para>
        /// The complexity is O(N log(N))(both average and worst-case).
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref=" IRandomAccessIterator{T}"/> implementation pointing 
        /// to the first element of the range to be sorted.
        /// </param>
        /// <param name="last">
        /// An <see cref=" IRandomAccessIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be sorted.
        /// </param>
        /// <remarks>
        /// <para>
        /// Sort is not guaranteed to be stable. That is, suppose that i.Value and j.Value
        /// are equivalent: neither one is less than the other. It is not guaranteed 
        /// that the relative order of these two elements will be preserved by Sort.
        /// </para>
        /// <para>
        /// Also using build in sort methods, e.g. <see cref="System.Collections.Generic.List{T}.Sort()"/> 
        /// of containers is usually faster.
        /// </para>
        /// </remarks>
        public static void
            Sort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last) where T: IComparable<T>
        {
            Sort(first, last, Compare.Less<T>());
        }
        private static void
            FinalInsertionSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> comparison)
        {
            if (last.Diff(first) > Threshold)
            {
                InsertionSort(first, first.Add(Threshold), comparison);
                UnguardedInsertionSort((first.Add(Threshold)), last, comparison);
            }
            else
                InsertionSort(first, last, comparison);
        }
        private static void
            IntrosortLoop<T>(IRandomAccessIterator<T> first,
                                IRandomAccessIterator<T> last,
                                int depthLimit, IBinaryFunction<T, T, bool> comparison)
        {
            first = (IRandomAccessIterator<T>)first.Clone();
            last = (IRandomAccessIterator<T>)last.Clone();

            while ((last.Diff(first)) > Threshold)
            {
                if (depthLimit == 0)
                {
                    PartialSort(first, last, last, comparison);
                    return;
                }
                --depthLimit;
                IRandomAccessIterator<T> cut =
                    UnguardedPartition(first, last,
                        Median(first.Value,
                                    (first.Add((last.Diff(first)) / 2)).Value,
                                    (last.Add(-1)).Value, comparison),
                                    comparison);
                IntrosortLoop(cut, last, depthLimit, comparison);
                last = (IRandomAccessIterator<T>)cut.Clone();
            }
        }
    }
}

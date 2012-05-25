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
using NStl.Collections;
using NStl.Linq;
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// EqualRange is a version of binary search: it attempts to find the element value in an ordered range [first, last). 
        /// The value returned by EqualRange is essentially a combination of the values returned by LowerBound and UpperBound.
        /// </para>
        /// <para>
        /// The number of comparisons is logarithmic: at most 2 * log(last - first) + 1. 
        /// If the input iterator is a <see cref="IRandomAccessIterator{T}"/> then the number of steps through the range is 
        /// also logarithmic; otherwise, the number of steps is proportional to last - first.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the input range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the input range.
        /// </param>
        /// <param name="val">The value to be searched for</param>
        /// <param name="less">The functor that rfepresents the order criteria.</param>
        /// <returns>
        /// A pair of iterators that represents the range of the found values. A pair of end 
        /// iterators if no value is found.
        /// </returns>
        /// <remarks>
        /// Note that you may use an ordering that is a strict weak ordering but not a total ordering; 
        /// that is, there might be values x and y such that x &lt; y, x &gt; y, and x == y are all false.
        /// </remarks>
        public static Range<T, InIt>
            EqualRange<T, InIt>(InIt first, InIt last, T val, IBinaryFunction<T, T, bool> less)
            where InIt : IInputIterator<T>
        {
            first = (InIt)first.Clone();
            int length = first.DistanceTo(last);//GEN

            while (length > 0)
            {
                int half = length / 2;
                InIt mid = (InIt)first.Clone();
                mid = (InIt)mid.Add(half);
                if (less.Execute(mid.Value, val))
                {
                    first = (InIt)mid.Clone();
                    first.PreIncrement();
                    length = length - half - 1;
                }
                else if (less.Execute(val, mid.Value))
                {
                    length = half;
                }
                else
                {
                    InIt left = LowerBound(first, mid, val, less);
                    first = (InIt)first.Add(length);
                    InIt right = UpperBound((InIt)mid.PreIncrement(), first, val, less);
                    return new Range<T, InIt>(left, right);
                }
            }
            return new Range<T, InIt>(first, first);

        }

        /// <summary>
        /// <para>
        /// EqualRange is a version of binary search: it attempts to find the element value in an ordered range [first, last). 
        /// The value returned by EqualRange is essentially a combination of the values returned by LowerBound and UpperBound.
        /// </para>
        /// <para>
        /// The complexity is logarithmic: at most 2 * log(last - first) + 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the input range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the input range.
        /// </param>
        /// <param name="val">The value to be searched for</param>
        /// <returns>
        /// A pair of iterators that represents the range of the found values. A pair of end 
        /// iterators if no value is found.
        /// </returns>
        /// <remarks>
        /// Note that you may use an ordering that is a strict weak ordering but not a total ordering; 
        /// that is, there might be values x and y such that x &lt; y, x &gt; y, and x == y are all false.
        /// </remarks>
        public static Range<T, InIt>
            EqualRange<T, InIt>(InIt first, InIt last, T val)
            where T : IComparable<T>
            where InIt : IForwardIterator<T>
        {
            return EqualRange(first, last, val, Compare.Less<T>());
        }
    }
}

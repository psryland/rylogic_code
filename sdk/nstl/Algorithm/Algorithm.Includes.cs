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
        /// Includes tests whether one sorted range includes another sorted range. 
        /// That is, it returns true if and only if, for every element in 
        /// [first2, last2), an equivalent element is also present in [first1, last1).
        /// </para>
        /// <para>
        /// The complexity is linear. Zero comparisons if either [first1, last1) 
        /// or [first2, last2) is an empty range, otherwise at most 
        /// 2 * ((last1 - first1) + (last2 - first2)) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the sorted range
        /// that is the potential super set.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the sorted range
        /// that is the potential super set.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the sorted range
        /// that is the potential sub set.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the sorted range
        /// that is the potential sub set.
        /// </param>
        /// <param name="less">
        /// The functor that is used to check if one element is less than the other.
        /// </param>
        /// <returns>
        /// True if the second range is a subset of the first, false otherwise.
        /// </returns>
        public static bool
            Includes<T>(IInputIterator<T> first1, IInputIterator<T> last1,
                        IInputIterator<T> first2, IInputIterator<T> last2,  
                        IBinaryFunction<T, T, bool> less)
        {
            first1 = (IInputIterator<T>)first1.Clone();
            first2 = (IInputIterator<T>)first2.Clone();
            while (!Equals(first1, last1) && !Equals(first2, last2))
            {
                if (less.Execute(first2.Value, first1.Value))
                    return false;
                else if (less.Execute(first1.Value, first2.Value))
                    first1.PreIncrement();
                else
                {
                    first1.PreIncrement();
                    first2.PreIncrement();
                }
            }
            return Equals(first2, last2);
        }
        /// <summary>
        /// <para>
        /// Includes tests whether one sorted range includes another sorted range. 
        /// That is, it returns true if and only if, for every element in 
        /// [first2, last2), an equivalent element is also present in [first1, last1).
        /// </para>
        /// <para>
        /// The complexity is linear. Zero comparisons if either [first1, last1) 
        /// or [first2, last2) is an empty range, otherwise at most 
        /// 2 * ((last1 - first1) + (last2 - first2)) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the sorted range
        /// that is the potential super set.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the sorted range
        /// that is the potential super set.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the sorted range
        /// that is the potential sub set.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the sorted range
        /// that is the potential sub set.
        /// </param>
        /// <returns>
        /// True if the second range is a subset of the first, false otherwise.
        /// </returns>
        public static bool
            Includes<T>(IInputIterator<T> first1, IInputIterator<T> last1,
                        IInputIterator<T> first2, IInputIterator<T> last2) 
            where T : IComparable<T>
        {
            return Includes(first1, last1, first2, last2, Compare.Less<T>());
        }
    }
}

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
        /// IsSorted returns true if the range [first, last) 
        /// is sorted in ascending order, and false otherwise.
        /// </para>
        /// <para>
        /// The complexity is linear. Zero comparisons if [first, last) 
        /// is an empty range, otherwise at most (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> implementation pointing
        /// to first element of the range to be checked.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> implementation pointing
        /// one past the final element of the range to be checked.
        /// </param>
        /// <param name="less">
        /// A functor that is used to determine if one element is less than the other.
        /// </param>
        /// <returns>
        /// True if the range is sorted, false otherwise.
        /// </returns>
        public static bool
            IsSorted<T>(IInputIterator<T> first, IInputIterator<T> last, IBinaryFunction<T, T, bool> less)
        {
            if (Equals(first, last))
                return true;
            IInputIterator<T> next = (IInputIterator<T>)first.Clone();
            for (next.PreIncrement(); !Equals(next, last); first = (IInputIterator<T>)next.Clone(), next.PreIncrement())
            {
                if (less.Execute(next.Value, first.Value))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// <para>
        /// IsSorted returns true if the range [first, last) 
        /// is sorted in ascending order, and false otherwise.
        /// </para>
        /// <para>
        /// The complexity is linear. Zero comparisons if [first, last) 
        /// is an empty range, otherwise at most (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> implementation pointing
        /// to first element of the range to be checked.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> implementation pointing
        /// one past the final element of the range to be checked.
        /// </param>
        /// <returns>
        /// True if the range is sorted, false otherwise.
        /// </returns>
        public static bool
            IsSorted<T>(IInputIterator<T> first, IInputIterator<T> last) where T : IComparable<T>
        {
            return IsSorted(first, last, Compare.Less<T>());
        }
    }
}

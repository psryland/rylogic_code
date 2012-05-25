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


using NStl.Iterators;
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// Every time a consecutive group of duplicate elements appears 
        /// in the range [first, last), the algorithm Unique removes all 
        /// but the first element. That is, Unique returns an iterator 
        /// newLast such that the range [first, newLast) contains no 
        /// two consecutive elements that are duplicates. The range [newLast, last) 
        /// contains unspecified elements. Unique is stable, meaning that the 
        /// relative order of elements that are not removed is unchanged.
        /// </para>
        /// <para>
        /// The complexity is linear.Exactly (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> pointing one past the final element of the range.
        /// </param>
        /// <param name="equals">
        /// A functor that is used to determine of two values are equal.
        /// </param>
        /// <returns>
        /// An iterator specifying the end of the unique range.
        /// </returns>
        public static FwdIt
            Unique<T, FwdIt>(FwdIt first, FwdIt last, IBinaryFunction<T, T, bool> equals)
            where FwdIt : IForwardIterator<T>
        {
            first = AdjacentFind(first, last, equals);
            return UniqueCopy(first, last, first, equals);
        }
        /// <summary>
        /// <para>
        /// Every time a consecutive group of duplicate elements appears 
        /// in the range [first, last), the algorithm Unique removes all 
        /// but the first element. That is, Unique returns an iterator 
        /// newLast such that the range [first, newLast) contains no 
        /// two consecutive elements that are duplicates. The range [newLast, last) 
        /// contains unspecified elements. Unique is stable, meaning that the 
        /// relative order of elements that are not removed is unchanged.
        /// </para>
        /// <para>
        /// The complexity is linear.Exactly (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> pointing one past the final element of the range.
        /// </param>
        /// <returns>
        /// An iterator specifying the end of the unique range.
        /// </returns>
        public static FwdIt
            Unique<T, FwdIt>(FwdIt first, FwdIt last) where FwdIt : IForwardIterator<T>
        {
            return Unique(first, last, Compare.EqualTo<T>());
        }
    }
}

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
        /// InplaceMerge combines two consecutive sorted ranges [first, middle)
        /// and [middle, last) into a single sorted range [first, last). 
        /// That is, it starts with a range [first, last) that consists of two pieces 
        /// each of which is in ascending order, and rearranges it so that the 
        /// entire range is in ascending order. InplaceMerge is stable, meaning 
        /// both that the relative order of elements within each input range is 
        /// preserved, and that for equivalent elements in both input 
        /// ranges the element from the first range precedes the element from 
        /// the second.
        /// </para>
        /// <para>
        /// InplaceMerge is an adaptive algorithm: it attempts to allocate 
        /// a temporary memory buffer, and its run-time complexity depends on 
        /// how much memory is available. Inplace_merge performs no comparisons 
        /// if [first, last) is an empty range. Otherwise, worst-case 
        /// behavior (if no auxiliary memory is available) is O(N log(N)), 
        /// where N is last - first, and best case (if a large enough auxiliary 
        /// memory buffer is available) is at most (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IBidirectionalIterator{T}"/> pointing to the start of the 
        /// first sub range to be merged.
        /// </param>
        /// <param name="middle">
        /// An <see cref="IBidirectionalIterator{T}"/> pointing one past the final
        /// element of the first sub range to be merged. It is also the first element 
        /// of the second subrange to to be merged.
        /// </param>
        /// <param name="last">
        /// An <see cref="IBidirectionalIterator{T}"/> pointing one past the final
        /// element of the second sub range to be merged.
        /// </param>
        /// <param name="less">
        /// A functor that is used to determine if one element is less then the other.
        /// </param>
        public static void
            InplaceMerge<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> middle,
                             IBidirectionalIterator<T> last, IBinaryFunction<T, T, bool> less)
        {
            int length1 = first.DistanceTo(middle);//GEN
            int length2 = middle.DistanceTo(last);//GEN

            T[] buf = CreateBuffer(first, last);
            if (buf == null)
                MergeWithoutBuffer(first, middle, last, length1, length2, less);
            else if (buf.Length == 0)
                MergeWithoutBuffer(first, middle, last, length1, length2, less);
            else
                MergeAdaptive(first, middle, last, length1, length2, buf.Begin(), buf.Length, less);
        }
        /// <summary>
        /// <para>
        /// InplaceMerge combines two consecutive sorted ranges [first, middle)
        /// and [middle, last) into a single sorted range [first, last). 
        /// That is, it starts with a range [first, last) that consists of two pieces 
        /// each of which is in ascending order, and rearranges it so that the 
        /// entire range is in ascending order. InplaceMerge is stable, meaning 
        /// both that the relative order of elements within each input range is 
        /// preserved, and that for equivalent elements in both input 
        /// ranges the element from the first range precedes the element from 
        /// the second.
        /// </para>
        /// <para>
        /// InplaceMerge is an adaptive algorithm: it attempts to allocate 
        /// a temporary memory buffer, and its run-time complexity depends on 
        /// how much memory is available. Inplace_merge performs no comparisons 
        /// if [first, last) is an empty range. Otherwise, worst-case 
        /// behavior (if no auxiliary memory is available) is O(N log(N)), 
        /// where N is last - first, and best case (if a large enough auxiliary 
        /// memory buffer is available) is at most (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IBidirectionalIterator{T}"/> pointing to the start of the 
        /// first sub range to be merged.
        /// </param>
        /// <param name="middle">
        /// An <see cref="IBidirectionalIterator{T}"/> pointing one past the final
        /// element of the first sub range to be merged. It is also the first element 
        /// of the second subrange to to be merged.
        /// </param>
        /// <param name="last">
        /// An <see cref="IBidirectionalIterator{T}"/> pointing one past the final
        /// element of the second sub range to be merged.
        /// </param>
        public static void
            InplaceMerge<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> middle,
                             IBidirectionalIterator<T> last) where T : IComparable<T>
        {
            InplaceMerge(first, middle, last, Compare.Less<T>());
        }
    }
}

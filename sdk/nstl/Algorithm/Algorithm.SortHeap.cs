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
        /// SortHeap turns a heap [first, last) into a sorted range. Note that this is 
        /// not a stable sort: the relative order of equivalent elements is not guaranteed 
        /// to be preserved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing one past the last element of the range.
        /// </param>
        /// <param name="comp">
        /// A binary predicate that defines the order of the heap.
        /// </param>
        public static void
            SortHeap<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> comp)
        {
            last = (IRandomAccessIterator<T>)last.Clone();
            while (last.Diff(first) > 1)
                PopHeap(first, (IRandomAccessIterator<T>)last.PostDecrement(), comp);
        }
        /// <summary>
        /// SortHeap turns a heap [first, last) into a sorted range. Note that this is 
        /// not a stable sort: the relative order of equivalent elements is not guaranteed 
        /// to be preserved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing one past the last element of the range.
        /// </param>
        public static void
            SortHeap<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last) where T : IComparable<T>
        {
            SortHeap(first, last, Compare.Less<T>());
        }
    }
}

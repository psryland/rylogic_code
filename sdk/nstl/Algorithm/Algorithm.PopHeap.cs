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
        /// PopHeap removes the largest element (that is, first.Value) from the heap [first, last). 
        /// The two versions of PopHeap differ in how they define whether one element is less 
        /// than another. This version compares objects using a function object comp.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing one past the final element of the range.
        /// </param>
        /// <param name="comp">A predicate that defines the order of the heap.</param>
        /// <remarks>
        /// <para>
        /// The postcondition for the first version of PopHeap is that 
        /// Algorithm.IsHeap(first, last-1) is true and that *(last - 1) is the element that 
        /// was removed from the heap. The postcondition for the second version is that 
        /// Algorithm.IsHeap(first, last-1, comp) is true and that (last - 1).Value is the 
        /// element that was removed from the heap.
        /// </para>
        /// <para>
        /// A heap is a particular way of ordering the elements in a range of <see cref="IRandomAccessIterator{T}"/>s [f, l). 
        /// The reason heaps are useful (especially for sorting, or as priority queues) is that they satisfy 
        /// two important properties. First, the value of f is the largest element in the heap. Second, 
        /// it is possible to add an element to a heap, 
        /// or to remove the first value, in logarithmic time. Internally, a heap is a tree represented as 
        /// a sequential range. The tree is constructed so that that each node is less than or equal to its parent node.
        /// </para>
        /// </remarks>    
        public static void
            PopHeap<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> comp)
        {
            if (Equals(first, last))
                return;
            first = (IRandomAccessIterator<T>)first.Clone();
            IRandomAccessIterator<T> lastLess1 = last.Add(-1);
            PopHeap(first, lastLess1, lastLess1, (lastLess1).Value, comp);
        }
        /// <summary>
        /// PopHeap removes the largest element (that is, first.Value) from the heap [first, last). 
        /// The two versions of PopHeap differ in how they define whether one element is less 
        /// than another. This version compares objects using the <see cref="IComparable{T}"/> implemetation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing one past the final element of the range.
        /// </param>
        /// <remarks>
        /// <para>
        /// The postcondition for the first version of PopHeap is that 
        /// Algorithm.IsHeap(first, last-1) is true and that *(last - 1) is the element that 
        /// was removed from the heap. The postcondition for the second version is that 
        /// Algorithm.IsHeap(first, last-1, comp) is true and that (last - 1).Value is the 
        /// element that was removed from the heap.
        /// </para>
        /// <para>
        /// A heap is a particular way of ordering the elements in a range of <see cref="IRandomAccessIterator{T}"/>s [f, l). 
        /// The reason heaps are useful (especially for sorting, or as priority queues) is that they satisfy 
        /// two important properties. First, the value of f is the largest element in the heap. Second, 
        /// it is possible to add an element to a heap, 
        /// or to remove the first value, in logarithmic time. Internally, a heap is a tree represented as 
        /// a sequential range. The tree is constructed so that that each node is less than or equal to its parent node.
        /// </para>
        /// </remarks>    
        public static void
            PopHeap<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last) where T : IComparable<T>
        {
            PopHeap(first, last, Compare.Less<T>());
        }
        private static void
            PopHeap<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last,
                       IRandomAccessIterator<T> result, T Value, IBinaryFunction<T, T, bool> comparison)
        {
            first = (IRandomAccessIterator<T>)first.Clone();
            result.Value = first.Value;
            AdjustHeap(first, 0, last.Diff(first), Value, comparison);
        }
    }
}

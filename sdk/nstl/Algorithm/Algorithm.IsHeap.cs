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
using NStl.Linq;
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// IsHeap returns true if the range [first, last) is a heap, and false otherwise. 
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
        /// <returns></returns>
        /// <remarks>
        /// A heap is a particular way of ordering the elements in a range of <see cref="IRandomAccessIterator{T}"/>s [f, l). 
        /// The reason heaps are useful (especially for sorting, or as priority queues) is that they satisfy 
        /// two important properties. First, the value of f is the largest element in the heap. Second, 
        /// it is possible to add an element to a heap, 
        /// or to remove the first value, in logarithmic time. Internally, a heap is a tree represented as 
        /// a sequential range. The tree is constructed so that that each node is less than or equal to its parent node.
        /// </remarks>
        public static bool IsHeap<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> comp)
        {
            return IsHeap(first, comp, first.DistanceTo(last));
        }
        /// <summary>
        /// IsHeap returns true if the range [first, last) is a heap, and false otherwise. The heap is
        /// ordered using <see cref="Compare.Less{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> pointing one past the last element of the range.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// A heap is a particular way of ordering the elements in a range of <see cref="IRandomAccessIterator{T}"/>s [f, l). 
        /// The reason heaps are useful (especially for sorting, or as priority queues) is that they satisfy 
        /// two important properties. First, the value of f is the largest element in the heap. Second, 
        /// it is possible to add an element to a heap, 
        /// or to remove the first value, in logarithmic time. Internally, a heap is a tree represented as 
        /// a sequential range. The tree is constructed so that that each node is less than or equal to its parent node.
        /// </remarks>
        public static bool IsHeap<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last)
            where T: IComparable<T>
        {
            return IsHeap(first, Compare.Less<T>(), first.DistanceTo(last));
        }
        private static bool IsHeap<T>(IRandomAccessIterator<T> first, IBinaryFunction<T, T, bool> comp, int n)
        {
          int parent = 0;
          for (int child = 1; child < n; ++child) {
              if (comp.Execute(first.Add(parent).Value, first.Add(child).Value))
              return false;
            if ((child & 1) == 0)
              ++parent;
          }
          return true;
        }
    }
}

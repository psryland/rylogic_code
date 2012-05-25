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
        /// PartialSort rearranges the elements in the range [first, last) 
        /// so that they are partially in ascending order. Specifically, 
        /// it places the smallest middle - first elements, sorted in 
        /// ascending order, into the range [first, middle). The remaining 
        /// last - middle elements are placed, in an unspecified order, into 
        /// the range [middle, last).
        /// </para>
        /// <para>
        /// Note that the elements in the range [first, middle) will be the same 
        /// (ignoring, for the moment, equivalent elements) as if you had sorted 
        /// the entire range using Algorithm.Sort(first, last). The reason for 
        /// using PartialSort in preference to sort is simply efficiency: a 
        /// partial sort, in general, takes less time.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// to the first element of the range.
        /// </param>
        /// <param name="middle">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// to one  past the final position to be sortded.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// one past the final element of the total range.</param>
        /// <param name="comp">
        /// A functor that is used to determine if two elements are less to another.</param>
        /// <remarks>
        /// PartialSort(first, last, last) has the effect of sorting the entire 
        /// range [first, last), just like sort(first, last). They use different 
        /// algorithms, however: sort uses the introsort algorithm (a variant of quicksort), 
        /// and PartialSort uses Algorithm.HeapSort. See section 5.2.3 of Knuth 
        /// (D. E. Knuth, The Art of Computer Programming. Volume 3: Sorting and 
        /// Searching. Addison-Wesley, 1975.), and J. W. J. Williams (CACM 7, 347, 1964). 
        /// Both heapsort and introsort have complexity of order N log(N), but 
        /// introsort is usually faster by a factor of 2 to 5.
        /// </remarks>
        public static void
            PartialSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> middle, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> comp)
        {
            MakeHeap(first, middle, comp);
            for (IRandomAccessIterator<T> i = (IRandomAccessIterator<T>)middle.Clone(); i.Less(last); i.PreIncrement())
                if (comp.Execute(i.Value, first.Value))
                    PopHeap(first, middle, i, i.Value, comp);
            SortHeap(first, middle, comp);
        }
        /// <summary>
        /// <para>
        /// PartialSort rearranges the elements in the range [first, last) 
        /// so that they are partially in ascending order. Specifically, 
        /// it places the smallest middle - first elements, sorted in 
        /// ascending order, into the range [first, middle). The remaining 
        /// last - middle elements are placed, in an unspecified order, into 
        /// the range [middle, last).
        /// </para>
        /// <para>
        /// Note that the elements in the range [first, middle) will be the same 
        /// (ignoring, for the moment, equivalent elements) as if you had sorted 
        /// the entire range using Algorithm.Sort(first, last). The reason for 
        /// using PartialSort in preference to sort is simply efficiency: a 
        /// partial sort, in general, takes less time.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// to the first element of the range.
        /// </param>
        /// <param name="middle">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// to one  past the final position to be sortded.
        /// </param>
        /// <param name="last">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing 
        /// one past the final element of the total range.</param>
        /// <remarks>
        /// PartialSort(first, last, last) has the effect of sorting the entire 
        /// range [first, last), just like sort(first, last). They use different 
        /// algorithms, however: sort uses the introsort algorithm (a variant of quicksort), 
        /// and PartialSort uses Algorithm.HeapSort. See section 5.2.3 of Knuth 
        /// (D. E. Knuth, The Art of Computer Programming. Volume 3: Sorting and 
        /// Searching. Addison-Wesley, 1975.), and J. W. J. Williams (CACM 7, 347, 1964). 
        /// Both heapsort and introsort have complexity of order N log(N), but 
        /// introsort is usually faster by a factor of 2 to 5.
        /// </remarks>
        public static void
            PartialSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> middle,
                            IRandomAccessIterator<T> last) where T : IComparable<T>
        {
            PartialSort(first, middle, last, Compare.Less<T>());
        }
    }
}

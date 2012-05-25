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
        /// Merge combines two sorted ranges [first1, last1) and [first2, last2) 
        /// into a single sorted range. 
        /// </para>
        /// <para>
        /// Merge is stable, meaning both that the relative order of 
        /// elements within each input range is preserved, and that 
        /// for equivalent elements in both input ranges the element 
        /// from the first range precedes the element from the second. 
        /// The return value is result + (last1 - first1) + (last2 - first2).
        /// </para>
        /// <para>
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutputIt"></typeparam>
        /// <param name="first1">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// to the first element of the first range to be merged.
        /// </param>
        /// <param name="last1">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the first range to be merged.
        /// </param>
        /// <param name="first2">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// to the first element of the second range to be merged.
        /// </param>
        /// <param name="last2">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the second range to be merged.
        /// </param>
        /// <param name="result">
        /// Am <see cref="IOutputIterator{T}"/> implementation pointing to the
        /// start of the target range.
        /// </param>
        /// <param name="less">
        /// A functor that is used to determine if one element 
        /// is less than another.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation 
        /// pointing one past the last element of the target range.
        /// </returns>
        /// <remarks>
        /// Note that you may use an ordering that is a strict weak ordering but 
        /// not a total ordering; that is, there might be values x and y such 
        /// that x &lt; y, x &gt; y, and x == y are all false. 
        /// Two elements x and y are equivalent if neither x &lt; y nor y &lt; x. 
        /// If you're using a total ordering, however (if you're using strcmp, 
        /// for example, or if you're using ordinary arithmetic comparison on integers), 
        /// then you can ignore this technical distinction: for a total ordering, 
        /// equality and equivalence are the same.
        /// </remarks>
        public static OutputIt
            Merge<T, OutputIt>(IInputIterator<T> first1, IInputIterator<T> last1,
                     IInputIterator<T> first2, IInputIterator<T> last2,
                     OutputIt result, IBinaryFunction<T, T, bool> less) where OutputIt : IOutputIterator<T>
        {
            first1 = (IInputIterator<T>)first1.Clone();
            first2 = (IInputIterator<T>)first2.Clone();
            result = (OutputIt)result.Clone();
            while (!Equals(first1,last1) && !Equals(first2, last2))
            {
                if (less.Execute(first2.Value, first1.Value))
                {
                    result.Value = first2.Value;
                    first2.PreIncrement();
                }
                else
                {
                    result.Value = first1.Value;
                    first1.PreIncrement();
                }
                result.PreIncrement();
            }
            return Copy(first2, last2, Copy(first1, last1, result));
        }
        /// <summary>
        /// <para>
        /// Merge combines two sorted ranges [first1, last1) and [first2, last2) 
        /// into a single sorted range. 
        /// </para>
        /// <para>
        /// Merge is stable, meaning both that the relative order of 
        /// elements within each input range is preserved, and that 
        /// for equivalent elements in both input ranges the element 
        /// from the first range precedes the element from the second. 
        /// The return value is result + (last1 - first1) + (last2 - first2).
        /// </para>
        /// <para>
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutputIt"></typeparam>
        /// <param name="first1">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// to the first element of the first range to be merged.
        /// </param>
        /// <param name="last1">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the first range to be merged.
        /// </param>
        /// <param name="first2">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// to the first element of the second range to be merged.
        /// </param>
        /// <param name="last2">
        /// An <see cref=" IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the second range to be merged.
        /// </param>
        /// <param name="result">
        /// Am <see cref="IOutputIterator{T}"/> implementation pointing to the
        /// start of the target range.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation 
        /// pointing one past the last element of the target range.
        /// </returns>
        /// <remarks>
        /// Note that you may use an ordering that is a strict weak ordering but 
        /// not a total ordering; that is, there might be values x and y such 
        /// that x &lt; y, x &gt; y, and x == y are all false. 
        /// Two elements x and y are equivalent if neither x &lt; y nor y &lt; x. 
        /// If you're using a total ordering, however (if you're using strcmp, 
        /// for example, or if you're using ordinary arithmetic comparison on integers), 
        /// then you can ignore this technical distinction: for a total ordering, 
        /// equality and equivalence are the same.
        /// </remarks>
        public static OutputIt
            Merge<T, OutputIt>(IInputIterator<T> first1, IInputIterator<T> last1,
                     IInputIterator<T> first2, IInputIterator<T> last2,
                     OutputIt result)
            where T : IComparable<T>
            where OutputIt : IOutputIterator<T>
        {
            return Merge(first1, last1, first2, last2, result, Compare.Less<T>());
        }
    }
}

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
        /// PartialSortCopy copies the smallest N elements from the range 
        /// [first, last) to the range [resultFirst, resultFirst + N), 
        /// where N is the smaller of last - first and resultLast - resultFirst. 
        /// The elements in [result_first, resultDirst + N) will be in ascending order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="RndIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing to first element
        /// of the input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing one past the final element
        /// of the input range.
        /// </param>
        /// <param name="resultFirst">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing to first element
        /// of the target range.
        /// </param>
        /// <param name="resultLast">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing one past the final element
        /// of the target range.
        /// </param>
        /// <param name="less">
        /// A functor that is used to determine if one element is less than another.
        /// </param>
        /// <returns>
        /// An <see cref="IRandomAccessIterator{T}"/> pointing to the end of the target range.
        /// </returns>
        public static RndIt
            PartialSortCopy<T, RndIt>(IForwardIterator<T> first, IForwardIterator<T> last,
                                 RndIt resultFirst, RndIt resultLast,
                                 IBinaryFunction<T, T, bool> less) where RndIt : IRandomAccessIterator<T>
        {
            if (Equals(resultFirst, resultLast))
                return resultLast;
            first = (IForwardIterator<T>)first.Clone();
            RndIt realResultLast = (RndIt)resultFirst.Clone();
            while (!Equals(first, last) && !Equals(realResultLast, resultLast))
            {
                realResultLast.Value = first.Value;
                realResultLast.PreIncrement();
                first.PreIncrement();
            }
            MakeHeap(resultFirst, realResultLast, less);
            while (!Equals(first, last))
            {
                if (less.Execute(first.Value, resultFirst.Value))
                    AdjustHeap(resultFirst, 0,
                            realResultLast.Diff(resultFirst),
                            first.Value, less);
                first.PreIncrement();
            }
            SortHeap(resultFirst, realResultLast, less);
            return realResultLast;
        }
        /// <summary>
        /// PartialSortCopy copies the smallest N elements from the range 
        /// [first, last) to the range [resultFirst, resultFirst + N), 
        /// where N is the smaller of last - first and resultLast - resultFirst. 
        /// The elements in [result_first, resultDirst + N) will be in ascending order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="RndIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing to first element
        /// of the input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing one past the final element
        /// of the input range.
        /// </param>
        /// <param name="resultFirst">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing to first element
        /// of the target range.
        /// </param>
        /// <param name="resultLast">
        /// An <see cref="IRandomAccessIterator{T}"/> implementation pointing one past the final element
        /// of the target range.
        /// </param>
        /// <returns>
        /// An <see cref="IRandomAccessIterator{T}"/> pointing to the end of the target range.
        /// </returns>
        public static RndIt
            PartialSortCopy<T, RndIt>(IForwardIterator<T> first, IForwardIterator<T> last,
                                        RndIt resultFirst, RndIt resultLast)
            where T : IComparable<T>
            where RndIt : IRandomAccessIterator<T>
        {
            return PartialSortCopy(first, last, resultFirst, resultLast, Compare.Less<T>());
        }
    }
}

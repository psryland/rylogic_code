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
using System.Collections.Generic;
using NStl.Linq;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// AdjacentDifference calculates the differences of 
        /// adjacent elements in the range [first, last). 
        /// This is, first.Value is assigned to result.Value, and, 
        /// for each iterator i in the range [first + 1, last), 
        /// the result of the passed in binary functor with i.Value and (i - 1).Value is assigned 
        /// to (result + (i - first)).Value.
        /// </para>
        /// <para>
        /// Note that result is permitted to be the same iterator as first. This is useful for computing differences "in place".
        /// </para>
        /// <para>
        /// The reason it is useful to store the value of the first 
        /// element, as well as simply storing the differences, 
        /// is that this provides enough information to reconstruct 
        /// the input range. In particular, if addition and subtraction 
        /// have the usual arithmetic definitions, then 
        /// AdjacentDifference and <see cref="PartialSum{T,FwdIt}"/> 
        /// inverses of each other.
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// to the first element of the input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// to the end element of the input range.
        /// </param>
        /// <param name="result">
        /// An <see cref="IOutputIterator{T}"/> implementation pointing
        /// to the first element of the target range.
        /// </param>
        /// <param name="difference">
        /// A functor object that is used to calculate the difference of two elements.
        /// </param>
        /// <returns>
        /// An <see cref="IOutputIterator{T}"/> pointing to the new end of the target range.
        /// </returns>
        public static OutIt
            AdjacentDifference<T, OutIt>(IInputIterator<T> first, IInputIterator<T> last,
                                   OutIt result, IBinaryFunction<T, T, T> difference) where OutIt : IOutputIterator<T>
        {
            return Copy(AdjacentDifference(first.AsEnumerable(last), difference), result);
        }
        /// <summary>
        /// <para>
        /// AdjacentDifference calculates the differences of 
        /// adjacent elements in the input range.
        /// </para>
        /// <para>
        /// The reason it is useful to store the value of the first 
        /// element, as well as simply storing the differences, 
        /// is that this provides enough information to reconstruct 
        /// the input range. In particular, if addition and subtraction 
        /// have the usual arithmetic definitions, then 
        /// AdjacentDifference and <see cref="PartialSum{T,FwdIt}"/> 
        /// inverses of each other.
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">The input range.</param>
        /// <param name="difference">
        /// A functor object that is used to calculate the difference of two elements.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> representing the result range.
        /// </returns>
        public static IEnumerable<T>
            AdjacentDifference<T>(IEnumerable<T> range, IBinaryFunction<T, T, T> difference)
        {
            return AdjacentDifference(range, difference.Execute);
        }
        /// <summary>
        /// <para>
        /// AdjacentDifference calculates the differences of 
        /// adjacent elements in the input range.
        /// </para>
        /// <para>
        /// The reason it is useful to store the value of the first 
        /// element, as well as simply storing the differences, 
        /// is that this provides enough information to reconstruct 
        /// the input range. In particular, if addition and subtraction 
        /// have the usual arithmetic definitions, then 
        /// AdjacentDifference and <see cref="PartialSum{T,FwdIt}"/> 
        /// inverses of each other.
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">The input range.</param>
        /// <param name="difference">
        /// A functor object that is used to calculate the difference of two elements.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> representing the result range.
        /// </returns>
        public static IEnumerable<T>
            AdjacentDifference<T>(IEnumerable<T> range, Func<T, T, T> difference)
        {
            using (IEnumerator<T> e = range.GetEnumerator())
            {
                if(!e.MoveNext())
                    yield break;
                T value = e.Current;
                yield return value;
                while (e.MoveNext())
                {
                    T tmp = e.Current;
                    yield return difference(tmp, value);
                    value = tmp;
                }
                yield break;
            }
        }
    }
}

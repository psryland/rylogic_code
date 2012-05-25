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
        /// UniqueCopy copies elements from the range [first, last) 
        /// to a range beginning with result, except that in 
        /// a consecutive group of duplicate elements only the first one is copied. 
        /// The return value is the end of the range to which the 
        /// elements are copied. This behavior is similar to the Unix filter uniq.
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly last - first comparisons, 
        /// and at most last - first assignments.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the range.
        /// </param>
        /// <param name="result">
        /// An <see cref="IOutputIterator{T}"/> specifying the first element of the target range.
        /// </param>
        /// <param name="equals">
        /// A functor that is used to determine of two values are equal.
        /// </param>
        /// <returns>
        /// An <see cref="IOutputIterator{T}"/> pointing to the end of the copied items.
        /// </returns>
        public static OutIt
            UniqueCopy<T, OutIt>(IInputIterator<T> first, IInputIterator<T> last, OutIt result, IBinaryFunction<T, T, bool> equals)
            where OutIt : IForwardIterator<T>
        {
            if (Equals(first, last))
                return (OutIt)result.Clone();
            first = (IInputIterator<T>)first.Clone();
            result = (OutIt)result.Clone();
            result.Value = first.Value;
            while (!Equals(first.PreIncrement(), last))
                if (!equals.Execute(result.Value, first.Value))
                    (result.PreIncrement()).Value = first.Value;
            return (OutIt)result.PreIncrement();
        }
        /// <summary>
        /// <para>
        /// UniqueCopy copies elements from the range [first, last) 
        /// to a range beginning with result, except that in 
        /// a consecutive group of duplicate elements only the first one is copied. 
        /// The return value is the end of the range to which the 
        /// elements are copied. This behavior is similar to the Unix filter uniq.
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly last - first comparisons, 
        /// and at most last - first assignments.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the range.
        /// </param>
        /// <param name="result">
        /// An <see cref="IOutputIterator{T}"/> specifying the first element of the target range.
        /// </param>
        /// <returns>
        /// An <see cref="IOutputIterator{T}"/> pointing to the end of the copied items.
        /// </returns>
        public static OutIt
            UniqueCopy<T, OutIt>(IInputIterator<T> first, IInputIterator<T> last, OutIt result)
            where OutIt : IForwardIterator<T>
        {
            return UniqueCopy(first, last, result, Compare.EqualTo<T>());
        }
    }
}

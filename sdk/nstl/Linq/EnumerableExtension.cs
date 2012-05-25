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

using System.Collections;
using System.Collections.Generic;
using NStl.Collections;
using NStl.Iterators;
using NStl.Iterators.Support;
using System.Linq;

namespace NStl.Linq
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods 
    /// for querying objects that implement <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class EnumerableExtension
    {
        /// <summary>
        /// Returns true, if the enumerable is empty, false otherwise.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [System.Obsolete("Use IEnumerable<T>.Any() extension method in the System.Linq namepsace instead!")]
        public static bool IsEmpty(this IEnumerable e)
        {
            return !e.GetEnumerator().MoveNext();
        }
        /// <summary>
        /// Returns an <see cref="EnumerableIterator{T}"/> pointing to 
        /// the first element of the range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e">The range.</param>
        /// <returns></returns>
        public static EnumerableIterator<T> Begin<T>(this IEnumerable<T> e)
        {
            return new EnumerableIterator<T>(e);
        }
        /// <summary>
        /// Returns an <see cref="EnumerableIterator{T}"/> pointing 
        /// one past the final element of the range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static EnumerableIterator<T> End<T>(this IEnumerable<T> e)
        {
            return new EnumerableIterator<T>(e, true);
        }
        /// <summary>
        /// Returns an <see cref="EnumerableIterator{T}"/> pointing to 
        /// the first element of the range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e">The range.</param>
        /// <returns></returns>
        public static EnumerableIterator<T> Begin<T>(this IEnumerable e)
        {
            return new EnumerableIterator<T>(e);
        }
        /// <summary>
        /// Returns an <see cref="EnumerableIterator{T}"/> pointing 
        /// one past the final element of the range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static EnumerableIterator<T> End<T>(this IEnumerable e)
        {
            return new EnumerableIterator<T>(e, true);
        }
        /// <summary>
        /// This method simply creates a copy of the enumerable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src">The input range to be copied.</param>
        /// <returns>A copy of the input range.</returns>
        public static IEnumerable<T> Copy<T>(this IEnumerable<T> src)
        {
            return new List<T>(src);
        }
        /// <summary>
        /// Returns an <see cref="ICollection"/> adapter for the enumerable.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static ICollection AsCollection(this IEnumerable e)
        {
            return new Range<object, IInputIterator<object>>(e.Begin<object>(), e.End<object>());
        }
    }
}

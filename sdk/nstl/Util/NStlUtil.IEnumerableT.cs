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


using System.Collections.Generic;
using System.Collections;
using NStl.Collections;
using NStl.Iterators;
using NStl.Iterators.Support;
using System;

namespace NStl
{
    public static partial class NStlUtil
    {
        /// <summary>
        /// Get a Start Iterator for a IEnumerable collection.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        /// <remarks>
        /// The returned iterator is a weak input iterator. This means that it can only be applied to
        /// non-mutative algorithms.
        /// </remarks>
        [Obsolete("Use the extension method IEnumerable<T>.Begin() in the NStl.Linq namepsace.")]
        public static EnumerableIterator<T>
            Begin<T>(IEnumerable<T> l)
        {
            return new EnumerableIterator<T>(l);
        }
        /// <summary>
        /// Get a Start Iterator for a IEnumerable collection.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        /// <remarks>
        /// The returned iterator is a weak input iterator. This means that it can only be applied to
        /// non-mutative algorithms.
        /// </remarks>
        [Obsolete("Use the extension method IEnumerable<T>.End() in the NStl.Linq namespace.")]
        public static EnumerableIterator<T>
            End<T>(IEnumerable<T> l)
        {
            return new EnumerableIterator<T>(l, true);//performance tweak, no enumerator object needed!
        }
        /// <summary>
        /// Returns an adapter that implements <see cref="IEnumerable&lt;T&gt;"/>, 
        /// so that any range bound by two iterators can be traversed the .NET way 
        /// using the foreach statement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An iterator addressing the position of the first element.
        /// </param>
        /// <param name="last">
        /// An iterator addressing the position one past the last element.
        /// </param>
        /// <returns>An <see cref="IEnumerable&lt;T&gt;"/> implemetation.</returns>
        [Obsolete("Use the extension method IInputIterator<T>.AsEnumerable() in the NStl.Linq namespace.")]
        public static IEnumerable<T>
            Enumerable<T>(IInputIterator<T> first, IInputIterator<T> last)
        {
            return new Range<T, IInputIterator<T>>(first, last);
        }
        /// <summary>
        /// Returns an adapter that implements <see cref="IEnumerable&lt;T&gt;"/>, 
        /// so that any range bound by two iterators can be traversed the .NET way 
        /// using the foreach statement.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">
        /// The range that needs an adapter for the IEnumerator interface.
        /// </param>
        /// <returns>An <see cref="IEnumerable&lt;T&gt;"/> implemetation.</returns>
        [Obsolete("Use the extension method IEnumerable.Cast<T>() in the System.Linq namespace.")]
        public static IEnumerable<T>
            Enumerable<T>(IRange<T> range)
        {
            return Enumerable(range.Begin(), range.End());
        }
        /// <summary>
        /// Returns an adapter that implements <see cref="IEnumerable&lt;T&gt;"/>,
        /// adapting an untyped IEnumerator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range"></param>
        /// <returns></returns>
        [Obsolete("Use the extension method IEnumerable.Cast<T>() in the System.Linq namespace.")]
        public static IEnumerable<T>
            Enumerable<T>(IEnumerable range)
        {
            foreach (T t in range)
                yield return t;
        }
    }
}

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
using System.Collections.Generic;
using NStl.Collections;
using NStl.Iterators.Support;
using NStl.Linq;

namespace NStl
{
    public static partial class Adapt
    {
        /// <summary>
        /// Adapts an <see cref="IEnumerable{T}"/> to a NSTL compatible range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is an alternative to the <see cref="NStlUtil"/>'s Begin(..) 
        /// an End(..) adapter methods.
        /// </remarks>
        [Obsolete("Use the Iterator<T>.ToRange(Iterator<T> last) extension method in the NStl.Linq namespace instead!")]
        public static Range<T, EnumerableIterator<T>> Range<T>(IEnumerable<T> e)
        {
            return new Range<T, EnumerableIterator<T>>(e.Begin(), e.End());
        }
        /// <summary>
        /// Adapts an <see cref="LinkedList{T}"/> to a NSTL compatible range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="l"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is an alternative to the <see cref="NStlUtil"/>'s Begin(..) 
        /// an End(..) adapter methods.
        /// </remarks>
        [Obsolete("Use the Iterator<T>.ToRange(Iterator<T> last) extension method in the NStl.Linq namespace instead!")]
        public static Range<T, LinkedListIterator<T>> Range<T>(LinkedList<T> l)
        {
            return new Range<T, LinkedListIterator<T>>(NStlUtil.Begin(l), NStlUtil.End(l));
        }
        /// <summary>
        /// Adapts an <see cref="IList{T}"/> to a NSTL compatible range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="l"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is an alternative to the <see cref="NStlUtil"/>'s Begin(..) 
        /// an End(..) adapter methods.
        /// </remarks>
        [Obsolete("Use the Iterator<T>.ToRange(Iterator<T> last) extension method in the NStl.Linq namespace instead!")]
        public static Range<T, ListIterator<T>> Range<T>(IList<T> l)
        {
            return new Range<T, ListIterator<T>>(l.Begin(), l.End());
        }
    }
}

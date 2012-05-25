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
using System.Collections;
using System.Collections.Generic;
using NStl.Iterators;
using NStl.Collections;
using NStl.Linq;

namespace NStl
{
    public static partial class NStlUtil
	{
        /// <summary>
        /// Returns an adapter that implements <see cref="ICollection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An iterator addressing the position of the first element.
        /// </param>
        /// <param name="last">
        /// An iterator addressing the position one past the last element.
        /// </param>
        /// <returns>An <see cref="ICollection"/> implemetation.</returns>
        [Obsolete("Use Iterator.AsCollection(...) extension method instead!")]
        public static ICollection
            Collection<T>(IInputIterator<T> first, IInputIterator<T> last)
        {
            return new Range<T, IInputIterator<T>>(first, last);
        }
        /// <summary>
        /// Returns an adapter that implements <see cref="ICollection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">
        /// The range that needs an adapter for the <see cref="ICollection"/> interface.
        /// </param>
        /// <returns>An <see cref="ICollection"/> implemetation.</returns>
        [Obsolete("Use the IEnumerable.AsCollection() extension method in the NStl.Linq namespace!")]
        public static ICollection
            Collection<T>(IRange<T> range)
        {
            return Collection(range.Begin(), range.End());
        }
        /// <summary>
        /// Returns an adapter that implements <see cref="ICollection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">
        /// The range that needs an adapter for the <see cref="ICollection"/> interface.
        /// </param>
        /// <returns>An <see cref="ICollection"/> implemetation.</returns>
        [Obsolete("Use the IEnumerable.AsCollection() extension method in the NStl.Linq namespace!")]
        public static ICollection
            Collection<T>(IEnumerable<T> range)
        {
            return Collection(range.Begin(), range.End());
        }
        /// <summary>
        /// Returns an adapter that implements <see cref="ICollection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range">
        /// The range that needs an adapter for the <see cref="ICollection"/> interface.
        /// </param>
        /// <returns>An <see cref="ICollection"/> implemetation.</returns>
        [Obsolete("Use the IEnumerable.AsCollection() extension method in the NStl.Linq namespace!")]
        public static ICollection
            Collection<T>(IEnumerable range)
        {
            return Collection(range.Begin<T>(), range.End<T>());
        }
	}
}

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
        [Obsolete("Use the IEnumerable.Begin<T> extension method in the NStl.Linq namespace.")]
        public static EnumerableIterator<T>
            Begin<T>(IEnumerable l)
        {
            return new EnumerableIterator<T>(Adapt.Generic<T>(l));
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
        [Obsolete("Use the IEnumerable.End<T> extension method in the NStl.Linq namespace.")]
        public static EnumerableIterator<T>
            End<T>(IEnumerable l)
        {
            return new EnumerableIterator<T>(Adapt.Generic<T>(l), true);//performance tweak, no enumerator object needed!
        }
    }
}

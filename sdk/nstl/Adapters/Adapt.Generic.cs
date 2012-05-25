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
using System.Collections;


namespace NStl
{
    public static partial class Adapt
    {
        /// <summary>
        /// Adapts an <see cref="IList"/> implementation to an <see cref="IList{T}"/>
        /// without copying it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        [Obsolete("Use the IList.Cast<T> extension method in the NStl.Linq namespace.")]
        public static IList<T> Generic<T>(IList src)
        {
            return new IList2IlistTAdapter<T>(src);
        }

        /// <summary>
        /// Adapts an <see cref="IEnumerable"/> implementation to an <see cref="IEnumerable{T}"/>
        /// without copying it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        [Obsolete("Use the Linq IEnumerable.Cast<T> extension method.")]
        public static IEnumerable<T> Generic<T>(IEnumerable src)
        {
            foreach (T t in src)
                yield return t;
        }
        /// <summary>
        /// Adapts an <see cref="IDictionary"/> implementation to an <see cref="IDictionary{Key, Value}"/>
        /// without copying it.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        [Obsolete("Use the IDictionary.Cast<T> extension method in the NStl.Linq namespace.")]
        public static IDictionary<Key, Value> Generic<Key, Value>(IDictionary src)
        {
            return new IDictionary2IDictionaryTAdapter<Key, Value>(src);
        }
    }
}

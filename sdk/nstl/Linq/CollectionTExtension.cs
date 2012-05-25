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
using NStl.Adapters;
using NStl.Iterators;
using NStl.Iterators.Private;

namespace NStl.Linq
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods 
    /// for extending objects of type <see cref="ICollection{T}"/>.
    /// </summary>
    public static class CollectionTExtension
    {
        /// <summary>
        /// Adds the elements of the specified enumerable to the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="toAdd">The collection to be added.</param>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> toAdd)
        {
            if(toAdd == null)
                return;
            foreach (var t in toAdd)
                collection.Add(t);
        }
        /// <summary>
        /// Adds the elements  to the collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="toAdd">The collection to be added.</param>
        public static void AddRange<T>(this ICollection<T> collection, params T[] toAdd)
        {
            AddRange(collection, (IEnumerable<T>) toAdd);
        }
        /// <summary>
        /// Returns an iterator adapter that can be used as an output
        /// iterator. It simply adds items to the collection by calling <see cref="ICollection{T}.Add"/>.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public static IOutputIterator<T>
            AddInserter<T>(this ICollection<T> l)
        {
            return new CollectionAddInsertIterator<T>(l);
        }
        /// <summary>
        /// Creates an adapter that allows to use a generic collectiona as its non generic counterpart.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is a dangerous tool only suited to satisfy compatibility issues
        /// with older APIs.
        /// </remarks>
        public static ICollection Weak<T>(this ICollection<T> list)
        {
            return new CollectionT2ICollectionAdapter<T>(list);
        }
    }
}

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
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// When implemented, it represents a readonly collection, i.e. a
    /// collection that cannot be modified.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public interface IReadOnlyCollection<T> : ICollection, IEnumerable<T>
    {
        /// <summary>
        /// Determines whether the container contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate</param>
        /// <returns>true if item is found in the container; otherwise, false.</returns>
        bool Contains(T item);
        /// <summary>
        /// Copies the content of this container into a given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex">The index in the array where the first element is to be inserted.</param>
        void CopyTo(T[] array, int arrayIndex);
        /// <summary>
        /// Copies the content of this container into a newly allocated array.
        /// </summary>
        /// <returns></returns>
        T[] ToArray();
        /// <summary>
        /// return true, if the container is empty, false otherwise.
        /// </summary>
        bool Empty{ get; }
        /// <summary>
        /// returns the first element in the container;
        /// </summary>
        /// <returns></returns>
        T Front();
    }
}

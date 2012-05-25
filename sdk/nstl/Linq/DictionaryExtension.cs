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
using NStl.Iterators;
using NStl.Iterators.Private;
using NStl.Iterators.Support;


namespace NStl.Linq
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods 
    /// for querying objects that implement <see cref="IDictionary{Key, Value}"/>.
    /// </summary>
    public static class DictionaryExtension
    {
        /// <summary>
        /// Converts the elements of an <see cref="IDictionary"/> to the specified type.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method returns an adapter object, so calling this methond is very efficient.
        /// </remarks>
        public static IDictionary<Key, Value> Cast<Key, Value>(this IDictionary dictionary)
        {
            return new IDictionary2IDictionaryTAdapter<Key, Value>(dictionary);
        }
        /// <summary>
        /// returns an <see cref="IOutputIterator{DictionaryEntry}"/> implementation.
        /// </summary>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static IOutputIterator<DictionaryEntry> AddInserter(this IDictionary dictionary)
        {
            return new DictionaryBackInsertIterator(dictionary);
        }
        /// <summary>
        /// Returns an <see cref="DictionaryIterator{K,V}"/> pointing to 
        /// the first element of the range.
        /// </summary>
        /// <param name="e">The range.</param>
        /// <returns></returns>
        public static DictionaryIterator<K, V> Begin<K, V>(this IDictionary<K, V> e)
        {
            return new DictionaryIterator<K, V>(e);
        }
        /// <summary>
        /// Returns an <see cref="DictionaryIterator{K, V}"/> pointing 
        /// one past the final element of the range.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static DictionaryIterator<K, V> End<K, V>(this IDictionary<K, V> e)
        {
            return new DictionaryIterator<K, V>(e, true);
        }
    }
}

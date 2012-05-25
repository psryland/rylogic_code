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

namespace NStl.SyntaxHelper
{
    /// <summary>
    /// Syntax helper that offers functors to select values of
    /// pair like objects such as <see cref="KeyValuePair{TKey,TValue}"/>
    /// or <see cref="DictionaryEntry"/>.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords")]
    public static class Select
    {
        /// <summary>
        /// Returns a functor that will extract the first element for a KeyValuePair object.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<KeyValuePair<Key, Value>, Key>
            FirstFromKeyValuePair<Key, Value>()
        {
            return new Select1st<Key, Value>();
        }
        /// <summary>
        /// Returns a funcor that will extract the first value of a <see cref="DictionaryEntry"/>.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<DictionaryEntry, Key>
            FirstFromDictionaryEntry<Key>()
        {
            return new Select1stDictionaryEntry<Key>();
        }
        /// <summary>
        /// Returns a functor that will extract the second element for a KeyValuePair object.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<KeyValuePair<Key, Value>, Value>
            SecondFromKeyValuePair<Key, Value>()
        {
            return new Select2nd<Key, Value>();
        }
        /// <summary>
        /// Returns a funcor that will extract the first value of a <see cref="DictionaryEntry"/>.
        /// </summary>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<DictionaryEntry, Value>
            SecondFromDictionaryEntry<Value>()
        {
            return new Select2ndDictionaryEntry<Value>();
        }
    }
}

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
using System;

namespace NStl.SyntaxHelper
{
    /// <summary>
    /// A syntax helper that offers all projection functors of the NSTL.
    /// </summary>
    public static class Project
    {
        /// <summary>
        /// Returns a functor that will clone the first element for a KeyValuePair object.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<KeyValuePair<Key, Value>, Key>
            FirstOfKeyValuePair<Key, Value>() where Key : ICloneable
        {
            return new Project1st<Key, Value>();
        }
        /// <summary>
        /// Returns a funcor that will clone the first value of a <see cref="DictionaryEntry"/>.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<DictionaryEntry, Key>
            FirstOfDictionaryEntry<Key>() where Key : ICloneable
        {
            return new Project1stDictionaryEntry<Key>();
        }
        /// <summary>
        /// Returns a functor that will clone the second element for a KeyValuePair object.
        /// </summary>
        /// <typeparam name="Key"></typeparam>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<KeyValuePair<Key, Value>, Value>
            SecondOfKeyValuePair<Key, Value>() where Value : ICloneable
        {
            return new Project2nd<Key, Value>();
        }
        /// <summary>
        /// Returns a funcor that willclone the second element of a <see cref="DictionaryEntry"/>.
        /// </summary>
        /// <typeparam name="Value"></typeparam>
        /// <returns></returns>
        public static IUnaryFunction<DictionaryEntry, Value>
            SecondOfDictionaryEntry<Value>() where Value : ICloneable
        {
            return new Project2ndDictionaryEntry<Value>();
        }
        /// <summary>
        /// Returns a unary function object that will simply return its argument.
        /// </summary>
        /// <returns></returns>
        /// <remarks>The identity functor is typically used as placeholder.</remarks>
        public static IUnaryFunction<T, T>
            Identity<T>()
        {
            return new Identity<T>();// faster, no boxing!
        }
    }
}

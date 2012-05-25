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

using NStl.Iterators;
using NStl.Iterators.Private;
using System.Collections.Generic;
using System;

namespace NStl
{
    public static partial class NStlUtil
    {
        /// <summary>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation
        /// that does front insertions on an <see cref="IFrontInsertable{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="frontInsertable"></param>
        /// <returns></returns>
        [Obsolete("Use IFrontInsertable<T>.AddFirstInsert extension method instead")]
        public static IOutputIterator<T>
            FrontInserter<T>(IFrontInsertable<T> frontInsertable)
        {
            return new FrontInsertIterator<T>(frontInsertable);
        }
        /// <summary>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation
        /// that does front insertions on a <see cref="LinkedList{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="frontInsertable"></param>
        /// <returns></returns>
        [Obsolete("Use LinkedList<T>.AddFirstInsert extension method instead")]
        public static IOutputIterator<T>
            FrontInserter<T>(LinkedList<T> frontInsertable)
        {
            return new FrontInsertIterator<T>(frontInsertable);
        }
    }
}

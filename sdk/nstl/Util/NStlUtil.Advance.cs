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


using System.Diagnostics;
using NStl.Iterators;
using System;

namespace NStl
{
    public static partial class NStlUtil
    {
        /// <summary>
        /// returns tan iterator that is advanced by the given value. it might modify the
        /// passed in iterator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="It"></typeparam>
        /// <param name="where"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        [Obsolete("Use Iterator<T>.Add() extension method instead!")]
        public static It
            Advance<T, It>(It where, int distance) where It : IInputIterator<T>
        {
            if (where is IRandomAccessIterator<T>)
                return (It)Advance((IRandomAccessIterator<T>)where, distance);
            if (where is IBidirectionalIterator<T>)
                return (It)Advance((IBidirectionalIterator<T>)where, distance);
            return (It)Advance(where, distance);
        }
        private static IBidirectionalIterator<T>
            Advance<T>(IBidirectionalIterator<T> where, int distance)
        {
            if (where is IRandomAccessIterator<T>)
                return Advance(where, distance);
            for (; 0 < distance; --distance)
                where.PreIncrement();
            for (; distance < 0; ++distance)
                where.PreDecrement();
            return where;
        }
        private static IInputIterator<T>
            Advance<T>(IInputIterator<T> where, int distance)
        {
            Debug.Assert(!(where is IRandomAccessIterator<T>));

            for (; 0 < distance; --distance)
                where.PreIncrement();
            return where;
        }
        private static IRandomAccessIterator<T>
            Advance<T>(IRandomAccessIterator<T> where, int distance)
        {
            return where.Add(distance);
        }
    }
}

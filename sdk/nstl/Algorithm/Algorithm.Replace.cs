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
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// ReplaceIf replaces every element in the range [first, last)
        /// that fulfills the predicate with newValue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the start of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the end of the range.
        /// </param>
        /// <param name="pred">
        /// The predicate functor that is used to determine if one element is replaced.
        /// </param>
        /// <param name="newValue">
        /// The replacing value.
        /// </param>
        public static void
            ReplaceIf<T>(IForwardIterator<T> first, IForwardIterator<T> last,
                          IUnaryFunction<T, bool> pred, T newValue)
        {
            first = (IForwardIterator<T>)first.Clone();
            for (; !Equals(first, last); first.PreIncrement())
                if (pred.Execute(first.Value))
                    first.Value = newValue;
        }
        /// <summary>
        /// Replace replaces every element in the range [first, last)
        /// that equals oldValue with newValue.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the start of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the end of the range.
        /// </param>
        /// <param name="newValue">
        /// The replacing value.
        /// </param>
        /// <param name="oldValue">
        /// The value to be replaced.
        /// </param>
        public static void
            Replace<T>(IForwardIterator<T> first, IForwardIterator<T> last,
                          T oldValue, T newValue)
        {
            ReplaceIf(first, last, Bind.Second(Compare.EqualTo<T>(), oldValue), newValue);
        }
    }
}

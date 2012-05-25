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
        /// ReplaceCopyIf copies elements from the range [first, last) 
        /// to the range [result, result + (last-first)), 
        /// except that any element for which predicate is true is not copied; 
        /// newValue is copied instead. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the start of the input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the end of the input range.
        /// </param>
        /// <param name="result">An <see cref="IOutputIterator{T}"/> 
        /// implementation pointing at the first element of the target range.
        /// </param>
        /// <param name="predicate">
        /// The predicate functor that is used to determine if an element is replaced during the copy.
        /// </param>
        /// <param name="newValue">The new value.</param>
        /// <returns>
        /// An <see cref="IOutputIterator{T}"/> implementation pointing to the end of the target range.
        /// </returns>
        public static FwdIt
            ReplaceCopyIf<T, FwdIt>(IInputIterator<T> first, IInputIterator<T> last, FwdIt result,
                               IUnaryFunction<T, bool> predicate, T newValue) where FwdIt : IOutputIterator<T>
        {
            first = (IInputIterator<T>)first.Clone();
            result = (FwdIt)result.Clone();
            for (; !Equals(first, last); first.PreIncrement(), result.PreIncrement())
                result.Value = predicate.Execute(first.Value)
                    ? (newValue) : first.Value;
            return result;
        }
        /// <summary>
        /// ReplaceCopy copies elements from the range [first, last) 
        /// to the range [result, result + (last-first)), 
        /// except that any element that equals oldValue is not copied; 
        /// newValue is copied instead. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the start of the input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation
        /// pointing to the end of the input range.
        /// </param>
        /// <param name="result">An <see cref="IOutputIterator{T}"/> 
        /// implementation pointing at the first element of the target range.
        /// </param>
        /// <param name="oldValue">The value to be replaced.</param>
        /// <param name="newValue">The replaced value.</param>
        /// <returns>
        /// An <see cref="IOutputIterator{T}"/> implementation pointing to the end of the target range.
        /// </returns>
        public static FwdIt
            ReplaceCopy<T, FwdIt>(IInputIterator<T> first, IInputIterator<T> last, FwdIt result,
                            T oldValue, T newValue) where FwdIt : IOutputIterator<T>
        {
            return ReplaceCopyIf(first, last, result, Bind.Second(Compare.EqualTo<T>(), oldValue), newValue);
        }
    }
}

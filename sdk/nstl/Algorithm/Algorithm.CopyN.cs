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
using System.Collections.Generic;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// CopyN copies elements from the range [first, first + n) to the range [result, result + n).
        /// </para>
        /// <para>
        /// he complexity is linear. Exactly n assignments are performed.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="n">
        /// The number of elements beginning at first that will be copied to the
        /// destination range.
        /// </param>
        /// <param name="result">
        /// An <see cref="IOutputIterator{T}"/> pointing to the first element of the destination range.
        /// </param>
        /// <returns></returns>
        /// <remarks>
        /// As the type inference will fail on this algorithm, it is recommended to use 
        /// <see cref="Algorithm.Copy{T, OutputIt}(IInputIterator{T},IInputIterator{T}, OutputIt)"/> instead.
        /// </remarks>
        public static KeyValuePair<InIt, OutIt>
            CopyN<T, InIt, OutIt>(InIt first, int n, OutIt result)
            where OutIt: IOutputIterator<T>
            where InIt: IInputIterator<T>
        {
            first = (InIt)first.Clone();
            result = (OutIt)result.Clone();
            for (; n > 0; --n)
            {
                result.Value = first.Value;
                first.PreIncrement();
                result.PreIncrement();
            }
            return new KeyValuePair<InIt, OutIt>(first, result);
        }
    }
}

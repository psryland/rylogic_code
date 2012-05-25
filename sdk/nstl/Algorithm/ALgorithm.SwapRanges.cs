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

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// SwapRanges swaps each of the elements in the range [first1, last1) 
        /// with the corresponding element in the range [first2, first2 + (last1 - first1)). 
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first1">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing
        /// to the first element of the first range to be swaped.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing
        /// one past the final element of the first range to be swaped.</param>
        /// <param name="first2">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing
        /// to the first element of the second range to be swaped.
        /// </param>
        /// <returns>
        /// Returns an iterator pointing one past the final swaped element in the
        /// second range.
        /// </returns>
        public static FwdIt
            SwapRanges<T, FwdIt>(IForwardIterator<T> first1, IForwardIterator<T> last1, FwdIt first2)
            where FwdIt : IForwardIterator<T>
        {
            first1 = (IForwardIterator<T>)first1.Clone();
            first2 = (FwdIt)first2.Clone();
            for (; !Equals(first1, last1); first1.PreIncrement(), first2.PreIncrement())
                IterSwap(first1, first2);
            return first2;
        }
    }
}

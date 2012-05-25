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
        /// <para>
        /// SearchN searches for a subsequence of count consecutive elements in the 
        /// range [first, last), all of which are equal to value. 
        /// </para>
        /// <para>
        /// The complexity is inear. SearchN performs at most last - first comparisons. 
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref=" IForwardIterator{T}"/> implementation pointing 
        /// to the first element of the range to be searched.
        /// </param>
        /// <param name="last">
        /// An <see cref=" IForwardIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be searched.</param>
        /// <param name="count">The length of the sub range.</param>
        /// <param name="val">The value contained in the sub range.</param>
        /// <param name="equal">A functor that is used to determine if to values are equal.</param>
        /// <returns>
        /// It returns an iterator pointing to the beginning of that subsequence, 
        /// or else last if no such subsequence exists.
        /// </returns>
        /// <remarks>
        /// Note that count is permitted to be zero: a subsequence of zero elements 
        /// is well defined. If you call SearchN with count equal to zero, then 
        /// the search will always succeed: no matter what value is, every range contains 
        /// a subrange of zero consecutive elements that are equal to value. When SearchN
        /// is called with count equal to zero, the return value is always first. 
        /// </remarks>
        public static FwdIt
            SearchN<T, FwdIt>(FwdIt first, FwdIt last,
                        int count, T val, IBinaryFunction<T, T, bool> equal) where FwdIt : IForwardIterator<T>
        {
            first = (FwdIt)first.Clone();
            if (count <= 0)
                return first;
            else
            {
                while (!Equals(first, last))
                {
                    if (equal.Execute(first.Value, val))
                        break;
                    first.PreIncrement();
                }
                while (!Equals(first, last))
                {
                    int n = count - 1;
                    FwdIt i = (FwdIt)first.Clone();
                    i.PreIncrement();
                    while (!Equals(i, last) && n != 0 && equal.Execute(i.Value, val))
                    {
                        i.PreIncrement();
                        --n;
                    }
                    if (n == 0)
                        return first;
                    else
                    {
                        while (!Equals(i, last))
                        {
                            if (equal.Execute(i.Value, val))
                                break;
                            i.PreIncrement();
                        }
                        first = (FwdIt)i.Clone();
                    }
                }
                return last;
            }
        }
        /// <summary>
        /// <para>
        /// SearchN searches for a subsequence of count consecutive elements in the 
        /// range [first, last), all of which are equal to value. 
        /// </para>
        /// <para>
        /// The complexity is inear. SearchN performs at most last - first comparisons. 
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref=" IForwardIterator{T}"/> implementation pointing 
        /// to the first element of the range to be searched.
        /// </param>
        /// <param name="last">
        /// An <see cref=" IForwardIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be searched.</param>
        /// <param name="count">The length of the sub range.</param>
        /// <param name="val">The value contained in the sub range.</param>
        /// <returns>
        /// It returns an iterator pointing to the beginning of that subsequence, 
        /// or else last if no such subsequence exists.
        /// </returns>
        /// <remarks>
        /// Note that count is permitted to be zero: a subsequence of zero elements 
        /// is well defined. If you call SearchN with count equal to zero, then 
        /// the search will always succeed: no matter what value is, every range contains 
        /// a subrange of zero consecutive elements that are equal to value. When SearchN
        /// is called with count equal to zero, the return value is always first. 
        /// </remarks>
        public static FwdIt
            SearchN<T, FwdIt>(FwdIt first, FwdIt last,
                               int count, T val) where FwdIt : IForwardIterator<T>
        {
            return SearchN(first, last, count, val, Compare.EqualTo<T>());
        }
    }
}

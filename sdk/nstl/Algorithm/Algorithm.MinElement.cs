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

using System;
using NStl.Iterators;
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// MinElement finds the smallest element in the range [first, last). 
        /// It returns the first iterator i in [first, last) such that no other 
        /// iterator in [first, last) points to a value smaller than i.Value. The 
        /// return value is last if and only if [first, last) is an empty range.
        /// </para>
        /// <para>
        /// The complexity is linear. Zero comparisons if [first, last) is an 
        /// empty range, otherwise exactly (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> pointing one past the last element of the range.
        /// </param>
        /// <param name="less">The function object that determines if one element is less than another.</param>
        /// <returns>
        /// An iterator pointing to the minimum element of the range.
        /// </returns>
        public static FwdIt
            MinElement<T, FwdIt>(FwdIt first, FwdIt last, IBinaryFunction<T, T, bool> less)
            where FwdIt : IForwardIterator<T>
        {
            first = (FwdIt)first.Clone();
            if (Equals(first, last))
                return first;
            FwdIt result = (FwdIt)first.Clone();
            while (!Equals(first.PreIncrement(), last))
                if (less.Execute(first.Value, result.Value))
                    result = (FwdIt)first.Clone();
            return result;
        }
        /// <summary>
        /// <para>
        /// MinElement finds the smallest element in the range [first, last). 
        /// It returns the first iterator i in [first, last) such that no other 
        /// iterator in [first, last) points to a value smaller than i.Value. The 
        /// return value is last if and only if [first, last) is an empty range.
        /// </para>
        /// <para>
        /// The two versions of MinElement differ in how they define whether 
        /// one element is less than another. The first version compares objects 
        /// using <see cref="IComparable{T}"/>, and the second compares objects 
        /// using a function object less.
        /// </para>
        /// <para>
        /// The complexity is linear. Zero comparisons if [first, last) is an 
        /// empty range, otherwise exactly (last - first) - 1 comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> pointing one past the last element of the range.
        /// </param>
        /// <returns>
        /// An iterator pointing to the minimum element of the range.
        /// </returns>
        public static FwdIt
            MinElement<T, FwdIt>(FwdIt first, FwdIt last)
            where T : IComparable<T>
            where FwdIt : IForwardIterator<T>
        { 
            return MinElement(first, last, Compare.Less<T>());
        }
    }
}

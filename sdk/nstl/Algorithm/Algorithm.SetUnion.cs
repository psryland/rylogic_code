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
        /// SetUnion constructs a sorted range that is the union of the sorted ranges 
        /// [first1, last1) and [first2, last2). The return value is the end of the output 
        /// range.
        /// </para>
        /// <para>
        /// In the simplest case, SetUnion performs the "union" operation from set theory:
        /// the output range contains a copy of every element that is contained in 
        /// [first1, last1), [first2, last2), or both. The general case is more complicated,
        /// because the input ranges may contain duplicate elements. The generalization 
        /// is that if a value appears m times in [first1, last1) and n times in 
        /// [first2, last2) (where m or n may be zero), then it appears max(m,n) times 
        /// in the output range. [1] SetUnion is stable, meaning both that the relative 
        /// order of elements within each input range is preserved, and that if an element 
        /// is present in both input ranges it is copied from the first range rather than 
        /// the second.
        /// </para>
        /// <para>
        /// The two versions of SetUnion differ in how they define whether 
        /// one element is less than another. The first version compares objects using
        /// <see cref="Compare.Less"/>, and the second compares objects using a function object comp.
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first1">
        /// An input iterator pointing to the first element of the first input range.
        /// </param>
        /// <param name="last1">
        /// An input iterator pointing one past the final element of the first input range.
        /// </param>
        /// <param name="first2">
        /// An input iterator pointing to the first element of the first second range.
        /// </param>
        /// <param name="last2">
        /// An input iterator pointing one past the final element of the first second range.
        /// </param>
        /// <param name="dest">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <param name="less">
        /// The predicate defining a strict weak ordering of the elements.
        /// </param>
        /// <returns>
        /// An iterator pointing one past the final element of the output range.
        /// </returns>
        public static OutIt
            SetUnion<T, OutIt>(IInputIterator<T> first1, IInputIterator<T> last1,
                               IInputIterator<T> first2, IInputIterator<T> last2,
                               OutIt dest, IBinaryFunction<T, T, bool> less) 
            where OutIt : IOutputIterator<T>
        {
            first1 = (IInputIterator<T>)first1.Clone();
            first2 = (IInputIterator<T>)first2.Clone();
            dest   = (OutIt)dest.Clone();
            while (!Equals(first1, last1) && !Equals(first2, last2))
            {
                if (less.Execute(first1.Value, first2.Value))
                {
                    dest.Value = first1.Value;
                    first1.PreIncrement();
                }
                else if (less.Execute(first2.Value, first1.Value))
                {
                    dest.Value = first2.Value;
                    first2.PreIncrement();
                }
                else
                {
                    dest.Value = first1.Value;
                    first1.PreIncrement();
                    first2.PreIncrement();
                }
                dest.PreIncrement();
            }
            return Copy(first2, last2, Copy(first1, last1, dest));
        }
        /// <summary>
        /// <para>
        /// SetUnion constructs a sorted range that is the union of the sorted ranges 
        /// [first1, last1) and [first2, last2). The return value is the end of the output 
        /// range.
        /// </para>
        /// <para>
        /// In the simplest case, SetUnion performs the "union" operation from set theory:
        /// the output range contains a copy of every element that is contained in 
        /// [first1, last1), [first2, last2), or both. The general case is more complicated,
        /// because the input ranges may contain duplicate elements. The generalization 
        /// is that if a value appears m times in [first1, last1) and n times in 
        /// [first2, last2) (where m or n may be zero), then it appears max(m,n) times 
        /// in the output range. [1] SetUnion is stable, meaning both that the relative 
        /// order of elements within each input range is preserved, and that if an element 
        /// is present in both input ranges it is copied from the first range rather than 
        /// the second.
        /// </para>
        /// <para>
        /// The two versions of SetUnion differ in how they define whether 
        /// one element is less than another. The first version compares objects using
        /// <see cref="Compare.Less"/>, and the second compares objects using a function object comp.
        /// </para>
        /// <para>
        /// The complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first1">
        /// An input iterator pointing to the first element of the first input range.
        /// </param>
        /// <param name="last1">
        /// An input iterator pointing one past the final element of the first input range.
        /// </param>
        /// <param name="first2">
        /// An input iterator pointing to the first element of the first second range.
        /// </param>
        /// <param name="last2">
        /// An input iterator pointing one past the final element of the first second range.
        /// </param>
        /// <param name="dest">
        /// An output iterator pointing to the first element of the destination range.
        /// </param>
        /// <returns>
        /// An iterator pointing one past the final element of the output range.
        /// </returns>
        public static OutIt
            SetUnion<T, OutIt>(IInputIterator<T> first1, IInputIterator<T> last1,
                               IInputIterator<T> first2, IInputIterator<T> last2,
                               OutIt dest)
            where T : IComparable<T>
            where OutIt : IOutputIterator<T>
        {
            return SetUnion(first1, last1, first2, last2, dest, Compare.Less<T>());
        }
    }
}

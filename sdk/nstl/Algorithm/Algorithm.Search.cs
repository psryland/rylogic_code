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
        /// Search finds a subsequence within the range [first1, last1) that 
        /// is identical to [first2, last2) when compared element-by-element. 
        /// It returns an iterator pointing to the beginning of that subsequence, 
        /// or else last1 if no such subsequence exists.
        /// </para>
        /// <para>
        /// The worst case behavior is quadratic: at most (last1 - first1) * (last2 - first2) 
        /// comparisons. This worst case, however, is rare. Average complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> implementation that points to 
        /// the first element of the range to be searched.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> implementation that points one past the final
        /// element of the range to be searched.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> implementation that points to 
        /// the first element of the sub range to be searched for.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> implementation that points one past the final
        /// element of the sub range to be searched for.
        /// </param>
        /// <param name="equal">A functor that is used to determine if two elements are equal.</param>
        /// <returns>
        /// An iterator pointing to the first element in [first1, last1)
        /// where a subsequence starts that is equal to [first2, last2). It returns last1 
        /// if no match is found or first1 if one or both ranges are empty.
        /// </returns>
        public static FwdIt
            Search<T, FwdIt>(FwdIt first1, FwdIt last1,
                    IInputIterator<T> first2, IInputIterator<T> last2, IBinaryFunction<T, T, bool> equal)
            where FwdIt : IInputIterator<T>
        {
            // is the range empty?
            if (Equals(first1, last1) || Equals(first2, last2))
                return (FwdIt)first1.Clone();//copy to stay const

            first1 = (FwdIt)first1.Clone();
            first2 = (IInputIterator<T>)first2.Clone();

            // Test for a pattern of length 1.
            IInputIterator<T> tmp = (IInputIterator<T>)first2.Clone();
            tmp.PreIncrement();
            if (Equals(tmp, last2))
            {
                while (!Equals(first1, last1) && !equal.Execute(first1.Value, first2.Value))
                    first1.PreIncrement();
                return first1;
            }

            // General case.

            IInputIterator<T> p1 = (IInputIterator<T>)first2.Clone();
            p1.PreIncrement();

            while (!Equals(first1, last1))
            {
                while (!Equals(first1, last1))
                {
                    if (equal.Execute(first1.Value, first2.Value))
                        break;
                    first1.PreIncrement();
                }
                while (!Equals(first1, last1) && !equal.Execute(first1.Value, first2.Value))
                    first1.PreIncrement();
                if (Equals(first1, last1))
                    return last1;

                IInputIterator<T> p = (IInputIterator<T>)p1.Clone();
                IInputIterator<T> current = (IInputIterator<T>)first1.Clone();
                if (Equals(current.PreIncrement(), last1))
                    return last1;

                while (equal.Execute(current.Value, p.Value))
                {
                    if (Equals(p.PreIncrement(), last2))
                        return first1;
                    if (Equals(current.PreIncrement(), last1))
                        return last1;
                }

                first1.PreIncrement();
            }
            return first1;
        }
        /// <summary>
        /// <para>
        /// Search finds a subsequence within the range [first1, last1) that 
        /// is identical to [first2, last2) when compared element-by-element. 
        /// It returns an iterator pointing to the beginning of that subsequence, 
        /// or else last1 if no such subsequence exists.
        /// </para>
        /// <para>
        /// The worst case behavior is quadratic: at most (last1 - first1) * (last2 - first2) 
        /// comparisons. This worst case, however, is rare. Average complexity is linear.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> implementation that points to 
        /// the first element of the range to be searched.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> implementation that points one past the final
        /// element of the range to be searched.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> implementation that points to 
        /// the first element of the sub range to be searched for.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> implementation that points one past the final
        /// element of the sub range to be searched for.
        /// </param>
        /// <returns>
        /// An iterator pointing to the first element in [first1, last1)
        /// where a subsequence starts that is equal to [first2, last2). It returns last1 
        /// if no match is found or first1 if one or both ranges are empty.
        /// </returns>
        public static FwdIt
            Search<T, FwdIt>(FwdIt first1, FwdIt last1,
                    IInputIterator<T> first2, IInputIterator<T> last2) where FwdIt : IInputIterator<T>
        {
            return Search(first1, last1, first2, last2, Compare.EqualTo<T>());
        }
    }
}

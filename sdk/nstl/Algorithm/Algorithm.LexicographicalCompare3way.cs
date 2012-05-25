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
using System;
using NStl.SyntaxHelper;


namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// LexicographicalCompare3Way is essentially a generalization of the function strcmp 
        /// from the standard C library: it returns a negative number if the range [first1, last1) 
        /// is lexicographically less than the range [first2, last2), a positive number if 
        /// [first2, last2) is lexicographically less than [first1, last1), and zero if neither 
        /// range is lexicographically less than the other.
        /// </para>
        /// <para>
        /// As with <see cref="Algorithm.LexicographicalCompare{T}(IInputIterator{T}, IInputIterator{T}, IInputIterator{T}, IInputIterator{T}, IBinaryFunction{T,T,bool})"/>, 
        /// lexicographical comparison means "dictionary" (element-by-element) ordering. That is, 
        /// LexicographicalCompare3Way returns a negative number if first1.value is less than first2.Value, 
        /// and a positive number if first1.Value is greater than first2.Value. If the two first 
        /// elements are equivalent then LexicographicalCompare3Way compares the two second elements, 
        /// and so on. LexicographicalCompare3Way returns 0 only if the two ranges [first1, last1) 
        /// and [first2, last2) have the same length and if every element in the first range is 
        /// equivalent to its corresponding element in the second.
        /// </para>
        /// <para>
        /// The complexity is linear. At most 2 * min(last1 - first1, last2 - first2) comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the first input range.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the first input range.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the second input range.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the second input range.
        /// </param>
        /// <param name="less">The functor that is used to decide if one elemt is less than the other.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item>-1 if the first range is lexicographical less than the second range.</item>
        /// <item>0 if the first range is lexicographical equal to the second range.</item>
        /// <item>1 if the first range is lexicographical greater than the second range.</item>
        /// </list>
        /// </returns>
        public static int 
            LexicographicalCompare3Way<T>( IInputIterator<T> first1, IInputIterator<T> last1,
                                           IInputIterator<T> first2, IInputIterator<T> last2, IBinaryFunction<T, T, bool> less)
        {
            first1 = (IInputIterator<T>) first1.Clone();
            first2 = (IInputIterator<T>) first2.Clone();
            while (!Equals(first1, last1) && !Equals(first2, last2))
            {
                if (less.Execute(first1.Value, first2.Value))
                    return -1;
                if (less.Execute(first2.Value, first1.Value))
                    return 1;
                first1.PreIncrement();
                first2.PreIncrement();
            }
            if (Equals(first2, last2))
            {
                return (Equals(first1, last1)) ? 0 : 1;
            }
            else
            {
                return -1;
            }
        }
        /// <summary>
        /// <para>
        /// LexicographicalCompare3Way is essentially a generalization of the function strcmp 
        /// from the standard C library: it returns a negative number if the range [first1, last1) 
        /// is lexicographically less than the range [first2, last2), a positive number if 
        /// [first2, last2) is lexicographically less than [first1, last1), and zero if neither 
        /// range is lexicographically less than the other.
        /// </para>
        /// <para>
        /// As with <see cref="Algorithm.LexicographicalCompare{T}(IInputIterator{T}, IInputIterator{T}, IInputIterator{T}, IInputIterator{T}, IBinaryFunction{T,T,bool})"/>, 
        /// lexicographical comparison means "dictionary" (element-by-element) ordering. That is, 
        /// LexicographicalCompare3Way returns a negative number if first1.value is less than first2.Value, 
        /// and a positive number if first1.Value is greater than first2.Value. If the two first 
        /// elements are equivalent then LexicographicalCompare3Way compares the two second elements, 
        /// and so on. LexicographicalCompare3Way returns 0 only if the two ranges [first1, last1) 
        /// and [first2, last2) have the same length and if every element in the first range is 
        /// equivalent to its corresponding element in the second.
        /// </para>
        /// <para>
        /// The complexity is linear. At most 2 * min(last1 - first1, last2 - first2) comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the first input range.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the first input range.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the second input range.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the second input range.
        /// </param>
        /// <returns>
        /// <list type="bullet">
        /// <item>-1 if the first range is lexicographical less than the second range.</item>
        /// <item>0 if the first range is lexicographical equal to the second range.</item>
        /// <item>1 if the first range is lexicographical greater than the second range.</item>
        /// </list>
        /// </returns>
        public static int
            LexicographicalCompare3Way<T>(IInputIterator<T> first1, IInputIterator<T> last1,
                                           IInputIterator<T> first2, IInputIterator<T> last2)
            where T: IComparable<T>
        {
            return LexicographicalCompare3Way(first1, last1, first2, last2, Compare.Less<T>());
        }
    }
}

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
        /// FindFirstOf is similar to Find, in that it performs linear search 
        /// through a range of <see cref="IInputIterator{T}"/>. The difference 
        /// is that while Find searches for one particular value, FindFirstOf 
        /// searches for any of several values. Specifically, FindFirstOf 
        /// searches for the first occurrance in the range [first1, last1) 
        /// of any of the elements in [first2, last2). (Note that this behavior 
        /// is reminiscent of the function strpbrk from the standard C library.)
        /// </para>
        /// <para>
        /// The complexity is linear. At most (last1 - first1) * (last2 - first2) 
        /// comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element 
        /// of the range to be searched.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element 
        /// of the range to be searched.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element 
        /// of the range to be searched for.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element 
        /// of the range to be searched for.
        /// </param>
        /// <param name="equal">
        /// A functor that is used to check two elements for equality.
        /// </param>
        /// <returns>
        /// An iterator pointing to the first occurence of one of the elements
        /// of the second range; otherwise the end iterator of the first range.
        /// </returns>
        public static InIt
            FindFirstOf<T, InIt>(InIt first1, InIt last1,
                                   IInputIterator<T> first2, IInputIterator<T> last2, IBinaryFunction<T, T, bool> equal)
            where InIt : IInputIterator<T>
        {
            first1 = (InIt)first1.Clone();
            first2 = (IInputIterator<T>)first2.Clone();
            for (; !Equals(first1, last1); first1.PreIncrement())
                for (IInputIterator<T> iter = (IInputIterator<T>)first2.Clone(); !Equals(iter, last2); iter.PreIncrement())
                    if (equal.Execute(first1.Value, iter.Value))
                        return first1;
            return last1;
        }
        /// <summary>
        /// <para>
        /// FindFirstOf is similar to Find, in that it performs linear search 
        /// through a range of <see cref="IInputIterator{T}"/>. The difference 
        /// is that while Find searches for one particular value, FindFirstOf 
        /// searches for any of several values. Specifically, FindFirstOf 
        /// searches for the first occurrance in the range [first1, last1) 
        /// of any of the elements in [first2, last2). (Note that this behavior 
        /// is reminiscent of the function strpbrk from the standard C library.)
        /// </para>
        /// <para>
        /// The complexity is linear. At most (last1 - first1) * (last2 - first2) 
        /// comparisons.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element 
        /// of the range to be searched.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element 
        /// of the range to be searched.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element 
        /// of the range to be searched for.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element 
        /// of the range to be searched for.
        /// </param>
        /// <returns>
        /// An iterator pointing to the first occurence of one of the elements
        /// of the second range; otherwise the end iterator of the first range.
        /// </returns>
        public static InIt
            FindFirstOf<T, InIt>(InIt first1, InIt last1,
                                   IInputIterator<T> first2, IInputIterator<T> last2)
            where InIt : IInputIterator<T>
        {
            return FindFirstOf(first1, last1, first2, last2, Compare.EqualTo<T>());
        }
    }
}

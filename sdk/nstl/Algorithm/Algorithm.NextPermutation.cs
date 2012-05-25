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
        /// NextPermutation transforms the range of elements [first, last) 
        /// into the lexicographically next greater permutation of the elements. 
        /// There is a finite number of distinct permutations 
        /// (at most N! [1], where N is last - first), so, if the permutations 
        /// are ordered by <see cref="LexicographicalCompare{T}(IInputIterator{T},IInputIterator{T},IInputIterator{T},IInputIterator{T},IBinaryFunction{T,T,bool})"/>
        /// there is an unambiguous definition of which permutation is lexicographically next. 
        /// If such a permutation exists, next_permutation transforms [first, last) 
        /// into that permutation and returns true. Otherwise it transforms [first, last) 
        /// into the lexicographically smallest permutation [2] and returns false.
        /// </para>
        /// The postcondition is that the new permutation of elements is 
        /// lexicographically greater than the old (as determined by lexicographical_compare) if and only if the return value is true.
        /// <para>
        /// </para>
        /// <para>
        /// The compexity is linear. At most (last - first) / 2 swaps.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IBidirectionalInputIterator{T}"/> implementation pointing
        /// to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IBidirectionalInputIterator{T}"/> implementation pointing
        /// one past the final element of the range.
        /// </param>
        /// <param name="less">
        /// A functor that is used to determine if one element is less than another.
        /// </param>
        /// <returns>
        /// True, if the range was transformed int the lexicographically 
        /// next greater permutation of the elements.
        /// </returns>
        public static bool
            NextPermutation<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> last, IBinaryFunction<T, T, bool> less)
        {
            if (Equals(first, last))
                return false;
            IBidirectionalIterator<T> i = (IBidirectionalIterator<T>)first.Clone();
            i.PreIncrement();
            if (Equals(i, last))
                return false;
            i = (IBidirectionalIterator<T>)last.Clone(); ;
            i.PreDecrement();

            for ( ; ; )
            {
                IBidirectionalIterator<T> it = (IBidirectionalIterator<T>)i.Clone();
                i.PreDecrement();
                if (less.Execute(i.Value, it.Value))
                {
                    IBidirectionalIterator<T> j = (IBidirectionalIterator<T>)last.Clone();
                    while (!less.Execute(i.Value, (j.PreDecrement()).Value))
                    { ;}
                    IterSwap(i, j);
                    Reverse(it, last);
                    return true;
                }
                if (Equals(i, first))
                {
                    Reverse(first, last);
                    return false;
                }
            }
        }
        /// <summary>
        /// <para>
        /// NextPermutation transforms the range of elements [first, last) 
        /// into the lexicographically next greater permutation of the elements. 
        /// There is a finite number of distinct permutations 
        /// (at most N! [1], where N is last - first), so, if the permutations 
        /// are ordered by <see cref="LexicographicalCompare{T}(IInputIterator{T},IInputIterator{T},IInputIterator{T},IInputIterator{T})"/>
        /// there is an unambiguous definition of which permutation is lexicographically next. 
        /// If such a permutation exists, next_permutation transforms [first, last) 
        /// into that permutation and returns true. Otherwise it transforms [first, last) 
        /// into the lexicographically smallest permutation [2] and returns false.
        /// </para>
        /// The postcondition is that the new permutation of elements is 
        /// lexicographically greater than the old (as determined by lexicographical_compare) if and only if the return value is true.
        /// <para>
        /// </para>
        /// <para>
        /// The compexity is linear. At most (last - first) / 2 swaps.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An <see cref="IBidirectionalInputIterator{T}"/> implementation pointing
        /// to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IBidirectionalInputIterator{T}"/> implementation pointing
        /// one past the final element of the range.
        /// </param>
        /// <returns>
        /// True, if the range was transformed int the lexicographically 
        /// next greater permutation of the elements.
        /// </returns>
        public static bool
            NextPermutation<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> last) where T : IComparable<T>
        {
            return NextPermutation(first, last, Compare.Less<T>());
        }
    }
}

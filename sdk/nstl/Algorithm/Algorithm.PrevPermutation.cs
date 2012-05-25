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
        /// PrevPermutation transforms the range of elements [first, last) into 
        /// the lexicographically next smaller permutation of the elements. 
        /// There is a finite number of distinct permutations 
        /// (at most N!, where N is last - first), so, if the permutations 
        /// are ordered by <see cref="LexicographicalCompare{T}(IInputIterator{T},IInputIterator{T},IInputIterator{T},IInputIterator{T},IBinaryFunction{T,T,bool})"/>, 
        /// there is an unambiguous definition of which permutation is 
        /// lexicographically previous. If such a permutation exists, 
        /// PrevPermutation transforms [first, last) into that permutation 
        /// and returns true. Otherwise it transforms [first, last) into the 
        /// lexicographically greatest permutation [2] and returns false.
        /// Note that the lexicographically greatest permutation is, by definition, 
        /// sorted in nonascending order.
        /// </para>
        /// <para>
        /// The complexity is linear. At most (last - first) / 2 swaps.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// A <see cref="IBidirectionalInputIterator{T}"/> implemenation pointing
        /// to the first element of the range.
        /// </param>
        /// <param name="last">
        /// A <see cref="IBidirectionalInputIterator{T}"/> implemenation pointing
        /// one past the final element of the range.
        /// </param>
        /// <param name="comp">
        /// A functor that is used to determine if one lement is less than another.
        /// </param>
        /// <returns>
        /// True, if the ange was transformed, false if this is the smallest permutation.
        /// </returns>
        public static bool
            PrevPermutation<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> last,
                                IBinaryFunction<T, T, bool> comp)
        {
            IBidirectionalIterator<T> next = (IBidirectionalIterator<T>)last.Clone();
            if (Equals(first, last) || Equals(first, next.PreDecrement()))
                return false;

            for (; ; )
            {
                IBidirectionalIterator<T> next1 = (IBidirectionalIterator<T>)next.Clone();
                if (!comp.Execute((next.PreDecrement()).Value, next1.Value))
                {
                    IBidirectionalIterator<T> mid = (IBidirectionalIterator<T>)last.Clone();
                    for (; comp.Execute(next.Value, (mid.PreDecrement()).Value); ) { }
                    IterSwap(next, mid);
                    Reverse(next1, last);
                    return true;
                }

                if (Equals(next, first))
                {
                    Reverse(first, last);
                    return (false);
                }
            }
        }
        /// <summary>
        /// <para>
        /// PrevPermutation transforms the range of elements [first, last) into 
        /// the lexicographically next smaller permutation of the elements. 
        /// There is a finite number of distinct permutations 
        /// (at most N!, where N is last - first), so, if the permutations 
        /// are ordered by <see cref="LexicographicalCompare{T}(IInputIterator{T},IInputIterator{T},IInputIterator{T},IInputIterator{T},IBinaryFunction{T,T,bool})"/>, 
        /// there is an unambiguous definition of which permutation is 
        /// lexicographically previous. If such a permutation exists, 
        /// PrevPermutation transforms [first, last) into that permutation 
        /// and returns true. Otherwise it transforms [first, last) into the 
        /// lexicographically greatest permutation [2] and returns false.
        /// Note that the lexicographically greatest permutation is, by definition, 
        /// sorted in nonascending order.
        /// </para>
        /// <para>
        /// The complexity is linear. At most (last - first) / 2 swaps.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// A <see cref="IBidirectionalInputIterator{T}"/> implemenation pointing
        /// to the first element of the range.
        /// </param>
        /// <param name="last">
        /// A <see cref="IBidirectionalInputIterator{T}"/> implemenation pointing
        /// one past the final element of the range.
        /// </param>
        /// <returns>
        /// True, if the ange was transformed, false if this is the smalles permutation.
        /// </returns>
        public static bool
            PrevPermutation<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> last) where T : IComparable<T>
        {
            return PrevPermutation(first, last, Compare.Less<T>());
        }
    }
}

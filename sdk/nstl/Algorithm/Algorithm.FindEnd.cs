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
        /// FindEnd is misnamed: it is much more similar to Search than to Find, 
        /// and a more accurate name would have been SearchEnd
        /// </para>
        /// <para>
        /// Like Search, FindEnd attempts to find a subsequence within the 
        /// range [first1, last1) that is identical to [first2, last2). 
        /// The difference is that while search finds the first such subsequence, 
        /// find_end finds the last such subsequence. FindEnd returns an iterator 
        /// pointing to the beginning of that subsequence; if no such subsequence 
        /// exists, it returns last1.
        /// </para>
        /// <para>
        /// The number of comparisons is proportional to (last1 - first1) * (last2 - first2). 
        /// </para>
        /// <para>
        /// The reason that this range is [first1, last1 - (last2 - first2)), 
        /// instead of simply [first1, last1), is that we are looking for 
        /// a subsequence that is equal to the complete sequence [first2, last2). 
        /// An iterator i can't be the beginning of such a subsequence unless last1 - i 
        /// is greater than or equal to last2 - first2. Note the implication of this: 
        /// you may call FindEnd with arguments such that last1 - first1 is less than last2 - first2,
        /// but such a search will always fail.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> implementation pointing to 
        /// the first element of the range to be searched.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be searched.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> implementation pointing to 
        /// the first element of the range to be found.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be found.
        /// </param>
        /// <param name="equal">
        /// The functor that is used to determine if two elements are equal.
        /// </param>
        /// <returns>An iterator pointing to the first element of the 
        /// detected sub range or last1
        /// </returns>
        public static FwdIt
            FindEnd<T, FwdIt>(FwdIt first1, FwdIt last1,
                      IInputIterator<T> first2, IInputIterator<T> last2, IBinaryFunction<T, T, bool> equal)
            where FwdIt : IInputIterator<T>
        {
            first1 = (FwdIt)first1.Clone();
            first2 = (IInputIterator<T>)first2.Clone();
            if (Equals(first2, last2))
                return last1;
            else
            {
                FwdIt result = (FwdIt)last1.Clone();
                for (; ; )
                {
                    FwdIt _New_result
                        = Search(first1, last1, first2, last2, equal);
                    if (Equals(_New_result, last1))
                        return result;
                    else
                    {
                        result = (FwdIt)_New_result.Clone();
                        first1 = (FwdIt)_New_result.Clone();
                        first1.PreIncrement();
                    }
                }
            }
        }

        /// <summary>
        /// <para>
        /// FindEnd is misnamed: it is much more similar to Search than to Find, 
        /// and a more accurate name would have been SearchEnd
        /// </para>
        /// <para>
        /// Like Search, FindEnd attempts to find a subsequence within the 
        /// range [first1, last1) that is identical to [first2, last2). 
        /// The difference is that while search finds the first such subsequence, 
        /// find_end finds the last such subsequence. FindEnd returns an iterator 
        /// pointing to the beginning of that subsequence; if no such subsequence 
        /// exists, it returns last1.
        /// </para>
        /// <para>
        /// The number of comparisons is proportional to (last1 - first1) * (last2 - first2). 
        /// </para>
        /// <para>
        /// The reason that this range is [first1, last1 - (last2 - first2)), 
        /// instead of simply [first1, last1), is that we are looking for 
        /// a subsequence that is equal to the complete sequence [first2, last2). 
        /// An iterator i can't be the beginning of such a subsequence unless last1 - i 
        /// is greater than or equal to last2 - first2. Note the implication of this: 
        /// you may call FindEnd with arguments such that last1 - first1 is less than last2 - first2,
        /// but such a search will always fail.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first1">
        /// An <see cref="IInputIterator{T}"/> implementation pointing to 
        /// the first element of the range to be searched.
        /// </param>
        /// <param name="last1">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be searched.
        /// </param>
        /// <param name="first2">
        /// An <see cref="IInputIterator{T}"/> implementation pointing to 
        /// the first element of the range to be found.
        /// </param>
        /// <param name="last2">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be found.
        /// </param>
        /// <returns>An iterator pointing to the first element of the 
        /// detected sub range or last1
        /// </returns>
        public static FwdIt
            FindEnd<T, FwdIt>(FwdIt first1, FwdIt last1,
                       IInputIterator<T> first2, IInputIterator<T> last2) where FwdIt : IInputIterator<T>
        {
            return FindEnd(first1, last1, first2, last2, Compare.EqualTo<T>());
        }
        //private static BidIt
        //    __find_end<T, BidIt>(BidIt first1, BidIt last1,
        //                  bidirectional_iterator<T> first2, bidirectional_iterator<T> last2, BinaryFuntion<T, T, bool> comparison)
        //    where BidIt : bidirectional_iterator<T>
        //{
        //    bidirectional_reverse_iterator<T> _Rlast1 = new bidirectional_reverse_iterator<T>(first1);
        //    bidirectional_reverse_iterator<T> _Rlast2 = new bidirectional_reverse_iterator<T>(first2);
        //    // ... rely on "insider" knowledge. If we push in a bidit iterator into an algorithm
        //    // ... that takes a fwd iterator, it will return a bidit iterator.
        //    bidirectional_reverse_iterator<T> _Rresult = new bidirectional_reverse_iterator<T>(
        //           (bidirectional_iterator<T>)__search(new bidirectional_reverse_iterator<T>(last1), _Rlast1, new bidirectional_reverse_iterator<T>(last2), _Rlast2, comparison));

        //    if (_Rresult == _Rlast1)
        //        return last1;
        //    else
        //    {
        //        BidIt result = _Rresult.base_it();
        //        result = __advance(result, util.distance(first2, last2) - 1);
        //        result = ((bidirectional_reverse_iterator<T>)result).base_it();
        //        result.pre_decrement();
        //        return result;
        //    }
        //}        
    }
}

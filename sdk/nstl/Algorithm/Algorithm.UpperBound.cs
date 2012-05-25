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
using NStl.Linq;
using NStl.SyntaxHelper;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// UpperBound is a version of binary search: it attempts to find the element value in 
        /// an ordered range [first, last). Specifically, it returns the last position where 
        /// value could be inserted without violating the ordering. The first version of UpperBound 
        /// uses <see cref="IComparable{T}"/> for comparison, and the second uses the function object less.
        /// </para>
        /// <para>
        /// The number of comparisons is logarithmic: at most log(last - first) + 1. If the iterator is an 
        /// <see cref="IRandomAccessIterator{T}"/> then the number of steps through the range is also 
        /// logarithmic; otherwise, the number of steps is proportional to last - first.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="InIt"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="val">
        /// The value to be searched for.
        /// </param>
        /// <param name="less">
        /// The order criterion of the range.
        /// </param>
        /// <returns>
        /// An iterator pointing to the found value or last.
        /// </returns>
        public static InIt
            UpperBound<T, InIt>(InIt first, InIt last, T val, IBinaryFunction<T, T, bool> less)
            where InIt : IInputIterator<T>
        {
            int __len = first.DistanceTo(last);

            while (__len > 0)
            {
                int __half = __len >> 1;
                InIt __middle = (InIt)first.Add(__half);
                if (less.Execute(val, __middle.Value))
                    __len = __half;
                else
                {
                    first = (InIt)__middle.Clone();
                    first.PreIncrement();
                    __len = __len - __half - 1;
                }
            }
            return first;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static FwdIt
            UpperBound<T, FwdIt>(FwdIt first, FwdIt last, T val)
            where T : IComparable<T>
            where FwdIt : IInputIterator<T>
        {
            return UpperBound(first, last, val, Compare.Less<T>());
        }
    }
}

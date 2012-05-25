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

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// RandomSampleN randomly copies a sample of the elements from the range [first, last) 
        /// into the range [out, out + n). Each element in the input range appears at most once 
        /// in the output range, and samples are chosen with uniform probability. 
        /// Elements in the output range appear in the same relative order as their relative order 
        /// within the input range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> that points to the first element of the sample input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> that points one past the final element of the sample input range.
        /// </param>
        /// <param name="target">
        /// An <see cref="IOutputIterator{T}"/> that points to the first element of target range.
        /// </param>
        /// <param name="n">The amout of elements to be copied.</param>
        /// <param name="randomNumber">A functor that generates random numbers.</param>
        /// <returns></returns>
        public static OutIt
            RandomSampleN<T, OutIt>(IInputIterator<T> first, IInputIterator<T> last, OutIt target, int n,
                                    IUnaryFunction<int, int> randomNumber)
            where OutIt : IOutputIterator<T>
        {
            first = (IInputIterator<T>) first.Clone();
            target = (OutIt) target.Clone();
            int remaining = first.DistanceTo(last);
            int m = Math.Min(n, remaining);

            while (m > 0){
                if (randomNumber.Execute(remaining) < m){
                    target.Value = first.Value;
                    target.PreIncrement();
                    --m;
                }
                --remaining;
                first.PreIncrement();
            }
            return target;
        }
        /// <summary>
        /// RandomSampleN randomly copies a sample of the elements from the range [first, last) 
        /// into the range [out, out + n). Each element in the input range appears at most once 
        /// in the output range, and samples are chosen with uniform probability. 
        /// Elements in the output range appear in the same relative order as their relative order 
        /// within the input range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="OutIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> that points to the first element of the sample input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> that points one past the final element of the sample input range.
        /// </param>
        /// <param name="target">
        /// An <see cref="IOutputIterator{T}"/> that points to the first element of target range.
        /// </param>
        /// <param name="n">The amout of elements to be copied.</param>
        /// <returns></returns>
        public static OutIt
            RandomSampleN<T, OutIt>(IInputIterator<T> first, IInputIterator<T> last, OutIt target, int n)
            where OutIt : IOutputIterator<T>
        {
            return RandomSampleN(first, last, target, n, Functional.PtrFun<int, int>(__random_number));
        }
    }
}

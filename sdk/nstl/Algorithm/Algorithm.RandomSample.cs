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

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// RandomSample randomly copies a sample of the elements from the range [first, last) into 
        /// the range [target, target + n). Each element in the input range appears at most once 
        /// in the output range, and samples are chosen with uniform probability. 
        /// Elements in the output range might appear in any order: relative order within the 
        /// input range is not guaranteed to be preserved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="RandIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> that points to the first element of the sample input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> that points one past the final element of the sample input range.
        /// </param>
        /// <param name="outFirst">
        /// An <see cref="IRandomAccessIterator{T}"/> that points to the first element of target range.
        /// </param>
        /// <param name="outLast">
        /// An <see cref="IRandomAccessIterator{T}"/> that points one past the final element of target range.
        /// </param>
        /// <param name="randomNumber">
        /// A <see cref="IUnaryFunction{Param,Result}"/> that generates random numbers.
        /// </param>
        /// <returns></returns>
        public static RandIt
            RandomSample<T, RandIt>(IInputIterator<T> first, IInputIterator<T> last,
                                    RandIt outFirst, RandIt outLast, IUnaryFunction<int, int> randomNumber)
            where RandIt : IRandomAccessIterator<T>
        {
            int n = outLast.Diff(outFirst);
            int m = 0;
            int t = n;
            first = (IInputIterator<T>)first.Clone();
            for (; first != last && m < n; ++m, first.PreIncrement())
                outFirst.Add(m).Value = first.Value;

            while (!Equals(first, last))
            {
                ++t;
                int m2 = randomNumber.Execute(t);
                if (m2 < n)
                    outFirst.Add(m2).Value = first.Value;
                first.PreIncrement();
            }

            return (RandIt)outFirst.Add(m);
        }
        /// <summary>
        /// RandomSample randomly copies a sample of the elements from the range [first, last) into 
        /// the range [target, target + n). Each element in the input range appears at most once 
        /// in the output range, and samples are chosen with uniform probability. 
        /// Elements in the output range might appear in any order: relative order within the 
        /// input range is not guaranteed to be preserved.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="RandIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> that points to the first element of the sample input range.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> that points one past the final element of the sample input range.
        /// </param>
        /// <param name="outFirst">
        /// An <see cref="IRandomAccessIterator{T}"/> that points to the first element of target range.
        /// </param>
        /// <param name="outLast">
        /// An <see cref="IRandomAccessIterator{T}"/> that points one past the final element of target range.
        /// </param>
        /// <returns></returns>
        public static RandIt
            RandomSample<T, RandIt>(IInputIterator<T> first, IInputIterator<T> last,
                                    RandIt outFirst, RandIt outLast)
            where RandIt : IRandomAccessIterator<T>
        {
            return RandomSample(first, last, outFirst, outLast, Functional.PtrFun<int, int>(__random_number));
        }
    }
}

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

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// Random_shuffle randomly rearranges the elements in the range [first, last):
        /// that is, it randomly picks one of the N! possible orderings, 
        /// where N is last - first.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        public static void
            RandomShuffle<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last)
        {
            RandomShuffle(first, last, Functional.PtrFun<int, int>(__random_number));
        }
        /// <summary>
        /// Random_shuffle randomly rearranges the elements in the range [first, last):
        /// that is, it randomly picks one of the N! possible orderings, 
        /// where N is last - first.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first">
        /// An input iterator pointing to the first element of the range.
        /// </param>
        /// <param name="last">
        /// An input iterator pointing one past the final element of the range.
        /// </param>
        /// <param name="randomNumber">A function that generates random numbers.</param>
        public static void
            RandomShuffle<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IUnaryFunction<int, int> randomNumber)
        {
            if (Equals(first, last)) return;
            for (IRandomAccessIterator<T> i = first.Add(1); !Equals(i, last); i.PreIncrement())
                IterSwap(i, first.Add(randomNumber.Execute(((i.Diff(first)) + 1))));
        }

        private static Random rand = new Random(unchecked((int)DateTime.Now.Ticks));
        private static int
            __random_number(int n)
        {
            return rand.Next() % n;
        }
    }
}

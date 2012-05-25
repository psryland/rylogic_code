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
        /// <para>
        /// Rotate rotates the elements in a range. That is, the element pointed 
        /// to by middle is moved to the position first, the element pointed to 
        /// by middle + 1 is moved to the position first + 1, and so on. 
        /// One way to think about this operation is that it exchanges the 
        /// two ranges [first, middle) and [middle, last). 
        /// </para>
        /// <para>
        /// The complexity is linear. At most last - first swaps are performed.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt">
        /// </typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing to
        /// the first element of the range to be rotated.
        /// </param>
        /// <param name="middle">
        /// An iterator pointing between first and last specifying the rotation axis.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing 
        /// one past the final element of the range to be rotated.</param>
        /// <returns>
        /// An iterator pointing to first + (last - middle).
        /// </returns>
        /// <remarks>
        /// Rotate uses a different algorithm depending on whether its arguments are 
        /// Forward Iterators, Bidirectional Iterators, or Random Access Iterators. 
        /// All three algorithms, however, are linear.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Better readable!")]
        public static FwdIt
            Rotate<T, FwdIt>(FwdIt first, FwdIt middle, FwdIt last) where FwdIt : IForwardIterator<T>
        {
            if (first is IRandomAccessIterator<T>)
                return (FwdIt)Rotate((IRandomAccessIterator<T>)first, (IRandomAccessIterator<T>)middle, (IRandomAccessIterator<T>)last);
            if (first is IBidirectionalIterator<T>)
                return (FwdIt)Rotate((IBidirectionalIterator<T>)first, (IBidirectionalIterator<T>)middle, (IBidirectionalIterator<T>)last);

            return (FwdIt)Rotate(first, middle, last);
        }

        private static IForwardIterator<T>
            Rotate<T>(IForwardIterator<T> first, IForwardIterator<T> middle, IForwardIterator<T> last)
        {
            first = (IForwardIterator<T>)first.Clone();
            if (Equals(first, middle))
                return last;
            if (Equals(last, middle))
                return first;

            IForwardIterator<T> first2 = (IForwardIterator<T>)middle.Clone();
            do
            {
                IterSwap(first.PostIncrement(), first2.PostIncrement());
                if (Equals(first, middle))
                    middle = (IForwardIterator<T>)first2.Clone();
            } while (!Equals(first2, last));

            IForwardIterator<T> _New_middle = (IForwardIterator<T>)first.Clone();

            first2 = (IForwardIterator<T>)middle.Clone();

            while (!Equals(first2, last))
            {
                IterSwap(first.PostIncrement(), first2.PostIncrement());
                if (Equals(first, middle))
                    middle = (IForwardIterator<T>)first2.Clone();
                else if (Equals(first2, last))
                    first2 = (IForwardIterator<T>)middle.Clone();
            }

            return _New_middle;
        }
        private static IBidirectionalIterator<T>
            Rotate<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> middle, IBidirectionalIterator<T> last)
        {
            first = (IBidirectionalIterator<T>)first.Clone();
            last = (IBidirectionalIterator<T>)last.Clone();
            if (Equals(first, middle))
                return last;
            if (Equals(last, middle))
                return first;

            Reverse(first, middle);
            Reverse(middle, last);

            while (!Equals(first, middle) && !Equals(middle, last))
                IterSwap(first.PostIncrement(), last.PreDecrement());

            if (Equals(first, middle))
            {
                Reverse(middle, last);
                return last;
            }
            else
            {
                Reverse(first, middle);
                return first;
            }
        }
        private static IRandomAccessIterator<T>
            Rotate<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> middle, IRandomAccessIterator<T> last)
        {
            first = (IRandomAccessIterator<T>)first.Clone();
            int n = last.Diff(first);
            int k = middle.Diff(first);
            int l = n - k;
            IRandomAccessIterator<T> result = first.Add(last.Diff(middle));

            if (k == 0)
                return last;
            else if (k == l)
            {
                SwapRanges(first, middle, middle);
                return result;
            }

            int d = __gcd(n, k);

            for (int i = 0; i < d; ++i)
            {
                T tmp = first.Value;
                IRandomAccessIterator<T> p = (IRandomAccessIterator<T>)first.Clone();

                if (k < l)
                {
                    for (int j = 0; j < l / d; ++j)
                    {
                        if (!(p.Less(first.Add(l))))
                        {
                            p.Value = (p.Add(-l)).Value;
                            p = p.Add(-l);
                        }

                        p.Value = (p.Add(k)).Value;
                        p = p.Add(k);
                    }
                }
                else
                {
                    for (int j = 0; j < k / d - 1; ++j)
                    {
                        if (p.Less(last.Add(-k)))
                        {
                            p.Value = (p.Add(k)).Value;
                            p = p.Add(k);
                        }

                        p.Value = (p.Add(-l)).Value;
                        p = p.Add(-l);
                    }
                }

                p.Value = tmp;
                first.PreIncrement();
            }

            return result;
        }

    }
}

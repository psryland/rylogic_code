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
        /// Partition reorders the elements in the range [first, last) based 
        /// on the function object pred, such that the elements that satisfy 
        /// pred precede the elements that fail to satisfy it. 
        /// </para>
        /// <para>
        /// The relative order of elements in these two blocks is not 
        /// necessarily the same as it was in the original sequence.
        /// A different algorithm, <see cref="StablePartition{T,FwdIt}"/>, 
        /// does guarantee to preserve the relative order.
        /// </para>
        /// <para>
        /// The complexity is linear. Exactly last - first applications 
        /// of pred, and at most (last - first)/2 swaps.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// A <see cref="IForwardIterator{T}"/> implementation pointing 
        /// to the first element of the range to be partitioned.
        /// </param>
        /// <param name="last">
        /// A <see cref="IForwardIterator{T}"/> implementation pointing 
        /// to the first element of the range to be partitioned.
        /// </param>
        /// <param name="pred">The predicate to be satisfied.</param>
        /// <returns>
        /// A <see cref="IForwardIterator{T}"/> implementation pointing to the
        /// partition boundary.
        /// </returns>
        public static FwdIt
            Partition<T, FwdIt>(FwdIt first, FwdIt last, IUnaryFunction<T, bool> pred)
            where FwdIt : IForwardIterator<T>
        {
            IBidirectionalIterator<T> bidFirst = first as IBidirectionalIterator<T>;
            if (bidFirst != null)
                return (FwdIt)PartitionBidIt(bidFirst, (IBidirectionalIterator<T>)last, pred);
            return (FwdIt)PartitionFwd(first, last, pred);
        }
        private static BidIt
           PartitionBidIt<T, BidIt>(BidIt first, BidIt last, IUnaryFunction<T, bool> predicate) where BidIt : IBidirectionalIterator<T>
        {
            first = (BidIt)first.Clone();
            last = (BidIt)last.Clone();
            for (; ; first.PreIncrement())
            {
                for (; !Equals(first, last) && predicate.Execute(first.Value); first.PreIncrement()) { }
                if (Equals(first, last))
                    break;

                for (; !Equals(first, last.PreDecrement()) && !predicate.Execute(last.Value); ) { }
                if (Equals(first, last))
                    break;

                IterSwap(first, last);
            }
            return first;
        }
        private static IForwardIterator<T>
            PartitionFwd<T>(IForwardIterator<T> first, IForwardIterator<T> last, IUnaryFunction<T, bool> predicate)
        {
            if (Equals(first, last))
                return first;
            first = (IForwardIterator<T>)first.Clone();
            while (predicate.Execute(first.Value))
                if (Equals(first.PreIncrement(), last))
                    return first;

            IForwardIterator<T> next = (IForwardIterator<T>)first.Clone();

            while (!Equals(next.PreIncrement(), last))
                if (predicate.Execute(next.Value))
                {
                    IterSwap(first, next);
                    first.PreIncrement();
                }

            return first;
        }
    }
}

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

using NStl.Linq;
using NStl.Iterators;

namespace NStl
{
    public static partial class Algorithm
    {
        /// <summary>
        /// <para>
        /// StablePartition is much like <see cref="Partition{T,FwdIt}"/>: 
        /// it reorders the elements in the range [first, last) based 
        /// on the function object pred, such that all of the elements 
        /// that satisfy pred appear before all of the elements that fail 
        /// to satisfy it. 
        /// </para>
        /// <para>
        /// StablePartition differs from <see cref="Partition{T,FwdIt}"/> 
        /// in that StablePartition is guaranteed to preserve relative order.
        ///  That is, if x and y are elements in [first, last) such that 
        /// pred.Execute(x) == pred.Execute(y), and if x precedes y, 
        /// then it will still be true after stable_partition is true that 
        /// x precedes y. Note that the complexity of StablePartition is 
        /// greater than that of <see cref="Partition{T,FwdIt}"/>: the guarantee 
        /// that the relative order will be preserved has a significant runtime cost. 
        /// If this guarantee isn't important to you, you should use <see cref="Partition{T,FwdIt}"/>.
        /// </para>
        /// <para>
        /// StablePartition is an adaptive algorithm: it attempts to allocate 
        /// a temporary memory buffer. The complexity is linear in N, pred is 
        /// applied exactly N times.
        /// </para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="FwdIt"></typeparam>
        /// <param name="first">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing
        /// to the first element of the range to be patitioned.
        /// </param>
        /// <param name="last">
        /// An <see cref="IForwardIterator{T}"/> implementation pointing
        /// one apst the final element of the range to be patitioned.
        /// </param>
        /// <param name="pred">
        /// The predciate functor that is used as the partition criterion.
        /// </param>
        /// <returns>
        /// Returns an iterator that points to the first element that
        /// does not fulfill pred: The partition boundary.
        /// </returns>
        public static FwdIt
            StablePartition<T, FwdIt>(FwdIt first, FwdIt last,
                             IUnaryFunction<T, bool> pred) where FwdIt : IForwardIterator<T>
        {
            if (Equals(first, last))
                return (FwdIt)first.Clone();
            T[] buffer = CreateBuffer(first, last);
            return StablePartitionAdaptive(first, last, pred,//TBD: Is this really the fastest in .NET??
                                                    buffer.Begin());

            // ... original STL implementation. Allocation an array should never fail
            // ... in .NET, so no if 
            //if (buffer.Length > 0)
            //    return __stable_partition_adaptive<T>(first, last, predicate,
            //                                       buffer.Length,
            //                                        util.begin(buffer), buffer.Length);
            //else
            //    return __inplace_stable_partition<T>(first, last, predicate, buffer.Length);
        }

        private static FwdIt1
            StablePartitionAdaptive<T, FwdIt1, FwdIt2>(FwdIt1 first, FwdIt1 last,
                                        IUnaryFunction<T, bool> predicate,
                                        FwdIt2 buffer)
            where FwdIt1 : IForwardIterator<T>
            where FwdIt2 : IForwardIterator<T>
        {
            first = (FwdIt1)first.Clone();
            FwdIt1 result1 = (FwdIt1)first.Clone();
            FwdIt2 result2 = (FwdIt2)buffer.Clone();
            for (; !Equals(first, last); first.PreIncrement())
                if (predicate.Execute(first.Value))
                {
                    result1.Value = first.Value;
                    result1.PreIncrement();
                }
                else
                {
                    result2.Value = first.Value;
                    result2.PreIncrement();
                }
            Copy(buffer, result2, result1);
            return result1;

            // ... original implementation, can be simpler in .NET,
            // ... because we always have a buffer!
            //if (length <= bufferSize)
            //{
            //    first = (FwdIt)first.clone();
            //    FwdIt result1 = (FwdIt)first.clone();
            //    FwdIt result2 = (FwdIt)buffer.clone();
            //    for (; first != last; first.pre_increment())
            //        if (predicate.execute(first.value))
            //        {
            //            result1.value = first.value;
            //            result1.pre_increment();
            //        }
            //        else
            //        {
            //            result2.value = first.value;
            //            result2.pre_increment();
            //        }
            //    __copy(buffer, result2, result1);
            //    return result1;
            //}
            //else
            //{
            //    FwdIt middle = (FwdIt)__advance(first, length / 2);
            //    return (FwdIt)__rotate(
            //        __stable_partition_adaptive<T, FwdIt>(first, middle, predicate, length / 2, buffer, bufferSize),
            //        middle,
            //        __stable_partition_adaptive<T, FwdIt>(middle, last, predicate, length - length / 2, buffer, bufferSize)
            //                     );
            //}
        }
        //private static IBidirectionalIterator<T>
        //    __inplace_stable_partition<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> last,
        //                               UnaryFunction<T, bool> predicate, int length)
        //{
        //    if (length == 1)
        //        return predicate.Execute(first.Value) ? (IBidirectionalIterator<T>)last.Clone() : (IBidirectionalIterator<T>)first.Clone();
        //    IBidirectionalIterator<T> middle = (IBidirectionalIterator<T>)first.Clone();
        //    middle = NstlUtil.Advance<T, IBidirectionalIterator<T>>(middle, length / 2);
        //    return __rotate(__inplace_stable_partition<T>(first, middle, predicate, length / 2),
        //                      middle,
        //                      __inplace_stable_partition<T>(middle, last, predicate, length - length / 2)
        //                     );
        //}
    }
}

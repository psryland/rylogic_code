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
using System.Collections;
using System.Collections.Generic;
using NStl.Collections;
using NStl.Iterators;
using NStl.Iterators.Private;
using NStl.Util;

namespace NStl.Linq
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) extension methods for iterators.
    /// </summary>
    public static class IteratorExtension
    {
        /// <summary>
        /// Adapts the input iterators to a range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static Range<T, IInputIterator<T>> AsRange<T>(this IInputIterator<T> first, IInputIterator<T> last)
        {
            return new Range<T, IInputIterator<T>>(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to a range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static Range<T, IForwardIterator<T>> AsRange<T>(this IForwardIterator<T> first, IForwardIterator<T> last)
        {
            return new Range<T, IForwardIterator<T>>(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to a range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static Range<T, IBidirectionalIterator<T>> AsRange<T>(this IBidirectionalIterator<T> first, IBidirectionalIterator<T> last)
        {
            return new Range<T, IBidirectionalIterator<T>>(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to a range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static Range<T, IBidirectionalInputIterator<T>> AsRange<T>(this IBidirectionalInputIterator<T> first, IBidirectionalInputIterator<T> last)
        {
            return new Range<T, IBidirectionalInputIterator<T>>(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to a range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static Range<T, IRandomAccessIterator<T>> AsRange<T>(this IRandomAccessIterator<T> first, IRandomAccessIterator<T> last)
        {
            return new Range<T, IRandomAccessIterator<T>>(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to a range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static Range<T, IRandomAccessInputIterator<T>> AsRange<T>(this IRandomAccessInputIterator<T> first, IRandomAccessInputIterator<T> last)
        {
            return new Range<T, IRandomAccessInputIterator<T>>(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to a range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Iter"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static Range<T, Iter> AsRange<T, Iter>(this Iter first, Iter last) where Iter : IInputIterator<T>
        {
            return new Range<T, Iter>(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to an <see cref="ICollection"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static ICollection AsCollection<T>(this IInputIterator<T> first, IInputIterator<T> last)
        {
            return AsRange(first, last);
        }
        /// <summary>
        /// Adapts the input iterators to an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public static IEnumerable<T> AsEnumerable<T>(this IInputIterator<T> first, IInputIterator<T> last)
        {
            return AsRange(first, last);
        }
        /// <summary>
        /// Clones the iterator and moves the clone by dist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="it"></param>
        /// <param name="dist">The distance that this iterator will be moved.</param>
        /// <returns>A cloned iterator moved by the specified distance.</returns>
        /// <exception cref="ArgumentException">Thrown when dist is negative.</exception>
        public static IInputIterator<T> Add<T>(this IInputIterator<T> it, int dist)
        {
            Verify.ArgumentGreaterEqualsZero(dist, "dist");
            it = (IInputIterator<T>)it.Clone();
            while (dist-- > 0)
                it.PreIncrement();
            return it;
        }
        /// <summary>
        /// Clones the iterator and moves the clone by dist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="it"></param>
        /// <param name="dist">The distance that this iterator will be moved.</param>
        /// <returns>A cloned iterator moved by the specified distance.</returns>
        /// <exception cref="ArgumentException">Thrown when dist is negative.</exception>
        public static IForwardIterator<T> Add<T>(this IForwardIterator<T> it, int dist)
        {
            return (IForwardIterator<T>) Add((IInputIterator<T>) it, dist);
        }

        /// <summary>
        /// Clones the iterator and moves the clone by dist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="it"></param>
        /// <param name="dist">The distance that this iterator will be moved.</param>
        /// <returns>A cloned iterator moved by the specified distance.</returns>
        public static IBidirectionalIterator<T> Add<T>(this IBidirectionalIterator<T> it, int dist)
        {
            if (dist >= 0)
                return (IBidirectionalIterator<T>)Add((IInputIterator<T>) it, dist);
            it = (IBidirectionalIterator<T>)it.Clone();
            while (dist++ < 0)
                it.PreDecrement();
            return it;
        }

        ///<summary>
        /// Determines the number of increments between the positions if this iterator and another.
        ///</summary>
        ///<param name="first"></param>
        ///<param name="last"></param>
        ///<typeparam name="T"></typeparam>
        ///<returns></returns>
        public static int DistanceTo<T>(this IInputIterator<T> first, IInputIterator<T> last)
        {
            Verify.ArgumentNotNull(last, "last");
            IRandomAccessIterator<T> f = first as IRandomAccessIterator<T>;
            if(f != null)
                return DistanceTo(f, (IRandomAccessIterator<T>) last);

            int distance = 0;
            first = (IInputIterator<T>)first.Clone();
            for (; !Equals(first, last); first.PreIncrement())
                ++distance;
            return distance;
        }
        ///<summary>
        /// Determines the number of increments between the positions if this iterator and another.
        ///</summary>
        ///<param name="first"></param>
        ///<param name="last"></param>
        ///<typeparam name="T"></typeparam>
        ///<returns></returns>
        public static int DistanceTo<T>(this IRandomAccessIterator<T> first, IRandomAccessIterator<T> last)
        {
            Verify.ArgumentNotNull(last, "last");
            return last.Diff(first);
        }
        /// <summary>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation that adds
        /// values to the start of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IOutputIterator<T> AddFirstInserter<T>(this IFrontInsertable<T> list)
        {
            return new FrontInsertIterator<T>(list);
        }
    }
}

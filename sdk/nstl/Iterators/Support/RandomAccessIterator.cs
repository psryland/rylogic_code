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


using System.Diagnostics.CodeAnalysis;
namespace NStl.Iterators.Support
{
    /// <summary>
    /// A random acess iterator extents the bidirectional iterator with
    /// the possibility to do a constant time jump forward or backwards.
    /// </summary>
    public abstract class RandomAccessIterator<T> : BidirectionalIterator<T>, IRandomAccessIterator<T>
    {
        /// <summary>
        /// When implemented, it returns a copy of this iterator moved
        /// count ahead/back. 
        /// </summary>
        /// <param name="count">How far the copied iterator will jump</param>
        /// <returns></returns>
        /// <remarks>This iterator stays where it is. The action performs in constant time</remarks>
        public abstract IRandomAccessIterator<T> Add(int count);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static RandomAccessIterator<T> operator +(RandomAccessIterator<T> it, int count)
        {
            return (RandomAccessIterator<T>)it.Add(count);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Method Plus is already exist!")]
        public static RandomAccessIterator<T> operator -(RandomAccessIterator<T> it, int count)
        {
            return (RandomAccessIterator<T>)it.Add(-count);
        }
        /// <summary>
        /// Computes the distance between two iterators
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns>The distance between two iterators</returns>
        public abstract int Diff(IRandomAccessIterator<T> rhs);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1013:OverloadOperatorEqualsOnOverloadingAddAndSubtract", Justification = "Equality operator is inherited!"), 
         SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Method Plus is already exist!")]
        public static int operator -(RandomAccessIterator<T> lhs, RandomAccessIterator<T> rhs)
        {
            return lhs.Diff(rhs);
        }
        /// <summary>
        /// Checks if this iterator is before the other
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public abstract bool Less(IRandomAccessIterator<T> rhs);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <(RandomAccessIterator<T> lhs, RandomAccessIterator<T> rhs)
        {
            return lhs.Less(rhs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >(RandomAccessIterator<T> lhs, RandomAccessIterator<T> rhs)
        {
            return !(lhs < rhs) && Equals(lhs, rhs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator <=(RandomAccessIterator<T> lhs, RandomAccessIterator<T> rhs)
        {
            return lhs < rhs || Equals(lhs, rhs);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool operator >=(RandomAccessIterator<T> lhs, RandomAccessIterator<T> rhs)
        {
            return lhs > rhs | Equals(lhs, rhs);
        }
        /// <summary>
        /// Overload for languages without operator overloading.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static int Compare(RandomAccessIterator<T> lhs, RandomAccessIterator<T> rhs)
        {
            return lhs > rhs ? 1 : (lhs < rhs ? - 1 : 0);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
        public static RandomAccessIterator<T> operator ++(RandomAccessIterator<T> it)
        {
            RandomAccessIterator<T> tmp = (RandomAccessIterator<T>)it.Clone();
            return (RandomAccessIterator<T>)tmp.PreIncrement();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Post/PreDecrement() already exist")]
        public static RandomAccessIterator<T> operator --(RandomAccessIterator<T> it)
        {
            RandomAccessIterator<T> tmp = (RandomAccessIterator<T>)it.Clone();
            return (RandomAccessIterator<T>)tmp.PreDecrement();
        }
    }
}

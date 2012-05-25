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


using System.Collections.Generic;
using NStl.Iterators.Support;
using NStl.Util;
using NStl.Iterators;
using NStl.Adapters;
using NStl.Iterators.Private;

namespace NStl.Linq
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods 
    /// for querying objects of type <see cref="List{T}"/>.
    /// </summary>
    public static class ListTExtension
    {
        /// <summary>
        /// Creates an iterator that points to the first element of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ListTIterator<T> Begin<T>(this List<T> list)
        {
            return new ListTIterator<T>(list, 0);
        }
        /// <summary>
        /// Creates an iterator that points one past the final element of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ListTIterator<T> End<T>(this List<T> list)
        {
            return new ListTIterator<T>(list, list.Count);
        }
        /// <summary>
        /// Erases the element at the specified position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">
        /// An <see cref="ListTIterator{T}"/> pointing to the element to be deleted.
        /// </param>
        /// <returns>
        /// An <see cref="ListTIterator{T}"/> pointing the first element remaining 
        /// beyond any elements removed.
        /// </returns>
        public static ListTIterator<T> Remove<T>(this List<T> list, ListTIterator<T> where)
        {
            Verify.InstanceEquals(list, where.List, "");
            Verify.ArgumentNotNull(where, "where");

            list.RemoveAt(where.Index);
            return (ListTIterator<T>)where.Clone();
        }
        /// <summary>
        /// Erases the elements of the specified sub range.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns>
        /// An <see cref="ListTIterator{T}"/> pointing the first element remaining 
        /// beyond any elements removed.
        /// </returns>
        public static ListTIterator<T> Remove<T>(this List<T> list, ListTIterator<T> first, ListTIterator<T> last)
        {
            Verify.InstanceEquals(list, first.List, "");
            Verify.InstanceEquals(list, last.List, "");
            Verify.ArgumentNotNull(first, "first");
            Verify.ArgumentNotNull(last, "last");

            list.RemoveRange(first.Index, last.Index - first.Index);

            return (ListTIterator<T>)first.Clone();
        }
        /// <summary>
        /// Inserts value at the position specified by the iterator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">An iterator pointing to the position of the insertion.</param>
        /// <param name="value">The value to be inserted.</param>
        /// <returns>An iterator pointing to the inserted element.</returns>
        public static ListTIterator<T> Insert<T>(this List<T> list, ListTIterator<T> where, T value)
        {
            Verify.InstanceEquals(list, where.List, "");
            Verify.ArgumentNotNull(where, "where");

            list.Insert(where.Index, value);
            return (ListTIterator<T>) where.Clone();
        }
        /// <summary>
        /// Inserts value at the position specified by the iterator n times.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">An iterator pointing to the position of the insertion.</param>
        /// <param name="count">The amount of insertions.</param>
        /// <param name="value">The value to be inserted.</param>
        /// <returns>An iterator pointing to the inserted element.</returns>
        public static ListTIterator<T> Insert<T>(this List<T> list, ListTIterator<T> where, int count, T value)
        {
            Verify.InstanceEquals(list, where.List, "");
            Verify.ArgumentNotNull(where, "where");

            while(count-- > 0)
            {
                list.Insert(where.Index, value);
            }
            return (ListTIterator<T>)where.Clone();
        }
        /// <summary>
        /// Creates an output iterator that allows insertion at the specified iterator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static IOutputIterator<T> InsertIterator<T>(this List<T> list, ListTIterator<T> where)
        {
            Verify.InstanceEquals(list, where.List, Resource.IteratorPointsToOtherRange);
            IInsertable<T, ListTIterator<T>> insert = new ListT2IInsertableAdaptor<T>(list);
            return new InsertIterator<T, ListTIterator<T>>(insert, where);
        }
        /// <summary>
        /// Resizes the list to countain as many members as its capacity. It 
        /// adds default(T) if it is greater tan the actual count.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Resize<T>(this List<T> list)
        {
            list.Resize(list.Capacity, default(T));
        }
    }
}

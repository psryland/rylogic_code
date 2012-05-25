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
using System.Collections;
using NStl.Iterators.Support;
using NStl.Util;
using System;
using NStl.Iterators;
using NStl.Adapters;
using NStl.Iterators.Private;

namespace NStl.Linq
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods 
    /// for querying objects that implement <see cref="IList{T}"/>.
    /// </summary>
    public static class ListExtension
    {
        /// <summary>
        /// Converts the elements of an <see cref="IList"/> to the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method returns an adapter object, so calling this methond is very efficient.
        /// </remarks>
        public static IList<T> Cast<T>(this IList list)
        {
            return new IList2IlistTAdapter<T>(list);
        }
        /// <summary>
        /// Creates an iterator that points to the first element of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ListIterator<T> Begin<T>(this IList<T> list)
        {
            return new ListIterator<T>(list, 0);
        }
        /// <summary>
        /// Creates an iterator that points one past the final element of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static ListIterator<T> End<T>(this IList<T> list)
        {
            return new ListIterator<T>(list, list.Count);
        }
        /// <summary>
        /// Returns the last element of an <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T Last<T>(this IList<T> list)
        {
            Verify.AreNotEqual<InvalidOperationException>(0, list.Count, Resource.ContainerIsEmpty);
            return list[list.Count - 1];
        }
        /// <summary>
        /// Resizes the list to contain count members. It shortens the list if 
        /// count is less than <see cref="ICollection{T}.Count"/> or adds value if
        /// it is greater.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="count">The new size of the list.</param>
        /// <param name="value">The value to be added if the list has to grow.</param>
        public static void Resize<T>(this IList<T> list, int count, T value)
        {
            if (list.Count >= count)
            {
                int diff = list.Count - count;
                while (diff-- > 0)
                    list.PopLast();
                return;
            }
            else
            {
                int diff = count - list.Count;

                while (diff-- > 0)
                    list.Add(value);
            }
        }
        /// <summary>
        /// Resizes the list to contain count members. It shortens the list if 
        /// count is less than <see cref="ICollection{T}.Count"/> or adds default(T) if
        /// it is greater.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="count">The new size of the list.</param>
        public static void Resize<T>(this IList<T> list, int count)
        {
            Resize(list, count, default(T));
        }
        /// <summary>
        /// Removes the last elememt of the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <exception cref="InvalidOperationException">Thrown when the list is empty.</exception>
        public static void PopLast<T>(this IList<T> list)
        {
            Verify.AreNotEqual<InvalidOperationException>(list.Count, 0, Resource.ContainerIsEmpty);
            list.RemoveAt(list.Count - 1);
        }
        /// <summary>
        /// Inserts the passed in value at the given position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">An iterator pointin to the position of the insertion.</param>
        /// <param name="value">The value to be inserted.</param>
        /// <returns>An interator pointing to the newly inserted element.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the iterator is null.</exception>
        public static ListIterator<T> Insert<T>(this IList<T> list, ListIterator<T> where, T value)
        {
            Verify.InstanceEquals(list, where.List, "");
            Verify.ArgumentNotNull(where, "where");

            list.Insert(where.Index, value);
            return (ListIterator<T>)where.Clone();
        }
        /// <summary>
        /// Inserts the passed in value n times at the given position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">An iterator pointin to the position of the insertion.</param>
        /// <param name="count">The amount of times that the value will be inserted.</param>
        /// <param name="value">The value to be inserted.</param>
        /// <returns>An interator pointing to the newly inserted element.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the iterator is null.</exception>
        public static ListIterator<T> Insert<T>(this IList<T> list, ListIterator<T> where, int count, T value)
        {
            Verify.InstanceEquals(list, where.List, "");
            Verify.ArgumentNotNull(where, "where");

            while (count-- > 0)
            {
                list.Insert(where.Index, value);
            }
            return (ListIterator<T>)where.Clone();
        }
        /// <summary>
        /// Creates an <see cref="IOutputIterator{T}"/> implementation
        /// that lets you insert values at the specified position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">The position of the insertion.</param>
        /// <returns></returns>
        public static IOutputIterator<T> InsertIterator<T>(this IList<T> list, ListIterator<T> where)
        {
            Verify.InstanceEquals(list, where.List, Resource.IteratorPointsToOtherRange);
            IInsertable<T, ListIterator<T>> insert = new List2IInsertableAdaptor<T>(list);
            return new InsertIterator<T, ListIterator<T>>(insert, where);
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
        public static ListIterator<T> Remove<T>(this IList<T> list, ListIterator<T> where)
        {
            Verify.InstanceEquals(list, where.List, "");
            Verify.ArgumentNotNull(where, "where");

            list.RemoveAt(where.Index);
            return (ListIterator<T>)where.Clone();
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
        public static ListIterator<T> Remove<T>(this IList<T> list, ListIterator<T> first, ListIterator<T> last)
        {
            Verify.InstanceEquals(list, first.List, "");
            Verify.InstanceEquals(list, last.List, "");
            Verify.ArgumentNotNull(first, "first");
            Verify.ArgumentNotNull(last, "last");

            int diff = last - first;
            while (diff-- > 0)
                list.RemoveAt(first.Index);

            return (ListIterator<T>)first.Clone();
        }
        /// <summary>
        /// Creates an iterator that points one before the first element of the list
        /// and will iterate backwards.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IRandomAccessIterator<T> RBegin<T>(this IList<T> list)
        {
            return NStlUtil.Reverse(End(list));
        }
        /// <summary>
        /// Creates an iterator that points on the final element of the list
        /// and iterates backwards.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IRandomAccessIterator<T> REnd<T>(this IList<T> list)
        {
            return NStlUtil.Reverse(Begin(list));
        }
        /// <summary>
        /// Creates an adapter that allows to use a generic collectiona as its non generic counterpart.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is a dangerous tool only suited to satisfy compatibility issues
        /// with older APIs.
        /// </remarks>
        public static IList Weak<T>(this IList<T> collection)
        {
            return new ListT2IListAdaptor<T>(collection);
        }
        /// <summary>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation that adds
        /// values to the end of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IOutputIterator<T> AddLastInserter<T>(this IList<T> list)
        {
            return new BackInsertableBackInsertIterator<T>(list);
        }
   }
}

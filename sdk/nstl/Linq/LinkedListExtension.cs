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
using System.Linq;
using NStl.Exceptions;
using NStl.Iterators;
using NStl.Iterators.Support;
using System.Collections.Generic;
using NStl.Util;
using NStl.Iterators.Private;

namespace NStl.Linq
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods 
    /// for querying objects of type <see cref="LinkedList{T}"/>.
    /// </summary>
    public static class LinkedListExtension
    {
        /// <summary>
        /// Creates a <see cref="LinkedListIterator{T}"/> pointing to 
        /// the first element of the <see cref="LinkedList{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static LinkedListIterator<T> Begin<T>(this LinkedList<T> list)
        {
            return new LinkedListIterator<T>(list, list.First);
        }
        /// <summary>
        /// Creates a <see cref="LinkedListIterator{T}"/> pointing 
        /// one past the last element of the <see cref="LinkedList{T}"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static LinkedListIterator<T> End<T>(this LinkedList<T> list)
        {
            return new LinkedListIterator<T>(list);
        }
        /// <summary>
        /// Returns an iterator addressing the first element in a reversed list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IBidirectionalIterator<T> RBegin<T>(this LinkedList<T> list)
        {
            return NStlUtil.Reverse(End(list));
        }
        /// <summary>
        /// Returns an iterator that addresses the location succeeding the last element in a reversed list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IBidirectionalIterator<T> REnd<T>(this LinkedList<T> list)
        {
            return NStlUtil.Reverse(Begin(list));
        }
        /// <summary>
        /// This method lets you specify the new size of the list. If the
        /// list is longer, it will be trimmed. If it is shorter, it will be filled with val.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="newSize">The new size of the list.</param>
        /// <param name="val">The value that is used to fill the lis if needed.</param>
        public static void Resize<T>(this LinkedList<T> list, int newSize, T val)
        {
            LinkedListIterator<T> iter = Begin(list);
            LinkedListIterator<T> end = End(list);
            while (newSize > 0)
            {
                if (Equals(iter, end))
                    iter = Insert(list, iter, val);

                iter.PreIncrement();
                --newSize;
            }
            if (!Equals(iter, end))
                Remove(list, iter, end);

        }
        /// <summary>
        /// This method lets you specify the new size of the list. If the
        /// list is longer, it will be trimmed. If it is shorter, it will be filled with default(T).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="newSize">The new size of the list.</param>
        public static void Resize<T>(this LinkedList<T> list, int newSize)
        {
            Resize(list, newSize, default(T));
        }
        /// <summary>
        /// Removes a range of elements in a list from specified positions.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="first">Position of the first element removed from the list.</param>
        /// <param name="last">Position just beyond the last element removed from the list.</param>
        /// <returns>
        /// A bidirectional iterator that designates the first element remaining beyond any 
        /// elements removed, or a pointer to the end of the list if no such element exists.
        /// </returns>
        public static LinkedListIterator<T> Remove<T>(this LinkedList<T> list, LinkedListIterator<T> first, LinkedListIterator<T> last)
        {
            first = (LinkedListIterator<T>)first.Clone();
            while(!Equals(first, last))
            {
                LinkedListNode<T> node = first.Node;
                ++first;
                list.Remove(node);
            }

            return new LinkedListIterator<T>(list, last.Node);
        }
        /// <summary>
        /// Inserts a range of element into the list at a specified position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">
        /// The position in the target list where the first element is inserted.
        /// </param>
        /// <param name="first">
        /// The position of the first element in the range of elements in the argument 
        /// list to be copied
        /// </param>
        /// <param name="last">
        /// The position of the first element beyond the range of elements in the 
        /// argument list to be copied
        /// </param>
        public static void Insert<T>(this LinkedList<T> list, LinkedListIterator<T> where, IInputIterator<T> first, IInputIterator<T> last)
        {
            first = (IInputIterator<T>)first.Clone();
            for (; !Equals(first, last); first.PreIncrement())
                Insert(list, where, first.Value);
        }

        /// <summary>
        /// Inserts an element into the list at a specified position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="where">
        /// The position in the target list where the first element is inserted.
        /// </param>
        /// <param name="val">The value of the element being inserted into the list.</param>
        /// <returns>
        /// The function returns an iterator that points to the position 
        /// where the new element was inserted into the list.
        /// </returns>
        public static LinkedListIterator<T> Insert<T>(this LinkedList<T> list, LinkedListIterator<T> where, T val)
        {
            Verify.InstanceEquals(list, where.List, "");

            if (!list.Any())
            {
                list.AddLast(val);
                return new LinkedListIterator<T>(list, list.First);
            }
            if (!Equals(where, list.End()))
                return new LinkedListIterator<T>(list, list.AddBefore(where.Node, val));
            return new LinkedListIterator<T>(list, list.AddAfter(list.Last, val));
        }
        /// <summary>
        /// Inserts an n element into the list at a specified position.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="where">
        /// The position in the target list where the first element is inserted
        /// </param>
        /// <param name="count">
        /// The number of elements being inserted into the list.
        /// </param>
        /// <param name="val">The value of the element being inserted into the list</param>
        /// <returns>
        /// The function returns an iterator that points to the position 
        /// where the new element was inserted into the list
        /// </returns>
        /// <remarks>Insert only works for forward iterators.</remarks>
        public static LinkedListIterator<T> Insert<T>(this LinkedList<T> list, LinkedListIterator<T> where, int count, T val)
        {
            LinkedListIterator<T> ret = null;
            while (count > 0)
            {
                ret = Insert(list, where, val);
                --count;
            }
            return ret;
        }
        /// <summary>
        /// Removes an element in a list from specified positions.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="where">Position of the element to be removed from the list.</param>
        /// <returns>
        /// A bidirectional iterator that designates the first element remaining beyond any 
        /// elements removed, or a pointer to the end of the list if no such element exists.
        /// </returns>
        /// <exception cref="ContainerEmptyException">Thrown when the DList is empty.</exception>
        public static LinkedListIterator<T> Remove<T>(this LinkedList<T> list, LinkedListIterator<T> where)
        {
            Verify.ArgumentNotNull(where, "where");
            if (!list.Any())
                throw new ContainerEmptyException();
            if (Equals(where, End(list)))
                throw new ArgumentException("The passed in Iterator is the end operator!");

            LinkedListNode<T> whereNode = where.Node;
            ++where;
            list.Remove(whereNode);
            return new LinkedListIterator<T>(list, where.Node);
        }

        /// <summary>
        /// Creates an <see cref="IOutputIterator{T}"/> implementation
        /// that lets you insert values at the specified position.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="insertable"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static IOutputIterator<T> InsertIterator<T>(this LinkedList<T> insertable, LinkedListIterator<T> where)
        {
            return new InsertIterator<T, LinkedListIterator<T>>(new LinkedListInsertableAdapater<T>(insertable), where);
        }
        private class LinkedListInsertableAdapater<T> : IInsertable<T, LinkedListIterator<T>>
        {
            private readonly LinkedList<T> list;
            public LinkedListInsertableAdapater(LinkedList<T> list)
            {
                this.list = list;
            }
            public LinkedListIterator<T> Insert(LinkedListIterator<T> where, T val)
            {
                return LinkedListExtension.Insert(list, where, val);
            }
        }

        /// <summary>
        /// Deletes the element at the front of a list.
        /// </summary>
        public static void PopFirst<T>(this LinkedList<T> list)
        {
            Remove(list, Begin(list));
        }
        /// <summary>
        /// Deletes the element at the end of a list.
        /// </summary>
        public static void PopLast<T>(this LinkedList<T> list)
        {
            LinkedListIterator<T> end = End(list);
            Remove(list, (LinkedListIterator<T>)end.PreDecrement());
        }
        /// <summary>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation that adds
        /// values to the end of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IOutputIterator<T> AddLastInserter<T>(this LinkedList<T> list)
        {
            return new BackInsertableBackInsertIterator<T>(list);
        }
        /// <summary>
        /// Returns an <see cref="IOutputIterator{T}"/> implementation that adds
        /// values to the start of the sequence.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static IOutputIterator<T> AddFirstInserter<T>(this LinkedList<T> list)
        {
            return new FrontInsertIterator<T>(list);
        }
    }
}

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
using System.Diagnostics;
using System.Linq;
using NStl.Debugging;
using NStl.Exceptions;
using NStl.Iterators;
using NStl.Iterators.Support;
using System.Runtime.Serialization;
using NStl.Linq;
using NStl.SyntaxHelper;
using NStl.Util;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// A DList is a doubly linked list. That is, it is a Sequence that supports both 
    /// forward and backward traversal, and (amortized) constant time insertion and 
    /// removal of elements at the beginning or the end, or in the middle. lists have 
    /// the important property that insertion and splicing do not invalidate iterators 
    /// to list elements, and that even removal invalidates only the iterators that 
    /// point to the elements that are removed. The ordering of iterators may be changed 
    /// (that is, bidirectional_iteratormight have a different predecessor or successor 
    /// after a list operation than it did before), but the iterators themselves will 
    /// not be invalidated or made to point to different elements unless that 
    /// invalidation or mutation is explicit. Note that singly linked lists, 
    /// which only support forward traversal, are also sometimes useful. If you do not 
    /// need backward traversal, then slist may be more efficient than list. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// .NET 2.0 ships with a <see cref="LinkedList{T}"/> that has the same functionality 
    /// as this class. However, as the <see cref="LinkedList{T}"/> has to be used with the
    /// <see cref="NStlUtil"/> adapter functions, this class was left inside the NSTL, as 
    /// it provides integrated <see cref="IBidirectionalIterator{T}"/> implementation and
    /// is more "handy" to use with NSTL algorithms.
    /// </para>
    /// <para>
    /// As soon as extension methods will be available in C# and VB and allow to add the Begin() and End() 
    /// methods to existing classes, this collection will be obsolete.
    /// </para>
    /// </remarks>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    [Obsolete("Use LinkedList<T> and the extension methods in the NStl.Linq namespace instead!")]
    public class DList<T> : IBackInsertableCollection<T>, IInsertable<T, DList<T>.Iterator>, IFrontInsertable<T>, IRange<T>, ISerializable, ICollection
    {
        #region Node
        [Serializable]
        internal class ListNode : ISerializable// internal on purpose, because iterator is public!!
        {
            public ListNode(T val)
            {
                this.val = val;
            }
            public ListNode Next
            {
                get { return next; }
                set { next = value; }
            }
            public ListNode Prev
            {
                get { return prev; }
                set { prev = value; }
            }
            public T Value
            {
                get { return val; }
                set { val = value; }
            }
            private ListNode next;
            private ListNode prev;
            private T val;
            private const int Version = 1;

            #region ISerializable Members
            private static class SerializationKey
            {
                internal const string Version = "Version";
                internal const string Next = "Next";
                internal const string Previous = "Prev";
                internal const string Value = "Value";
            }
            protected ListNode(SerializationInfo info, StreamingContext ctxt)
            {
                int version = info.GetInt32(SerializationKey.Version);
                Verify.VersionsAreEqual(version, Version);

                next = (ListNode)info.GetValue(SerializationKey.Next, typeof(ListNode));
                prev = (ListNode)info.GetValue(SerializationKey.Previous, typeof(ListNode));
                val = (T)info.GetValue(SerializationKey.Value, typeof(T));
            }
            /// <summary>
            /// See <see cref="ISerializable.GetObjectData"/> for details.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
            [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
            public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(SerializationKey.Version, Version);
                info.AddValue(SerializationKey.Next, next);
                info.AddValue(SerializationKey.Previous, prev);
                info.AddValue(SerializationKey.Value, val);
            }

            #endregion
        }
        #endregion
        #region iterator
        /// <summary>
        /// The itertor of the list
        /// </summary>
        public class Iterator : BidirectionalIterator<T>
        {
            internal Iterator(ListNode node)
            {
                this.node = node;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override IBidirectionalIterator<T> PreDecrement()
            {
                node = node.Prev;
                return this;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override IForwardIterator<T> PreIncrement()
            {
                node = node.Next;
                return this;
            }
            /// <summary>
            /// 
            /// </summary>
            public override T Value
            {
                get{ return node.Value; }
                set{ node.Value = value;}
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override IIterator<T> Clone()
            {
                return new Iterator(node);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            protected override bool Equals(EquatableIterator<T> obj)
            {
                Iterator rhs = obj as Iterator;
                if (rhs == null)
                    return false;
                return rhs.node == node;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            protected override int HashCode()
            {
                return node.GetHashCode();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
            public static Iterator operator ++(Iterator it)
            {
                Iterator tmp = (Iterator)it.Clone();
                return (Iterator)tmp.PreIncrement();
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Post/PreDecrement() already exist")]
            public static Iterator operator --(Iterator it)
            {
                Iterator tmp = (Iterator)it.Clone();
                return (Iterator)tmp.PreDecrement();
            }
            internal ListNode Node
            {
                get { return node; }
            }
            private ListNode node;
        }
        #endregion
        /// <summary>
        /// Constructs an empty container.
        /// </summary>
        public DList()
        {
            Init();
        }
        /// <summary>
        /// Construct the container and copies the content of the <see cref="IEnumerable{T}"/>
        /// into it.
        /// </summary>
        /// <param name="list"></param>
        public DList(IEnumerable<T> list)
            : this(list.Begin(), list.End())
        { }
        /// <summary>
        /// Construct the container and copies the content of the <see cref="IEnumerable"/>
        /// into it.
        /// </summary>
        /// <param name="list"></param>
        public DList(IEnumerable list)
            : this(list.Cast<T>())
        { }
        /// <summary>
        /// Constructs the container and  copies the given parameters into it.
        /// </summary>
        public DList(params T[] list)
            : this((IList<T>)list)
        { }
        /// <summary>
        /// Constructs the container and  copies the given range into it.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public DList(IInputIterator<T> first, IInputIterator<T> last)
        {
            Init();
            Algorithm.Copy(first, last, this.AddInserter());
        }
        /// <summary>
        /// Creates thec container with the given length.
        /// </summary>
        /// <param name="count"></param>
        public DList(int count)
            : this(count, default(T))
        { }
        /// <summary>
        /// Creates the container of the given length, filling it with the passed in object
        /// </summary>
        /// <param name="count"></param>
        /// <param name="val"></param>
        public DList(int count, T val)
        {
            Init();
            for (int i = 0; i < count; ++i)
                PushBack(val);
        }

        /// <summary>
        /// Returns an iterator addressing the first element in a list
        /// </summary>
        /// <returns></returns>
        public Iterator Begin()
        {
            return new Iterator(nodes.Next);
        }
        /// <summary>
        /// Returns an iterator that addresses the location succeeding the last element in a list
        /// </summary>
        /// <returns></returns>
        public Iterator End()
        {
            return new Iterator(nodes);
        }/// <summary>
        /// Returns an iterator addressing the first element in a reversed list
        /// </summary>
        /// <returns></returns>
        public IBidirectionalIterator<T> RBegin()
        {
            return NStlUtil.Reverse(End());
        }
        /// <summary>
        /// Returns an iterator that addresses the location succeeding the last element in a reversed list
        /// </summary>
        /// <returns></returns>
        public IBidirectionalIterator<T> REnd()
        {
            return NStlUtil.Reverse(Begin());
        }
        /// <summary>
        /// Inserts an element into the list at a specified position.
        /// </summary>
        /// <param name="where">
        /// The position in the target list where the first element is inserted.
        /// </param>
        /// <param name="val">The value of the element being inserted into the list.</param>
        /// <returns>
        /// The function returns an iterator that points to the position 
        /// where the new element was inserted into the list.
        /// </returns>
        public Iterator
            Insert(Iterator where, T val)
        {
            ListNode current = where.Node;
            ListNode prev = current.Prev;
            ListNode newNode = new ListNode(val);

            prev.Next = newNode;
            newNode.Prev = prev;

            newNode.Next = current;
            current.Prev = newNode;
            return new Iterator(newNode);
        }
        /// <summary>
        /// The size of the list
        /// </summary>
        /// <returns></returns>
        /// <remarks>As this function will run through the whole list and count all elements,
        /// this can be a performance intensive operation for a large list. So if you
        /// just want to know whether the list is empty, use the list.empty member function
        /// which has a constant time performace</remarks>
        public int Count
        {
            get { return NStlUtil.Distance(Begin(), End()); }
        }
        /// <summary>
        /// Specifies a new size for a list
        /// </summary>
        /// <param name="newSize">The new size of the list</param>
        /// <param name="val">
        /// The value of the new elements to be added to the list if the new size is 
        /// larger that the original size
        /// </param>
        public void
            Resize(int newSize, T val)
        {
            Iterator iter = Begin();
            Iterator end = End();
            while(newSize > 0)
            {
                if(Equals(iter, end))
                    iter = Insert(iter, val);

                iter.PreIncrement();
                --newSize;
            }
            if(!Equals(iter, end))
                Erase(iter, end);

            //more efficient, if there would be a constant time Size property:

            //int size = Size;
            //if (newSize == size)
            //    return;
            //else if (newSize > size)
            //{
            //    newSize = newSize - size;
            //    for (int i = 0; i < newSize; ++i)
            //        PushBack(val);

            //}
            //else
            //{
            //    Iterator it = NStlUtil.Advance<T, Iterator>(Begin(), newSize);
            //    Erase(it, End());
            //}
        }
        /// <summary>
        /// Specifies a new size for a list
        /// </summary>
        /// <param name="newSize">The new size of the list</param>
        public void
            Resize(int newSize)
        {
            Resize(newSize, default(T));
        }
        /// <summary>
        /// Inserts a range of element into the list at a specified position
        /// </summary>
        /// <param name="where">
        /// The position in the target list where the first element is inserted
        /// </param>
        /// <param name="first">
        /// The position of the first element in the range of elements in the argument 
        /// list to be copied
        /// </param>
        /// <param name="last">
        /// The position of the first element beyond the range of elements in the 
        /// argument list to be copied
        /// </param>
        public void
            Insert(Iterator where, IInputIterator<T> first, IInputIterator<T> last)
        {
            first = (IInputIterator<T>)first.Clone();
            for ( ; !Equals(first, last); first.PreIncrement())
                Insert(where, first.Value);
        }

        /// <summary>
        /// Inserts an n element into the list at a specified position
        /// </summary>
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
        public Iterator
            Insert(Iterator where, int count, T val)
        {
            Iterator ret = null;
            while (count > 0)
            {
                ret = Insert(where, val);
                --count;
            }
            return ret;
        }
        /// <summary>
        /// Test if the list is empty.
        /// </summary>
        /// <returns>TRUE, if the list is empty.</returns>
        public bool Empty
        {
            get
            {
                return nodes.Next == nodes
                       && nodes.Prev == nodes;
            }
        }
        /// <summary>
        /// Erases elements from a list and copies a new set of elements to a target list
        /// </summary>
        /// <param name="first">
        /// Position of the first element in the range of elements to be copied from 
        /// the argument range
        /// </param>
        /// <param name="last">
        /// Position of the first element just beyond the range of elements to be copied 
        /// from the argument range
        /// </param>
        public void
            Assign(IInputIterator<T> first, IInputIterator<T> last)
        {
            Clear();
            first = (IInputIterator<T>) first.Clone();
            for (; !Equals(first, last); first.PreIncrement())
                PushBack(first.Value);
        }
        /// <summary>
        /// Erases elements from a list and copies a new set of elements to a target list
        /// </summary>
        /// <param name="count">The number of copies of an element being inserted into the list</param>
        /// <param name="val">The value of the element being inserted into the list.</param>
        public void
            Assign(int count, T val)
        {
            DeInit();//throw away the old stuff
            Init();
            while (count > 0)
            {
                Insert(End(), val);
                --count;
            }
        }
        /// <summary>
        /// Adds an element to the end of a list
        /// </summary>
        /// <param name="val">The element added to the end of the list</param>
        public void PushBack(T val)
        {
            Insert(End(), val);
        }
        /// <summary>
        /// Adds an element to the beginning of a list
        /// </summary>
        /// <param name="val">The element added to the beginning of the list</param>
        public void PushFront(T val)
        {
            Insert(Begin(), val);
        }
        /// <summary>
        /// Returns a reference to the last element of a list
        /// </summary>
        /// <returns>The last element of the list. If the list is empty, the 
        /// return value is undefined</returns>
        public T
            Back()
        {
            if (Empty)
                throw new ContainerEmptyException();
            IBidirectionalIterator<T>it = End();
            it.PreDecrement();
            return it.Value;
        }
        /// <summary>
        /// Returns a reference to the first element of a list
        /// </summary>
        /// <returns>The first element of the list. If the list is empty, the 
        /// return value is undefined</returns>
        public T
            Front()
        {
            if (Empty)
                throw new ContainerEmptyException();
            return Begin().Value;
        }
        /// <summary>
        /// Erases all the elements of a list
        /// </summary>
        public void
            Clear()
        {
            DeInit();
            Init();
        }
        /// <summary>
        /// Removes a range of elements in a list from specified positions
        /// </summary>
        /// <param name="first">Position of the first element removed from the list</param>
        /// <param name="last">Position just beyond the last element removed from the list</param>
        /// <returns>
        /// A bidirectional iterator that designates the first element remaining beyond any 
        /// elements removed, or a pointer to the end of the list if no such element exists
        /// </returns>
        public Iterator
            Erase(Iterator first, Iterator last)
        {
            ListNode preFirst = first.Node.Prev;
            ListNode lastNode = last.Node;

            preFirst.Next = lastNode;
            lastNode.Prev = preFirst;
            return new Iterator(lastNode);
        }
        /// <summary>
        /// Removes an element in a list from specified positions
        /// </summary>
        /// <param name="where">Position of the element to be removed from the list</param>
        /// <returns>
        /// A bidirectional iterator that designates the first element remaining beyond any 
        /// elements removed, or a pointer to the end of the list if no such element exists
        /// </returns>
        /// <exception cref="ContainerEmptyException">Thrown when the DList is empty.</exception>
        public Iterator
            Erase(Iterator where)
        {
            if (Empty)
                throw new ContainerEmptyException();
            if(Equals(where, End()))
                throw new ArgumentException("The passed in Iterator is the end operator!");
            ListNode preFirst = where.Node.Prev;
            Iterator wh = (Iterator)where.Clone();
            ListNode last = ((Iterator)(wh.PreIncrement())).Node;

            preFirst.Next = last;
            last.Prev = preFirst;
            return new Iterator(last);
        }
        /// <summary>
        /// Deletes the element at the front of a list.
        /// </summary>
        public void PopFront()
        {
            Erase(Begin());
        }
        /// <summary>
        /// Deletes the element at the end of a list.
        /// </summary>
        public void
            PopBack()
        {
            Iterator end = End();
            Erase((Iterator)end.PreDecrement());
        }

        /// <summary>
        /// Removes elements from the argument list and inserts them into the target list.
        /// </summary>
        /// <param name="position">
        /// The position in the target list before which the elements of the argument list are to be inserted.
        /// </param>
        /// <param name="srcList">
        /// The argument list that is to be inserted into the target list
        /// </param>
        /// <param name="first">
        /// The first element in the range to be inserted from the argument list.
        /// </param>
        /// <param name="last">
        /// The first element beyond the range to be inserted from the argument list
        /// </param>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Scope = "member", Justification = "Compatibility with teh original signature of the C++ STL.")]
        public void
            Splice(Iterator position, DList<T> srcList, Iterator first, Iterator last)
        {
            if (!Equals(first, last))
                Transfer(position, first, last);
        }
        /// <summary>
        /// Removes elements from the argument list and inserts them into the target list.
        /// </summary>
        /// <param name="position">
        /// The position in the target list before which the elements of the argument list are to be inserted.
        /// </param>
        /// <param name="srcList">
        /// The argument list that is to be inserted into the target list
        /// </param>
        public void
            Splice(Iterator position, DList<T> srcList)
        {
            if (!srcList.Empty)
                Transfer(position, srcList.Begin(), srcList.End());
        }
        /// <summary>
        /// Removes the element from the argument list and inserts it into the target list.
        /// </summary>
        /// <param name="position">
        /// The position in the target list before which the elements of the argument list are to be inserted.
        /// </param>
        /// <param name="srcList">
        /// The argument list that is to be inserted into the target list
        /// </param>
        /// <param name="from">
        /// The element in the range to be inserted from the argument list.
        /// </param>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Scope = "member", Justification = "Compatibility with teh original signature of the C++ STL.")]
        public void
            Splice(Iterator position, DList<T> srcList, Iterator from)
        {
            Iterator j = (Iterator)from.Clone();
            j.PreIncrement();
            if (Equals(position, from) || Equals(position, j))
                return;
            Transfer(position, from, j);
        }
        /// <summary>
        /// Exchanges the elements of two lists.
        /// </summary>
        /// <param name="rhs"></param>
        public void
            Swap(DList<T> rhs)
        {
            Algorithm.Swap(ref nodes, ref rhs.nodes);
        }
        /// <summary>
        /// The content of this list copied to a native array.
        /// </summary>
        public T[] ToArray()
        {
            T[] tmp = new T[Count];
            ((ICollection)this).CopyTo(tmp, 0);
            return tmp;
        }
        private void
            Init()
        {
            nodes = new ListNode(default(T));
            nodes.Next = nodes;
            nodes.Prev = nodes;
        }
        private void
            DeInit()
        {
            if (nodes != null)
            {
                // ... break the ring, not sure if the GC needs it, but we will
                // ... free up the linked nodes
                ListNode last = nodes.Prev;
                nodes.Prev = null;
                last.Next = null;
                nodes = null;
            }
        }
        private static void
            Transfer(Iterator position, Iterator first, Iterator last)
        {
            if (!Equals(position, last))
            {
                // Remove [first, last) from its old position.
                last.Node.Prev.Next = position.Node;
                first.Node.Prev.Next = last.Node;
                position.Node.Prev.Next = first.Node;

                // Splice [first, last) into its new position.
                ListNode tmp = position.Node.Prev;
                position.Node.Prev = last.Node.Prev;
                last.Node.Prev = first.Node.Prev;
                first.Node.Prev = tmp;
            }
        }
        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return NStlUtil.Enumerator(Begin(), End());
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return NStlUtil.Enumerator(Begin(), End());
        }

        #endregion
        #region IInsertable<T> Members

        Iterator IInsertable<T, Iterator>.Insert(Iterator where, T val)
        {
            return Insert(where, val);
        }

        #endregion
        #region IRange<T> Members

        IInputIterator<T> IRange<T>.Begin()
        {
            return Begin();
        }

        IInputIterator<T> IRange<T>.End()
        {
            return End();
        }

        #endregion
        #region IBackInsertable<T> Members

        void IBackInsertable<T>.PushBack(T val)
        {
            PushBack(val);
        }

        #endregion
        #region IDeque<T> Members

        void IFrontInsertable<T>.PushFront(T val)
        {
            PushFront(val);
        }

        #endregion
        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            foreach (T t in this)
                array.SetValue(t, index++);
        }

        int ICollection.Count
        {
            get { return Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        #endregion
        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            PushBack(item);
        }

        void ICollection<T>.Clear()
        {
            Clear();
        }

        bool ICollection<T>.Contains(T item)
        {
            return !Algorithm.FindIf(Begin(), End(), Bind.Second(Compare.EqualTo<T>(), item)).Equals(End());
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            Algorithm.Copy(Begin(), End(), array.Begin() + arrayIndex);
        }

        int ICollection<T>.Count
        {
            get { return Count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<T>.Remove(T item)
        {
            Iterator found =
                Algorithm.FindIf(Begin(), End(), Bind.Second(Compare.EqualTo<T>(), item));

            if(found.Equals(End()))
                return false;

            Erase(found);
            return true;
        }

        #endregion
        #region ISerializable Members

        private const int Version = 1;
        /// <summary>
        /// Desirialization constuctor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected DList(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);
            nodes = (ListNode)info.GetValue("Nodes", typeof(ListNode));
        }
        /// <summary>
        /// See <see cref="ISerializable.GetObjectData"/> for details.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Version", Version);
            info.AddValue("Nodes", nodes);
        }

        #endregion
        private ListNode nodes;
    }
}

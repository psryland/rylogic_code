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
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using NStl.Debugging;
using NStl.Exceptions;
using NStl.Iterators;
using NStl.Iterators.Private;
using NStl.Iterators.Support;
using System.Runtime.Serialization;
using NStl.Linq;
using NStl.Util;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// A vector is a sequence that supports random access to elements, constant time 
    /// insertion and removal of elements at the end, and linear time insertion and 
    /// removal of elements at the beginning or in the middle. The number of elements 
    /// in a vector may vary dynamically; memory management is automatic. Vector is 
    /// the simplest of the NSTL's container classes, and in many cases the most efficient.
    /// </summary>
    /// <remarks>
    /// The vector is very similar to <see cref="List{T}"/>. It addditionally features more possibilities 
    /// to query and regulate the size and allocated storage of the container. If these features are not
    /// needed, <see cref="List{T}"/> should be preferred.
    /// </remarks>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    [Obsolete("Use List<T> and the extension methods in the NStl.Linq namespace instead!")]
    public class Vector<T> : IList<T>, IBackInsertable<T>, IInsertable<T, Vector<T>.Iterator>, IRange<T>, ISerializable, ICollection
    {
        #region IList<T> Members

        int IList<T>.IndexOf(T item)
        {
            for (int i = 0; i < end; ++i)
                if (Equals(buffer[i], item))
                    return i;
            return -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            Iterator where = CreateIterator(index);
            Insert(where, item);
        }

        void IList<T>.RemoveAt(int index)
        {
            Iterator where = CreateIterator(index);
            Erase(where);
        }

        T IList<T>.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = value;
            }
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
            get { return buffer.SyncRoot; }
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
            return ((IList<T>)this).IndexOf(item) >= 0;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < end; ++i)
                array[arrayIndex + i] = buffer[i];
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
            int index = ((IList<T>)this).IndexOf(item);
            if (index < 0)
                return false;
            Iterator where = CreateIterator(index);
            Erase(where);
            return true;
        }

        #endregion
        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return NStlUtil.Enumerator(Begin(), End());
        }

        #endregion
        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return NStlUtil.Enumerator(Begin(), End());
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
        #region IInsertable<T> Members

        Iterator IInsertable<T, Iterator>.Insert(Iterator where, T val)
        {
            return Insert(where, val);
        }

        #endregion
        #region IBackInsertable<T> Members

        void IBackInsertable<T>.PushBack(T val)
        {
            PushBack(val);
        }

        #endregion
        #region Iterator
        /// <summary>
        /// The iterator of the vector container.
        /// </summary>
        public sealed class Iterator : ListIteratorBase<IList<T>, T>
        {
            internal Iterator(IList<T> list, int idx)
                : base(list, idx)
            { }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override IIterator<T> Clone()
            {
                return new Iterator(List, Index);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <param name="rhs"></param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Method Plus is already exist!")]
            public static Iterator operator -(Iterator it, int rhs)
            {
                return (Iterator)it.Add(-rhs);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="lhs"></param>
            /// <param name="rhs"></param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Method Plus is already exist!")]
            public static Iterator operator +(Iterator lhs, int rhs)
            {
                return (Iterator)lhs.Add(rhs);
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
        }
        #endregion
        #region ISerializable Members
        private const int Version = 1;
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
            info.AddValue("Buffer", buffer);
            info.AddValue("End", end);
        }
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected Vector(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            buffer = (T[])info.GetValue("Buffer", typeof(T[]));
            end    = (int)info.GetValue("End", typeof(int));
        }
        #endregion
        
        /// <summary>
        /// ctor
        /// </summary>
        public Vector()
        { buffer = new T[min_capacity_]; }
        /// <summary>
        /// ctor, initializes the vector with count "null" entries
        /// </summary>
        /// <param name="count"></param>
        public Vector(int count)
        {
            buffer = new T[min_capacity_ < count ? count : min_capacity_];
            Resize(count);
        }
        
        /// <summary>
        /// ctor, copies the given list into this vector.
        /// </summary>
        /// <param name="list"></param>
        public Vector(IEnumerable list)
            : this()
        {
            foreach (T t in list)
                PushBack(t);
        }

        /// <summary>
        /// ctor, copies the given parameters into this vector
        /// </summary>
        public Vector(params T[] list)
            : this((IEnumerable<T>)list)
        { }
        /// <summary>
        /// Ctor. Copies the given range into this vector
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public Vector(IInputIterator<T> first, IInputIterator<T> last)
            : this(NStlUtil.Enumerable(first, last))
        {}
        /// <summary>
        /// Returns a random-access iterator to the first element in the container.
        /// </summary>
        /// <returns></returns>
        public Iterator Begin()
        { return CreateIterator(0); }
        /// <summary>
        /// Returns a random-access iterator that points just beyond the end of the vector.
        /// </summary>
        /// <returns></returns>
        public Iterator End()
        { return CreateIterator(Count); }
        /// <summary>
        /// Returns a reverse-random-access iterator to the last element in the container.
        /// </summary>
        /// <returns></returns>
        public IRandomAccessIterator<T> RBegin()
        { return new RandomAccessReverseIterator<T>(End()); }
        /// <summary>
        /// Returns a reverse-random-access iterator that points just before the begin of the vector.
        /// </summary>
        /// <returns></returns>
        public IRandomAccessIterator<T> REnd()
        { return new RandomAccessReverseIterator<T>(Begin()); }

        /// <summary>
        /// Returns the number of elements in the vector.
        /// </summary>
        /// <returns></returns>
        public int Count
        {
            get { return end; }
        }
        /// <summary>
        /// Copies the content of this vector to a native array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] ts = new T[Count];
            ((ICollection) this).CopyTo(ts, 0);
            return ts;
        }
        /// <summary>
        /// Returns a reference to the vector element at a specified position. 
        /// Fast uchecked access.
        /// </summary>
        public T this[int idx]
        {
            get 
            {
                CheckIndex(idx);
                return buffer[idx]; 
            }
            set
            {
                CheckIndex(idx);
                buffer[idx] = value;
            }
        }
        private void CheckIndex(int idx)
        {
            Verify.IndexInRange(idx, 0, Count - 1, "idx");
        }
        /// <summary>
        /// Returns a reference to the last element of the vector.
        /// </summary>
        /// <returns>
        /// Returns a reference to the last element of the vector.
        /// </returns>
        /// <remarks>
        /// If the vector is empty, this method will cause undefined behavior. 
        /// Under .NET this means an Exception ;-)
        /// </remarks>
        public T Back()
        {
            if (Empty)
                throw new ContainerEmptyException();
            return buffer[Count - 1]; 
        }
        /// <summary>
        /// Returns a reference to the first element in a vector.
        /// </summary>
        /// <returns>Returns a reference to the first element in a vector.</returns>
        /// <remarks>
        /// If the vector is empty, this method will cause undefined behavior.
        /// Under .NET this means an Exception ;-)</remarks>
        public T Front()
        {
            if (Empty)
                throw new ContainerEmptyException();
            return buffer[0]; 
        }
        /// <summary>
        /// Returns the number of elements that the vector could 
        /// contain without allocating more storage.
        /// </summary>
        /// <returns></returns>
        public int Capacity()
        { return buffer.Length; }
        /// <summary>
        /// Specifies a new size for a vector.
        /// </summary>
        /// <param name="newSize">The new size of the vector</param>
        /// <param name="val">
        /// The value of new elements added to the vector if the new size is larger that 
        /// the original size
        /// </param>
        /// <remarks>
        /// If newSize is smaller that the current size, the elements that are too many 
        /// are erased from the vector
        /// </remarks>
        public void Resize(int newSize, T val)
        {
            int oldsize = Count;

            // ... make sure that the vector has enough capacity
            EnsureSize(newSize);

            // ... copy the values into the end of the vector
            for (; oldsize < newSize; ++oldsize)
                PushBack(val);
        }
        /// <summary>
        /// Specifies a new size for a vector.
        /// </summary>
        /// <param name="newSize">The new size of the vector</param>
        public void Resize(int newSize)
        {
            Resize(newSize, default(T));
        }
        /// <summary>
        /// Erases the elements of the vector.
        /// </summary>
        public void Clear()
        {
            ClearRange(buffer, 0, buffer.Length);
            end = 0;
        }
        private static void ClearRange(T[] array, int from, int size)
        {
            int e = from + size;
            for (; from < e; ++from)
            {
                array[from] = default(T);
            }
        }

        /// <summary>
        /// Tests if the vector is empty.
        /// </summary>
        /// <returns></returns>
        public bool Empty
        {
            get { return Count == 0; }
        }

        /// <summary>
        /// Removes an a range of elements in a vector from specified positions.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns>
        /// An iterator that designates the first element remaining 
        /// beyond any elements removed, or a pointer to the end of 
        /// the Vector if no such element exists.
        /// </returns>
        public Iterator
            Erase(Iterator first, Iterator last)
        { 
            Debug.Assert(first.List == buffer);

            // ... copy from last to this.end() to first
            int dist = NStlUtil.Distance(first, last);
            Algorithm.Copy(last, End(), first);
            end -= dist;
            ClearRange(buffer, end, buffer.Length - end);
            return (Iterator)first.Clone();
        }
        /// <summary>
        /// Removes an an element in a vector from a specified position.
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public Iterator
            Erase(Iterator where)
        {
            Iterator endOfErase = (Iterator)where.Clone();
            endOfErase.PreIncrement();
            return Erase(where, endOfErase);
        }
        /// <summary>
        /// Inserts an element of elements of elements into the 
        /// vector at a specified position.
        /// </summary>
        /// <param name="where"></param>
        /// <param name="val"></param>
        /// <returns>
        /// The first insert function returns an iterator that points 
        /// to the position where the new element was inserted into the vector.
        /// </returns>
        public Iterator
            Insert(Iterator where, T val)
        {
            T[] tmp = new T[] { val };
            Insert(where, tmp.Begin(), tmp.End());
            return (Iterator)where.Clone();
        }
        /// <summary>
        /// Inserts a range  of elements into the vector at a specified position.
        /// </summary>
        /// <param name="where"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public void Insert(Iterator where, IForwardIterator<T> first, IForwardIterator<T> last)
        {
            Iterator currentEndIt = End();

            // ... copy everything behind where into a temp buffer
            T[] tmpBuffer  = CreateBuffer(where, currentEndIt);

            // ... make sure that we have enough storage
            int dist = NStlUtil.Distance(first, last);
            Reserve(Count + dist);

            // ... at this point, a reallocation might have happened, so where is 
            // ... invalidated. However, its index is still valid, so we simply recreate it
            where = CreateIterator(where);

            // ... insert
            where = Algorithm.Copy(first, last, where);

            // ... copy the rest back
            Algorithm.Copy(tmpBuffer.Begin(), tmpBuffer.End(), where);

            // ... as we got longer, we have to adjust the end
            end += dist;
        }
        private static T[]
            CreateBuffer(Iterator first, Iterator last)
        {
            int dist = NStlUtil.Distance(first, last);
            T[] buffer = new T[dist];
            Algorithm.Copy(first, last, buffer.Begin());
            return buffer;
        }
        /// <summary>
        /// Inserts a number of elements into the vector at a specified position.
        /// </summary>
        /// <param name="where">The position in the vector where the first element is inserted</param>
        /// <param name="count">The number of elements being inserted into the vector</param>
        /// <param name="val">The value of the element being inserted into the vector</param>
        public void Insert(Iterator where, int count, T val)
        {
            T[] tmp = new T[count];
            for (int i = 0; i < count; ++i)
                tmp[i] = val;
            Insert(where, tmp.Begin(), tmp.End());
        }
        /// <summary>
        /// Deletes the element at the end of the vector.
        /// </summary>
        /// <remarks>The vector must not be empty!</remarks>
        public void PopBack()
        {
            if (Empty)
                throw new ContainerEmptyException();

            ClearRange(buffer, end-1, 1);
            end -= 1;
        }
        /// <summary>
        /// Adds an element to the end of the vector.
        /// </summary>
        /// <param name="val"></param>
        public void PushBack(T val)
        {
            // ... make sure we have enough storage
            Reserve(Count + 1);

            // ... insert the element at the first free position
            buffer[end++] = val;
        }
        /// <summary>
        /// Adds a range of elements to the endo of this vector.
        /// </summary>
        /// <param name="vals"></param>
        public void PushBackRange(IEnumerable<T> vals)
        {
            foreach(T t in vals)
                PushBack(t);
        }
        /// <summary>
        /// Reserves a minimum length of storage for a vector object, allocating space if necessary.
        /// </summary>
        /// <param name="newSize"></param>
        /// <remarks>If the new size is les than the current size, nothing will happen</remarks>
        public void Reserve(int newSize)
        {
            if (newSize > Capacity())
                EnsureSize(newSize);
        }
        /// <summary>
        /// Exchanges the elements of two vectors.
        /// </summary>
        /// <param name="right"></param>
        /// <remarks>This is an extremely cheap operation, as it is just a buffer swap!!</remarks>
        public void Swap(Vector<T> right)
        {
            Algorithm.Swap(ref buffer, ref right.buffer);
            Algorithm.Swap(ref end, ref right.end);
        }

        /// <summary>
        /// private helper, to create an iterator
        /// </summary>
        /// <param name="atIdx"></param>
        /// <returns></returns>
        private Iterator
            CreateIterator(int atIdx)
        { return new Iterator(buffer, atIdx); }
        /// <summary>
        /// private helper, to create an iterator
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        private Iterator
            CreateIterator(Iterator it)
        {
            return new Iterator(buffer, it.Index);
        }
        /// <summary>
        /// Enlarges or shrinks the undelying buffer
        /// </summary>
        /// <param name="requestedSize"></param>
        private void EnsureSize(int requestedSize)
        {
            int curLen = buffer.Length;

            // ... check if we have to grow phyiscally
            if (curLen < requestedSize && requestedSize >= min_capacity_)
            {
                // ... we need more space. enlarge capacity by the specific factor
                T[] newBuffer = new T[grow_factor_ * Capacity()];

                // ... copy the content in the new buffer
                Algorithm.Copy(Begin(), End(), newBuffer.Begin());

                // ... swap the buffer in, end stays where it was
                buffer = newBuffer;
            }
            else if (curLen > requestedSize && requestedSize >= min_capacity_)
            {
                // ... we need to shrink physically , but only if requestedSize >= vector.min_capacity_
                T[] newBuffer = new T[requestedSize];
                // ... copy the content in the new buffer
                Algorithm.Copy(Begin(), Begin().Add(requestedSize - 1), newBuffer.Begin());
                // ... swap the buffer in
                buffer = newBuffer;

                end = requestedSize;
            }
            else
            {// NoOp
                Debug.Assert(curLen == requestedSize || requestedSize < min_capacity_);
            }

            // ... At this point, he buffer was physically adjusted, now we have to adjust
            // ... the logical length
            end = end > requestedSize ? requestedSize : end;
            ClearRange(buffer, end, buffer.Length - end);
        }
        internal T[] Buffer { get { return buffer;  } }
        private T[] buffer;
        private int end = 0;
        private const int min_capacity_ = 16;
        private const int grow_factor_ = 2;
    }
}

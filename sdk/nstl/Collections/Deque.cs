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
using NStl.Collections.Private;
using NStl.Iterators;
using NStl.Iterators.Support;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using NStl.Debugging;
using NStl.Exceptions;
using System.Runtime.Serialization;
using NStl.Linq;
using NStl.Util;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// <para>
    /// A deque is very much like a <see cref="Vector{T}"/> or <see cref="List{T}"/>: 
    /// like vector, it is a sequence that supports random access to elements, 
    /// constant time insertion and removal of elements at the end of the sequence, 
    /// and linear time insertion and removal of elements in the middle.
    /// </para>
    /// <para>
    /// The main way in which deque differs from vector is that deque also supports 
    /// constant time insertion and removal of elements at the beginning of the sequence. 
    /// Additionally, deque does not have any member functions analogous to vector's Capacity() 
    /// and Reserve(), and does not provide any of the guarantees on iterator validity that are 
    /// associated with those member functions. 
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// The name Deque is pronounced "deck", and stands for "double-ended queue." Knuth (section 2.6) 
    /// reports that the name was coined by E. J. Schweppe. See section 2.2.1 of Knuth for more 
    /// information about deques. (D. E. Knuth, The Art of Computer Programming. Volume 1: Fundamental 
    /// Algorithms, second edition. Addison-Wesley, 1973.)
    /// </remarks>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public class Deque<T> : ICollection, IList<T>, IFrontInsertable<T>, IInsertable<T, Deque<T>.Iterator>, IRange<T>, ISerializable
    {
        #region Iterator
        /// <summary>
        /// The iterator of a Deque{T}
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This scoping binds the iterator to its container and allows internal container access.")]
        public sealed class Iterator : RandomAccessIterator<T>
        {
            private readonly Deque<T> deque;
            private int currentPos;

            internal Iterator(Deque<T> deque, int pos)
            {
                this.deque = deque;
                currentPos = pos;
            }

            internal Deque<T> Deque
            {
                get { return deque;  }
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
                return ReferenceEquals(deque, rhs.deque) && Equals(CurrentPos, rhs.CurrentPos);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            protected override int HashCode()
            {
                return deque.GetHashCode() ^ CurrentPos.GetHashCode();
            }
            /// <summary>
            /// See base class for details.
            /// </summary>
            /// <param name="count"></param>
            /// <returns></returns>
            public override IRandomAccessIterator<T> Add(int count)
            {
                return new Iterator(deque, CurrentPos + count);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            //[SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Method Plus already avaliable")]
            public static Iterator operator +(Iterator it, int count)
            {
                return (Iterator)it.Add(count);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public static Iterator operator -(Iterator it, int count)
            {
                return (Iterator)it.Add(-count);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public static Iterator Subtract(Iterator it, int count)
            {
                return it - count;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="rhs"></param>
            /// <returns></returns>
            public override int Diff(IRandomAccessIterator<T> rhs)
            {
                return CurrentPos - ((Iterator) rhs).CurrentPos;
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="rhs"></param>
            /// <returns></returns>
            public override bool Less(IRandomAccessIterator<T> rhs)
            {
                return CurrentPos < ((Iterator) rhs).CurrentPos;
            }
            /// <summary>
            /// See base class for details.
            /// </summary>
            /// <returns></returns>
            public override IBidirectionalIterator<T> PreDecrement()
            {
                currentPos = CurrentPos - 1;
                return this;
            }
            /// <summary>
            /// See base class for details.
            /// </summary>
            /// <returns></returns>
            public override IForwardIterator<T> PreIncrement()
            {
                currentPos = CurrentPos + 1;
                return this;
            }
            /// <summary>
            /// See base class for details.
            /// </summary>
            public override T Value
            {
                get { return deque[CurrentPos]; }
                set { deque[CurrentPos] = value;}
            }

            internal int CurrentPos
            {
                get { return currentPos; }
            }
            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override IIterator<T> Clone()
            {
                return new Iterator(deque, CurrentPos);
            }
        }

        #endregion

        internal const float AllocFactor = 2.0f;
        private T[][] map;
        private Position begin;
        private Position end;

        /// <summary>
        /// Construct an empty deque.
        /// </summary>
        public Deque()
        {
            map    = new T[3][];
            map[1] = new T[Position.xMax];
            Clear();
        }
        /// <summary>
        /// Construct a deque andy copies the input range into this deque.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public Deque(IInputIterator<T> first, IInputIterator<T> last)
            : this(first.AsEnumerable(last))
        {}
        /// <summary>
        /// Construct a deque andy copies the input range into this deque.
        /// </summary>
        /// <param name="e"></param>
        public Deque(IEnumerable e)
            : this(e.Cast<T>())
        {}
        /// <summary>
        /// Construct a deque andy copies the input range into this deque.
        /// </summary>
        /// <param name="e"></param>
        public Deque(IEnumerable<T> e)
            : this()
        {
            foreach (T t in e)
                PushBack(t);
        }
        /// <summary>
        /// Construct a deque andy copies the input range into this deque.
        /// </summary>
        /// <param name="paramaters"></param>
        public Deque(params T[] paramaters)
            : this((IEnumerable<T>)paramaters)
        {}
        /// <summary>
        /// Construct a deque andy copies value "count" times into this deque.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="value"></param>
        public Deque(int count, T value)
            : this()
        {
            Resize(count, value);
        }
        /// <summary>
        ///  Construct a deque andy copies default(T) "count" times into this deque.
        /// </summary>
        /// <param name="count"></param>
        public Deque(int count)
            : this(count, default(T))
        {}
        /// <summary>
        /// Resizes the deque to contain count members.
        /// </summary>
        /// <param name="count"></param>
        /// <param name="value"></param>
        public void Resize(int count, T value)
        {
            if (count > Count)
            {
                count = count - Count;
                while(count-- > 0)
                    PushBack(value);
            }
            else if(count < Count)
            {
                count = Count - count;
                while(count-- > 0)
                    PopBack();
            }
        }
        /// <summary>
        /// Resizes the deque to contain count members.
        /// </summary>
        /// <param name="count"></param>
        public void Resize(int count)
        {
            Resize(count, default(T));
        }
        /// <summary>
        /// Returns and <see cref="Iterator"/> pointing to the first element of the deque.
        /// </summary>
        /// <returns></returns>
        public Iterator Begin()
        {
            return new Iterator(this, 0);
        }
        /// <summary>
        /// Returns and <see cref="Iterator"/> pointing one past the final element of the deque.
        /// </summary>
        /// <returns></returns>
        public Iterator End()
        {
            return new Iterator(this, Count);
        }
        /// <summary>
        /// Adds the given item to the end of the deque in constant time.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        public void PushBack(T item)
        {
            EnsureSizeFwd();
            this[end++] = item;
        }
        private void EnsureSizeFwd()
        {
            // if we have switched the chunk, alloc one if necessary
            if(end.X == 0)
            {
                // if the buffer map itself is empty, reallocate it
                EnsureMapBuffer();

                T[] nextChunk = map[end.Y];
                if(nextChunk == null)
                    map[end.Y] = new T[Position.xMax]; 
            }
        }
        private static int NewBufferLength(int oldLength)
        {
            int newLength = (int)(oldLength * AllocFactor);
            if (newLength % 2 == 0)
                ++newLength;
            return newLength < 0 ? int.MaxValue : newLength;
        }
        private void EnsureMapBuffer()
        {
            if (end.Y < map.Length && !(begin.X == 0 && begin.Y == 0))
                return;

            Enlarge();
        }
        private void Enlarge()
        {
            int newLength = NewBufferLength(map.Length);
            Enlarge(newLength);
        }

        private void Enlarge(int newLength)
        {
            int beginToEnd = end.Y - begin.Y;
            int mid = map.Length/2;
            int beginMid = begin.Y - mid;

            int newMid = newLength/2;
            Debug.Assert(newMid == mid * 2 + 1);


            T[][] tmp = new T[newLength][];
            map.CopyTo(tmp, newMid - mid);

            begin = new Position(begin.X, newMid + beginMid);
            end = new Position(end.X, begin.Y + beginToEnd);

            map = tmp;
        }
        private T this[Position pos]
        {
            get { return map[pos.Y][pos.X]; }
            set { map[pos.Y][pos.X] = value; }
        }
        /// <summary>
        /// Random indexed access.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                CheckIndex(index);
                return this[begin + index];
            }
            set
            {
                CheckIndex(index);
                this[begin + index] = value;
            }
        }

        private void CheckIndex(int index)
        {
            Verify.IndexInRange(index, 0, Count-1, "index");
        }
        /// <summary>
        /// Adds the given item at the front of the deque in constant time.
        /// </summary>
        /// <param name="item"></param>
        public void PushFront(T item)
        {
            EnsureSizeBckwrd();
            this[--begin] = item;
        }
        private void EnsureSizeBckwrd()
        {
            // if we have switched the chunk, alloc one if necessary
            if (begin.X == 0)
            {
                // if the buffer map itself is empty, reallocate it
                EnsureMapBuffer();

                int prevY = begin.Y - 1;
                T[] nextChunk = map[prevY];
                if (nextChunk == null)
                    map[prevY] = new T[Position.xMax];
            }
        }
        /// <summary>
        /// The amount of elements inside of the deque.
        /// </summary>
        public int Count
        {
            get { return end - begin; }
        }
        /// <summary>
        /// Clears the  content of the deque in constant time. The internal allocated memory 
        /// is not released.
        /// </summary>
        public void Clear()
        {
            ClearRange(0, Count);

            int halfMapLength = map.Length/2;
            begin = new Position(Position.xMax / 2, halfMapLength);
            end   = new Position(Position.xMax / 2, halfMapLength);
        }
        private void ClearRange(int start, int len)
        {
            int count = start + len;
            for (; start < count; ++start)
                this[start] = default(T);
        }
        /// <summary>
        /// Inserts T at the given position.
        /// </summary>
        /// <param name="where">The position where the insertion should occur.</param>
        /// <param name="item">The item to be inserted.</param>
        /// <returns>An iterator pointing to the position where the insertion occured.</returns>
        public  Iterator Insert(Iterator where, T item)
        {
            if(where == null)
                throw new ArgumentNullException("where");
            if (where.Deque != this)
                throw new InvalidOperationException("The provided iterator points to a different deque");
            PushBack(item);
            Algorithm.Rotate<T, Iterator>(where, End() - 1, End());
            return (Iterator)where.Clone();
        }
        /// <summary>
        /// Inserts a range at the given position.
        /// </summary>
        /// <param name="where">The position where the insertion should occur.</param>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> pointing to the first element of the range to be inserted.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> pointing one past the final element of the range to be inserted.
        /// </param>
        /// <returns>An iterator pointing to the position where the insertion occured.</returns>
        public Iterator Insert(Iterator where, IInputIterator<T> first, IInputIterator<T> last)
        {
            Iterator rv = (Iterator)where.Clone();
            for (first = (IInputIterator<T>)first.Clone(); !Equals(first, last); first.PreIncrement())
                Insert((Iterator)where.PostIncrement(), first.Value);
            return rv;
        }
        /// <summary>
        /// Inserts value count times at the given position.
        /// </summary>
        /// <param name="where"></param>
        /// <param name="count"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Iterator Insert(Iterator where, int count, T value)
        {
            Iterator rv = (Iterator)where.Clone();
            for (int i = 0; i < count; ++i)
                where = Insert(where, value);
            return rv;
        }
        /// <summary>
        /// Erases the element at the given position.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Iterator Erase(Iterator pos)
        {
            return Erase(pos, pos + 1);
        }
        /// <summary>
        /// Erases a range of elements from teh deque.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns></returns>
        public Iterator Erase(Iterator first, Iterator last)
        {
            Iterator endIt = End();
            int newEndIndex = first.CurrentPos + (endIt - last);
            Position newEnd = begin + newEndIndex;
            Algorithm.Rotate<T, Iterator>(first, last, endIt);

            ClearRange(newEndIndex, Count - newEndIndex);

            end = newEnd;

            return (Iterator)first.Clone();
        }
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
        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            foreach (T o in this)
                array.SetValue(o, index++);
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
        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Begin().AsEnumerable(End()).GetEnumerator();
        }

        #endregion
        #region IList<T> Members

        int IList<T>.IndexOf(T item)
        {
            Iterator it = Algorithm.Find(Begin(), End(), item);
            if (!Equals(it, End()))
                return it.CurrentPos;
            return -1;
        }

        void IList<T>.Insert(int index, T item)
        {
            Iterator where = (Begin() + index);
            Insert(where, item);
        }

        

        void IList<T>.RemoveAt(int index)
        {
            Iterator where = (Begin() + index);
            Erase(where);
        }

        T IList<T>.this[int index]
        {
            get { return this[index]; }
            set { this[index] = value;}
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
            Iterator it = Algorithm.Find(Begin(), End(), item);
            return !Equals(it, End());
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }
        /// <summary>
        /// Copies the content of the deque into the given array starting at arrayIndex.
        /// </summary>
        /// <param name="array">The target array</param>
        /// <param name="arrayIndex">The index in the target array at which copying starts.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (T o in this)
                array[arrayIndex++] = o;
        }
        /// <summary>
        /// Returns an array containing the content of this range.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] ts = new T[Count];
            CopyTo(ts, 0);
            return ts;
        }

        int ICollection<T>.Count
        {
            get { return Count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }
        /// <summary>
        /// TRUE, when the deque is empty. This is a constant time operation.
        /// </summary>
        public bool Empty
        {
            get { return begin.Equals(end); }
        }

        bool ICollection<T>.Remove(T item)
        {
            Iterator it = Algorithm.Find(Begin(), End(), item);
            bool rv;
            if(rv = !Equals(it, End()))
                Erase(it);
            return rv;
        }

        #endregion
        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Begin().AsEnumerable(End()).GetEnumerator();
        }

        #endregion
        #region IFrontInsertable<T> Members

        void IFrontInsertable<T>.PushFront(T val)
        {
            PushFront(val);
        }

        #endregion
        #region IInsertable<T> Members

        Iterator IInsertable<T, Iterator>.Insert(Iterator where, T val)
        {
            return Insert(where, val);
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
            info.AddValue("Map", map);
            info.AddValue("Begin", begin);
            info.AddValue("End", end);
        }
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected Deque(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            map = (T[][])info.GetValue("Map", typeof(T[][]));
            begin = (Position)info.GetValue("Begin", typeof(Position));
            end = (Position)info.GetValue("End", typeof(Position));
        }
        #endregion
        /// <summary>
        /// Returns and reverse iterator pointing to the last element of the deque.
        /// </summary>
        /// <returns></returns>
        public IRandomAccessIterator<T> RBegin()
        {
            return NStlUtil.Reverse(End());
        }
        /// <summary>
        /// Returns and reverse iterator pointing one before the first element of the deque.
        /// </summary>
        /// <returns></returns>
        public IRandomAccessIterator<T> REnd()
        {
            return NStlUtil.Reverse(Begin());
        }

        /// <summary>
        /// The first element of the deque.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ContainerEmptyException">Thrown when the deque is empty.</exception>
        public T Front()
        {
            if (Empty)
                throw new ContainerEmptyException();
            return this[begin];
        }
        /// <summary>
        /// The last element of the deque.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ContainerEmptyException">Thrown when the deque is empty.</exception>
        public T Back()
        {
            if (Empty)
                throw new ContainerEmptyException();
            return this[end - 1]; 
        }
        /// <summary>
        /// Removes the last element of the deque.
        /// </summary>
        /// <exception cref="ContainerEmptyException">Thrown when the deque is empty.</exception>
        public void PopBack()
        {
            if (Empty)
                throw new ContainerEmptyException();           
            --end;
            this[end] = default(T);
        }
        /// <summary>
        /// Removes the first element of the deque.
        /// </summary>
        /// <exception cref="ContainerEmptyException">Thrown when the deque is empty.</exception>
        public void PopFront()
        {
            if (Empty)
                throw new ContainerEmptyException();
            this[begin] = default(T);
            ++begin;
        }
        /// <summary>
        /// Swaps the the content of to deques in constant time.
        /// </summary>
        /// <param name="rhs"></param>
        public void Swap(Deque<T> rhs)
        {
            Algorithm.Swap(ref map, ref rhs.map);
            Algorithm.Swap(ref begin, ref rhs.begin);
            Algorithm.Swap(ref end, ref rhs.end);
        }
        /// <summary>
        /// Gets a linear copy of the
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<T> Buffer
        {
            get
            {
                for(int i = 0; i < map.Length; ++i)
                {
                    if(map[i] == null)
                        continue;
                    foreach(T t in map[i])
                        yield return t;
                }
            }
        }
    }
}

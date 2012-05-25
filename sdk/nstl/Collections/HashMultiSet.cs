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
using System.Linq;
using NStl.Collections.Private;
using NStl.Iterators;
using NStl.Iterators.Support;
using NStl.Linq;
using NStl.Util;
using NStl.Exceptions;
using NStl.Debugging;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NStl.SyntaxHelper;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// <para>
    /// HashMultiSet is a Hashed Associative Container that stores objects of type T. 
    /// HashMultiSet is a Simple Associative Container, meaning that its value type, 
    /// as well as its key type, is T.
    /// </para>
    /// <para>
    /// HashMultiSet is useful in applications where it is important to be able to search 
    /// for an element quickly. If it is important for the elements to be in a 
    /// particular order, however, then <see cref="MultiSet{T}"/> is more appropriate. 
    /// </para>
    /// </summary>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public class HashMultiSet<T> : ICollection, IRange<T>, ICollection<T>, ISerializable
    {
        private HashContainer<T, T> hc;
        /// <summary>
        /// Construct an empty container with an initial buffer size and the provided 
        /// <see cref="IEqualityComparer{Key}"/>implemntation.
        /// </summary>
        /// <param name="initialBuffer"></param>
        /// <param name="hashStrategy"></param>
        public HashMultiSet(int initialBuffer, IEqualityComparer<T> hashStrategy)
        {
            hc = new HashContainer<T, T>(initialBuffer, Project.Identity<T>(),hashStrategy);
        }
        /// <summary>
        /// Constructs an empty container.
        /// </summary>
        public HashMultiSet()
            : this(0, EqualityComparer<T>.Default)
        {}
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="src"></param>
        public HashMultiSet(IEnumerable<T> src)
            : this()
        {
            Insert(src);
        }
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="src"></param>
        public HashMultiSet(params T[] src)
            : this()
        {
            Insert(src);
        }
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="src"></param>
        public HashMultiSet(IEnumerable src)
            : this()
        {
            Insert(src.Cast<T>());
        }
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public HashMultiSet(IInputIterator<T> first, IInputIterator<T> last)
            : this()
        {
            Insert(first, last);
        }
        #region Iterator
        /// <summary>
        /// The iterator of the <see cref="HashMultiSet{T}"/>.
        /// </summary>
        public sealed class Iterator : InputIterator<T>
        {
            private readonly InputIterator<T> hashContainerIterator;

            internal Iterator(HashContainer<T, T>.Iterator hashContainerIterator)
                : this((InputIterator<T>)hashContainerIterator)
            { }
            private Iterator(IIterator<T> hashContainerIterator)
            {
                this.hashContainerIterator = (InputIterator<T>)hashContainerIterator.Clone();
            }
            /// <summary>
            /// Returns a clone of this iterator moved dist ahead.
            /// </summary>
            /// <param name="dist"></param>
            /// <returns></returns>
            public Iterator Add(int dist)
            {
                IInputIterator<T> inIt = this;
                return (Iterator)inIt.Add(dist);
            }
            /// <summary>
            /// See <see cref="IInputIterator{T}.PreIncrement()"/> for more information.
            /// </summary>
            /// <returns></returns>
            public override IInputIterator<T> PreIncrement()
            {
                hashContainerIterator.PreIncrement();
                return this;
            }
            /// <summary>
            /// See <see cref="IInputIterator{T}.Value"/> for more information.
            /// </summary>
            public override T Value
            {
                get { return hashContainerIterator.Value; }
            }

            internal HashContainer<T, T>.Iterator Inner
            {
                get { return (HashContainer<T, T>.Iterator)hashContainerIterator; }
            }

            /// <summary>
            /// See <see cref="IIterator{T}.Clone()"/> for more information.
            /// </summary>
            /// <returns></returns>
            public override IIterator<T> Clone()
            {
                return new Iterator(hashContainerIterator);
            }
            /// <summary>
            /// See <see cref="EquatableIterator{T}.Equals(EquatableIterator{T})"/> for more information.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            protected override bool Equals(EquatableIterator<T> obj)
            {
                Iterator rhs = obj as Iterator;
                if (rhs == null)
                    return false;
                return hashContainerIterator.Equals(rhs.hashContainerIterator);
            }
            /// <summary>
            /// See <see cref="EquatableIterator{T}.HashCode"/> for more information. 
            /// </summary>
            /// <returns></returns>
            protected override int HashCode()
            {
                return hashContainerIterator.GetHashCode();
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
        }

        #endregion
        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            hc.CopyTo(array, index);
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
            get { return hc; }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return NStlUtil.Enumerator(Begin(), End());
        }

        #endregion
        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            Insert(item);
        }

        void ICollection<T>.Clear()
        {
            Clear();
        }

        bool ICollection<T>.Contains(T item)
        {
            return !Equals(hc.Find(item), hc.End());
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
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
            return Erase(item) > 0;
        }

        #endregion
        #region IEnumerable<KeyValuePair<Key,Value>> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return NStlUtil.Enumerator(hc.Begin(), hc.End());
        }

        #endregion
        #region IRange<KeyValuePair<Key,Value>> Members

        IInputIterator<T> IRange<T>.Begin()
        {
            return Begin();
        }

        IInputIterator<T> IRange<T>.End()
        {
            return End();
        }

        #endregion
        #region ISerializable Members
        private const int Version = 1;
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected HashMultiSet(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            hc = (HashContainer<T, T>) info.GetValue("HashContainer", typeof(HashContainer<T, T>));
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
            info.AddValue("HashContainer", hc);
        }

        #endregion
        /// <summary>
        /// Returns an iterator that points to the first element of the range.
        /// </summary>
        /// <returns></returns>
        public Iterator Begin()
        {
            return new Iterator(hc.Begin());
        }
        /// <summary>
        /// Returns an iterator that points one past the final element of the range.
        /// </summary>
        /// <returns></returns>
        public Iterator End()
        {
            return new Iterator(hc.End());
        }
        /// <summary>
        /// Inserts the item into the container.
        /// </summary>
        /// <param name="item">The item to be inserted.</param>
        /// <returns>An iterator pointing to the inserted item.</returns>
        public Iterator Insert(T item)
        {
            return new Iterator(hc.InsertEqual(item));
        }
        /// <summary>
        /// Inserts a range of items int this container.
        /// </summary>
        /// <param name="first">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// to the first element to be inserted.
        /// </param>
        /// <param name="last">
        /// An <see cref="IInputIterator{T}"/> implementation pointing 
        /// one past final the element to be inserted.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the first or last is null.</exception>
        public void Insert(IInputIterator<T> first, IInputIterator<T> last)
        {
            Verify.ArgumentNotNull(first, "first");
            Verify.ArgumentNotNull(last, "last");
            Insert(first.AsEnumerable(last));
        }
        /// <summary>
        /// Inserts a range of items int this container.
        /// </summary>
        /// <param name="range">The range to be inserted.</param>
        /// <exception cref="ArgumentNullException">Thrown when the range is null.</exception>
        public void Insert(IEnumerable<T> range)
        {
            Verify.ArgumentNotNull(range, "range");
            foreach(T p in range)
                Insert(p);
        }
        /// <summary>
        /// Looks up the position of a given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// An iterator pointing to the found value or the end iterator if the key
        /// is not present.
        /// </returns>
        /// <remarks>
        /// If you wish to find all values associated with the key, use
        /// <see cref="EqualRange"/> instead.
        /// </remarks>
        public Iterator Find(T item)
        {
            return new Iterator(hc.Find(item));
        }
        /// <summary>
        /// Erases all values.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The amount of erased values.</returns>
        public int Erase(T item)
        {
            return hc.Erase(item);
        }
        /// <summary>
        /// Erases the value at the position the iterator points to.
        /// </summary>
        /// <param name="where">An iterator pointing at the element to be erased.</param>
        /// <exception cref="ArgumentNullException">Thrown when the iterator is null.</exception>
        /// <exception cref="NotTheSameInstanceException">Thrown when the iterator points to another range.</exception>
        /// <exception cref="EndIteratorIsNotAValidInputException">Thrown when the iterator is the end iterator.</exception>
        public void Erase(Iterator where)
        {
            Verify.ArgumentNotNull(where, "where");
            Verify.InstanceEquals(where.Inner.HashContainer, hc, "Iterator points to different container!");
            hc.Erase(where.Inner);
        }
        /// <summary>
        /// Erases a range of elements from the container.
        /// </summary>
        /// <param name="first">An iterator pointing on the first element to be erased.</param>
        /// <param name="last">An iterator pointing one past the final element to be erased.</param>
        /// <exception cref="ArgumentNullException">Thrown when one of the iterators is null.</exception>
        public void Erase(Iterator first, Iterator last)
        {
            Verify.ArgumentNotNull(first, "first");
            Verify.ArgumentNotNull(last, "last");
            hc.Erase(first.Inner, last.Inner);
        }
        /// <summary>
        /// Counts the values.
        /// </summary>
        /// <param name="item">The key in question.</param>
        /// <returns>The amount of values.</returns>
        /// <remarks>This is a linear operation that depends on count of values.</remarks>
        public int CountOf(T item)
        {
            Verify.ArgumentNotNull(item, "key");
            return hc.CountOf(item);
        }
        /// <summary>
        /// Finds a range containing all elements associated with a given key.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// The returned object is a NSTL range and also implements 
        /// <see cref="IEnumerable{T}"/>.
        /// </returns>
        public Range<T, Iterator> EqualRange(T item)
        {
            Verify.ArgumentNotNull(item, "key");
            KeyValuePair<HashContainer<T, T>.Iterator, HashContainer<T, T>.Iterator> rv = hc.EqualRange(item);
            return new Range<T, Iterator>(new Iterator(rv.Key), new Iterator(rv.Value));
        }
        
        /// <summary>
        /// Copies the content of this container into an array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] tmp = new T[hc.Count];
            hc.CopyTo(tmp, 0);
            return tmp;
        }
        /// <summary>
        /// Copies the content of this container into a given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(T[] array, int index)
        {
            hc.CopyTo(array, index);
        }
        /// <summary>
        /// The amount of entries in the container.
        /// </summary>
        public int Count
        {
            get { return hc.Count; }
        }
        /// <summary>
        /// True, when the container is empty
        /// </summary>
        public bool Empty
        {
            get { return hc.Count == 0; }
        }       
        /// <summary>
        /// Clears the containers content.
        /// </summary>
        public void Clear()
        {
            hc.Clear();
        }
        /// <summary>
        /// Swaps this containers content with the others.
        /// </summary>
        /// <param name="rhs">The container to be swapped.</param>
        /// <remarks>This is a constant time operation!</remarks>
        public void Swap(HashMultiSet<T> rhs)
        {
            Algorithm.Swap(ref hc, ref rhs.hc);
        }
    }
}

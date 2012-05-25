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
using NStl.Collections.Private;
using NStl.Debugging;
using NStl.Exceptions;
using NStl.Iterators;
using NStl.Iterators.Support;
using NStl.Linq;
using NStl.Util;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NStl.SyntaxHelper;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// <para>
    /// HashSet is a Hashed Associative Container that stores objects of type Key. 
    /// HashSet is a Simple Associative Container, meaning that its value type, 
    /// as well as its key type, is Key. It is also a Unique Associative Container, 
    /// meaning that no two elements compare equal using a <see cref="IEqualityComparer{T}"/>
    /// implementation.
    /// </para>
    /// <para>
    /// HashSet is useful in applications where it is important to be able to search 
    /// for an element quickly. If it is important for the elements to be in a 
    /// particular order, however, then <see cref="UniqueSet{T}"/> is more appropriate. 
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof (CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    [Obsolete("Use System.Collections.Generic.Hashset<T> instead!")]
    public class HashSet<T> : ICollection<T>, IRange<T>, ISerializable, ICollection
    {
        private HashContainer<T, T> hc;
        /// <summary>
        /// Construct and empty container.
        /// </summary>
        public HashSet()
            : this(0, EqualityComparer<T>.Default)
        {
        }
        /// <summary>
        /// Construct an empty container with the provided initial buffer size. The
        /// provided <see cref="IEqualityComparer{T}"/> is used for hashing and comparing.
        /// </summary>
        /// <param name="initalBuffer">The initial buffer size.</param>
        /// <param name="hashStrategy">The comparer to be used for hashing.</param>
        public HashSet(int initalBuffer, IEqualityComparer<T> hashStrategy)
        {
            hc = new HashContainer<T, T>(initalBuffer, Project.Identity<T>(), hashStrategy);
        }
        /// <summary>
        /// Constructs the container and copies the provided range into it.
        /// </summary>
        /// <param name="src">The range to be copied.</param>
        public HashSet(IEnumerable<T> src)
            : this()
        {
            Verify.ArgumentNotNull(src, "src");
            foreach (T t in src)
                Insert(t);
        }
        /// <summary>
        /// Constructs the container and copies the provided range into it.
        /// </summary>
        /// <param name="src">The rangeto be copied.</param>
        public HashSet(IEnumerable src)
            : this(src.Cast<T>())
        {
        }
        /// <summary>
        /// Constructs the container and copies the provided range into it.
        /// </summary>
        /// <param name="first">An iterator pointing to the first element of the range to be copied.</param>
        /// <param name="last">An iterator pointing one past the final element of the range to be copied.</param>
        public HashSet(IInputIterator<T> first, IInputIterator<T> last)
            : this(first.AsEnumerable(last))
        {
        }
        /// <summary>
        /// Constructs the container and copies the provided items into it.
        /// </summary>
        /// <param name="items">The items to be copied</param>
        public HashSet(params T[] items)
            : this((IEnumerable<T>) items)
        {
        }

        #region Iterator

        /// <summary>
        /// The HashSet iterator
        /// </summary>
        public sealed class Iterator : InputIterator<T>
        {
            private readonly InputIterator<T> hashContainerIterator;

            internal Iterator(HashContainer<T, T>.Iterator hashContainerIterator)
                : this((InputIterator<T>) hashContainerIterator)
            {
            }

            private Iterator(InputIterator<T> hashContainerIterator)
            {
                this.hashContainerIterator = (InputIterator<T>) hashContainerIterator.Clone();
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
                get { return (HashContainer<T, T>.Iterator) hashContainerIterator; }
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
            /// See <see cref="IIterator{T}.Clone()"/> for more information.
            /// </summary>
            /// <returns></returns>
            public override IIterator<T> Clone()
            {
                return new Iterator(hashContainerIterator);
            }

            /// <summary>
            /// See <see cref="object.Equals(object)"/> for details.
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
            /// See <see cref="object.GetHashCode()"/> for details.
            /// </summary>
            /// <returns></returns>
            protected override int HashCode()
            {
                return hashContainerIterator.GetHashCode();
            }
            /// <summary>
            /// Increments the iterator one step.
            /// </summary>
            /// <param name="it"></param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
            public static Iterator operator ++(Iterator it)
            {
                Iterator tmp = (Iterator) it.Clone();
                return (Iterator) tmp.PreIncrement();
            }
        }

        #endregion
        /// <summary>
        /// The amount of items in the container.
        /// </summary>
        public int Count
        {
            get { return hc.Count; }
        }
        /// <summary>
        /// True, if the container contains no elements.
        /// </summary>
        public bool Empty
        {
            get { return hc.Count == 0; }
        }
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
            return !Equals(Find(item), End());
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
            return Erase(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
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
        #region ISerializable Members

        private const int Version = 1;
        /// <summary>
        /// Desirialization constuctor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected HashSet(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);
            hc =
               (HashContainer<T, T>)
                   info.GetValue("HashContainer", typeof(HashContainer<T, T>));
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
        /// Returns an iterator pointing one past the final element of the range.
        /// </summary>
        /// <returns></returns>
        public Iterator End()
        {
            return new Iterator(hc.End());
        }
        /// <summary>
        /// Returns an iterator pointing to the first element of the range.
        /// </summary>
        /// <returns></returns>
        public Iterator Begin()
        {
            return new Iterator(hc.Begin());
        }

        /// <summary>
        /// Inserts the item into the container.
        /// </summary>
        /// <param name="item">The item to be inserted.</param>
        /// <returns>
        /// A <see cref="KeyValuePair{TKey,TValue}"/> containing an iterator pointing
        /// to the inserted element and true if the element was not present
        /// in the container, false otherwise.
        /// </returns>
        public KeyValuePair<Iterator, bool> Insert(T item)
        {
            KeyValuePair<HashContainer<T, T>.Iterator, bool> rv =
                hc.InsertUnique(item);

            return new KeyValuePair<Iterator, bool>(new Iterator(rv.Key), rv.Value);
        }
        /// <summary>
        /// Inserts the given range into the container.
        /// </summary>
        /// <param name="first">
        /// An iterator pointing to the first element of the range to be inserted.
        /// </param>
        /// <param name="last">
        /// An iterator pointing one past teh final element of the range to be inserted.
        /// </param>
        public void Insert(IInputIterator<T> first, IInputIterator<T> last)
        {
            foreach (T t in first.AsEnumerable(last))
                Insert(t);
        }
        /// <summary>
        /// Erases the iben item
        /// </summary>
        /// <param name="item">The item to be erased</param>
        /// <returns>True, if the item was in the container.</returns>
        public bool Erase(T item)
        {
            return hc.Erase(item) != 0;
        }
        /// <summary>
        /// Erases the item at the specified position.
        /// </summary>
        /// <param name="where">An iterator pointing to the item to be erased.</param>
        public void Erase(Iterator where)
        {
            Verify.ArgumentNotNull(where, "where");
            hc.Erase(where.Inner);
        }
        /// <summary>
        /// Erases a range of elements.
        /// </summary>
        /// <param name="first">
        /// An iterator pointing to the first element of the range to be erased.
        /// </param>
        /// <param name="last">
        /// An iterator pointing one past teh final element of the range to be erased.
        /// </param>
        public void Erase(Iterator first, Iterator last)
        {
            Verify.InstanceEquals(hc, first.Inner.HashContainer, "first point to different container!");
            Verify.InstanceEquals(hc, last.Inner.HashContainer, "last point to different container!");
            hc.Erase(first.Inner, last.Inner);
        }
        /// <summary>
        /// Copies the content of this container into the passed in array.
        /// </summary>
        /// <param name="array">The copy target</param>
        /// <param name="index">The index of the target at which copying will start.</param>
        public void CopyTo(T[] array, int index)
        {
            foreach (T t in this)
                array[index++] = t;
        }
        /// <summary>
        /// Copies the content of this container into a newly created array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] t = new T[Count];
            CopyTo(t, 0);
            return t;
        }
        /// <summary>
        /// Searches the given element.
        /// </summary>
        /// <param name="item">The element to be searched for.</param>
        /// <returns>An iterator pointing to the found element, or the end 
        /// iterator if the element is not inside the container.</returns>
        public Iterator Find(T item)
        {
            return new Iterator(hc.Find(item));
        }
        /// <summary>
        /// CLears the container.
        /// </summary>
        public void Clear()
        {
            hc.Clear();
        }
        /// <summary>
        /// Swaps ththis ontainers content with the others.
        /// </summary>
        /// <param name="rhs">The container to be swapped.</param>
        /// <remarks>This is a constant time operation!</remarks>
        public void Swap(HashSet<T> rhs)
        {
            Algorithm.Swap(ref hc, ref rhs.hc);
        }
    }
}

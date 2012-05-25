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
    /// HashMultiMap is a Hashed Associative Container that associates objects of 
    /// type Key with objects of type Data. HashMultiMap is a Pair Associative 
    /// Container, meaning that its value type is <see cref="KeyValuePair{Key, Value}"/>. 
    /// It is also a Multiple Associative Container, meaning that there is no limit  
    /// on the number of elements with the same key.
    /// </summary>
    /// <typeparam name="Key"></typeparam>
    /// <typeparam name="Value"></typeparam>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public class HashMultiMap<Key, Value> : ICollection, IDictionary<Key, Value>, IRange<KeyValuePair<Key, Value>>, ISerializable
    {
        private readonly HashContainer<Key, KeyValuePair<Key, Value>> hc;
        /// <summary>
        /// Construct an empty container with an initial bugffer size and the provided 
        /// <see cref="IEqualityComparer{Key}"/>implemntation.
        /// </summary>
        /// <param name="initialBuffer"></param>
        /// <param name="hashStrategy"></param>
        public HashMultiMap(int initialBuffer, IEqualityComparer<Key> hashStrategy)
        {
            hc =
                new HashContainer<Key, KeyValuePair<Key, Value>>(initialBuffer, Select.FirstFromKeyValuePair<Key, Value>(),
                                                                hashStrategy);
        }
        /// <summary>
        /// Constructs an empty container.
        /// </summary>
        public HashMultiMap()
            : this(0, EqualityComparer<Key>.Default)
        {}
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="src"></param>
        public HashMultiMap(IEnumerable<KeyValuePair<Key, Value>> src)
            : this()
        {
            Insert(src);
        }
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="src"></param>
        public HashMultiMap(params KeyValuePair<Key, Value>[] src)
            : this()
        {
            Insert(src);
        }
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="src"></param>
        public HashMultiMap(IEnumerable src)
            : this()
        {
            Insert(src.Cast<KeyValuePair<Key, Value>>());
        }
        /// <summary>
        /// Constructs the container and copies the passed in range into it.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public HashMultiMap(IInputIterator<KeyValuePair<Key, Value>> first, IInputIterator<KeyValuePair<Key, Value>> last)
            : this()
        {
            Insert(first, last);
        }
        #region Iterator
        /// <summary>
        /// The iterator of the <see cref="HashMultiMap{Key, Value}"/>.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This scoping binds the iterator to its container and allows internal container access.")]
        public sealed class Iterator : InputIterator<KeyValuePair<Key, Value>>
        {
            private readonly InputIterator<KeyValuePair<Key, Value>> hashContainerIterator;

            internal Iterator(HashContainer<Key, KeyValuePair<Key, Value>>.Iterator hashContainerIterator)
                : this((InputIterator<KeyValuePair<Key, Value>>)hashContainerIterator)
            { }
            private Iterator(IIterator<KeyValuePair<Key, Value>> hashContainerIterator)
            {
                this.hashContainerIterator = (InputIterator<KeyValuePair<Key, Value>>)hashContainerIterator.Clone();
            }

            /// <summary>
            /// See <see cref="IInputIterator{T}.PreIncrement()"/> for more information.
            /// </summary>
            /// <returns></returns>
            public override IInputIterator<KeyValuePair<Key, Value>> PreIncrement()
            {
                hashContainerIterator.PreIncrement();
                return this;
            }
            /// <summary>
            /// See <see cref="IInputIterator{T}.Value"/> for more information.
            /// </summary>
            public override KeyValuePair<Key, Value> Value
            {
                get { return hashContainerIterator.Value; }
            }

            internal HashContainer<Key, KeyValuePair<Key, Value>>.Iterator Inner
            {
                get { return (HashContainer<Key, KeyValuePair<Key, Value>>.Iterator)hashContainerIterator; }
            }

            /// <summary>
            /// See <see cref="IIterator{T}.Clone()"/> for more information.
            /// </summary>
            /// <returns></returns>
            public override IIterator<KeyValuePair<Key, Value>> Clone()
            {
                return new Iterator(hashContainerIterator);
            }
            /// <summary>
            /// See <see cref="object.Equals(object)"/> for details.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            protected override bool Equals(EquatableIterator<KeyValuePair<Key, Value>> obj)
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
        #region IDictionary<Key,Value> Members

        void IDictionary<Key, Value>.Add(Key key, Value value)
        {
            Verify.ArgumentNotNull(key, "key");
            Insert(new KeyValuePair<Key, Value>(key, value));
        }

        bool IDictionary<Key, Value>.ContainsKey(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return !Equals(hc.Find(key), hc.End());
        }

        ICollection<Key> IDictionary<Key, Value>.Keys
        {
            get { return ReadonlyCollectionAdaptor<Key>.KeyCollection(hc); }
        }

        bool IDictionary<Key, Value>.Remove(Key key)
        {
            return Erase(key) != 0;
        }

        bool IDictionary<Key, Value>.TryGetValue(Key key, out Value value)
        {
            Iterator i = Find(key);
            if (!Equals(i, End()))
            {
                value = i.Value.Value;
                return true;
            }
            value = default(Value);
            return false;
        }

        ICollection<Value> IDictionary<Key, Value>.Values
        {
            get { return ReadonlyCollectionAdaptor<Key>.ValueCollection(hc); }
        }

        Value IDictionary<Key, Value>.this[Key key]
        {
            get { return this[key]; }
            set { this[key] = value; }
        }

        #endregion
        #region ICollection<KeyValuePair<Key,Value>> Members

        void ICollection<KeyValuePair<Key, Value>>.Add(KeyValuePair<Key, Value> item)
        {
            Insert(item);
        }

        void ICollection<KeyValuePair<Key, Value>>.Clear()
        {
            Clear();
        }

        bool ICollection<KeyValuePair<Key, Value>>.Contains(KeyValuePair<Key, Value> item)
        {
            return ((IDictionary<Key, Value>)this).ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<Key, Value>>.CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<Key, Value>>.Count
        {
            get { return Count; }
        }

        bool ICollection<KeyValuePair<Key, Value>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<Key, Value>>.Remove(KeyValuePair<Key, Value> item)
        {
            return Erase(item.Key) != 0;
        }

        #endregion
        #region IEnumerable<KeyValuePair<Key,Value>> Members

        IEnumerator<KeyValuePair<Key, Value>> IEnumerable<KeyValuePair<Key, Value>>.GetEnumerator()
        {
            return NStlUtil.Enumerator(hc.Begin(), hc.End());
        }

        #endregion
        #region IRange<KeyValuePair<Key,Value>> Members

        IInputIterator<KeyValuePair<Key, Value>> IRange<KeyValuePair<Key, Value>>.Begin()
        {
            return Begin();
        }

        IInputIterator<KeyValuePair<Key, Value>> IRange<KeyValuePair<Key, Value>>.End()
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
        protected HashMultiMap(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            hc =
                (HashContainer<Key, KeyValuePair<Key, Value>>)
                    info.GetValue("HashContainer", typeof(HashContainer<Key, KeyValuePair<Key, Value>>));
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
        /// Returns an iterator that points to one past the final element of the range.
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
        /// <exception cref="ArgumentNullException">Thrown when item.Key is null.</exception>
        public Iterator Insert(KeyValuePair<Key, Value> item)
        {
            Verify.ArgumentNotNull(item.Key, "item.Key");
            return new Iterator(hc.InsertEqual(item));
        }
        /// <summary>
        /// Inserts the item into the container.
        /// </summary>
        /// <param name="key">The key to be inserted.</param>
        /// <param name="value">The value to be associated with the key.</param>
        /// <returns>An iterator pointing to the inserted item.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the key is null.</exception>
        public Iterator Insert(Key key, Value value)
        {
            Verify.ArgumentNotNull(key, "key");
            return Insert(new KeyValuePair<Key, Value>(key, value));
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
        /// one past the element to be inserted.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the first or last is null.</exception>
        public void Insert(IInputIterator<KeyValuePair<Key, Value>> first, IInputIterator<KeyValuePair<Key, Value>> last)
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
        public void Insert(IEnumerable<KeyValuePair<Key, Value>> range)
        {
            Verify.ArgumentNotNull(range, "range");
            foreach(KeyValuePair<Key, Value> p in range)
                Insert(p);
        }
        /// <summary>
        /// Looks up the value for a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>
        /// An iterator pointing to the found value or the end iterator if the key
        /// is not present.
        /// </returns>
        /// <remarks>
        /// If you wish to find all values associated with the key, use
        /// <see cref="EqualRange(Key)"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        public Iterator Find(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return new Iterator(hc.Find(key));
        }
        /// <summary>
        /// Erases all values associated with a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The amount of erased values.</returns>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        public int Erase(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return hc.Erase(key);
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
            if (Equals(where, End()))
                throw new EndIteratorIsNotAValidInputException();
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
        /// Counts the values associated with a key.
        /// </summary>
        /// <param name="key">The key in question.</param>
        /// <returns>The amount of values associated with the key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        /// <remarks>This is a linear operation that depends on count of values associated with the key.</remarks>
        public int CountOf(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return hc.CountOf(key);
        }
        /// <summary>
        /// Finds a range containing all elements associated with a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>
        /// The returned object is a NSTL range and also implements 
        /// <see cref="IEnumerable{T}"/>.
        /// </returns>
        public Range<KeyValuePair<Key, Value>, Iterator> EqualRange(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            KeyValuePair<HashContainer<Key, KeyValuePair<Key, Value>>.Iterator, HashContainer<Key, KeyValuePair<Key, Value>>.Iterator> rv = hc.EqualRange(key);
            return new Range<KeyValuePair<Key, Value>, Iterator>(new Iterator(rv.Key), new Iterator(rv.Value));
        }
        
        /// <summary>
        /// Copies the content of this container into an array.
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<Key, Value>[] ToArray()
        {
            KeyValuePair<Key, Value>[] tmp = new KeyValuePair<Key, Value>[hc.Count];
            hc.CopyTo(tmp, 0);
            return tmp;
        }
        /// <summary>
        /// Copies the content of this container into a given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(KeyValuePair<Key, Value>[] array, int index)
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
        /// Accesses the values by a given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal Value this[Key key]
        {
            get
            {
                Verify.ArgumentNotNull(key, "key");

                HashContainer<Key, KeyValuePair<Key, Value>>.Iterator it =
                    hc.Find(key);

                if (Equals(it, hc.End()))
                    throw new KeyNotFoundException();

                return it.Value.Value;
            }
            set
            {
                Verify.ArgumentNotNull(key, "key");

                HashContainer<Key, KeyValuePair<Key, Value>>.Iterator it =
                    hc.Find(key);

                if (!Equals(it, hc.End()))
                    it.SetValue(new KeyValuePair<Key, Value>(key, value));
                else
                    hc.InsertUnique(new KeyValuePair<Key, Value>(key, value));
            }
        }
        /// <summary>
        /// Clears the containers content.
        /// </summary>
        public void Clear()
        {
            hc.Clear();
        }
    }
}

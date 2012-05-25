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
using NStl.Exceptions;
using NStl.Iterators;
using NStl.Iterators.Support;
using NStl.Linq;
using NStl.Util;
using System.Diagnostics;
using NStl.Debugging;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NStl.SyntaxHelper;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// <para>
    /// HashMap is a Hashed Associative Container that associates objects of type Key 
    /// with objects of type Data. HashMap is a Pair Associative Container, meaning 
    /// that its value type is KeyValuePair&lt;const Key, Data&gt;. It is also a Unique Associative 
    /// Container, meaning that no two elements have keys that compare equal using <see cref="IEqualityComparer{Key}"/>.
    /// </para>
    /// <para>
    /// Looking up an element in a HashMap by its key is efficient, so HashMap is useful 
    /// for "dictionaries" where the order of elements is irrelevant. If it is important 
    /// for the elements to be in a particular order, however, then <see cref="Map{Key, Data}"/> 
    /// is more appropriate. 
    /// </para>
    /// </summary>
    /// <typeparam name="Key"></typeparam>
    /// <typeparam name="Data"></typeparam>
    /// <remarks>
    /// This class is very similar to <see cref="Dictionary{Key, Data}"/> except that it allows the 
    /// modification if the values while iterating over the keys.
    /// </remarks>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    [Obsolete("Use System.Collections.Generic.Dictionary<K,V> and the extension methods in the NStl.Linq namespace instead!")]
    public class HashMap<Key, Data> : IDictionary<Key, Data>, IRange<KeyValuePair<Key, Data>>, ISerializable, ICollection
    {
        private HashContainer<Key, KeyValuePair<Key, Data>> hc;
        #region Iterator
        /// <summary>
        /// The HashMap iterator
        /// </summary>
        public sealed class Iterator : InputIterator<KeyValuePair<Key, Data>>
        {
            private readonly InputIterator<KeyValuePair<Key, Data>> hashContainerIterator;

            internal Iterator(HashContainer<Key, KeyValuePair<Key, Data>>.Iterator hashContainerIterator)
                : this((InputIterator<KeyValuePair<Key, Data>>)hashContainerIterator)
            {}
            private Iterator(InputIterator<KeyValuePair<Key, Data>> hashContainerIterator)
            {
                this.hashContainerIterator = (InputIterator<KeyValuePair<Key, Data>>)hashContainerIterator.Clone();
            }

            /// <summary>
            /// See <see cref="IInputIterator{T}.PreIncrement()"/> for more information.
            /// </summary>
            /// <returns></returns>
            public override IInputIterator<KeyValuePair<Key, Data>> PreIncrement()
            {
                hashContainerIterator.PreIncrement();
                return this;
            }
            /// <summary>
            /// See <see cref="IInputIterator{T}.Value"/> for more information.
            /// </summary>
            public override KeyValuePair<Key, Data> Value
            {
                get { return hashContainerIterator.Value; }
            }

            internal HashContainer<Key, KeyValuePair<Key, Data>>.Iterator Inner
            {
                get { return (HashContainer<Key, KeyValuePair<Key, Data>>.Iterator)hashContainerIterator; }
            }

            /// <summary>
            /// See <see cref="IIterator{T}.Clone()"/> for more information.
            /// </summary>
            /// <returns></returns>
            public override IIterator<KeyValuePair<Key, Data>> Clone()
            {
                return new Iterator(hashContainerIterator);
            }
            /// <summary>
            /// See <see cref="object.Equals(object)"/> for details.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            protected override bool Equals(EquatableIterator<KeyValuePair<Key, Data>> obj)
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

        /// <summary>
        /// Constructs an empty HashMap with th specified minimal buffer size and hash provider.
        /// </summary>
        /// <param name="initialBuffer">The minimum initial allocated size of the hashtable.</param>
        /// <param name="hashStrategy">The hash and equality provider to be used.</param>
        public HashMap(int initialBuffer, IEqualityComparer<Key> hashStrategy)
        {
            hc =
                new HashContainer<Key, KeyValuePair<Key, Data>>(initialBuffer, Select.FirstFromKeyValuePair<Key, Data>(),
                                                                hashStrategy);
        }
        /// <summary>
        /// Constructs an empty HashMap with th specified minimal buffer size.
        /// </summary>
        /// <param name="initialBuffer"></param>
        public HashMap(int initialBuffer)
            : this(initialBuffer, EqualityComparer<Key>.Default)
        {
        }

        /// <summary>
        /// Constructs an empty HashMap.
        /// </summary>
        public HashMap()
            : this(0, EqualityComparer<Key>.Default)
        {
        }
        /// <summary>
        /// Constructs a HashMap and copies given the range into it.
        /// </summary>
        /// <param name="e"></param>
        public HashMap(IEnumerable<KeyValuePair<Key, Data>> e)
            : this(0, EqualityComparer<Key>.Default)
        {
            foreach (KeyValuePair<Key, Data> p in e)
                if (!Insert(p).Value)
                    throw new ArgumentException("Key already present: " + p.Key);
        }
        /// <summary>
        /// Constructs a HashMap and copies given the range into it.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public HashMap(IInputIterator<KeyValuePair<Key, Data>> first, IInputIterator<KeyValuePair<Key, Data>> last)
            : this(first.AsEnumerable(last))
        {
        }
        /// <summary>
        /// Constructs a HashMap and copies given the range into it.
        /// </summary>
        public HashMap(params KeyValuePair<Key, Data>[] range)
            : this((IEnumerable<KeyValuePair<Key, Data>>)range)
        {}
        /// <summary>
        /// Constructs a HashMap and copies given the range into it.
        /// </summary>
        /// <param name="e"></param>
        public HashMap(IEnumerable e)
            : this(e.Cast<KeyValuePair<Key, Data>>())
        {}
        #region ISerializable Members
        private const int Version = 1;
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected HashMap(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            hc =
                (HashContainer<Key, KeyValuePair<Key, Data>>)
                    info.GetValue("HashContainer", typeof (HashContainer<Key, KeyValuePair<Key, Data>>));
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
        #region IDictionary<Key,Data> Members

        void IDictionary<Key, Data>.Add(Key key, Data value)
        {
            Verify.ArgumentNotNull(key, "key");
            if (!hc.InsertUnique(new KeyValuePair<Key, Data>(key, value)).Value)
                throw new ArgumentException(Resource.KeyAlreadyExists, "key");
        }

        bool IDictionary<Key, Data>.ContainsKey(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return !Equals(hc.Find(key), hc.End());
        }


        ICollection<Key> IDictionary<Key, Data>.Keys
        {
            get { return ReadonlyCollectionAdaptor<Key>.KeyCollection(hc); }
        }

        bool IDictionary<Key, Data>.Remove(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return hc.Erase(key) > 0;
        }

        bool IDictionary<Key, Data>.TryGetValue(Key key, out Data value)
        {
            Verify.ArgumentNotNull(key, "key");
            HashContainer<Key, KeyValuePair<Key, Data>>.Iterator it =
                hc.Find(key);

            if (!Equals(it, hc.End()))
            {
                value = it.Value.Value;
                return true;
            }
            value = default(Data);
            return false;
        }

        ICollection<Data> IDictionary<Key, Data>.Values
        {
            get { return ReadonlyCollectionAdaptor<Data>.ValueCollection(hc); }
        }

        Data IDictionary<Key, Data>.this[Key key]
        {
            get{ return this[key]; }
            set{ this[key] = value;}
        }

        #endregion
        #region ICollection<KeyValuePair<Key,Data>> Members

        void ICollection<KeyValuePair<Key, Data>>.Add(KeyValuePair<Key, Data> item)
        {
            ((IDictionary<Key, Data>) this).Add(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<Key, Data>>.Clear()
        {
            Clear();
        }

        bool ICollection<KeyValuePair<Key, Data>>.Contains(KeyValuePair<Key, Data> item)
        {
            return ((IDictionary<Key, Data>) this).ContainsKey(item.Key);
        }

        void ICollection<KeyValuePair<Key, Data>>.CopyTo(KeyValuePair<Key, Data>[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<Key, Data>>.Count
        {
            get { return hc.Count; }
        }

        bool ICollection<KeyValuePair<Key, Data>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<Key, Data>>.Remove(KeyValuePair<Key, Data> item)
        {
            return Erase(item.Key);
        }

        #endregion
        #region IEnumerable<KeyValuePair<Key,Data>> Members

        IEnumerator<KeyValuePair<Key, Data>> IEnumerable<KeyValuePair<Key, Data>>.GetEnumerator()
        {
            return hc.Begin().AsEnumerable(hc.End()).GetEnumerator();
        }

        #endregion
        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return hc.Begin().AsEnumerable(hc.End()).GetEnumerator();
        }

        #endregion
        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            foreach (KeyValuePair<Key, Data> p in this)
                array.SetValue(p, index++);
        }

        int ICollection.Count
        {
            get { return hc.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return hc; }
        }

        #endregion
        #region IRange<KeyValuePair<Key,Data>> Members

        IInputIterator<KeyValuePair<Key, Data>> IRange<KeyValuePair<Key, Data>>.Begin()
        {
            return Begin();
        }

        IInputIterator<KeyValuePair<Key, Data>> IRange<KeyValuePair<Key, Data>>.End()
        {
            return End();
        }

        #endregion
        /// <summary>
        /// Inserts a given key value pair into this container.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public KeyValuePair<Iterator, bool>
            Insert(Key key, Data data)
        {
            return Insert(new KeyValuePair<Key, Data>(key, data));
        }
        /// <summary>
        /// Inserts a key value pair into the container.
        /// </summary>
        /// <param name="pair">The pair to be inserted.</param>
        /// <returns>
        /// A KeyValuePair containg an iterator and a boll flag indication success.
        /// If the bool flag is TRUE, item was inserted and the iterator points
        /// to the inserted item. otherwise it is the end iterator.
        /// </returns>
        public KeyValuePair<Iterator, bool> Insert(KeyValuePair<Key, Data> pair)
        {
            KeyValuePair<HashContainer<Key, KeyValuePair<Key, Data>>.Iterator, bool> res =
                hc.InsertUnique(pair);
            return new KeyValuePair<Iterator, bool>(ConvertIterator(res.Key), res.Value);
        }
        private static Iterator ConvertIterator(HashContainer<Key, KeyValuePair<Key, Data>>.Iterator it)
        {
            return new Iterator(it);
        }
        /// <summary>
        /// Inserts a range of key value pairs into the container.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public void Insert(IInputIterator<KeyValuePair<Key, Data>> first, IInputIterator<KeyValuePair<Key, Data>> last)
        {
            foreach (KeyValuePair<Key, Data> p in first.AsEnumerable(last))
                Insert(p);
        }
        /// <summary>
        /// The number of elements inside the container.
        /// </summary>
        public int Count
        {
            get { return hc.Count; }
        }
        /// <summary>
        /// True, if the container is empty.
        /// </summary>
        public bool Empty
        {
            get { return hc.Count == 0; }
        }
        /// <summary>
        /// Empties the container.
        /// </summary>
        public void Clear()
        {
            hc.Clear();
        }
        /// <summary>
        /// Copies the content of this container into the given array starting at the given index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<Key, Data>[] array, int arrayIndex)
        {
            foreach (KeyValuePair<Key, Data> t in this)
                array[arrayIndex++] = t;
        }
        /// <summary>
        /// Copies the content of this container into an array.
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<Key, Data>[] ToArray()
        {
            KeyValuePair<Key, Data>[] tmp = new KeyValuePair<Key, Data>[Count];
            CopyTo(tmp, 0);
            return tmp;
        }
        /// <summary>
        /// Returns an iterator pointing to the first element of the container.
        /// </summary>
        /// <returns></returns>
        public Iterator Begin()
        {
            return new Iterator(hc.Begin());
        }
        /// <summary>
        /// Returns an iterator pointing one past the final element of the container.
        /// </summary>
        /// <returns></returns>
        public Iterator End()
        {
            return new Iterator(hc.End());
        }
        /// <summary>
        /// Erases the element at the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True, if an element existed to be erased.</returns>
        public bool Erase(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return hc.Erase(key) != 0;
        }
        /// <summary>
        /// Searches for an element for the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>An iterator pointing to the found element, the end iterator if no item was found.</returns>
        public Iterator Find(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return ConvertIterator(hc.Find(key));
        }
        /// <summary>
        /// Erases the element that the iterator points to.
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public bool Erase(Iterator where)
        {
            Verify.ArgumentNotNull(where, "where");
            Verify.InstanceEquals(where.Inner.HashContainer, hc, "Iterator points to different container!");
            hc.Erase(where.Inner);
            return !Equals(where.Inner, hc.End());
        }
        /// <summary>
        /// Erases a range of elements.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        public void Erase(Iterator first, Iterator last)
        {
            Verify.ArgumentNotNull(first, "first");
            Verify.ArgumentNotNull(last, "last");
            hc.Erase(first.Inner, last.Inner);
        }
        /// <summary>
        /// Swaps this container with the other.
        /// </summary>
        /// <param name="rhs">The container to be swapped with this container.</param>
        /// <remarks>This is a constant time operation.</remarks>
        public void Swap(HashMap<Key, Data> rhs)
        {
            Verify.ArgumentNotNull(rhs, "rhs");
            Algorithm.Swap(ref hc, ref rhs.hc);
        }
        /// <summary>
        /// Returns a value for a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when no value stored for the key.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Exception is required by the implemented interface!")]
        public Data this[Key key]
        {
            get
            {
                Verify.ArgumentNotNull(key, "key");

                HashContainer<Key, KeyValuePair<Key, Data>>.Iterator it =
                    hc.Find(key);

                if (Equals(it, hc.End()))
                    throw new KeyNotFoundException();

                return it.Value.Value;
            }
            set
            {
                Verify.ArgumentNotNull(key, "key");

                HashContainer<Key, KeyValuePair<Key, Data>>.Iterator it =
                    hc.Find(key);

                if (!Equals(it, hc.End()))
                    it.SetValue(new KeyValuePair<Key, Data>(key, value));
                else
                    hc.InsertUnique(new KeyValuePair<Key, Data>(key, value));
            }
        }
    }
}

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
using System;
using System.Collections;
using NStl.Exceptions;
using NStl.Iterators;
using NStl.Iterators.Support;
using System.Runtime.Serialization;
using NStl.Util;
using NStl.Linq;
using System.Security.Permissions;

namespace NStl.Collections.Private
{
    [Serializable]
    sealed class HashContainerNode<Val> : ISerializable
    {
        internal HashContainerNode(){}
        internal HashContainerNode(Val v)
        {
            Value = v;
        }
        internal HashContainerNode<Val> Next;
        internal Val Value;

        #region ISerializable Members
        private const int Version = 1;
        public HashContainerNode(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);
            Next = (HashContainerNode<Val>)info.GetValue("Next", typeof(HashContainerNode<Val>));
            Value = (Val)info.GetValue("Value", typeof(Val));            
        }
        /// <summary>
        /// See <see cref="ISerializable.GetObjectData"/> for details.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Version", Version);
            info.AddValue("Next", Next);
            info.AddValue("Value", Value);
        }

        #endregion
    };

    static class HashContainerHelper
    {
        static readonly int[] PrimeList = new int[]{
                      53,         97,         193,       389,       769,
                      1543,       3079,       6151,      12289,     24593,
                      49157,      98317,      196613,    393241,    786433,
                      1572869,    3145739,    6291469,   12582917,  25165843,
                      50331653,   100663319,  201326611, 402653189, 805306457, 
                      1610612741, /*3221225473, 4294967291*/ int.MaxValue
                    };

        internal static ListIterator<int> last = PrimeList.End();
        internal static ListIterator<int> first = PrimeList.Begin();
        internal static int NextPrime(int n)
        {
            ListIterator<int> pos =
                Algorithm.LowerBound(first, last, n);

            return pos.Equals(last) ? (last -1).Value : pos.Value;
        }
    }
    // Test:
    //      resize w/o collision, 
    //      collision, 
    //      loadfactor, 
    //      size overflow(??)
    //      Performance compared with Dictionary<K,V>
    [Serializable]
    class HashContainer<KeyType, ValueType> : ICollection, ISerializable
    {
        #region Iterator<T>
        internal class Iterator : InputIterator<ValueType>
        {
            public Iterator(HashContainerNode<ValueType> cur, HashContainer<KeyType, ValueType> ht)
            {
                hashContainer = ht;
                current = cur;
            }

            public override IInputIterator<ValueType> PreIncrement()
            {
                if (Current == null)
                    return this;
                HashContainerNode<ValueType> old = Current;
                current = current.Next;
                if (current == null)
                {
                    int bucket = HashContainer.BucketNumber(old.Value);
                    while (current == null && ++bucket < HashContainer.buckets.Count)
                        current = HashContainer.buckets[bucket];
                }
                return this;
            }

            public override ValueType Value
            {
                get
                {
                    if (Current == null)
                        throw new DereferenceEndIteratorException();
                    return Current.Value;
                }
                
            }
            internal void SetValue(ValueType v)
            {
                if (Current == null)
                    throw new DereferenceEndIteratorException();

                Current.Value = v;
            }

            internal HashContainerNode<ValueType> Current
            {
                get { return current; }
            }

            internal HashContainer<KeyType, ValueType> HashContainer
            {
                get { return hashContainer; }
            }

            public override IIterator<ValueType> Clone()
            {
                return new Iterator(Current, HashContainer);
            }
            protected override bool Equals(EquatableIterator<ValueType> obj)
            {
                Iterator rhs = obj as Iterator;
                if (rhs == null)
                    return false;

                return HashContainer == rhs.HashContainer && Current == rhs.Current;
            }
            protected override int HashCode()
            {
                return HashContainer.GetHashCode() ^ Current.GetHashCode();
            }
            private readonly HashContainer<KeyType, ValueType> hashContainer;
            private HashContainerNode<ValueType> current;
        }
        #endregion

        private int numElements;
        private IList<HashContainerNode<ValueType>> buckets;
        
        private readonly IUnaryFunction<ValueType, KeyType> extractKey;
        //private readonly float loadFactor = 1.0f;
        private readonly IEqualityComparer<KeyType> hashStrategy = EqualityComparer<KeyType>.Default;

        internal HashContainer(int initialBuffer, IUnaryFunction<ValueType, KeyType> extractKey)
            : this(initialBuffer, extractKey, EqualityComparer<KeyType>.Default)
        { }
        internal HashContainer(int initialBuffer, IUnaryFunction<ValueType, KeyType> extractKey, IEqualityComparer<KeyType> hashStrategy)
        {
            InitializeBuckets(initialBuffer);
            this.extractKey = extractKey;
            this.hashStrategy = hashStrategy;
        }
        internal ValueType[] ToArray()
        {
            ValueType[] rv = new ValueType[Count];
            CopyTo(rv, 0);
            return rv;
        }
        public void CopyTo(Array array, int idx)
        {
            foreach (ValueType v in this)
                array.SetValue(v, idx++);
        }


        private void InitializeBuckets(int n)
        {
            int bucketCount = NextSize(n);
            buckets = new HashContainerNode<ValueType>[bucketCount];
            numElements = 0;
        }
        internal void Clear()
        {
            InitializeBuckets(Count);
        }
        private static int NextSize(int n)
        {
            return HashContainerHelper.NextPrime(n);
        }
        internal int Capacity
        {
            get { return buckets.Count; }
        }
        public int Count
        {
            get { return numElements; }
        }

        public int CountOf(KeyType key)
          {
            int n = BucketNumberKey(key);
            int result = 0;

            for (HashContainerNode<ValueType> cur = buckets[n]; cur != null; cur = cur.Next)
                if (hashStrategy.Equals(extractKey.Execute(cur.Value), key))
                    ++result;
            return result;
          }
        internal KeyValuePair<Iterator, bool> InsertUnique(ValueType val)
        {
          Resize(numElements + 1);
          return InsertUniqueNoResize(val);
        }

        internal Iterator Find(KeyType key)
        {
            int n = BucketNumberKey(key);
            HashContainerNode<ValueType> first;
            for (first = buckets[n];
                 first != null && !hashStrategy.Equals(extractKey.Execute(first.Value), key);
                 first = first.Next)
            { }
            return new Iterator(first, this);
        }
        internal Iterator Begin()
        {
            for (int n = 0; n < buckets.Count; ++n)
                if (buckets[n] != null)
                    return new Iterator(buckets[n], this);
            return End();
        }
        internal Iterator End()
        {
            return new Iterator(null, this);
        }
        internal Iterator InsertEqual(ValueType val)
        {
            Resize(numElements + 1);
            return InsertEqualNoResize(val);
        }
        internal int Erase(KeyType key)
        {
            int n = BucketNumberKey(key);
            HashContainerNode<ValueType> first = buckets[n];
            int erased = 0;

            if (first == null)
                return 0;

            HashContainerNode<ValueType> cur = first;
            HashContainerNode<ValueType> next = cur.Next;
            while (next != null)
            {
                if (hashStrategy.Equals(extractKey.Execute(next.Value), key))
                {
                    cur.Next = next.Next;
                    //_M_delete_node(__next);
                    next = cur.Next;
                    ++erased;
                    --numElements;
                }
                else
                {
                    cur = next;
                    next = cur.Next;
                }
            }
            if (hashStrategy.Equals(extractKey.Execute(first.Value), key))
            {
                buckets[n] = first.Next;
                //_M_delete_node(__first);
                ++erased;
                --numElements;
            }

            return erased;
        }

        internal void Erase(Iterator it)
        {
            HashContainerNode<ValueType> p = it.Current;
            if(p == null)
                return;
            int n = BucketNumber(p.Value);
            HashContainerNode<ValueType> cur = buckets[n];

            if (cur == p)
            {
                buckets[n] = cur.Next;
                //_M_delete_node(__cur);
                --numElements;
            }
            else
            {
                HashContainerNode<ValueType> next = cur.Next;
                while (next != null)
                {
                    if (next == p)
                    {
                        cur.Next = next.Next;
                        //_M_delete_node(__next);
                        --numElements;
                        break;
                    }
                    else
                    {
                        cur = next;
                        next = cur.Next;
                    }
                }
            }
        }
        internal void Erase(Iterator first, Iterator last)
        {
            int firstBucket = first.Current != null 
                            ? BucketNumber(first.Current.Value) 
                            : buckets.Count;

            int lastBucket = last.Current != null 
                           ? BucketNumber(last.Current.Value) 
                           : buckets.Count;

            if (first.Current == last.Current)
                return;
            else if (firstBucket == lastBucket)
                EraseBucket(firstBucket, first.Current, last.Current);
            else
            {
                EraseBucket(firstBucket, first.Current, null);
                for (int n = firstBucket + 1; n < lastBucket; ++n)
                    EraseBucket(n, null);
                if (lastBucket != buckets.Count)
                    EraseBucket(lastBucket, last.Current);
            }
        }
        internal ValueType FindOrInsert(ValueType val)
        {
            Resize(numElements + 1);

            int n = BucketNumber(val);
            HashContainerNode<ValueType> first = buckets[n];

            for (HashContainerNode<ValueType> cur = first; cur != null; cur = cur.Next)
                if (hashStrategy.Equals(extractKey.Execute(cur.Value), extractKey.Execute(val)) )
                    return cur.Value;

            HashContainerNode<ValueType> tmp = new HashContainerNode<ValueType>(val);
            tmp.Next = first;
            buckets[n] = tmp;
            ++numElements;
            return tmp.Value;
        }
        internal KeyValuePair<Iterator, Iterator>
            EqualRange(KeyType key)
        {
            int n = BucketNumberKey(key);

            for (HashContainerNode<ValueType> first = buckets[n]; first != null; first = first.Next)
                if (hashStrategy.Equals(extractKey.Execute(first.Value), key)){
                    for (HashContainerNode<ValueType> cur = first.Next; cur != null; cur = cur.Next)
                        if (!hashStrategy.Equals(extractKey.Execute(cur.Value), key))
                            return new KeyValuePair<Iterator, Iterator>(new Iterator(first, this), new Iterator(cur, this));
                    for (int m = n + 1; m < buckets.Count; ++m)
                        if (buckets[m] != null)
                            return new KeyValuePair<Iterator, Iterator>(new Iterator(first, this),
                                       new Iterator(buckets[m], this));
                    return new KeyValuePair<Iterator, Iterator>(new Iterator(first, this), End());
                }
            return new KeyValuePair<Iterator, Iterator>(End(), End());
        }
        private void EraseBucket(int n, HashContainerNode<ValueType> last)
        {
            HashContainerNode<ValueType> cur = buckets[n];
            while (cur != last)
            {
                HashContainerNode<ValueType> next = cur.Next;
                //_M_delete_node(__cur);
                cur = next;
                buckets[n] = cur;
                --numElements;
            }
        }

        private void EraseBucket(int n, HashContainerNode<ValueType> first, HashContainerNode<ValueType> last)
        {
            HashContainerNode<ValueType> cur = buckets[n];
            if (cur == first)
                EraseBucket(n, last);
            else
            {
                HashContainerNode<ValueType> next;
                for (next = cur.Next;
                     next != first;
                     cur = next, next = cur.Next){}

                while (next != last)
                {
                    cur.Next = next.Next;
                    //_M_delete_node(__next);
                    next = cur.Next;
                    --numElements;
                }
            }

        }


        private Iterator InsertEqualNoResize(ValueType obj)
        {
            int n = BucketNumber(obj);
            HashContainerNode<ValueType> first = buckets[n];
            HashContainerNode<ValueType> tmp;

            for (HashContainerNode<ValueType> cur = first; cur != null; cur = cur.Next)
            {
                if (hashStrategy.Equals(extractKey.Execute(cur.Value), extractKey.Execute(obj)))
                {
                    tmp = new HashContainerNode<ValueType>(obj);
                    tmp.Next = cur.Next;
                    cur.Next = tmp;
                    ++numElements;
                    return new Iterator(tmp, this);
                }
            }

            tmp = new HashContainerNode<ValueType>(obj);
            tmp.Next = first;
            buckets[n] = tmp;
            ++numElements;
            return new Iterator(tmp, this);

        }

        private KeyValuePair<Iterator, bool> InsertUniqueNoResize(ValueType obj)
        {
            int n = BucketNumber(obj);
            HashContainerNode<ValueType> first = buckets[n];

            for (HashContainerNode<ValueType> cur = first; cur != null; cur = cur.Next)
                if (hashStrategy.Equals(extractKey.Execute(cur.Value), extractKey.Execute(obj)))
                    return new KeyValuePair<Iterator, bool>(new Iterator(cur, this), false);

            HashContainerNode<ValueType> tmp = new HashContainerNode<ValueType>(obj);
            tmp.Next = first;
            buckets[n] = tmp;
            ++numElements;
            return new KeyValuePair<Iterator, bool>(new Iterator(tmp, this), true);
        }
        int BucketNumber(ValueType obj) 
        {
            return BucketNumberKey(extractKey.Execute(obj));
        }
        int BucketNumber(ValueType obj, int n)
        {
            return BucketNumberKey(extractKey.Execute(obj), n);
        }
        int BucketNumberKey(KeyType key)
        {
            return BucketNumberKey(key, buckets.Count);
        }
        int BucketNumberKey(KeyType key, int n) 
        {
            return (hashStrategy.GetHashCode(key) & int.MaxValue)% n;
        }
        private void Resize(int numElementsHint)
        {
            int oldN = buckets.Count;
            if (numElementsHint > oldN)
            {
                int n = NextSize(numElementsHint);
                if (n > oldN)
                {
                    IList<HashContainerNode<ValueType>> tmp = new HashContainerNode<ValueType>[n];

                    for (int bucket = 0; bucket < oldN; ++bucket)
                    {
                        HashContainerNode<ValueType> first = buckets[bucket];
                        while (first != null)
                        {
                            int newBucket = BucketNumber(first.Value, n);
                            buckets[bucket] = first.Next;
                            first.Next = tmp[newBucket];
                            tmp[newBucket] = first;
                            first = buckets[bucket];
                        }
                    }
                    buckets = tmp;
                }
            }
        }

        #region ICollection Members



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

        #region ISerializable Members
        private const int Version = 1;
        protected HashContainer(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            numElements = info.GetInt32("numElements");
            buckets = (HashContainerNode<ValueType>[])info.GetValue("buckets", typeof(HashContainerNode<ValueType>[]));

            extractKey = (IUnaryFunction<ValueType, KeyType>)info.GetValue("extractKey", typeof(IUnaryFunction<ValueType, KeyType>));
            hashStrategy = (IEqualityComparer<KeyType>)info.GetValue("hashStrategy", typeof(IEqualityComparer<KeyType>));
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

            info.AddValue("numElements", numElements);
            info.AddValue("buckets", buckets);

            info.AddValue("extractKey", extractKey, typeof(IUnaryFunction<ValueType, KeyType>));
            info.AddValue("hashStrategy", hashStrategy, typeof(IEqualityComparer<KeyType>));
        }

        #endregion
    }
}

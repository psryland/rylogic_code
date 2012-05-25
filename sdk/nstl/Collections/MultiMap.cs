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
using NStl.Iterators;
using NStl.Iterators.Support;
using NStl.Linq;
using NStl.Util;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NStl.SyntaxHelper;
using NStl.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// MultiMap is a Sorted Associative Container that associates objects of type Key 
    /// with objects of type Value. multimap is a Pair Associative Container, meaning 
    /// that its value type is a <see cref="KeyValuePair{Key, Value}"/>. It is also a Multiple Associative 
    /// Container, meaning that there is no limit on the number of elements with the 
    /// same key. MultiMap has the important property that inserting a new element 
    /// into a MultiMap does not invalidate iterators that point to existing elements. 
    /// Erasing an element from a MultiMap also does not invalidate any iterators, 
    /// except, of course, for iterators that actually point to the element that is 
    /// being erased. 
    /// </summary>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public class MultiMap<Key, Value> : IInsertable<KeyValuePair<Key, Value>, MultiMap<Key, Value>.Iterator>, ICollection, IDictionary<Key, Value>,
                                        IRange<KeyValuePair<Key, Value>>, ISerializable, ISortedRange<KeyValuePair<Key, Value>>
    {
        /// <summary>
        /// Constructs an empty map using the provided comparer functor
        /// </summary>
        /// <param name="comparison"></param>
        public MultiMap(IBinaryFunction<Key, Key, bool> comparison)
            : this(new RbTree<Key, KeyValuePair<Key, Value>>(comparison, Select.FirstFromKeyValuePair<Key, Value>()))
        {
        }

        /// <summary>
        /// Constructs a map with an external tree implementation
        /// </summary>
        /// <param name="tree"></param>
        private MultiMap(RbTree<Key, KeyValuePair<Key, Value>> tree)
        {
            this.tree = tree;
        }

        /// <summary>
        /// ctor, copies the passed in range into this map
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="first">The position of the first element to be copied to the map</param>
        /// <param name="last">The position just beyond the last element to be copied to the map</param>
        public MultiMap(IBinaryFunction<Key, Key, bool> comparison, IInputIterator<KeyValuePair<Key, Value>> first,
                        IInputIterator<KeyValuePair<Key, Value>> last)
            : this(comparison)
        {
            Insert(first, last);
        }

        /// <summary>
        /// ctor, copies the passed in range into this map.
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="list">The IList to be copied into this map</param>
        public MultiMap(IBinaryFunction<Key, Key, bool> comparison, IEnumerable list)
            : this(comparison, list.Cast<KeyValuePair<Key, Value>>())
        {
        }
        /// <summary>
        /// Constructs the container and copies the parameters into it.
        /// </summary>
        /// <param name="comparison"></param>
        /// <param name="list"></param>
        public MultiMap(IBinaryFunction<Key, Key, bool> comparison, params KeyValuePair<Key, Value>[] list)
            : this(comparison, (IEnumerable<KeyValuePair<Key, Value>>)list)
        {}

        /// <summary>
        /// ctor, copies the passed in range into this map.
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="list">The IList to be copied into this map</param>
        public MultiMap(IBinaryFunction<Key, Key, bool> comparison, IEnumerable<KeyValuePair<Key, Value>> list)
            : this(comparison, list.Begin(), list.End())
        {}

       
        /// <summary>
        /// Returns an iterator that addresses the first element in the set.
        /// </summary>
        /// <returns></returns>
        public Iterator
            Begin()
        {
            return new Iterator(tree.Begin());
        }

        /// <summary>
        /// Returns an iterator pointing one past the last element of the set
        /// </summary>
        /// <returns></returns>
        public Iterator
            End()
        {
            return new Iterator(tree.End());
        }

        /// <summary>
        /// Returns an iterator addressing the first element in a reversed set
        /// </summary>
        /// <returns></returns>
        public IBidirectionalIterator<KeyValuePair<Key, Value>>
            RBegin()
        {
            return tree.RBegin();
        }

        /// <summary>
        /// Returns an iterator pointing one past the last element of the reversed set
        /// </summary>
        /// <returns></returns>
        public IBidirectionalIterator<KeyValuePair<Key, Value>>
            REnd()
        {
            return tree.REnd();
        }

        /// <summary>
        /// Inserts a key value pair into the multimap
        /// </summary>
        /// <param name="val">The key value pair to be inserted</param>
        /// <returns>
        /// An iterator that points to the position where the new element was inserted
        /// </returns>
        public Iterator
            Insert(KeyValuePair<Key, Value> val)
        {
            Verify.ArgumentNotNull(val.Key, "val.Key");
            return new Iterator(tree.InsertEqual(val));
        }

        /// <summary>
        /// Inserts a key value pair into the multimap
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns>
        /// An iterator that points to the position where the new element was inserted
        /// </returns>
        public Iterator
            Insert(Key key, Value item)
        {
            Verify.ArgumentNotNull(key, "key");
            return new Iterator(tree.InsertEqual(new KeyValuePair<Key, Value>(key, item)));
        }

        /// <summary>
        /// Inserts an element into the multimap
        /// </summary>
        /// <param name="where">
        /// The place to start searching for the correct point of insertion. 
        /// (Insertion can occur in constant time, instead of logarithmic 
        /// time, if the insertion point immediately follows where.) 
        /// </param>
        /// <param name="val">
        /// The value of an element to be inserted into the set 
        /// </param>
        /// <returns>
        /// An iterator that points to the position where the new element was inserted
        /// </returns>
        public Iterator
            Insert(Iterator where, KeyValuePair<Key, Value> val)
        {
            return new Iterator(tree.InsertEqual(where.TreeIterator, val));
        }

        /// <summary>
        /// Insert a range of elements into the uniqueset
        /// </summary>
        /// <param name="first">The position of the first element to be copied to the set</param>
        /// <param name="last">The position just beyond the last element to be copied to the set</param>
        public void
            Insert(IInputIterator<KeyValuePair<Key, Value>> first, IInputIterator<KeyValuePair<Key, Value>> last)
        {
            tree.InsertEqual(first, last);
        }

        /// <summary>
        /// Erases all elements
        /// </summary>
        public void Clear()
        {
            tree.Clear();
        }

        /// <summary>
        /// Returns the number of elements in the set using the internal comparison functor
        /// </summary>
        /// <param name="key">The object to be counted</param>
        /// <returns></returns>
        public int CountOf(Key key)
        {
            return tree.Count(key);
        }

        /// <summary>
        /// Checks if the set is empty
        /// </summary>
        /// <returns></returns>
        public bool Empty
        {
            get { return tree.Empty(); }
        }

        /// <summary>
        /// Finds a range containing all elements whose key is Key
        /// </summary>
        /// <param name="key">
        /// The argument key to be compared with the sort key of an element from the 
        /// set being searched
        /// </param>
        /// <returns>
        /// A pair of iterators where the first is the lower_bound of the key and 
        /// the second is the upper_bound of the key
        /// </returns>
        public Range<KeyValuePair<Key, Value>, Iterator>
            EqualRange(Key key)
        {
            KeyValuePair<RbTreeIterator<KeyValuePair<Key, Value>>, RbTreeIterator<KeyValuePair<Key, Value>>> r =
                tree.EqualRange(key);
            return new Range<KeyValuePair<Key, Value>, Iterator>(new Iterator(r.Key), new Iterator(r.Value));
        }

        /// <summary>
        /// Removes the element at the specified position
        /// </summary>
        /// <param name="where">Position of the element to be removed from the set</param>
        public void
            Erase(Iterator where)
        {
            Verify.ArgumentNotNull(where, "where");
            tree.Erase(where.TreeIterator);
        }

        /// <summary>
        /// Removes a range of elements from the set
        /// </summary>
        /// <param name="first">Position of the first element removed from the set</param>
        /// <param name="last">Position just beyond the last element removed from the set</param>
        public void
            Erase(Iterator first, Iterator last)
        {
            tree.Erase(first.TreeIterator, last.TreeIterator);
        }

        /// <summary>
        /// Erases a specific value from the set
        /// </summary>
        /// <param name="key">The key of the elements to be removed from the s</param>
        /// <returns>the count of the erased elements. This is either 1 or 0</returns>
        public int
            Erase(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return tree.Erase(key);
        }

        /// <summary>
        /// Finds the location of a specific element
        /// </summary>
        /// <param name="key">The key to be found using the internal comparison functor</param>
        /// <returns></returns>
        public Iterator
            Find(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return new Iterator(tree.Find(key));
        }

        /// <summary>
        /// Returns an iterator to the first element in a set with a key that is equal 
        /// to or less than a specified key
        /// </summary>
        /// <param name="key">The value to be found</param>
        /// <returns></returns>
        /// <remarks>Comparison is one by the internal comparison Predicate</remarks>
        public Iterator
            LowerBound(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return new Iterator(tree.LowerBound(key));
        }

        /// <summary>
        /// Returns an iterator one past the first element in a set with a key that is 
        /// equal to or greater than a specified key
        /// </summary>
        /// <param name="key">The value to be found</param>
        /// <returns></returns>
        public Iterator
            UpperBound(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return new Iterator(tree.UpperBound(key));
        }

        /// <summary>
        /// Returns the number of elements in the set
        /// </summary>
        /// <returns></returns>
        public int Count
        {
            get { return tree.Size(); }
        }

        /// <summary>
        /// Swaps the content of this set with the other set
        /// </summary>
        /// <param name="rhs"></param>
        /// <remarks>Extreme fast operation, as this is a buffer swap</remarks>
        public void
            Swap(MultiMap<Key, Value> rhs)
        {
            Algorithm.Swap(ref tree, ref rhs.tree);
        }

        #region IInsertable<pair<Key,Value>> Members

        Iterator IInsertable<KeyValuePair<Key, Value>, Iterator>.Insert(
            Iterator where, KeyValuePair<Key, Value> val)
        {
            return Insert((Iterator) where, val);
        }

        #endregion

        #region IRange<pair<Key,Value>> Members

        IInputIterator<KeyValuePair<Key, Value>> IRange<KeyValuePair<Key, Value>>.Begin()
        {
            return Begin();
        }

        IInputIterator<KeyValuePair<Key, Value>> IRange<KeyValuePair<Key, Value>>.End()
        {
            return End();
        }

        #endregion

        private RbTree<Key, KeyValuePair<Key, Value>> tree;

        #region IDictionary<Key,Value> Members

        void IDictionary<Key, Value>.Add(Key key, Value value)
        {
            Insert(key, value);
        }

        bool IDictionary<Key, Value>.ContainsKey(Key key)
        {
            return !Equals(Find(key), End());
        }

        ICollection<Key> IDictionary<Key, Value>.Keys
        {
            get
            {
                Key[] keys = new Key[Count];
                Algorithm.Transform(Begin(), End(), keys.Begin(), Select.FirstFromKeyValuePair<Key, Value>());
                return keys;
            }
        }

        bool IDictionary<Key, Value>.Remove(Key key)
        {
            return Erase(key) > 0;
        }

        bool IDictionary<Key, Value>.TryGetValue(Key key, out Value value)
        {
            IBidirectionalInputIterator<KeyValuePair<Key, Value>> it = Find(key);
            if (!Equals(it, End()))
            {
                value = it.Value.Value;
                return true;
            }
            value = default(Value);
            return false;
        }

        ICollection<Value> IDictionary<Key, Value>.Values
        {
            get
            {
                Value[] vals = new Value[Count];
                Algorithm.Transform(Begin(), End(), vals.Begin(), Select.SecondFromKeyValuePair<Key, Value>());
                return vals;
            }
        }

        Value IDictionary<Key, Value>.this[Key key]
        {
            get
            {
                IBidirectionalInputIterator<KeyValuePair<Key, Value>> i = LowerBound(key);
                if (!Equals(i, End()))
                    return i.Value.Value;
                throw new KeyNotFoundException();
            }
            set
            {
                Iterator i = LowerBound(key);
                if (!Equals(i, End()))
                    i.SetValue(value);
                else
                    Insert(key, value);
            }
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
            Iterator v = Find(item.Key);
            return !Equals(v, End()) && v.Value.Value.Equals(item.Value);
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
            Iterator v = Find(item.Key);
            if (!Equals(v, End()) && v.Value.Value.Equals(item.Value))
            {
                Erase(v);
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<KeyValuePair<Key,Value>> Members

        IEnumerator<KeyValuePair<Key, Value>> IEnumerable<KeyValuePair<Key, Value>>.GetEnumerator()
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

        #region Iterator

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This scoping binds the iterator to its container and allows internal container access.")]
        public sealed class Iterator : BidirectionalIterator2InputBidirectionalIteratorAdapter<KeyValuePair<Key, Value>>
        {
            internal Iterator(RbTreeIterator<KeyValuePair<Key, Value>> treeIt)
                : base(treeIt)
            {
            }

            internal Iterator(IBidirectionalIterator<KeyValuePair<Key, Value>> treeIt)
                : this((RbTreeIterator<KeyValuePair<Key, Value>>) treeIt)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
            public static Iterator operator ++(Iterator it)
            {
                Iterator tmp = (Iterator) it.Clone();
                return (Iterator) tmp.PreIncrement();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="it"></param>
            /// <returns></returns>
            [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Post/PreDecrement() already exist")]
            public static Iterator operator --(Iterator it)
            {
                Iterator tmp = (Iterator) it.Clone();
                return (Iterator) tmp.PreDecrement();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override IIterator<KeyValuePair<Key, Value>> Clone()
            {
                return new Iterator(Inner);
            }

            internal RbTreeIterator<KeyValuePair<Key, Value>> TreeIterator
            {
                get { return (RbTreeIterator<KeyValuePair<Key, Value>>) Inner; }
            }
            internal void SetValue(Value value)
            {
                Inner.Value = new KeyValuePair<Key, Value>(TreeIterator.Value.Key, value);
            }
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            foreach (KeyValuePair<Key, Value> t in this)
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
        /// <summary>
        /// Copies the content of this container into an array.
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<Key, Value>[] ToArray()
        {
            KeyValuePair<Key, Value>[] a = new KeyValuePair<Key, Value>[Count];
            CopyTo(a, 0);
            return a;
        }
        /// <summary>
        /// Copies the content of this container into the passed in array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
        {
            Algorithm.Copy(Begin(), End(), array.Begin() + arrayIndex);
        }

        #region ISerializable Members
        private const int Version = 1;
        /// <summary>
        /// Deserialization contructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected MultiMap(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            tree = (RbTree<Key, KeyValuePair<Key, Value>>)info.GetValue("tree", typeof(RbTree<Key, KeyValuePair<Key, Value>>));
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
            info.AddValue("tree", tree);
        }
        #endregion
    }
}

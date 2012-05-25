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
using NStl;
using NStl.Collections.Private;
using NStl.Debugging;
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
    /// Map is a Sorted Associative Container that associates objects of type Key with 
    /// objects of type Value. map is a Pair Associative Container, meaning that its 
    /// value type is pair[Key, Value]. It is also a Unique Associative Container, 
    /// meaning that no two elements have the same key. map has the important property 
    /// that inserting a new element into a map does not invalidate iterators that 
    /// point to existing elements. Erasing an element from a map also does not 
    /// invalidate any iterators, except, of course, for iterators that actually point 
    /// to the element that is being erased. 
    /// </summary>
    /// <remarks>
    /// Map is avery similar to <see cref="SortedDictionary{K,V}"/> and <see cref="SortedList{K,V}"/>.
    /// It is closer to SortedDictionary in performance and offers <see cref="IBidirectionalIterator{T}"/>
    /// implementations. If this is not of importance, the .NET collections sould be prefered.
    /// </remarks>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public class Map<Key, Value> : IRange<KeyValuePair<Key, Value>>,
                                   IInsertable<KeyValuePair<Key, Value>, Map<Key, Value>.Iterator>, IDictionary<Key, Value>, ISerializable, ICollection, ISortedRange<KeyValuePair<Key, Value>>
    {
        /// <summary>
        /// Constructs an empty map using the provided comparer functor
        /// </summary>
        /// <param name="comparison"></param>
        public Map(IBinaryFunction<Key, Key, bool> comparison)
        {
            tree = new RbTree<Key, KeyValuePair<Key, Value>>(comparison, Select.FirstFromKeyValuePair<Key, Value>());
        }

        /// <summary>
        /// Constructs a map with an external tree implementation
        /// </summary>
        /// <param name="tree"></param>
        internal Map(RbTree<Key, KeyValuePair<Key, Value>> tree)
        {
            this.tree = tree;
        }

        /// <summary>
        /// ctor, copies the passed in range into this map
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="first">The position of the first element to be copied to the map</param>
        /// <param name="last">The position just beyond the last element to be copied to the map</param>
        public Map(IBinaryFunction<Key, Key, bool> comparison, IInputIterator<KeyValuePair<Key, Value>> first,
                   IInputIterator<KeyValuePair<Key, Value>> last)
            : this(comparison)
        {
            Insert(first, last);
        }

        /// <summary>
        /// ctor, copies the passed in range into this map.
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="list">The IEnumerable to be copied into this map</param>
        public Map(IBinaryFunction<Key, Key, bool> comparison, IEnumerable list)
            : this(comparison, list.Begin<KeyValuePair<Key, Value>>(), list.End<KeyValuePair<Key, Value>>())
        {}
        /// <summary>
        /// ctor, copies the passed in range into this map.
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="list">The IEnumerable to be copied into this map</param>
        public Map(IBinaryFunction<Key, Key, bool> comparison, params KeyValuePair<Key, Value>[] list)
            : this(comparison, (IEnumerable<KeyValuePair<Key, Value>>)list)
        { }

        /// <summary>
        /// ctor, copies the passed in range into this map.
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="list">The IEnumerable to be copied into this map</param>
        public Map(IBinaryFunction<Key, Key, bool> comparison, IEnumerable<KeyValuePair<Key, Value>> list)
            : this(comparison, list.Begin(), list.End())
        {}
        /// <summary>
        /// Returns an iterator that addresses the first element in the map.
        /// </summary>
        /// <returns></returns>
        public Iterator
            Begin()
        {
            return new Iterator(tree.Begin());
        }

        /// <summary>
        /// Returns an iterator that addresses the location succeeding the last element in the map
        /// </summary>
        /// <returns></returns>
        public Iterator
            End()
        {
            return new Iterator(tree.End());
        }

        /// <summary>
        /// Returns an iterator addressing the first element in a reversed map
        /// </summary>
        /// <returns></returns>
        public IBidirectionalInputIterator<KeyValuePair<Key, Value>>
            RBegin()
        {
            return NStlUtil.Reverse(End());
        }

        /// <summary>
        /// Returns an iterator that points to the position past the last element in a reversed map
        /// </summary>
        /// <returns></returns>
        public IBidirectionalInputIterator<KeyValuePair<Key, Value>>
            REnd()
        {
            return NStlUtil.Reverse(Begin());
        }

        /// <summary>
        /// Insert an element into the map. 
        /// </summary>
        /// <param name="val">
        /// The [key, value] pair to be inserted into the map
        /// </param>
        /// <returns>A pair of [iterator, bool]. The bool value will be true, if the
        /// map didn't contain the inserted key and the iterator will point to the inserted pair. 
        /// Otherwise it will be false and the iterator will point to the end of the map</returns>
        public KeyValuePair<Iterator, bool>
            Insert(KeyValuePair<Key, Value> val)
        {
            Verify.ArgumentNotNull(val.Key, "val.Key");
            KeyValuePair<IBidirectionalIterator<KeyValuePair<Key, Value>>, bool> r =
                tree.InsertUnique(val);
            return new KeyValuePair<Iterator, bool>(new Iterator(r.Key), r.Value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public KeyValuePair<Iterator, bool>
            Insert(Key key, Value val)
        {
            return Insert(new KeyValuePair<Key, Value>(key, val));
        }

        /// <summary>
        /// Insert an element into the map
        /// </summary>
        /// <param name="where">
        /// The place to start searching for the correct point of insertion. 
        /// (Insertion can occur in amortized constant time, instead of logarithmic 
        /// time, if the insertion point immediately follows where.) 
        /// </param>
        /// <param name="val">
        /// The value of an element to be inserted into the map unless the map 
        /// already contains that element 
        /// </param>
        /// <returns>
        /// An iterator that points to the position where the new element was inserted or
        /// the end iterator in the case of a failure
        /// </returns>
        public Iterator
            Insert(Iterator where, KeyValuePair<Key, Value> val)
        {
            return new Iterator(tree.InsertUnique(where.TreeIterator, val));
        }

        /// <summary>
        /// Insert a range of elements into the map
        /// </summary>
        /// <param name="first">The position of the first element to be copied to the map</param>
        /// <param name="last">The position just beyond the last element to be copied to the map</param>
        public void
            Insert(IInputIterator<KeyValuePair<Key, Value>> first, IInputIterator<KeyValuePair<Key, Value>> last)
        {
            tree.InsertUnique(first, last);
        }

        /// <summary>
        /// The size of the map
        /// </summary>
        /// <returns></returns>
        public int Count
        {
            get { return tree.Size(); }
        }

        /// <summary>
        /// CLears tha maps content
        /// </summary>
        public void Clear()
        {
            tree.Clear();
        }

        /// <summary>
        /// Checks if the map is empty
        /// </summary>
        /// <returns></returns>
        public bool Empty
        {
            get { return tree.Empty(); }
        }

        /// <summary>
        /// Returns the number of elements in a map whose key matches the parameter
        /// </summary>
        /// <param name="key">The key value of the elements to be matched from the map</param>
        /// <returns>1 if the map contains the key, 0 otherwise</returns>
        public int CountOf(Key key)
        {
            return tree.Count(key);
        }

        /// <summary>
        /// Returns two iterators that specify a sub range in the set where the values
        /// match the given key
        /// </summary>
        /// <param name="key">The argument key to be searched</param>
        /// <returns>A pair of iterators where the first is the lower_bound of the key and 
        /// the second is the upper_bound of the key</returns>
        public Range<KeyValuePair<Key, Value>, Iterator>
            EqualRange(Key key)
        {
            KeyValuePair
                <RbTreeIterator<KeyValuePair<Key, Value>>, RbTreeIterator<KeyValuePair<Key, Value>>> r =
                    tree.EqualRange(key);
            return new Range<KeyValuePair<Key, Value>, Iterator>(new Iterator(r.Key), new Iterator(r.Value));
        }

        /// <summary>
        /// Removes the element at the specified position
        /// </summary>
        /// <param name="where">Position of the element to be removed from the map</param>
        /// <remarks>
        /// The C++ STL set returns an valid iterator, however the SGI set does not. As I never quite
        /// saw the usefullness of the returned iterators, I stuck to the SGI implementation.
        /// If there is a strong wish from a lot of users, I will put the return value back in again.
        /// </remarks>
        public void
            Erase(Iterator where)
        {
            Verify.ArgumentNotNull(where, "where");
            tree.Erase(where.TreeIterator);
        }

        /// <summary>
        /// Erases a range of elements from the map
        /// </summary>
        /// <param name="first">Position of the first element removed from the map</param>
        /// <param name="last">Position just beyond the last element removed from the map</param>
        /// <remarks>
        /// The C++ STL set returns an valid iterator, however the SGI set does not. As I never quite
        /// saw the usefullness of the returned iterators, I stuck to the SGI implementation.
        /// If there is a strong wish from a lot of users, I will put the return value back in again.
        /// </remarks>
        public void
            Erase(Iterator first, Iterator last)
        {
            tree.Erase(first.TreeIterator, last.TreeIterator);
        }

        /// <summary>
        /// Erases a range of elements from the map if they satisfy a predicate
        /// </summary>
        /// <param name="first">Position of the first element removed from the map</param>
        /// <param name="last">Position just beyond the last element removed from the map</param>
        /// <param name="predicate">A unary predicate that needs to be satisfied</param>
        /// <remarks>
        /// This method is not part of the C++ STL. Unexperienced C++ Developers often stumble
        /// over the fact, that you can't use the remove-erase idiom that works for sequence
        /// containers for associative sorted containers like a map. So this method is provided to
        /// avoid this glitch in the .NET world
        /// </remarks>
        public void
            EraseIf(Iterator first, Iterator last, IUnaryFunction<KeyValuePair<Key, Value>, bool> predicate)
        {
            for (Iterator i = first; !Equals(i, last); )
            {
                if (predicate.Execute(i.Value))
                    tree.Erase(((Iterator) i.PostIncrement()).TreeIterator);
                else
                    i.PreIncrement();
            }
        }

        /// <summary>
        /// Erases all elements from the map that satisfy a predicate
        /// </summary>
        /// <param name="predicate">A unary predicate that needs to be satisfied</param>
        /// <remarks>
        /// This method is not part of the C++ STL. Unexperienced C++ Developers often stumble
        /// over the fact, that you can't use the remove-erase idiom that works for sequence
        /// containers for associative sorted containers like a map. So this method is provided to
        /// avoid this glitch in the .NET world
        /// </remarks>
        public void
            EraseIf(IUnaryFunction<KeyValuePair<Key, Value>, bool> predicate)
        {
            EraseIf(Begin(), End(), predicate);
        }

        /// <summary>
        /// Erases a specific value from the map
        /// </summary>
        /// <param name="val">The key of the elements to be removed from the map</param>
        /// <returns>the count of the erased elements. This is either 1 or 0</returns>
        public int
            Erase(Key val)
        {
            return tree.Erase(val);
        }

        /// <summary>
        /// Finds the location of a specific element by its key
        /// </summary>
        /// <param name="key">The key to be found using the internal comparison functor</param>
        /// <returns>An iterator pointing to the found value or the end iterator if the value is
        /// not in the map</returns>
        public Iterator
            Find(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
            return new Iterator(tree.Find(key));
        }

        /// <summary>
        /// Finds a specific value by its key.
        /// </summary>
        /// <remarks>
        /// Will throw, if the Key is not part of the map, as null/nothing is
        /// a valid entry for a value. If you want to be on te safe side, use map.find instead
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Required by implemented interface!")]
        public Value this[Key key]
        {
            get
            {
                Verify.ArgumentNotNull(key, "key");

                Iterator it = Find(key);
                if (Equals(it, End()))
                    throw new KeyNotFoundException("The key " + key + " is not part of the map");
                return it.Value.Value;
            }
            set
            {
                Verify.ArgumentNotNull(key, "key");

                // .. this is ok, as the key stays the same!!
                Iterator it = Find(key);
                if (Equals(it, End()))
                    Insert(key, value);
                else
                    it.SetValue(value);
            }
        }

        /// <summary>
        /// Returns an iterator to the first element in a map with a key that is equal 
        /// to or less than a specified key
        /// </summary>
        /// <param name="key">The Key to be found</param>
        /// <returns></returns>
        /// <remarks>Comparison is one by the internal comparison Predicate</remarks>
        public Iterator
            LowerBound(Key key)
        {
            return new Iterator(tree.LowerBound(key));
        }

        /// <summary>
        /// Returns an iterator one past the first element in a map with a key that is 
        /// equal to or greater than a specified key
        /// </summary>
        /// <param name="key">The Key to be found</param>
        /// <returns></returns>
        public Iterator
            UpperBound(Key key)
        {
            return new Iterator(tree.UpperBound(key));
        }

        /// <summary>
        /// Swaps the content of this map with the other set
        /// </summary>
        /// <param name="rhs"></param>
        /// <remarks>Extreme fast operation, as this is a buffer swap</remarks>
        public void
            Swap(Map<Key, Value> rhs)
        {
            RbTree<Key, KeyValuePair<Key, Value>> tmp = rhs.tree;
            rhs.tree = tree;
            tree = tmp;
        }
        #region IInsertable Members

        Iterator IInsertable<KeyValuePair<Key, Value>, Iterator>.Insert(Iterator where, KeyValuePair<Key, Value> val)
        {
            return Insert(where, val);
        }

        #endregion
        #region IRange Members

        IInputIterator<KeyValuePair<Key, Value>> IRange<KeyValuePair<Key, Value>>.Begin()
        {
            return Begin();
        }

        IInputIterator<KeyValuePair<Key, Value>> IRange<KeyValuePair<Key, Value>>.End()
        {
            return End();
        }

        #endregion
        #region IDictionary<Key,Value> Members

        void IDictionary<Key, Value>.Add(Key key, Value value)
        {
            Verify.ArgumentNotNull(key, "key");
            KeyValuePair<Iterator, bool> rv =
                Insert(key, value);

            if (!rv.Value)
                throw new ArgumentException(Resource.KeyAlreadyExists, "key");
        }

        bool IDictionary<Key, Value>.ContainsKey(Key key)
        {
            Verify.ArgumentNotNull(key, "key");
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
            Verify.ArgumentNotNull(key, "key");
            return Erase(key) > 0;
        }

        bool IDictionary<Key, Value>.TryGetValue(Key key, out Value value)
        {
            Verify.ArgumentNotNull(key, "key");
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
            IBidirectionalInputIterator<KeyValuePair<Key, Value>> v = Find(item.Key);
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
                return new Iterator(TreeIterator);
            }
            internal void SetValue(Value v)
            {
                Inner.Value = new KeyValuePair<Key, Value>(Inner.Value.Key, v);
            }
            internal RbTreeIterator<KeyValuePair<Key, Value>> TreeIterator
            {
                get { return (RbTreeIterator<KeyValuePair<Key, Value>>) Inner; }
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
        private RbTree<Key, KeyValuePair<Key, Value>> tree;

       
        /// <summary>
        /// Copies the content of this container into an array.
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<Key, Value>[] ToArray()
        {
            KeyValuePair<Key, Value>[] array = new KeyValuePair<Key, Value>[Count];
            CopyTo(array, 0);
            return array;
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
        protected Map(SerializationInfo info, StreamingContext ctxt)
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

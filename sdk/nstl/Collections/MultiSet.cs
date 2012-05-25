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
using NStl;
using NStl.Debugging;
using NStl.Collections.Private;
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
    /// multiset is a Sorted Associative Container that stores objects of type T. 
    /// multiset is a Simple Associative Container, meaning that its value type, as 
    /// well as its object type, is object. It is also a Multiple Associative Container, 
    /// meaning that two or more elements may be identical. UniqueSet and multiset are 
    /// particularly well suited to the set algorithms includes, Algorithm.SetUnion(..), 
    /// Algorithm.SetIntersection(..), Algorithm.SetDifference(..), 
    /// and Algorithm.SetSymmetricDifference(..). The reason for 
    /// this is twofold. First, the set algorithms require their arguments to be sorted 
    /// ranges, and, since UniqueSet and MultiSet are Sorted Associative Containers, their 
    /// elements are always sorted in ascending order. Second, the output range of 
    /// these algorithms is always sorted, and inserting a sorted range into a UniqueSet or MultiSet
    /// is a fast operation: the Unique Sorted Associative Container and 
    /// Multiple Sorted Associative Container requirements guarantee that inserting 
    /// a range takes only linear time if the range is already sorted. MultiSet has 
    /// the important property that inserting a new element into a MultiSet does not 
    /// invalidate iterators that point to existing elements. Erasing an element from 
    /// a multiset also does not invalidate any iterators, except, of course, for 
    /// iterators that actually point to the element that is being erased. 
    /// </summary>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    public class MultiSet<T> : IInsertable<T, MultiSet<T>.Iterator>, ICollection, ICollection<T>, IRange<T>, ISerializable, ISortedRange<T>
    {
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
            Iterator i = Find(item);
            if (Equals(i, End()))
                return false;
            Erase(i);
            return true;
        }

        #endregion
        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return NStlUtil.Enumerator(this);
        }

        #endregion
        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return NStlUtil.Enumerator(this);
        }

        #endregion
        #region Iterator

        /// <summary>
        /// 
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This scoping binds the iterator to its container and allows internal container access.")]
        public sealed class Iterator : BidirectionalIterator2InputBidirectionalIteratorAdapter<T>
        {
            internal Iterator(RbTreeIterator<T> treeIt)
                : base(treeIt)
            {
            }

            internal Iterator(IBidirectionalIterator<T> treeIt)
                : this((RbTreeIterator<T>) treeIt)
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
            public override IIterator<T> Clone()
            {
                return new Iterator(TreeIterator);
            }

            internal RbTreeIterator<T> TreeIterator
            {
                get { return (RbTreeIterator<T>) Inner; }
            }
        }

        #endregion
        /// <summary>
        /// Constructs an empty set using the provided comparer functor
        /// </summary>
        /// <param name="comparison"></param>
        public MultiSet(IBinaryFunction<T, T, bool> comparison)
            : this(new RbTree<T, T>(comparison, Project.Identity<T>()))
        {
        }

        /// <summary>
        /// Constructs a multiset with an external tree implementation
        /// </summary>
        /// <param name="tree"></param>
        internal MultiSet(RbTree<T, T> tree)
        {
            this.tree = tree;
        }

        /// <summary>
        /// ctor, copies the passed in range into this set
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="first">The position of the first element to be copied to the set</param>
        /// <param name="last">The position just beyond the last element to be copied to the set</param>
        public MultiSet(IBinaryFunction<T, T, bool> comparison, IInputIterator<T> first, IInputIterator<T> last)
            : this(comparison)
        {
            Insert(first, last);
        }

        /// <summary>
        /// ctor, copies the passed in range into this set
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="list">The IList to be copied into this set</param>
        public MultiSet(IBinaryFunction<T, T, bool> comparison, IEnumerable list)
            : this(comparison, list.Cast<T>())
        {
        }

        /// <summary>
        /// ctor, copies the passed in range into this set
        /// </summary>
        /// <param name="comparison">Comparison functor</param>
        /// <param name="list">The IList to be copied into this set</param>
        public MultiSet(IBinaryFunction<T, T, bool> comparison, IEnumerable<T> list)
            : this(comparison, list.Begin(), list.End())
        {
        }
        /// <summary>
        /// ctor, copies the passed in range into this set
        /// </summary>
        /// <param name="comparison"></param>
        /// <param name="list"></param>
        public MultiSet(IBinaryFunction<T, T, bool> comparison, params T[] list)
            : this(comparison, (IEnumerable < T > )list)
        {
        }


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
        public IBidirectionalIterator<T>
            RBegin()
        {
            return tree.RBegin();
        }

        /// <summary>
        /// Returns an iterator pointing one past the last element of the reversed set
        /// </summary>
        /// <returns></returns>
        public IBidirectionalIterator<T>
            REnd()
        {
            return tree.REnd();
        }

        /// <summary>
        /// Inserts an element into the multiset
        /// </summary>
        /// <param name="val">The object to be inserted</param>
        /// <returns>An iterator that points to the position where the new element was inserted</returns>
        public Iterator
            Insert(T val)
        {
            return new Iterator(tree.InsertEqual(val));
        }

        /// <summary>
        /// Inserts an element into the multiset
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
            Insert(Iterator where, T val)
        {
            Verify.ArgumentNotNull(where, "where");
            return new Iterator(tree.InsertEqual(where.TreeIterator, val));
        }

        /// <summary>
        /// Insert a range of elements into the uniqueset
        /// </summary>
        /// <param name="first">The position of the first element to be copied to the set</param>
        /// <param name="last">The position just beyond the last element to be copied to the set</param>
        public void
            Insert(IInputIterator<T> first, IInputIterator<T> last)
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
        /// <param name="val">The object to be counted</param>
        /// <returns></returns>
        public int CountOf(T val)
        {
            return tree.Count(val);
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
        /// Finds a range containing all elements whose key is key
        /// </summary>
        /// <param name="key">
        /// The argument key to be compared with the sort key of an element from the 
        /// set being searched
        /// </param>
        /// <returns>
        /// A pair of iterators where the first is the lower_bound of the key and 
        /// the second is the upper_bound of the key
        /// </returns>
        public Range<T, Iterator>
            EqualRange(T key)
        {
            KeyValuePair<RbTreeIterator<T>, RbTreeIterator<T>> r =
                tree.EqualRange(key);
            return new Range<T, Iterator>(new Iterator(r.Key), new Iterator(r.Value));
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
        /// <remarks>
        /// The C++ STL set returns an valid iterator, however the SGI set does not. As I never quite
        /// saw the usefullness of the returned iterators, I stuck to the SGI implementation.
        /// If there is a strong wish from a lot of users, I will put the return value back in again.
        /// </remarks>
        public void
            Erase(Iterator first, Iterator last)
        {
            Verify.ArgumentNotNull(first, "first");
            Verify.ArgumentNotNull(last, "last");
            tree.Erase(first.TreeIterator, last.TreeIterator);
        }

        /// <summary>
        /// Erases a specific value from the set
        /// </summary>
        /// <param name="val">The key of the elements to be removed from the s</param>
        /// <returns>the count of the erased elements. This is either 1 or 0</returns>
        public int
            Erase(T val)
        {
            return tree.Erase(val);
        }

        /// <summary>
        /// Finds the location of a specific element
        /// </summary>
        /// <param name="val">The element to be found using the internal comparison functor</param>
        /// <returns></returns>
        public Iterator
            Find(T val)
        {
            return new Iterator(tree.Find(val));
        }

        /// <summary>
        /// Returns an iterator to the first element in a set with a key that is equal 
        /// to or less than a specified key
        /// </summary>
        /// <param name="val">The value to be found</param>
        /// <returns></returns>
        /// <remarks>Comparison is one by the internal comparison Predicate</remarks>
        public Iterator
            LowerBound(T val)
        {
            return new Iterator(tree.LowerBound(val));
        }

        /// <summary>
        /// Returns an iterator one past the first element in a set with a key that is 
        /// equal to or greater than a specified key
        /// </summary>
        /// <param name="val">The value to be found</param>
        /// <returns></returns>
        public Iterator
            UpperBound(T val)
        {
            return new Iterator(tree.UpperBound(val));
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
            Swap(MultiSet<T> rhs)
        {
            Algorithm.Swap(ref tree, ref rhs.tree);
        }

        /// <summary>
        /// Copies the content of the container to an array.
        /// </summary>
        public T[] ToArray()
        {
            T[] o = new T[Count];
            CopyTo(o, 0);
            return o;
        }
        /// <summary>
        /// Copies the content of the container to an array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            Algorithm.Copy(Begin(), End(), array.Begin() + arrayIndex);
        }
        private RbTree<T, T> tree;

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
        #region ISerializable Members
        private const int Version = 1;
        /// <summary>
        /// Deserialization contructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected MultiSet(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            tree = (RbTree<T, T>)info.GetValue("tree", typeof(RbTree<T, T>));
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

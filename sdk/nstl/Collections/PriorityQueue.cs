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
using System.Collections;
using System.Diagnostics;
using NStl.Debugging;
using NStl.Iterators;
using System.Runtime.Serialization;
using NStl.Util;
using System.Security.Permissions;
using NStl.SyntaxHelper;
using NStl.Linq;
using System.Diagnostics.CodeAnalysis;

namespace NStl.Collections
{
    /// <summary>
    /// <para>
    /// A PriorityQueue is a container that provides a restricted subset of 
    /// container functionality: it provides insertion of elements, and inspection 
    /// and removal of the top element. It is guaranteed that the top element is 
    /// the largest element in the PriorityQueue, where the function object 
    /// comp is used for comparisons. 
    /// </para>
    /// <para>
    /// As the PriorityQueue is internally organized as a heap, it does only 
    /// allow readonly iteration through its elements to avoid disturbance of the heap structure.
    /// </para>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>
    /// A heap is a particular way of ordering the elements in a range of <see cref="IRandomAccessIterator{T}"/>s [f, l). 
    /// The reason heaps are useful (especially for sorting, or as priority queues) is that they satisfy 
    /// two important properties. First, the value of f is the largest element in the heap. Second, 
    /// it is possible to add an element to a heap, 
    /// or to remove the first value, in logarithmic time. Internally, a heap is a tree represented as 
    /// a sequential range. The tree is constructed so that that each node is less than or equal to its parent node.
    /// </remarks>
    [DebuggerDisplay("Dimension:[{Count}]")]
    [DebuggerTypeProxy(typeof(CollectionDebuggerProxy))]
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type")]
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type")]
    public class PriorityQueue<T> : ICollection, ICollection<T>, IRange<T>, ISerializable
    {
        private readonly IBinaryFunction<T, T, bool> comp;
        private readonly IList<T> inner;
        /// <summary>
        /// Constructs an empty PriorityQueue.
        /// </summary>
        /// <param name="comp">
        /// <see cref="IBinaryFunction{Param1,Param2,Result}"/> that is used to maintain the heap structure.
        /// </param>
        public PriorityQueue(IBinaryFunction<T, T, bool> comp)
            : this(comp, new List<T>())
        {}
        /// <summary>
        /// Constructs an empty PriorityQueue.
        /// </summary>
        /// <param name="comp">
        /// <see cref="IComparer{T}"/> implementation that is used to maintain the heap structure.
        /// </param>
        public PriorityQueue(IComparer<T> comp)
            : this(comp, new List<T>())
        { }
        /// <summary>
        /// Constructs an empty PriorityQueue.
        /// </summary>
        /// <param name="comp">
        /// <see cref="IComparer"/> implementation that is used to maintain the heap structure.
        /// </param>
        public PriorityQueue(IComparer comp)
            : this(comp, new List<T>())
        { }
        private PriorityQueue(IBinaryFunction<T, T, bool> comp, IList<T> inner)
        {
            this.comp = comp;
            this.inner = inner;

            if(Count > 0)
                Algorithm.MakeHeap(RwBegin(), RwEnd(), comp);
        }

        /// <summary>
        /// Constructs a PriorityQueue containing the content of the passed in range. 
        /// The values are copied, the complexity is linear, at most 3*items.Length comparisons.
        /// </summary>
        /// <param name="comp">
        /// The <see cref="IBinaryFunction{Param1,Param2,Result}"/> that is used to maintain the heap structure.
        /// </param>
        /// <param name="items">The content to be copied into the PriorityQueue.</param>
        public PriorityQueue(IBinaryFunction<T, T, bool> comp, params T[] items)
            : this(comp, new List<T>(items))
        {}
        /// <summary>
        /// Constructs a PriorityQueue containing the content of the passed in range. 
        /// The values are copied, the complexity is linear, at most 3*CountOf(items) comparisons.
        /// </summary>
        /// <param name="comp">
        /// The <see cref="IBinaryFunction{Param1,Param2,Result}"/> that is used to maintain the heap structure.
        /// </param>
        /// <param name="items">The content to be copied into the PriorityQueue.</param>
        public PriorityQueue(IBinaryFunction<T, T, bool> comp, IEnumerable<T> items)
            : this(comp, new List<T>(items))
        {}

        /// <summary>
        /// Constructs a PriorityQueue containing the content of the passed in range. 
        /// The values are copied, the complexity is linear, at most 3*ContOf(items) comparisons.
        /// </summary>
        /// <param name="comp">
        /// A <see cref="IComparer{T}"/> implementation that is used to maintain the heap structure.
        /// </param>
        /// <param name="items">The content to be copied into the PriorityQueue.</param>
        public PriorityQueue(IComparer<T> comp, IEnumerable<T> items)
            : this(Compare.With(CompareAction.Less, comp), new List<T>(items))
        { }
        /// <summary>
        /// Constructs a PriorityQueue containing the content of the passed in range. 
        /// The values are copied, the complexity is linear, at most 3*CountOf(items) comparisons.
        /// </summary>
        /// <param name="comp">
        /// A <see cref="IComparer"/> implementation that is used to maintain the heap structure.
        /// </param>
        /// <param name="items">The content to be copied into the PriorityQueue.</param>
        public PriorityQueue(IComparer comp, IEnumerable<T> items)
            : this(Compare.With<T, T>(CompareAction.Less, comp), new List<T>(items))
        { }
        /// <summary>
        /// Returns a the element at the top of the PriorityQueue. The element at the top 
        /// is guaranteed to be the largest element in the priority queue, as determined 
        /// by the comparison function. That is, for every other element x in the PriorityQueue, 
        /// comp.Execute(q.Peek, x) is false.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the PriorityQueue is empty, that is when IsEmpty returns true.
        /// </exception>
        public T Peek
        {
            get
            {
                if (Empty)
                    throw new InvalidOperationException("The queue is empty!");

                return inner[0];
            }
        }
        /// <summary>
        /// Returns true, when the PriorityQueue is empty.
        /// </summary>
        public bool Empty
        {
            get { return inner.Count == 0; }
        }
        /// <summary>
        /// Inserts an item into the PriorityQueue. The complexity is logarithmic.
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            inner.Add(item);
            Algorithm.PushHeap(RwBegin(), RwEnd(), comp);
        }

        private IRandomAccessIterator<T> RwEnd()
        {
            return inner.End();
        }

        private IRandomAccessIterator<T> RwBegin()
        {
            return inner.Begin();
        }
        /// <summary>
        /// Returns a read only iterator pointing one past the final element of the PriorityQueue.
        /// </summary>
        /// <returns></returns>
        public IInputIterator<T> End()
        {
            return inner.End();
        }
        /// <summary>
        /// Returns a read only iterator pointing on the first element of the PriorityQueue.
        /// </summary>
        /// <returns></returns>
        public IInputIterator<T> Begin()
        {
            return inner.Begin();
        }
        /// <summary>
        /// Dequeues the element with the highest priority of the PriorityQueue. The complexity is logarithmic.
        /// </summary>
        /// <returns>The element with the highest priority.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the PriorityQueue is empty, that is when IsEmpty return true.
        /// </exception>
        public T Dequeue()
        {
            if (Empty)
                throw new InvalidOperationException("The queue is empty!");

            T t = Peek;
            Algorithm.PopHeap(RwBegin(), RwEnd(), comp);
            inner.RemoveAt(inner.Count - 1);
            return t;
        }
        /// <summary>
        /// Clears the content of the PriorityQueue.
        /// </summary>
        public void Clear()
        {
            inner.Clear();
        }
        /// <summary>
        /// Returns true, if the elemet is contained in the PriorityQueue. The performance is linear.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return inner.Contains(item);
        }

        #region ICollection Members
        /// <summary>
        /// Thec length of the PriorityQueue.
        /// </summary>
        public int Count
        {
            get { return inner.Count; }
        }
        void ICollection.CopyTo(Array array, int index)
        {
            foreach (T t in this)
                array.SetValue(t, index++);
        }
        /// <summary>
        /// Copies the content of the PriorityQueue into the passed in array starting at the given index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(T[] array, int index)
        {
            foreach (T t in this)
                array[index++] = t;
        }
        /// <summary>
        /// Returns if the PriorityQueue is syncronized.
        /// </summary>
        public bool IsSynchronized
        {
            get { return false; }
        }
        /// <summary>
        /// Returns an object the can be used o syncronize external access to the PriorityQueue.
        /// </summary>
        public object SyncRoot
        {
            get { return this; }
        }

        #endregion
        #region IEnumerable Members
        IEnumerator IEnumerable.GetEnumerator()
        {
            return inner.GetEnumerator();
        }
        #endregion
        /// <summary>
        /// Returns an array containing the content of this PriorityQueue.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] t = new T[Count];
            CopyTo(t, 0);
            return t;
        }

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return inner.GetEnumerator();
        }

        #endregion
        /// <summary>
        /// Removes an element form the PriorityQueue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True, if a value was removed, false if no value was found.</returns>
        /// <remarks>
        /// Removing an element destroys the internal heap structure and forces the heap to be rebuilded.
        /// Therefore the complexity is a combination of the linear search for the remove operation and the
        /// rebuild of the heap: c = (last-first) + 3 * (last-first).
        /// </remarks>
        public bool Remove(T item)
        {
            bool rv = inner.Remove(item);
            if(rv && Count > 0)
                Algorithm.MakeHeap(RwBegin(), RwEnd(), comp);
            return rv;
        }

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            Enqueue(item);
        }

        void ICollection<T>.Clear()
        {
            Clear();
        }

        bool ICollection<T>.Contains(T item)
        {
            return Contains(item);
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
            return Remove(item);
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
        /// Deserialization constructor.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ctxt"></param>
        protected PriorityQueue(SerializationInfo info, StreamingContext ctxt)
        {
            int version = info.GetInt32("Version");
            Verify.VersionsAreEqual(version, Version);

            inner = (IList<T>) info.GetValue("inner", typeof (IList<T>));
            comp = (IBinaryFunction<T, T, bool>) info.GetValue("comp", typeof (IBinaryFunction<T, T, bool>));
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
           info.AddValue("inner", inner, typeof(IList<T>));
           info.AddValue("comp",  comp, typeof(IBinaryFunction<T, T, bool>));
        }

        #endregion
    }
}

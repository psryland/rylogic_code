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
using System.Diagnostics.CodeAnalysis;
using NStl.Exceptions;

namespace NStl.Iterators.Support
{
    /// <summary>
    /// A <see cref="IBidirectionalIterator{T}"/> implementation for a <see cref="LinkedList{T}"/> container;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedListIterator<T> : BidirectionalIterator<T>
    {
        internal LinkedList<T> List { get; private set; }
        private LinkedListNode<T> currentNode;
        internal LinkedListIterator(LinkedList<T> list)
        {
            List = list;
        }
        internal LinkedListIterator(LinkedList<T> list, LinkedListNode<T> currentNode)
            : this(list)
        {
            this.currentNode = currentNode;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <returns></returns>
        public override IBidirectionalIterator<T> PreDecrement()
        {
            currentNode = Node == null ? List.Last : Node.Previous;
            return this;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <returns></returns>
        public override IForwardIterator<T> PreIncrement()
        {
            if (Node != null)
                currentNode = Node.Next;
            return this;
        }
        /// <summary>
        /// See base class.
        /// </summary>
        public override T Value
        {
            get
            {
                if (Node == null)
                    throw new DereferenceEndIteratorException();
                return Node.Value;
            }
            set
            {
                if (Node == null)
                    throw new DereferenceEndIteratorException();
                Node.Value = value;
            }
        }
        /// <summary> 
        /// </summary>
        public LinkedListNode<T> Node
        {
            get { return currentNode; }
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <returns></returns>
        public override IIterator<T> Clone()
        {
            return new LinkedListIterator<T>(List, Node);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            LinkedListIterator<T> rhs = obj as LinkedListIterator<T>;
            if(rhs == null)
                return false;
            if(!ReferenceEquals(List, rhs.List))
                return false;

            // both are end??
            if(Node == null && rhs.Node == null)
                return true;

            return Node == rhs.Node;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override int HashCode()
        {
            return List.GetHashCode() ^ (Node != null ? Node.GetHashCode() : 0);
        }
        #region operators
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
        public static LinkedListIterator<T> operator ++(LinkedListIterator<T> it)
        {
            LinkedListIterator<T> i = (LinkedListIterator<T>)it.Clone();
            return (LinkedListIterator<T>)i.PreIncrement();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Scope = "member", Justification = "Pre/PostIncrement() already exist")]
        public static LinkedListIterator<T> operator --(LinkedListIterator<T> it)
        {
            LinkedListIterator<T> i = (LinkedListIterator<T>)it.Clone();
            return (LinkedListIterator<T>)i.PreDecrement();
        }
        #endregion
    }
}

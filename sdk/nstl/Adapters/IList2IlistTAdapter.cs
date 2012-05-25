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


using System.Collections;
using System.Collections.Generic;

namespace NStl
{
    internal class IList2IlistTAdapter<T> : IList<T>
    {
        public IList2IlistTAdapter(IList list)
        {
            this.list = list;
        }

        #region IList<T> Members

        int IList<T>.IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        void IList<T>.Insert(int index, T item)
        {
            list.Insert(index, item);
        }

        void IList<T>.RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        T IList<T>.this[int index]
        {
            get { return (T) list[index]; }
            set { list[index] = value; }
        }

        #endregion

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            list.Add(item);
        }

        void ICollection<T>.Clear()
        {
            list.Clear();
        }

        bool ICollection<T>.Contains(T item)
        {
            return list.Contains(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        int ICollection<T>.Count
        {
            get { return list.Count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return list.IsReadOnly; }
        }

        #region

        #endregion

        bool ICollection<T>.Remove(T item)
        {
            if (!list.Contains(item))
                return false;
            list.Remove(item);
            return true;
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new IEnumerator2IEnumeratorTAdapter<T>(list.GetEnumerator());
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        internal IList List
        {
            get { return list; }
        }

        public override bool Equals(object obj)
        {
            IList2IlistTAdapter<T> rhs = obj as IList2IlistTAdapter<T>;
            if (rhs == null)
                return base.Equals(obj);
            return List == rhs.List;
        }

        public override int GetHashCode()
        {
            return List.GetHashCode();
        }

        private readonly IList list;
    }
}

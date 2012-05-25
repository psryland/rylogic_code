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
using NStl.Exceptions;
using NStl.Collections;

namespace NStl.Adapters
{
    [Serializable]
    internal class ReadOnlyCollectionAdaptor<T> : IReadOnlyCollection<T>
    {
        private readonly IEnumerable<T> enumerable;

        public ReadOnlyCollectionAdaptor(IEnumerable<T> enumerable)
        {
            this.enumerable = enumerable;
        }

        public bool Contains(T item)
        {
            foreach(T t in this)
                if(Equals(t, item))
                    return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach(T t in this)
                array[arrayIndex++] = t;
        }

        public T[] ToArray()
        {
            T[] ts = new T[Count];
            CopyTo(ts, 0);
            return ts;
        }

        public bool Empty
        {
            get { return !GetEnumerator().MoveNext(); }
        }

        public T Front()
        {
            IEnumerator<T> e = GetEnumerator();
            if(!e.MoveNext())
                throw new ContainerEmptyException();
            return e.Current;
        }

        public void CopyTo(Array array, int index)
        {
            foreach (T t in this)
                array.SetValue(t, index++);
        }

        public virtual int Count
        {
            get
            {
                ICollection<T> collectionT = Enumerable as ICollection<T>;
                if (collectionT != null)
                    return collectionT.Count;

                ICollection collection = enumerable as ICollection;
                if (collection != null)
                    return collection.Count;

                int count = 0;
                IEnumerator e = GetEnumerator();
                while(e.MoveNext())
                    ++count;
                return count;
            }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return enumerable; }
        }

        protected IEnumerable<T> Enumerable
        {
            get { return enumerable; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return enumerable.GetEnumerator();
        }
    }
}

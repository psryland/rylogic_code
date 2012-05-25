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


namespace NStl
{
    /// <summary>
    /// Weak readonly collection adaptor.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class Collection2ReadOnlyCollectionTAdapter<T> : ICollection<T>
    {
        // Sometimes I just scratch my head about the .NET framework developers: IDictionary<K,V>
        // has a property "Keys" returning an ICollection<K> that lets you work
        // with the keys of a Hashtable. That was ok in the old System.Collection namespace as ICollection 
        // was immutable, however, in C# 2.0 you can add items to an ICollection. So guess what this
        // does: dict.Keys.Add(new k()); Well, it crashes. Bad enough to have this
        // violation of Liskov, it also forces me to do the same to imlement a Dictionary2IDictionary<K,V>
        // implementation :-(
        public Collection2ReadOnlyCollectionTAdapter(ICollection c)
        {
            col = c;
        }
        private readonly ICollection col;

        public void Add(T item)
        {
            throw new NotImplementedException("This is a readonly collection!");
        }

        public void Clear()
        {
            throw new NotImplementedException("This is a readonly collection!");
        }

        public bool Contains(T item)
        {
            foreach (T t in col)
                if (Equals(t, item))
                    return true;
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            col.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException("This is a readonly collection!");
        }

        public int Count
        {
            get { return col.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new IEnumerator2IEnumeratorTAdapter<T>(col.GetEnumerator());
        }

        public IEnumerator GetEnumerator()
        {
            return col.GetEnumerator();
        }
    }
}

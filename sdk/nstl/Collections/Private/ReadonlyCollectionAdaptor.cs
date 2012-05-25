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
using NStl.Collections.Private;

namespace NStl.Collections.Private
{
    [Serializable]
    internal class ReadonlyCollectionAdaptor<T> : ICollection<T>
    {
        private readonly IEnumerable<T> e;
        private readonly int count;

        internal static ICollection<KT> KeyCollection<KT, VT>(HashContainer<KT, KeyValuePair<KT, VT>> hc)
        {
            return new ReadonlyCollectionAdaptor<KT>(Keys(hc), hc.Count);
        }

        private static IEnumerable<KT> Keys<KT, VT>(HashContainer<KT, KeyValuePair<KT, VT>> hc)
        {
            foreach (KeyValuePair<KT, VT> k in hc)
                yield return k.Key;
        }

        internal static ICollection<VT> ValueCollection<KT, VT>(HashContainer<KT, KeyValuePair<KT, VT>> hc)
        {
            return new ReadonlyCollectionAdaptor<VT>(Values(hc), hc.Count);
        }

        private static IEnumerable<VT> Values<KT, VT>(HashContainer<KT, KeyValuePair<KT, VT>> hc)
        {
            foreach (KeyValuePair<KT, VT> v in hc)
                yield return v.Value;
        }

        private ReadonlyCollectionAdaptor(IEnumerable<T> e, int count)
        {
            this.e = e;
            this.count = count;
        }

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException("This is a readonly collection!");
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("This is a readonly collection!");
        }

        bool ICollection<T>.Contains(T item)
        {
            foreach (T t in e)
                if (Equals(t, item))
                    return true;
            return false;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            foreach (T t in e)
                array[arrayIndex++] = t;
        }

        int ICollection<T>.Count
        {
            get { return count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException("This is a readonly collection!");
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return e.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return e.GetEnumerator();
        }

        #endregion
    }
}

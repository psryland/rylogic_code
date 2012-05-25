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
using System;
using System.Collections.Generic;

namespace NStl.Adapters
{
    [Serializable]
    class ListT2IListAdaptor<T> : CollectionT2ICollectionAdapter<T>, IList
    {
        public ListT2IListAdaptor(IList<T> list)
            : base(list)
        {}
        private IList<T> List
        {
            get { return (IList<T>)enumerable; }
        }

        #region IList Members

        public int Add(object value)
        {
            int rv = Count;
            List.Add((T)value);
            return rv;
        }

        public void Clear()
        {
            List.Clear();
        }

        public bool Contains(object value)
        {
            return List.Contains((T) value);
        }

        public int IndexOf(object value)
        {
            return List.IndexOf((T)value);
        }

        public void Insert(int index, object value)
        {
            List.Insert(index, (T)value);
        }

        public bool IsFixedSize
        {
            get { return List.IsReadOnly; }
        }

        public bool IsReadOnly
        {
            get { return List.IsReadOnly; }
        }

        public void Remove(object value)
        {
            List.Remove((T)value);
        }

        public void RemoveAt(int index)
        {
            List.RemoveAt(index);
        }

        public object this[int index]
        {
            get
            {
                return List[index];
            }
            set
            {
                List[index] = (T) value;
            }
        }

        #endregion
    }
}

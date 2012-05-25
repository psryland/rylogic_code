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
    class IDictionary2IDictionaryTAdapter<Key, Value> : IDictionary<Key, Value>
    {
        public IDictionary2IDictionaryTAdapter(IDictionary dict)
        {
            this.dict = dict;
        }
        IDictionary dict;
        public bool ContainsKey(Key key)
        {
            return dict.Contains(key);
        }

        public void Add(Key key, Value value)
        {
            dict.Add(key, value);
        }

        public bool Remove(Key key)
        {
            bool b = dict.Contains(key);
            dict.Remove(key);
            return b;
        }

        public bool TryGetValue(Key key, out Value value)
        {
            object val = dict[key];
            if(val != null)
            {
                value = (Value) val;
                return true;
            }
            value = default(Value);
            return dict.Contains(key);
        }

        public Value this[Key key]
        {
            get
            {
                object val = dict[key];
                if (val != null)
                    return (Value)val;

                // as null is a valid value in a IDictionary, whe have to
                // check for contains
                if (!dict.Contains(key))
                    throw new KeyNotFoundException();
                return (Value)val;
            }
            set { dict[key] = value; }
        }

        public ICollection<Key> Keys
        {
            get { return new Collection2ReadOnlyCollectionTAdapter<Key>(dict.Keys); }
        }

        public ICollection<Value> Values
        {
            get { return new Collection2ReadOnlyCollectionTAdapter<Value>(dict.Values); }
        }

        public void Add(KeyValuePair<Key, Value> item)
        {
           dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            dict.Clear();
        }

        public bool Contains(KeyValuePair<Key, Value> item)
        {
            object val =  dict[item.Key];
            if (val != null)
                return val.Equals(item.Value);
            return false;
        }

        public void CopyTo(KeyValuePair<Key, Value>[] array, int arrayIndex)
        {
            foreach(KeyValuePair<Key, Value> cur in this)
                array[arrayIndex++] = cur;
        }

        public bool Remove(KeyValuePair<Key, Value> item)
        {
            object val = dict[item.Key];
            if(val != null && val.Equals(item.Value))
            {
                dict.Remove(item.Key);
                return true;
            }
            return false;
        }

        public int Count
        {
            get { return dict.Count; }
        }

        public bool IsReadOnly
        {
            get { return dict.IsReadOnly; }
        }

        private class EnumeratorAdaptor: IEnumerator2IEnumeratorTAdapter<KeyValuePair<Key, Value>>
        {
            public EnumeratorAdaptor(IDictionary dict)
                : base(dict.GetEnumerator())
            {}
            protected override KeyValuePair<Key, Value> Current
            {
                get
                {
                    DictionaryEntry entry = (DictionaryEntry)innerEnum.Current;
                    return new KeyValuePair<Key, Value>((Key)entry.Key, (Value)entry.Value);
                }
            }
        }
        IEnumerator<KeyValuePair<Key, Value>> IEnumerable<KeyValuePair<Key, Value>>.GetEnumerator()
        {
            return new EnumeratorAdaptor(dict);
        }

        public IEnumerator GetEnumerator()
        {
            return new EnumeratorAdaptor(dict);
        }
    }
}

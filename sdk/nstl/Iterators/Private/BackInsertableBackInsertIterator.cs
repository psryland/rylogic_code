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

using NStl.Iterators.Support;
using System.Collections.Generic;

namespace NStl.Iterators.Private
{
    class BackInsertableBackInsertIterator<T> : OutputIterator<T>
    {
        internal BackInsertableBackInsertIterator(IBackInsertable<T> vec)
        {
            this.vec = vec;
        }
        internal BackInsertableBackInsertIterator(LinkedList<T> list)
            : this(new List2BackInsertableAdaptor<T>(list))
        {}
        internal BackInsertableBackInsertIterator(IList<T> list)
            : this(new List2BackInsertableAdaptor<T>(list))
        { }
        public override T Value
        {
            set
            {
                vec.PushBack(value);
            }
        }
        private IBackInsertable<T> vec;

        private class List2BackInsertableAdaptor<U> : IBackInsertable<U>
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields", Justification = "It is definitely used!!")]
            private readonly ICollection<U> inner;// we check through the ctor that we are back insertable
            internal List2BackInsertableAdaptor(LinkedList<U> list)
            {
                inner = list;
            }
            internal List2BackInsertableAdaptor(IList<U> list)
            {
                inner = list;
            }
            #region IBackInsertable<U> Members

            public void PushBack(U val)
            {
                inner.Add(val);
            }

            #endregion
        }

    }
}

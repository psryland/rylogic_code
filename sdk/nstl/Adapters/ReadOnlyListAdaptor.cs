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

using NStl.Collections;
using System.Collections.Generic;
using System;
using NStl.Exceptions;
using NStl.Util;

namespace NStl.Adapters
{
    [Serializable]
    class ReadOnlyListAdaptor<T> : ReadOnlyCollectionAdaptor<T>, IReadOnlyList<T>
    {
        public ReadOnlyListAdaptor(IList<T> list) 
            : base(list)
        {}
        private new IList<T> Enumerable
        {
            get { return (IList<T>)base.Enumerable; }
        }
        #region IReadOnlyList<T> Members

        public int IndexOf(T item)
        {
            return Enumerable.IndexOf(item);
        }

        public T this[int index]
        {
            get { return Enumerable[index]; }
        }

        public T Back()
        {
            if (Enumerable.Count == 0)
                throw new ContainerEmptyException();
            return Enumerable[Enumerable.Count - 1];
        }
        public override int Count
        {
            get { return Enumerable.Count; }
        }
        #endregion

        #region IReadOnlyCollection<T> Members

        #endregion

        #region ICollection Members

        #endregion

        #region IEnumerable<T> Members

        #endregion
    }
}

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
using NStl.Iterators;

namespace NStl
{
    internal class Iterator2IEnumeratorAdaptor<T> : IEnumerator<T>
    {
        public Iterator2IEnumeratorAdaptor(IInputIterator<T> first, IInputIterator<T> last)
        {
            this.first = (IInputIterator<T>)first.Clone();
            this.last = (IInputIterator<T>)last.Clone();
        }
        private readonly IInputIterator<T> first;
        private readonly IInputIterator<T> last;
        private IInputIterator<T> current;
        #region IEnumerator<T> Members

        T IEnumerator<T>.Current
        {
            get { return current.Value; }
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current
        {
            get { return current.Value; }
        }

        bool IEnumerator.MoveNext()
        {
            if (Equals(current, null))
                current = (IInputIterator<T>)first.Clone();
            else
                current.PreIncrement();
            return !Equals(current, last);
        }

        void IEnumerator.Reset()
        {
            current = null;
        }

        #endregion
    }
}

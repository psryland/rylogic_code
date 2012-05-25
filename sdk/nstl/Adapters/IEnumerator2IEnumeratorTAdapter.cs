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
using System.Collections.Generic;


namespace NStl
{
    /// <summary>
    /// A simple adapter implementation, that uses the new yield syntax, but
    /// also allows the iterator to be reseted.
    /// </summary>
    /// <typeparam name="U"></typeparam>
    internal class IEnumerator2IEnumeratorTAdapter<U> : IEnumerator<U>
    {
        public IEnumerator2IEnumeratorTAdapter(System.Collections.IEnumerator e)
        {
            innerEnum = e;
        }
        protected System.Collections.IEnumerator innerEnum;
        #region IEnumerator<U> Members

        U IEnumerator<U>.Current
        {
            get { return Current; }
        }

        #endregion

        #region IDisposable Members

        void System.IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }
        protected virtual U Current
        {
            get { return (U)innerEnum.Current; }
        }

        bool System.Collections.IEnumerator.MoveNext()
        {
            return innerEnum.MoveNext();
        }

        void System.Collections.IEnumerator.Reset()
        {
            innerEnum.Reset();
        }

        #endregion
    }

}

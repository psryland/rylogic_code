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


namespace NStl.Iterators.Support
{
    /// <summary>
    /// A bidirectional iterator extents the forward iterator 
    /// with the possibility to run backwards over the range.
    /// </summary>
    public abstract class BidirectionalIterator<T> : ForwardIterator<T>, IBidirectionalIterator<T>
    {
        /// <summary>
        ///Moves this iterator one step back.
        /// </summary>
        /// <returns>Returns this iterator.</returns>
        public abstract IBidirectionalIterator<T> PreDecrement();
        /// <summary>
        /// oves this iterator one step back.
        /// </summary>
        /// <returns>returns a copy of this iterator poining to the previous position.</returns>
        public virtual IBidirectionalIterator<T> PostDecrement()
        {
            IBidirectionalIterator<T> old = (IBidirectionalIterator<T>)Clone();
            PreDecrement();
            return old;
        }

        #region IBidirectionalInputIterator<T> Members

        IBidirectionalInputIterator<T> IBidirectionalInputIterator<T>.PreDecrement()
        {
            return PreDecrement();
        }

        IBidirectionalInputIterator<T> IBidirectionalInputIterator<T>.PostDecrement()
        {
            return PostDecrement();
        }

        #endregion
    }
}

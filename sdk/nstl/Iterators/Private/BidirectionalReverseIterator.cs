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

namespace NStl.Iterators.Private
{
    class BidirectionalReverseIterator<T> : BidirectionalIterator<T>
    {
        public BidirectionalReverseIterator(IBidirectionalIterator<T> i)
        {
            it = (IBidirectionalIterator<T>)i.Clone();
        }
        private readonly IBidirectionalIterator<T> it;
        public override IIterator<T> Clone()
        {
            return new BidirectionalReverseIterator<T>(it);
        }

        public override IBidirectionalIterator<T> PreDecrement()
        {
            it.PreIncrement();
            return this;
        }
        public override IForwardIterator<T> PreIncrement()
        {
            it.PreDecrement();
            return this;
        }
        /// <summary>
        /// The wrapped iterator
        /// </summary>
        /// <returns></returns>
        public IBidirectionalIterator<T> InnerIterator()
        {
            return it;
        }
        private static IBidirectionalIterator<T>
            ExtractInnerIterator(IIterator<T> other)
        {
            BidirectionalReverseIterator<T> bidRev = other as BidirectionalReverseIterator<T>;
            if (bidRev != null)
                return bidRev.InnerIterator();

            return (IBidirectionalIterator<T>) other;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected override bool Equals(EquatableIterator<T> obj)
        {
            BidirectionalReverseIterator<T> rhs = obj as BidirectionalReverseIterator<T>;
            if (rhs == null)
                return false;
            return Equals(InnerIterator(), ExtractInnerIterator(rhs));
        }
        protected override int HashCode()
        {
            return InnerIterator().GetHashCode();
        }

        public override T Value
        {
            get
            {
                IBidirectionalIterator<T> tmp = (IBidirectionalIterator<T>)(InnerIterator().Clone());
                tmp.PreDecrement();
                return (tmp.Value);
            }
            set
            {
                IBidirectionalIterator<T> tmp = (IBidirectionalIterator<T>)(InnerIterator().Clone());
                tmp.PreDecrement();
                tmp.Value = value;
            }
        }
    }
}

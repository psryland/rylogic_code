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
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Globalization;
namespace NStl.Collections.Private
{
    [Serializable]
    internal struct FixedSizeInt32 : ISerializable
    {
        private readonly int val;
        private readonly int MaxLength;
        internal FixedSizeInt32(int value, int maxLength)
        {
            val = value;
            MaxLength = maxLength;
        }
        #region ISerializable Members
        public FixedSizeInt32(SerializationInfo info, StreamingContext ctxt)
        {
            val = (int)info.GetValue("Val", typeof(int));
            MaxLength = (int)info.GetValue("MaxLength", typeof(int));
        }
        /// <summary>
        /// See <see cref="ISerializable.GetObjectData"/> for details.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Val", val);
            info.AddValue("MaxLength", MaxLength);
        }

        #endregion
        public int Value
        {
            get { return val; }
        }


        public override string ToString()
        {
            return string.Format(new NumberFormatInfo(), "Value: {0}, MaxValue:  {1}", Value, MaxLength);
        }
        public static bool operator ==(FixedSizeInt32 lhs, FixedSizeInt32 rhs )
        {
            return Equals(lhs, rhs);
        }
        public static bool operator !=(FixedSizeInt32 lhs, FixedSizeInt32 rhs)
        {
            return !Equals(lhs, rhs);
        }
        public static FixedSizeInt32 operator++(FixedSizeInt32 i)
        {
            return i + 1;
        }
        public static FixedSizeInt32 operator --(FixedSizeInt32 i)
        {
            return i - 1;
        }
        private static FixedSizeInt32 Plus(FixedSizeInt32 i, int p)
        {
            //   3,4, 0,1,2,3,4, 0,1,2,3,4, 0,1,2
            //   0,1, 2,3,4,5,6, 7,8,9,0,1, 2,3,4

            int pModMaxLength = p%i.MaxLength;

            int newX;
            if ((newX = i.Value + pModMaxLength) < i.MaxLength)
                return new FixedSizeInt32(newX, i.MaxLength);

            return new FixedSizeInt32(pModMaxLength - (i.MaxLength - i.Value), i.MaxLength);
        }
        private static FixedSizeInt32 Minus(FixedSizeInt32 i, int p)
        {
            //   2,1,0, 4,3,2,1,0, 4,3,2,1,0, 4,3
            //   0,1,2, 3,4,5,6,7, 8,9,0,1,2, 3,4

            int pModMaxLength = p%i.MaxLength;

            int newX;
            if ((newX = i.Value - pModMaxLength) >= 0)
                return new FixedSizeInt32(newX, i.MaxLength);

            return new FixedSizeInt32(i.MaxLength - (pModMaxLength - i.Value), i.MaxLength);
        }
        public static FixedSizeInt32 operator+(FixedSizeInt32 i, int p)
        {
            if (p >= 0)
                return Plus(i, p);
            else 
                return Minus(i, -p);
        }
        public static FixedSizeInt32 operator -(FixedSizeInt32 i, int p)
        {
            if (p > 0)
                return Minus(i, p);
            else
                return Plus(i, -p);
        }
        public static explicit operator int(FixedSizeInt32 i)
        {
            return i.Value;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is FixedSizeInt32))
                return false;
            FixedSizeInt32 rhs = (FixedSizeInt32)obj;
            return Value == rhs.Value && MaxLength == rhs.MaxLength;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ MaxLength.GetHashCode();
        }

        
    }
}

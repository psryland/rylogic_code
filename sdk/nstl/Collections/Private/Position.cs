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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Globalization;

namespace NStl.Collections.Private
{
    [Serializable]
    [DebuggerDisplay("X:{X}, Y:{Y}")]
    internal struct Position : ISerializable
    {
        internal const int xMax = 10;

        private readonly FixedSizeInt32 x;
        internal readonly int Y;
        internal int X
        {
            get{ return (int)FixedX;}
        }
        #region ISerializable Members
        public Position(SerializationInfo info, StreamingContext ctxt)
        {
            x = (FixedSizeInt32)info.GetValue("X", typeof(FixedSizeInt32));
            Y = (int)info.GetValue("Y", typeof(int));
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
            info.AddValue("X", x);
            info.AddValue("Y", Y);
        }

        #endregion
        internal FixedSizeInt32 FixedX
        {
            get { return x; }
        }

        public Position(int x, int y)
            : this(new FixedSizeInt32(x, xMax), y)
        {}
        public Position(FixedSizeInt32 x, int y)
        {
            this.x = x;
            Y = y;
        }

        private Position(Position p)
            : this(p.X, p.Y)
        {}
        

        public static bool operator <(Position lhs, Position rhs)
        {
            if (lhs.Y == rhs.Y)
                return lhs.X < rhs.X;
            else if (lhs.Y > rhs.Y)
                return true;
            else
                return false;
        }

        public static bool operator >(Position lhs, Position rhs)
        {
            return rhs < lhs;
        }

        public static bool operator ==(Position lhs, Position rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(Position lhs, Position rhs)
        {
            return !Equals(lhs, rhs);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Position))
                return false;
            Position rhs = (Position) obj;
            return (!(this < rhs) && !(rhs < this));
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static Position operator +(Position p, int i)
        {
            if (i == 0)
                return new Position(p);
            if (i < 0)
                return Minus(p, -i);
            return Plus(p, i);
        }

        public static Position operator ++(Position pos)
        {
            return pos + 1;
        }

        public static Position operator --(Position pos)
        {
            return pos - 1;
        }

        public static Position operator -(Position p, int i)
        {
            if (i == 0)
                return new Position(p);
            if (i < 0)
                return Plus(p, -i);
            return Minus(p, i);
        }

        private static Position Plus(Position p, int i)
        {
            int pXPlusI;
            if ((pXPlusI = p.X + i) < xMax)
                return new Position(pXPlusI, p.Y);

            if (pXPlusI == xMax)
                return new Position(0, p.Y + 1);

            FixedSizeInt32 newX = (p.FixedX + i);
            int newY = p.Y + Math.Max(pXPlusI / xMax, 1);

            return new Position(newX, newY);            
        }
        public override string ToString()
        {
            return string.Format(new NumberFormatInfo(), "{0}, {1}", X, Y);
        }
        private static Position Minus(Position p, int i)
        {
            int pXMinusI;
            if((pXMinusI = p.X - i) >= 0)
                return new Position(pXMinusI, p.Y);

            FixedSizeInt32 newX = p.FixedX - i;

            int iLessXByxMax = (i - p.X) / xMax;

            if (newX.Value == 0)
                return new Position(newX, p.Y - iLessXByxMax);

            return new Position(newX, p.Y - iLessXByxMax -  1);
        }

        // 0,1,2,3,4,5,6,7,8,9
        // 0,1,2,3,4,5,6,7,8,9
        // 0,1,2,3,4,5,6,7,8,9
        // 0,1,2,3,4,5,6,7,8,9

        public static int operator -(Position lhs, Position rhs)
        {
            int i = lhs.X + (xMax - rhs.X) + ((lhs.Y - rhs.Y - 1)*xMax);

            return i;
        }
    }
}

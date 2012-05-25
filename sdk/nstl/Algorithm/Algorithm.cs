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

using NStl.Iterators;
using NStl.Linq;

namespace NStl
{
    /// <summary>
    /// This static class contains all algorithms of the <b>NSTL</b>.
    /// </summary>
    public static partial class Algorithm
    {
        private const int Threshold = 16;
        private const int ChunkSize = 7;


        private static int
            __gcd(int m, int n)
        {
            while (n != 0)
            {
                int t = m % n;
                m = n;
                n = t;
            }
            return m;
        }
        private static void
            AdjustHeap<T>(IRandomAccessIterator<T> first, int holeIndex, int length, T Value, IBinaryFunction<T, T, bool> comparison)
        {
            int topIndex = holeIndex;
            int secondChild = 2 * holeIndex + 2;
            while (secondChild < length)
            {
                if (comparison.Execute((first.Add(secondChild)).Value, (first.Add(secondChild - 1)).Value))
                {
                    --secondChild;
                }
                (first.Add(holeIndex)).Value = (first.Add(secondChild)).Value;
                holeIndex = secondChild;
                secondChild = 2 * (secondChild + 1);
            }
            if (secondChild == length)
            {
                (first.Add(holeIndex)).Value = (first.Add(secondChild - 1)).Value;
                holeIndex = secondChild - 1;
            }
            PushHeap(first, holeIndex, topIndex, Value, comparison);
        }
        private static T
            Median<T>(T a, T b, T c, IBinaryFunction<T, T, bool> comparison)
        {
            if (comparison.Execute(a, b))
                if (comparison.Execute(b, c))
                    return b;
                else if (comparison.Execute(a, c))
                    return c;
                else
                    return a;
            else if (comparison.Execute(a, c))
                return a;
            else if (comparison.Execute(b, c))
                return c;
            else
                return b;
        }
        private static IRandomAccessIterator<T>
            UnguardedPartition<T>(IRandomAccessIterator<T> first,
                                     IRandomAccessIterator<T> last, T pivot, IBinaryFunction<T, T, bool> comparison)
        {
            first = (IRandomAccessIterator<T>)first.Clone();
            last = (IRandomAccessIterator<T>)last.Clone();
            for (; ; )
            {
                while (comparison.Execute(first.Value, pivot))
                    first.PreIncrement();
                last.PreDecrement();
                while (comparison.Execute(pivot, last.Value))
                    last.PreDecrement();
                if (!(first.Less(last)))
                    return first;
                IterSwap(first, last);
                first.PreIncrement();
            }
        }
        private static int
            __lg(int n)
        {
            int k;
            for (k = 0; n != 1; n >>= 1)
                ++k;
            return k;
        }

        private static void
            InsertionSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> comparison)
        {
            first = (IRandomAccessIterator<T>)first.Clone();
            if (Equals(first, last))
                return;
            for (IRandomAccessIterator<T> i = first.Add(1); !Equals(i, last); i.PreIncrement())
                LinearInsert(first, i, comparison);
        }
        private static void
            LinearInsert<T>(IRandomAccessIterator<T> first,
                                  IRandomAccessIterator<T> last,/* obj*,*/ IBinaryFunction<T, T, bool> comparison)
        {
            T val = last.Value;
            if (comparison.Execute(val, first.Value))
            {
                CopyBackward(first, last, last.Add(1));//GEN
                first.Value = val;
            }
            else
                UnguardedLinearInsert(last, val, comparison);
        }
        private static void
            UnguardedLinearInsert<T>(IRandomAccessIterator<T> last, T val, IBinaryFunction<T, T, bool> comparison)
        {
            last = (IRandomAccessIterator<T>)last.Clone();
            IRandomAccessIterator<T> next = (IRandomAccessIterator<T>)last.Clone();
            next.PreDecrement();
            while (comparison.Execute(val, next.Value))
            {
                last.Value = next.Value;
                last = (IRandomAccessIterator<T>)next.Clone();
                next.PreDecrement();
            }
            last.Value = val;
        }
        private static void
            UnguardedInsertionSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last, IBinaryFunction<T, T, bool> comparison)
        {
            for (IRandomAccessIterator<T> i = (IRandomAccessIterator<T>)first.Clone(); !Equals(i, last); i.PreIncrement())
                UnguardedLinearInsert(i, i.Value, comparison);
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We just need to be sure that a valid buffer is returned!")]
        private static T[]
            CreateBuffer<T>(IInputIterator<T> first, IInputIterator<T> last)
        {
            try
            {
                int dist = first.DistanceTo(last); //GEN
                T[] buffer = new T[dist];
                Copy(first, last, buffer.Begin());
                return buffer;
            }
            catch
            {
                return null;
            }
        }
        private static void
            MergeWithoutBuffer<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> middle,
                                   IBidirectionalIterator<T> last,
                                   int length1, int length2,
                                   IBinaryFunction<T, T, bool> comparison)
        {
            if (length1 == 0 || length2 == 0)
                return;
            if (length1 + length2 == 2)
            {
                if (comparison.Execute(middle.Value, first.Value))
                    IterSwap(first, middle);
                return;
            }
            IBidirectionalIterator<T> FirstCut = (IBidirectionalIterator<T>)first.Clone();
            IBidirectionalIterator<T> SecondCut = (IBidirectionalIterator<T>)middle.Clone();
            int len11;
            int len22;
            if (length1 > length2)
            {
                len11 = length1 / 2;
                FirstCut = FirstCut.Add(len11);

                // cast below is safe, algorithms pass back the same iterators that the take on the right side
                SecondCut = LowerBound(middle, last, FirstCut.Value, comparison);
                len22 = middle.DistanceTo(SecondCut);//GEN
            }
            else
            {
                len22 = length2 / 2;
                SecondCut = SecondCut.Add(len22);
                // cast below is safe, algorithms pass back the same iterators that the take on the right side
                FirstCut = UpperBound(first, middle, SecondCut.Value, comparison);
                len11 = first.DistanceTo(FirstCut);//GEN
            }
            IBidirectionalIterator<T> _New_middle = Rotate(FirstCut, middle, SecondCut);
            MergeWithoutBuffer(first, FirstCut, _New_middle, len11, len22, comparison);
            MergeWithoutBuffer(_New_middle, SecondCut, last, length1 - len11, length2 - len22, comparison);
        }
        private static IBidirectionalIterator<T>
            MergeBackward<T>(IBidirectionalIterator<T> first1, IBidirectionalIterator<T> last1,
                             IBidirectionalIterator<T> first2, IBidirectionalIterator<T> last2,
                             IBidirectionalIterator<T> result, IBinaryFunction<T, T, bool> comparison)
        {
            if (Equals(first1, last1))
                return CopyBackward(first2, last2, result);
            if (Equals(first2, last2))
                return CopyBackward(first1, last1, result);
            last1 = (IBidirectionalIterator<T>)last1.Clone();
            last2 = (IBidirectionalIterator<T>)last2.Clone();
            result = (IBidirectionalIterator<T>)result.Clone();

            last1.PreDecrement();
            last2.PreDecrement();
            for (; ; )
            {
                if (comparison.Execute(last2.Value, last1.Value))
                {
                    (result.PreDecrement()).Value = last1.Value;
                    if (Equals(first1, last1))
                        return CopyBackward(first2, (IBidirectionalIterator<T>)last2.PreIncrement(), result);//GEN
                    last1.PreDecrement();
                }
                else
                {
                    (result.PreDecrement()).Value = last2.Value;
                    if (Equals(first2, last2))
                        return CopyBackward(first1, (IBidirectionalIterator<T>)last1.PreIncrement(), result);
                    last2.PreDecrement();
                }
            }
        }
        private static IBidirectionalIterator<T>
            RotateAdaptive<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> middle,
                              IBidirectionalIterator<T> last,
                              int length1, int length2,
                              IBidirectionalIterator<T> buffer,
                              int bufferSize)
        {
            IBidirectionalIterator<T> _Buffer_end;
            if (length1 > length2 && length2 <= bufferSize)
            {
                //cast is safe, what goes into an algorithm come out!
                _Buffer_end = Copy(middle, last, buffer);
                CopyBackward(first, middle, last);
                return Copy(buffer, _Buffer_end, first);//cast is safe, what goes in a algorithm come out!
            }
            else if (length1 <= bufferSize)
            {
                //cast is safe, what goes into an algorithm come out!
                _Buffer_end = Copy(first, middle, buffer);
                Copy(middle, last, first);
                return CopyBackward(buffer, _Buffer_end, last);
            }
            else
                return Rotate(first, middle, last);
        }
        private static void
            MergeAdaptive<T>(IBidirectionalIterator<T> first, IBidirectionalIterator<T> middle, IBidirectionalIterator<T> last,
                             int length1, int length2,
                             IBidirectionalIterator<T> buffer, int bufferSize,
                             IBinaryFunction<T, T, bool> comparison)
        {
            if (length1 <= length2 && length1 <= bufferSize)
            {
                IBidirectionalIterator<T> _Buffer_end = Copy(first, middle, buffer);
                Merge(buffer, _Buffer_end, middle, last, first, comparison);
            }
            else if (length2 <= bufferSize)
            {
                IBidirectionalIterator<T> _Buffer_end = Copy(middle, last, buffer);
                MergeBackward(first, middle, buffer, _Buffer_end, last, comparison);
            }
            else
            {
                IBidirectionalIterator<T> _First_cut = (IBidirectionalIterator<T>)first.Clone();
                IBidirectionalIterator<T> _Second_cut = (IBidirectionalIterator<T>)middle.Clone();
                int len11;
                int len22;
                if (length1 > length2)
                {
                    len11 = length1 / 2;
                    _First_cut = _First_cut.Add(len11);
                    _Second_cut = LowerBound(middle, last, _First_cut.Value, comparison);
                    len22 = middle.DistanceTo(_Second_cut);
                }
                else
                {
                    len22 = length2 / 2;
                    _Second_cut = _Second_cut.Add(len22);
                    _First_cut = UpperBound(first, middle, _Second_cut.Value, comparison);
                    len11 = first.DistanceTo(_First_cut);//GEN
                }
                IBidirectionalIterator<T> _New_middle =
                    RotateAdaptive(_First_cut, middle, _Second_cut, length1 - len11, len22, buffer, bufferSize);
                MergeAdaptive(first, _First_cut, _New_middle, len11, len22, buffer, bufferSize, comparison);
                MergeAdaptive(_New_middle, _Second_cut, last, length1 - len11, length2 - len22, buffer, bufferSize, comparison);
            }
        }
        private static void
            ChunkInsertionSort<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last,
                                   int chunkSize, IBinaryFunction<T, T, bool> comparison)
        {
            while (last.Diff(first) >= chunkSize)
            {
                InsertionSort(first, first.Add(chunkSize), comparison);
                first = first.Add(chunkSize);
            }
            InsertionSort(first, last, comparison);
        }
        private static void
            MergeSortLoop<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last,
                              IRandomAccessIterator<T> result, int stepSize, IBinaryFunction<T, T, bool> comparison)
        {
            int twoSteps = 2 * stepSize;

            while (last.Diff(first) >= twoSteps)
            {
                result = Merge(first, first.Add(stepSize),
                                 first.Add(stepSize), first.Add(twoSteps),
                                 result, comparison);
                first = first.Add(twoSteps);
            }
            stepSize = Min(last.Diff(first), stepSize);
            Merge(first, first.Add(stepSize),
                      first.Add(stepSize), last,
                      result, comparison);
        }
        private static void
            MergeSortWithBuffer<T>(IRandomAccessIterator<T> first, IRandomAccessIterator<T> last,
                                     IRandomAccessIterator<T> buffer, IBinaryFunction<T, T, bool> comparison)
        {
            int length = last.Diff(first);
            IRandomAccessIterator<T> _Buffer_last = buffer.Add(length);

            int stepSize = ChunkSize;
            ChunkInsertionSort(first, last, stepSize, comparison);

            while (stepSize < length)
            {
                MergeSortLoop(first, last, buffer, stepSize, comparison);
                stepSize *= 2;
                MergeSortLoop(buffer, _Buffer_last, first, stepSize, comparison);
                stepSize *= 2;
            }
        }
    }
}

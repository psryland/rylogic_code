//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System.Collections.Generic;
using System.Text;
using pr.extn;

namespace pr.extn
{
	public static class StringBuilderExtensions
	{
		/// <summary>Append a bunch of stuff</summary>
		public static StringBuilder Append(this StringBuilder sb, params object[] parts)
		{
			foreach (var p in parts) sb.Append(p.ToString());
			return sb;
		}

		/// <summary>Trim chars from the end of the string builder</summary>
		public static StringBuilder TrimEnd(this StringBuilder sb, params char[] chars)
		{
			while (sb.Length != 0 && chars.IndexOf(sb[sb.Length-1]) != -1) --sb.Length;
			return sb;
		}

		/// <summary>Trim chars from the start of the string builder</summary>
		public static StringBuilder TrimStart(this StringBuilder sb, params char[] chars)
		{
			int i = 0;
			while (i != sb.Length && chars.IndexOf(sb[i]) != -1) ++i;
			return sb.Remove(0, i);
		}

		/// <summary>Trim characters from the start/end of the string</summary>
		public static StringBuilder Trim(this StringBuilder sb, params char[] chars)
		{
			return sb.TrimStart(chars).TrimEnd(chars);
		}

		/// <summary>Append 'lines' ensuring exactly one new line between each</summary>
		public static StringBuilder AppendLineList(this StringBuilder sb, params string[] lines)
		{
			sb.TrimEnd('\n','\r').AppendLine();
			foreach (var line in lines)
				sb.AppendLine(line.Trim('\n','\r'));
			return sb;
		}

		/// <summary>Append 'line' ensuring exactly one new line in between</summary>
		public static StringBuilder AppendLineList(this StringBuilder sb, string line)
		{
			return sb.TrimEnd('\n','\r').AppendLine().AppendLine(line.Trim('\n','\r'));
		}

		/// <summary>Append 'line' ensuring exactly one new line in between</summary>
		public static StringBuilder AppendLineList(this StringBuilder sb, StringBuilder line)
		{
			return sb.TrimEnd('\n','\r').AppendLine().AppendLine(line.ToString());
		}

		/// <summary>Reverse the characters in the string</summary>
		public static StringBuilder Reverse(this StringBuilder sb)
		{
			var end = sb.Length - 1;
			for (int i = 0, iend = sb.Length / 2; i < iend; ++i)
			{
				var tmp = sb[i];
				sb[i] = sb[end - i];
				sb[end - i] = tmp;
			}
			return sb;
		}

		///// <summary>Return a substring of the internal string</summary>
		//public static string Substring(this StringBuilder sb, int startIndex, int length)
		//{
		//    return sb.ToString(startIndex, length);
		//}

		///// <summary>Remove all occurances of 'ch' from the internal string</summary>
		//public static StringBuilder Remove(this StringBuilder sb, char ch)
		//{
		//    for (int i = 0; i < sb.Length; )
		//    {
		//        if (sb[i] == ch)
		//            sb.Remove(i, 1);
		//        else
		//            i++;
		//    }
		//    return sb;
		//}

		///// <summary>Truncate the string by 'num' characters</summary>
		//public static StringBuilder RemoveFromEnd(this StringBuilder sb, int num)
		//{
		//    return sb.Remove(sb.Length - num, num);
		//}

		///// <summary>Trim left spaces of string</summary>
		//public static StringBuilder LTrim(this StringBuilder sb)
		//{
		//    if (sb.Length != 0)
		//    {
		//        int length = 0;
		//        int num2 = sb.Length;
		//        while ((sb[length] == ' ') && (length < num2))
		//        {
		//            length++;
		//        }
		//        if (length > 0)
		//        {
		//            sb.Remove(0, length);
		//        }
		//    }
		//    return sb;
		//}

		///// <summary>rim right spaces of string</summary>
		//public static StringBuilder RTrim(this StringBuilder sb)
		//{
		//    if (sb.Length != 0)
		//    {
		//        int length = sb.Length;
		//        int num2 = length - 1;
		//        while ((sb[num2] == ' ') && (num2 > -1))
		//        {
		//            num2--;
		//        }
		//        if (num2 < (length - 1))
		//        {
		//            sb.Remove(num2 + 1, (length - num2) - 1);
		//        }
		//    }
		//    return sb;
		//}

		///// <summary>
		///// Get index of a char
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="c"></param>
		///// <returns></returns>
		//public static int IndexOf(this StringBuilder sb, char value)
		//{
		//    return IndexOf(sb, value, 0);
		//}

		///// <summary>
		///// Get index of a char starting from a given index
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="c"></param>
		///// <param name="startIndex"></param>
		///// <returns></returns>
		//public static int IndexOf(this StringBuilder sb, char value, int startIndex)
		//{
		//    for (int i = startIndex; i < sb.Length; i++)
		//    {
		//        if (sb[i] == value)
		//        {
		//            return i;
		//        }
		//    }
		//    return -1;
		//}

		///// <summary>
		///// Get index of a string
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="text"></param>
		///// <returns></returns>
		//public static int IndexOf(this StringBuilder sb, string value)
		//{
		//    return IndexOf(sb, value, 0, false);
		//}

		///// <summary>
		///// Get index of a string from a given index
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="text"></param>
		///// <param name="startIndex"></param>
		///// <returns></returns>
		//public static int IndexOf(this StringBuilder sb, string value, int startIndex)
		//{
		//    return IndexOf(sb, value, startIndex, false);
		//}

		///// <summary>
		///// Get index of a string with case option
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="text"></param>
		///// <param name="ignoreCase"></param>
		///// <returns></returns>
		//public static int IndexOf(this StringBuilder sb, string value, bool ignoreCase)
		//{
		//    return IndexOf(sb, value, 0, ignoreCase);
		//}

		///// <summary>
		///// Get index of a string from a given index with case option
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="text"></param>
		///// <param name="startIndex"></param>
		///// <param name="ignoreCase"></param>
		///// <returns></returns>
		//public static int IndexOf(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
		//{
		//    int num3;
		//    int length = value.Length;
		//    int num2 = (sb.Length - length) + 1;
		//    if (ignoreCase == false)
		//    {
		//        for (int i = startIndex; i < num2; i++)
		//        {
		//            if (sb[i] == value[0])
		//            {
		//                num3 = 1;
		//                while ((num3 < length) && (sb[i + num3] == value[num3]))
		//                {
		//                    num3++;
		//                }
		//                if (num3 == length)
		//                {
		//                    return i;
		//                }
		//            }
		//        }
		//    }
		//    else
		//    {
		//        for (int j = startIndex; j < num2; j++)
		//        {
		//            if (char.ToLower(sb[j]) == char.ToLower(value[0]))
		//            {
		//                num3 = 1;
		//                while ((num3 < length) && (char.ToLower(sb[j + num3]) == char.ToLower(value[num3])))
		//                {
		//                    num3++;
		//                }
		//                if (num3 == length)
		//                {
		//                    return j;
		//                }
		//            }
		//        }
		//    }
		//    return -1;
		//}

		///// <summary>
		///// Determine whether a string starts with a given text
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="value"></param>
		///// <returns></returns>
		//public static bool StartsWith(this StringBuilder sb, string value)
		//{
		//    return StartsWith(sb, value, 0, false);
		//}

		///// <summary>
		///// Determine whether a string starts with a given text (with case option)
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="value"></param>
		///// <param name="ignoreCase"></param>
		///// <returns></returns>
		//public static bool StartsWith(this StringBuilder sb, string value, bool ignoreCase)
		//{
		//    return StartsWith(sb, value, 0, ignoreCase);
		//}

		///// <summary>
		///// Determine whether a string is begin with a given text
		///// </summary>
		///// <param name="sb"></param>
		///// <param name="value"></param>
		///// <param name="startIndex"></param>
		///// <param name="ignoreCase"></param>
		///// <returns></returns>
		//public static bool StartsWith(this StringBuilder sb, string value, int startIndex, bool ignoreCase)
		//{
		//    int length = value.Length;
		//    int num2 = startIndex + length;
		//    if (ignoreCase == false)
		//    {
		//        for (int i = startIndex; i < num2; i++)
		//        {
		//            if (sb[i] != value[i - startIndex])
		//            {
		//                return false;
		//            }
		//        }
		//    }
		//    else
		//    {
		//        for (int j = startIndex; j < num2; j++)
		//        {
		//            if (char.ToLower(sb[j]) != char.ToLower(value[j - startIndex]))
		//            {
		//                return false;
		//            }
		//        }
		//    }
		//    return true;
		//}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;

	[TestFixture] public static partial class UnitTests
	{
		[Test] public static void TestStringBuilderExtensions()
		{
			const string  s = "  \t\nA string XXX\n  \t  ";

			{// Trim start
				var sb = new StringBuilder(s);
				Assert.AreEqual(s.TrimStart(' ', '\t', '\n', 'X'), sb.TrimStart(' ', '\t', '\n', 'X').ToString());
			}
			{// Trim end
				StringBuilder sb = new StringBuilder(s);
				Assert.AreEqual(s.TrimEnd(' ', '\t', '\n', 'X'), sb.TrimEnd(' ', '\t', '\n', 'X').ToString());
			}
			{// Trim
				StringBuilder sb = new StringBuilder(s);
				Assert.AreEqual(s.Trim(' ', '\t', '\n', 'X'), sb.Trim(' ', '\t', '\n', 'X').ToString());
			}
		}
	}
}
#endif
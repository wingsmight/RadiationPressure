using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class StringExt
{
    public static string RemoveAllOccurrences(this string text, char occurrence)
    {
        return text.RemoveAllOccurrences(occurrence.ToString());
    }
    public static string RemoveAllOccurrences(this string text, string occurrence)
    {
        int occurrenceIndex;
        do
        {
            occurrenceIndex = text.IndexOf(occurrence);
            if (occurrenceIndex >= 0)
            {
                text = text.Remove(occurrenceIndex, occurrence.Length);
            }
        } while (occurrenceIndex >= 0);

        return text;
    }
    public static string ReplaceAt(this string input, int index, char newChar)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }
        char[] chars = input.ToCharArray();
        chars[index] = newChar;
        return new string(chars);
    }
    public static List<int> AllIndexesOf(this string str, string value)
    {
        if (String.IsNullOrEmpty(value))
            throw new ArgumentException("the string to find may not be empty", "value");
        List<int> indexes = new List<int>();
        for (int index = 0; ; index += value.Length)
        {
            index = str.IndexOf(value, index);
            if (index == -1)
                return indexes;
            indexes.Add(index);
        }
    }
    public static string AddColorTag(this string text, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);

        return "<color=#" + colorHex + ">" + text + "</color>";
    }
    public static string AddIndentTag(this string text, int percent)
    {
        string indentTagStart = "<indent=";
        string indentTagFinish = "%></indent>";

        if (text.StartsWith(indentTagStart))
        {
            text = text.Remove(0, indentTagStart.Length);

            while (text.Length > 0 && text[0] != indentTagFinish[0])
            {
                text = text.Remove(0, 1);
            }

            text = text.Remove(0, indentTagFinish.Length);
        }

        text = indentTagStart + percent + indentTagFinish + text;
        return text;
    }
    public static List<int> ExtractNumbers(this string text)
    {
        List<int> numbers = new List<int>();

        string[] numbersAsString = Regex.Split(text, @"\D+");
        foreach (string numberAsString in numbersAsString)
        {
            if (!string.IsNullOrEmpty(numberAsString))
            {
                numbers.Add(int.Parse(numberAsString));
            }
        }

        return numbers;
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RateListener.ExtensionMethods;

public static class StringExtensions
{
    private static readonly char[] ExcludeChars = [' ', '.', ',', ';', '-', '(', ')', '#', '+'];

    /// <summary>
    /// Эквивалент string.IsNullOrEmpty(inStr)
    /// </summary>
    extension(string inStr)
    {
        public bool IsNullOrEmpty() =>
            string.IsNullOrEmpty(inStr);

        /// <summary>
        /// Эквивалент string.IsNullOrWhiteSpace(inStr)
        /// </summary>
        public bool IsNullOrWhiteSpace() =>
            string.IsNullOrWhiteSpace(inStr);

        /// <summary>
        /// Эквивалент !string.IsNullOrEmpty(inStr)
        /// </summary>
        public bool IsFilled() =>
            !string.IsNullOrEmpty(inStr);

        public bool IsAllDigits() =>
            inStr.All(c => c.IsDigit());

        public string Formatted(params object[] args) =>
            string.Format(inStr, args);

        /// <summary>
        /// Удаляет все повторяющиеся запятые в строке, затем схлопывает повторяющиеся табы и пробелы до 1 пробела.
        /// Например, строка "    , ,  ab,  ,,   ,cdef, g  , ,h34, ,,5 , " будет сокращена до "ab, cdef, g, h34, 5"
        /// </summary>
        public string RemoveEmptyCommas(string replaceTo = ", ")
        {
            Regex regexAll = new Regex(@"(\A\s*,\s*)|(\s*,\s*,)|(\s*,\s*\z)");
            List<Match> allMatches;
            StringBuilder sb = new StringBuilder(inStr);
            do
            {
                allMatches = regexAll.Matches(sb.ToString()).ToList();
                allMatches.Reverse();
                foreach (var match in allMatches)
                {
                    sb.Remove(match.Index, match.Length);
                    if (match.Index > 0 && match.Index < sb.Length)
                    {
                        sb.Insert(match.Index, replaceTo);
                    }
                }
            }
            while (allMatches.Any());
            var regexSpaces = new Regex(@"(,\s{2,})");
            allMatches = regexSpaces.Matches(sb.ToString()).ToList();
            allMatches.Reverse();
            foreach (var match in allMatches)
            {
                sb.Remove(match.Index, match.Length);
                sb.Insert(match.Index, replaceTo);
            }
            return sb.ToString();
        }

        public string UpFirstLetter()
        {
            if (inStr.IsNullOrEmpty())
            {
                return inStr;
            }
            var sb = new StringBuilder(inStr);
            sb[0] = char.ToUpper(sb[0]);
            return sb.ToString();
        }

        public string Last10PhoneDigits()
        {
            string newValue = new string(inStr.Where(t => !ExcludeChars.Contains(t)).ToArray());
            if (newValue.Length < 10)
            {
                return string.Empty;
            }

            newValue = newValue.Substring(newValue.Length - 10);
            if (!long.TryParse(newValue, out var _))
            {
                return string.Empty;
            }

            return newValue;
        }
    }

    extension(Regex regex)
    {
        public bool IsFullMatch(string value) =>
            regex.IsMatch(value) && regex.Matches(value)[0].Index == 0 && regex.Matches(value)[0].Length == value.Length;
    }

    extension(char inChar)
    {
        public bool IsDigit() =>
            inChar is >= '0' and <= '9';
    }

    extension(int number)
    {
        /// <summary>
        /// Возвращает слова в падеже, зависимом от заданного числа
        /// </summary>
        public string GetDeclension(string nominative, string genitive, string plural, bool includeNumber = true)
        {
            number %= 100;
            if (number is >= 11 and <= 19)
                return includeNumber ? $"{number} {plural}" : $"{plural}";

            string result;
            var i = number % 10;
            switch (i)
            {
                case 1:
                    result = nominative;
                    break;
                case 2:
                case 3:
                case 4:
                    result = genitive;
                    break;
                default:
                    result = plural;
                    break;
            }
            return includeNumber ? $"{number} {result}" : $"{result}";
        }
    }

    extension<T>(IEnumerable<T> enumerable)
    {
        public string JoinedString(string separator = ",") =>
            string.Join(separator, enumerable);
    }

    extension(double effRate)
    {
        public string RateToDisplay(bool isPrecise = false) =>
            isPrecise
                ? effRate < 1 ? $"{1.0 / effRate:N5}" : $"{effRate:N5}"
                : effRate < 1 ? $"{1.0 / effRate:N2}" : $"{effRate:N2}";
    }
}

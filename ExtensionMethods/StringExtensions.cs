using RateListener.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RateListener.ExtensionMethods
{
    public static class StringExtensions
    {
        /// <summary>
        /// Эквивалент string.IsNullOrEmpty(inStr)
        /// </summary>
        public static bool IsNullOrEmpty(this string inStr)
        {
            return string.IsNullOrEmpty(inStr);
        }

        /// <summary>
        /// Эквивалент string.IsNullOrWhiteSpace(inStr)
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string inStr)
        {
            return string.IsNullOrWhiteSpace(inStr);
        }

        /// <summary>
        /// Эквивалент !string.IsNullOrEmpty(inStr)
        /// </summary>
        public static bool IsFilled(this string inStr)
        {
            return !string.IsNullOrEmpty(inStr);
        }

        public static bool IsFullMatch(this Regex regex, string value)
        {
            return regex.IsMatch(value) && regex.Matches(value)[0].Index == 0 && regex.Matches(value)[0].Length == value.Length;
        }

        public static bool IsDigit(this char inChar)
        {
            return inChar >= '0' && inChar <= '9';
        }

        public static string Formatted(this string inStr, params object[] args)
        {
            return string.Format(inStr, args);
        }

        public static bool IsAllDigits(this string inStr)
        {
            return inStr.All(IsDigit);
        }

        /// <summary>
        /// Удаляет все повторяющиеся запятые в строке, затем схлопывает повторяющиеся табы и пробелы до 1 пробела.
        /// Например, строка "    , ,  ab,  ,,   ,cdef, g  , ,h34, ,,5 , " будет сокращена до "ab, cdef, g, h34, 5"
        /// </summary>
        /// <param name="inStr">входная строка</param>
        /// <param name="replaceTo">Необязательный параметр. при желании, можно заменять запятые любыми строками (а не ", ")</param>
        /// <returns></returns>
        public static string RemoveEmptyCommas(this string inStr, string replaceTo = ", ")
        {
            Regex regexAll = new Regex(@"(\A\s*,\s*)|(\s*,\s*,)|(\s*,\s*\z)");
            List<Match> allMatches;
            StringBuilder sb = new StringBuilder(inStr);
            do
            {
                allMatches = regexAll.Matches(sb.ToString()).OfType<Match>().ToList();
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
            Regex regexSpaces = new Regex(@"(,\s{2,})");
            allMatches = regexSpaces.Matches(sb.ToString()).OfType<Match>().ToList();
            allMatches.Reverse();
            foreach (var match in allMatches)
            {
                sb.Remove(match.Index, match.Length);
                sb.Insert(match.Index, replaceTo);
            }
            return sb.ToString();
        }

        public static string UpFirstLetter(this string inStr)
        {
            if (inStr.IsNullOrEmpty())
            {
                return inStr;
            }
            var sb = new StringBuilder(inStr);
            sb[0] = char.ToUpper(sb[0]);
            return sb.ToString();
        }

        public static string JoinedString<T>(this IEnumerable<T> enumerable, string separator = ",")
        {
            return string.Join(separator, enumerable);
        }

        private static readonly char[] _excludeChars = { ' ', '.', ',', ';', '-', '(', ')', '#', '+' };

        public static string Last10PhoneDigits(this string phoneNumber)
        {
            string newValue = new string(phoneNumber.Where(t => !_excludeChars.Contains(t)).ToArray());
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

        /// <summary>
        /// Возвращает слова в падеже, зависимом от заданного числа 
        /// </summary>
        /// <param name="number">Число от которого зависит выбранное слово</param>
        /// <param name="nominative">Именительный падеж слова. Например "день"</param>
        /// <param name="genitive">Родительный падеж слова. Например "дня"</param>
        /// <param name="plural">Множественное число слова. Например "дней"</param>
        /// <param name="includeNumber">включать ли в результирующую строку само число</param>
        /// <returns></returns>
        public static string GetDeclension(this int number, string nominative, string genitive, string plural, bool includeNumber = true)
        {
            number = number % 100;
            if (number >= 11 && number <= 19)
            {
                return includeNumber ? $"{number} {plural}" : $"{plural}";
            }

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

        public static string RateToDisplay(this double effRate, bool isPrecise = false)
        {
            return
                isPrecise
                ? effRate < 1 ? $"{1.0 / effRate:N5}" : $"{effRate:N5}"
                : effRate < 1 ? $"{1.0 / effRate:N2}" : $"{effRate:N2}";
        }
    }
}

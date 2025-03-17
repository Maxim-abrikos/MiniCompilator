using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Compiler
{
    internal static class LeksAnalisation
    {
        private static readonly HashSet<string> _variables = new HashSet<string>();


        //public static List<string> Analyze(TextEditor TE)
        //{
        //    var result = new List<string>();

        //    for (int i = 0; i < TE.LineCount; i++)
        //    {
        //        var line = TE.Document.GetText(TE.Document.GetLineByNumber(i + 1)).Trim();

        //        if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
        //            continue;

        //        var lineErrors = new List<string>();

        //        if (!line.EndsWith(";"))
        //        {
        //            var errorPosition = line.Length;
        //            lineErrors.Add($"Ошибка синтаксиса окончания строки: строка должна заканчиваться символом ';'. Позиция: {errorPosition}");
        //        }
        //        else
        //        {
        //            line = line[..^1].Trim();
        //        }

        //        var parts = line.Split(new[] { '=' }, 2);
        //        if (parts.Length != 2)
        //        {
        //            var errorPosition = line.Length;
        //            lineErrors.Add($"Ошибка присваивания: отсутствует символ '='. Позиция: {errorPosition}");
        //        }
        //        else
        //        {
        //            var leftPart = parts[0].Trim();
        //            var rightPart = parts[1].Trim();

        //            if (!CheckLeftPart(leftPart, out var variableName, out var leftError, out var leftErrorPosition))
        //            {
        //                lineErrors.Add($"Ошибка объявления: {leftError}. Позиция: {leftErrorPosition}");
        //            }

        //            if (!CheckRightPart(rightPart, parts[0].Length + 1, out var rightError, out var rightErrorPosition))
        //            {
        //                lineErrors.Add($"Ошибка инициализации: {rightError}. Позиция: {rightErrorPosition}");
        //            }

        //            if (lineErrors.Count == 0)
        //            {
        //                _variables.Add(variableName);
        //            }
        //        }

        //        if (lineErrors.Count > 0)
        //        {
        //            result.Add($"Строка {i + 1}:");
        //            result.AddRange(lineErrors);
        //        }
        //        else
        //        {
        //            var tokens = TokenizeLine(line);
        //            result.Add($"Строка {i + 1}:");
        //            foreach (var token in tokens)
        //            {
        //                result.Add($"{token.Code} - {token.Category} - {token.Text} - {token.IndexInfo}");
        //            }
        //            result.Add("10 - Конец оператора - ; - " + line.Length);
        //        }
        //    }

        //    return result;
        //}
        public static List<string> Analyze(TextEditor TE)
        {
            var result = new List<string>();
            var currentLine = new StringBuilder();
            int lineNumber = 1;

            for (int i = 0; i < TE.LineCount; i++)
            {
                var line = TE.Document.GetText(TE.Document.GetLineByNumber(i + 1)).Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    lineNumber++;
                    continue;
                }

                currentLine.Append(line);

                if (!line.EndsWith(";"))
                {
                    currentLine.Append(" "); // Добавляем пробел для разделения строк
                    continue;
                }

                var fullLine = currentLine.ToString();
                currentLine.Clear();

                var lineErrors = new List<string>();

                if (!fullLine.EndsWith(";"))
                {
                    var errorPosition = fullLine.Length;
                    lineErrors.Add($"Ошибка синтаксиса окончания строки: строка должна заканчиваться символом ';'. Позиция: {errorPosition}");
                }
                else
                {
                    fullLine = fullLine[..^1].Trim();
                }

                var parts = fullLine.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    var errorPosition = fullLine.Length;
                    lineErrors.Add($"Ошибка присваивания: отсутствует символ '='. Позиция: {errorPosition}");
                }
                else
                {
                    var leftPart = parts[0].Trim();
                    var rightPart = parts[1].Trim();

                    if (!CheckLeftPart(leftPart, out var variableName, out var leftError, out var leftErrorPosition))
                    {
                        lineErrors.Add($"Ошибка объявления: {leftError}. Позиция: {leftErrorPosition}");
                    }

                    if (!CheckRightPart(rightPart, parts[0].Length + 1, out var rightError, out var rightErrorPosition))
                    {
                        lineErrors.Add($"Ошибка инициализации: {rightError}. Позиция: {rightErrorPosition}");
                    }

                    if (lineErrors.Count == 0)
                    {
                        _variables.Add(variableName);
                    }
                }

                if (lineErrors.Count > 0)
                {
                    result.Add($"Строка {lineNumber}:");
                    result.AddRange(lineErrors);
                }
                else
                {
                    var tokens = TokenizeLine(fullLine);
                    result.Add($"Строка {lineNumber}:");
                    foreach (var token in tokens)
                    {
                        result.Add($"{token.Code} - {token.Category} - {token.Text} - {token.IndexInfo}");
                    }
                    result.Add("10 - Конец оператора - ; - " + fullLine.Length);
                }

                lineNumber++;
            }

            return result;
        }


        private static List<Token> TokenizeLine(string line)
        {
            var tokens = new List<Token>();
            var buffer = "";
            var startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (char.IsWhiteSpace(ch))
                {
                    if (!string.IsNullOrEmpty(buffer))
                    {
                        tokens.Add(new Token(buffer, startIndex, i - 1));
                        buffer = "";
                    }
                    tokens.Add(new Token(ch.ToString(), i, i));
                    startIndex = i + 1;
                }
                else if (IsSpecialSymbol(ch))
                {
                    if (!string.IsNullOrEmpty(buffer))
                    {
                        tokens.Add(new Token(buffer, startIndex, i - 1));
                        buffer = "";
                    }
                    tokens.Add(new Token(ch.ToString(), i, i));
                    startIndex = i + 1;
                }
                else
                {
                    buffer += ch;
                }
            }

            if (!string.IsNullOrEmpty(buffer))
            {
                tokens.Add(new Token(buffer, startIndex, line.Length - 1));
            }

            return tokens;
        }

        private static bool IsSpecialSymbol(char ch)
        {
            return ch == '=' || ch == '(' || ch == ')' || ch == ',' || ch == ';';
        }

        private static bool CheckLeftPart(string leftPart, out string variableName, out string error, out int errorPosition)
        {
            variableName = null;
            error = null;
            errorPosition = -1;

            var parts = leftPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "ожидается формат 'Complex <имя_переменной>'";
                errorPosition = leftPart.IndexOf(leftPart, StringComparison.Ordinal);
                return false;
            }

            if (parts[0] != "Complex")
            {
                error = "ключевое слово 'Complex' отсутствует или указано неверно";
                errorPosition = leftPart.IndexOf(parts[0], StringComparison.Ordinal);
                return false;
            }

            if (!IsValidVariableName(parts[1]))
            {
                error = $"недопустимое имя переменной: '{parts[1]}'";
                errorPosition = leftPart.IndexOf(parts[1], StringComparison.Ordinal);
                return false;
            }

            variableName = parts[1];
            return true;
        }

        private static bool CheckRightPart(string rightPart, int rightPartStartOffset, out string error, out int errorPosition)
        {
            error = null;
            errorPosition = -1;

            if (_variables.Contains(rightPart))
                return true;

            var match = Regex.Match(rightPart, @"^(new)\s+(Complex)\s*\(\s*([^,]+)\s*,\s*([^)]+)\s*\)$");
            if (!match.Success)
            {

                var newMatch = Regex.Match(rightPart, @"^(new)\s");
                var complexMatch = Regex.Match(rightPart, @"\b(Complex)\b");

                if (!newMatch.Success && !complexMatch.Success)
                {
                    error = "ожидается 'new Complex(<число>, <число>)' или имя существующей переменной";
                    errorPosition = rightPartStartOffset;
                }
                else if (!newMatch.Success)
                {
                    error = "ключевое слово 'new' отсутствует или указано неверно";
                    errorPosition = rightPartStartOffset + rightPart.IndexOf("new", StringComparison.Ordinal);
                    if (errorPosition == -1)
                    {
                        errorPosition = rightPartStartOffset;
                    }
                }
                else if (!complexMatch.Success)
                {
                    error = "ключевое слово 'Complex' отсутствует или указано неверно";
                    errorPosition = rightPartStartOffset + rightPart.IndexOf("new", StringComparison.Ordinal) + "new".Length;
                }
                return false;
            }

            var number1 = match.Groups[3].Value.Trim();
            var number2 = match.Groups[4].Value.Trim();

            if (!IsValidNumber(number1) || !IsValidNumber(number2))
            {
                error = "некорректные аргументы: ожидаются два числа";
                errorPosition = rightPartStartOffset + rightPart.IndexOf(number1, StringComparison.Ordinal);
                return false;
            }

            return true;
        }

        private static bool IsValidVariableName(string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private static bool IsValidNumber(string number)
        {
            return Regex.IsMatch(number, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?$");
        }
    }
}

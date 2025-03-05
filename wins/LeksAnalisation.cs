using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compiler
{
    internal static class LeksAnalisation
    {
        private static readonly HashSet<string> _variables = new HashSet<string>();
        public static List<string> Analyze(TextEditor TE)
        {
            var errors = new List<string>();

            for (int i = 0; i < TE.Document.LineCount; i++)
            {
                var line = TE.Document.GetText(TE.Document.GetLineByNumber(i + 1)).Trim();

                // Игнорируем пустые строки и комментарии
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                // Список ошибок для текущей строки
                var lineErrors = new List<string>();

                // Проверка окончания строки на ';'
                if (!line.EndsWith(";"))
                {
                    lineErrors.Add($"Ошибка синтаксиса окончания строки: строка должна заканчиваться символом ';'.");
                }

                // Убираем ';' для дальнейшего анализа (если он есть)
                line = line.TrimEnd(';').Trim();

                // Разделяем строку на две части по символу '='
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    lineErrors.Add($"Ошибка присваивания: отсутствует символ '='.");
                }
                else
                {
                    var leftPart = parts[0].Trim();
                    var rightPart = parts[1].Trim();

                    // Проверка левой части
                    if (!CheckLeftPart(leftPart, out var variableName, out var leftError))
                    {
                        lineErrors.Add($"Ошибка объявления: {leftError}.");
                    }

                    // Проверка правой части
                    if (!CheckRightPart(rightPart, out var rightError))
                    {
                        lineErrors.Add($"Ошибка инициализации: {rightError}.");
                    }

                    // Если левая часть корректна, добавляем переменную в список
                    if (lineErrors.Count == 0)
                    {
                        _variables.Add(variableName);
                    }
                }

                // Добавляем все ошибки текущей строки в общий список
                if (lineErrors.Count > 0)
                {
                    errors.Add($"Строка {i + 1}:");
                    errors.AddRange(lineErrors);
                }
            }

            // Вывод ошибок
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }
            }
            else
            {
                Console.WriteLine("Ошибок не найдено.");
            }

            // Вывод ошибок
            if (errors.Count > 0)
            {
                return errors;
            }
            else
            {
                return null;
            }
        }

        private static bool CheckLeftPart(string leftPart, out string variableName, out string error)
        {
            variableName = null;
            error = null;

            // Проверяем формат: "Complex <имя_переменной>"
            var parts = leftPart.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                error = "ожидается формат 'Complex <имя_переменной>'";
                return false;
            }

            if (parts[0] != "Complex")
            {
                error = "ключевое слово 'Complex' отсутствует или указано неверно";
                return false;
            }

            // Проверяем имя переменной
            if (!IsValidVariableName(parts[1]))
            {
                error = $"недопустимое имя переменной: '{parts[1]}'";
                return false;
            }

            variableName = parts[1];
            return true;
        }

        private static bool CheckRightPart(string rightPart, out string error)
        {
            error = null;

            // Проверяем, является ли правая часть именем существующей переменной
            if (_variables.Contains(rightPart))
                return true;

            // Проверяем, является ли правая часть конструкцией "new Complex(<число>, <число>)"
            var match = Regex.Match(rightPart, @"^new\s+Complex\s*\(\s*([^,]+)\s*,\s*([^)]+)\s*\)$");
            if (!match.Success)
            {
                error = "ожидается 'new Complex(<число>, <число>)' или имя существующей переменной";
                return false;
            }

            // Проверяем корректность чисел
            var number1 = match.Groups[1].Value.Trim();
            var number2 = match.Groups[2].Value.Trim();

            if (!IsValidNumber(number1) || !IsValidNumber(number2))
            {
                error = "некорректные аргументы: ожидаются два числа";
                return false;
            }

            return true;
        }

        private static bool IsValidVariableName(string name)
        {
            // Имя переменной должно начинаться с буквы и содержать только буквы, цифры и '_'
            return Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private static bool IsValidNumber(string number)
        {
            // Проверяем, что число корректное (целое, дробное, с экспонентой)
            return Regex.IsMatch(number, @"^[+-]?\d+(\.\d+)?([eE][+-]?\d+)?$");
        }


    }
}

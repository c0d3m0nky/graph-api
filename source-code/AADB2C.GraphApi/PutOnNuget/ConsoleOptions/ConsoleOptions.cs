using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using AADB2C.GraphApi.PutOnNuget.Extensions;

namespace AADB2C.GraphApi.PutOnNuget.ConsoleOptions
{
    public static class ConsoleOptions
    {
        public static bool CancelPromptShown = false;
        public static IEnumerator<string> Script = null;
        public static TimeSpan ScriptSleep = TimeSpan.Zero;
        public static ConsoleColor CancelColor = ConsoleColor.Magenta;
        public static ConsoleColor InfoColor = ConsoleColor.DarkYellow;
        public static ConsoleColor ErrorColor = ConsoleColor.Red;
        public static ConsoleColor AskDividerColor = ConsoleColor.Green;
        public static ConsoleColor AskColor = ConsoleColor.DarkCyan;

        private static void ShowCancelPrompt()
        {
            if (!CancelPromptShown)
            {
                var currentColor = Console.ForegroundColor;

                Console.ForegroundColor = InfoColor;
                Console.WriteLine();
                Console.Write("When you see prompts in ");
                Console.ForegroundColor = CancelColor;
                Console.Write(CancelColor.ToString());
                Console.ForegroundColor = InfoColor;
                Console.Write(" it means you can enter 'c' to cancel");
                Console.WriteLine();
                CancelPromptShown = true;
                Console.ForegroundColor = currentColor;
            }
        }

        private static (string question, Action setColor, Action resetColor, bool isMultiline, bool isEmpty) QuestionMeta(string question, bool allowCancel)
        {
            var currentColor = Console.ForegroundColor;
            Action nullAction = () => { };
            Action setCancelColor = () => Console.ForegroundColor = CancelColor;
            Action resetCancelColor = () => Console.ForegroundColor = currentColor;

            question = question.IfNullOrWhitespace("").Trim();

            return (question, setColor: allowCancel ? setCancelColor : nullAction, allowCancel ? resetCancelColor : nullAction, question.Contains("\n"), question.IsNullOrWhitespace());
        }

        internal static string InputOrScript()
        {
            const string pauseChar = "*";

            bool handledByScript(out string r)
            {
                r = null;

                if (Script == null) return false;

                do
                {
                    var end = !Script.GetNext(out r);

                    if (end)
                    {
                        Script = null;
                        r = null;
                        return false;
                    }

                    if (r == pauseChar)
                    {
                        Console.Write("Script Paused. Enter to continue, Ctrl+c to end");
                        Console.ReadLine();
                    }
                } while (r != pauseChar);

                Console.WriteLine(r);
                if (ScriptSleep > TimeSpan.Zero) Thread.Sleep((int) ScriptSleep.TotalMilliseconds);

                return true;
            }

            if (!handledByScript(out var resp)) return Console.ReadLine();

            return resp;
        }

        public static bool? YesNoCancel(string question)
        {
            ShowCancelPrompt();
            bool? yn = null;
            var meta = QuestionMeta(question, true);

            do
            {
                meta.setColor();

                if (meta.isMultiline) Console.WriteLine($"{meta.question}");
                else Console.Write($"{meta.question} ");

                meta.resetColor();

                var resp = InputOrScript();

                if (string.Equals(resp, "c", StringComparison.CurrentCultureIgnoreCase)) break;

                yn = resp.ParseBool();
            } while (!yn.HasValue);

            return yn;
        }

        public static bool YesNo(string question)
        {
            bool? yn;
            var meta = QuestionMeta(question, true);

            do
            {
                meta.setColor();

                if (meta.isMultiline) Console.WriteLine($"{question}");
                else Console.Write($"{question} ");

                meta.resetColor();

                yn = InputOrScript().ParseBool();
            } while (!yn.HasValue);

            return yn.Value;
        }

        public static Dictionary<string, object> GetFields(string question, Func<string, string> mutate = null, Func<string, bool> isValid = null, bool allowCancel = true)
        {
            var meta = QuestionMeta(question, allowCancel);
            var explain = $"Enter fields as key:value";

            Console.WriteLine(question);
            Console.WriteLine(explain);
            var fields = new Dictionary<string, object>();

            do
            {
                var field = GetInput("", s => s.IfNullOrWhitespace(""), allowCancel: allowCancel);

                if (field == null) return null;

                if (field == "")
                {
                    if (YesNo("Done?")) break;

                    continue;
                }

                var spl = field.Trim().Split(new[] {':', '='}, StringSplitOptions.RemoveEmptyEntries).Select(s => s?.Trim()).ToArray();

                if (spl.Length != 2 || spl.Any(s => s.IsNullOrWhitespace()))
                {
                    Console.ForegroundColor = ErrorColor;
                    Console.WriteLine();
                    Console.WriteLine("Invalid entry");
                    Console.WriteLine(explain);
                    meta.resetColor();
                    continue;
                }

                var key = spl[0];
                var val = spl[1].ParseBestGuess();

                while (fields.ContainsKey(key))
                {
                    var replace = YesNoCancel("Key exists. Replace value?");

                    if (!replace.HasValue) return null;

                    if (!replace.Value)
                    {
                        key = GetInput("Key:");

                        if (key == null) return null;
                    }
                }

                fields[key] = val;
            } while (true);

            return fields;
        }

        public static string GetInput(string question, Func<string, bool> isValid = null, Func<string, string> mutate = null, bool allowCancel = true)
            => GetInput(question, isValid, mutate ?? (s => s), null, allowCancel);

        public static T GetInput<T>(string question, Func<string, bool> isValid, Func<string, T> mutate, bool allowCancel = true)
            => GetInput(question, isValid, mutate, null, allowCancel);

        public static T GetInput<T>(string question, Func<string, T> mutate, Func<T, bool> isValid = null, bool allowCancel = true)
            => GetInput(question, null, mutate, isValid, allowCancel);

        public static T GetInput<T>(string question, Func<string, bool> preValid, Func<string, T> mutate, Func<T, bool> postValid, bool allowCancel)
        {
            if (allowCancel) ShowCancelPrompt();

            if (question.IsNullOrWhitespace()) return default;

            if (mutate == null) throw new ArgumentException($"{nameof(mutate)} cannot be null");

            var meta = QuestionMeta(question, true);

            preValid = preValid ?? (s => true);
            postValid = postValid ?? (s => true);

            do
            {
                meta.setColor();

                if (meta.isMultiline || meta.isEmpty) Console.WriteLine($"{question}");
                else Console.Write($"{question} ");

                meta.resetColor();

                var resp = InputOrScript();

                if (allowCancel && string.Equals(resp, "c", StringComparison.CurrentCultureIgnoreCase)) return default;

                if (preValid(resp))
                {
                    var mut = mutate(resp);

                    if (postValid(mut)) return mut;
                }
            } while (true);
        }
    }

    public class ConsoleOptions<T>
    {
        private readonly bool _multiline;
        private readonly bool _allowCancel;
        private readonly string _cancelChar;
        private readonly string _cancelStr;
        private readonly Dictionary<int, Option<T>> _options;

        public ConsoleOptions(string question, IEnumerable<Option<T>> options, string cancelText = "Cancel")
        {
            var multiline = false;
            _options = options.ToDictionary((o, i) => i, (o, i) =>
            {
                multiline = _multiline || o.Description.Contains("\n");
                return o;
            });

            _multiline = multiline;
            Question = question;
            _allowCancel = !cancelText.IsNullOrWhitespace();

            if (_allowCancel)
            {
                _cancelStr = cancelText.Trim();
                _cancelChar = _cancelStr.First().ToString().ToLower();
                _multiline = _multiline || _cancelStr.Contains("\n");
            }
        }

        private string Question { get; }

        public IReadOnlyDictionary<int, Option<T>> Options => new ReadOnlyDictionary<int, Option<T>>(_options);

        public Option<T> Ask()
        {
            if (Options.All(o => !o.Value.Show)) return null;

            var cc = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleOptions.AskDividerColor;
            Console.WriteLine();
            Console.WriteLine("-----");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleOptions.AskColor;

            Option<T> opt = null;

            do
            {
                Console.WriteLine(Question);
                PrintOptions();
                var resp = ConsoleOptions.InputOrScript();

                if (string.Equals(resp, _cancelChar, StringComparison.CurrentCultureIgnoreCase)) break;

                opt = _options.GetValueOrDefault(resp.ParseInt());
            } while (opt == null);

            Console.ForegroundColor = cc;

            return opt;
        }

        private void PrintOptions()
        {
            var fg = Console.ForegroundColor;
            var format = _multiline
                ? @"-- {0} --
{1}"
                : "\t{0} -- {1}";

            Options.Where(o => o.Value.Show).ForEach(p =>
            {
                if (p.Value.Color.HasValue) Console.ForegroundColor = p.Value.Color.Value;
                Console.WriteLine(format, p.Key, p.Value.Description);
                Console.ForegroundColor = fg;
            });

            if (_allowCancel) Console.WriteLine($"\t{_cancelChar} -- {_cancelStr}");
        }
    }
}
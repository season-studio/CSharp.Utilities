using System.Globalization;

namespace SeasonStudio.Common
{
    public static class ConsoleEx
    {
        public class ConsoleExInstance
        {
            internal ConsoleExInstance() { }

            private static ConsoleColor? ParseColor(string _colorStr)
            {
                if (Enum.TryParse(_colorStr, true, out ConsoleColor color))
                {
                    return color;
                }
                else if (int.TryParse(_colorStr, System.Globalization.NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int colorIndex)
                    && (colorIndex >= 0) && (colorIndex <= 15))
                {
                    return (ConsoleColor)colorIndex;
                }
                else
                {
                    return null;
                }
            }

            private static void SetColor(string? _colorFmt)
            {
                if (string.IsNullOrWhiteSpace(_colorFmt))
                {
                    Console.ResetColor();
                }
                else
                {
                    var items = _colorFmt.Split(';');
                    if (items.Length > 0)
                    {
                        var color = ParseColor(items[0].Trim());
                        Console.ForegroundColor = color ?? ConsoleColor.White;

                        if (items.Length > 1)
                        {
                            color = ParseColor(items[1].Trim());
                            Console.BackgroundColor = color ?? ConsoleColor.Black;
                        }
                    }
                }
            }

            public ConsoleExInstance Write(params object[] _args)
            {
                foreach (var item in _args)
                {
                    if (item is string maybeColor)
                    {
                        if (maybeColor[0] == '\x1B')
                        {
                            SetColor(maybeColor.Substring(1));
                            continue;
                        }
                    }
                    Console.Write(item);
                }
                return this;
            }

            public ConsoleExInstance WriteLine(params object[] _args)
            {
                Write(_args);
                Console.Write(Environment.NewLine);
                return this;
            }

            public static ConsoleColor TipColor = ConsoleColor.Green;
            public static ConsoleColor InfoColor = ConsoleColor.Blue;
            public static ConsoleColor ErrorColor = ConsoleColor.Red;

            public ConsoleExInstance Tip()
            {
                Console.ForegroundColor = TipColor;
                return this;
            }

            public ConsoleExInstance Error()
            {
                Console.ForegroundColor = ErrorColor;
                return this;
            }

            public ConsoleExInstance Info()
            {
                Console.ForegroundColor = InfoColor;
                return this;
            }

            public ConsoleExInstance Reset()
            {
                Console.ResetColor();
                return this;
            }
        }

        private static ConsoleExInstance instance = new ConsoleExInstance();

        public static ConsoleExInstance Write(params object[] _args)
        {
            instance.Write(_args);
            return instance;
        }

        public static ConsoleExInstance WriteLine(params object[] _args)
        {
            instance.Write(_args);
            Console.Write(Environment.NewLine);
            return instance;
        }

        public static ConsoleExInstance Tip()
        {
            Console.ForegroundColor = ConsoleExInstance.TipColor;
            return instance;
        }

        public static ConsoleExInstance Error()
        {
            Console.ForegroundColor = ConsoleExInstance.ErrorColor;
            return instance;
        }

        public static ConsoleExInstance Info()
        {
            Console.ForegroundColor = ConsoleExInstance.InfoColor;
            return instance;
        }

        public static ConsoleExInstance Reset()
        {
            Console.ResetColor();
            return instance;
        }
    }
}

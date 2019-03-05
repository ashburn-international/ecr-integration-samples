using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SingleThread
{
    public class MenuItem
    {
        public string Key;
        public string Title;
        public Action Action;
    }

    public static class ConsoleGui
    {
        const ConsoleColor ConsoleColorError = ConsoleColor.Red;
        const ConsoleColor ConsoleColorOk = ConsoleColor.Green;
        const ConsoleColor ConsoleColorWarning = ConsoleColor.Yellow;

        public static bool LogTimeStamp = false;

        public enum CONSOLE_MESSAGE_TYPE
        {
            IDENT,
            OK,
            FAILED,
            WARN,
            NONE
        }



        static void ListMenu(List<MenuItem> menu, string Title = "")
        {
            Console.WriteLine();
            if (!string.IsNullOrEmpty(Title)) Console.WriteLine($"==== {Title}");
            menu.ForEach(i => Console.WriteLine($" {i.Key}. {i.Title}"));
            Console.WriteLine("To exit press enter without entering anything...");
        }


        public static MenuItem GetChoiceMenu(List<MenuItem> menuItems, string Title = "")
        {
            if (menuItems == null || menuItems.Count == 0) throw new ArgumentException(nameof(menuItems));

            var choice = null as string;
            MenuItem selectedItem = null;

            do
            {
                selectedItem = null;
                choice = Console.ReadLine().Trim().ToUpper();

                if (string.IsNullOrEmpty(choice))
                {
                    // do nothing
                }
                else if (!menuItems.Select(i => i.Key).Contains(choice))
                {
                    Console.WriteLine($"unknown option [{choice}]");
                }
                else
                {
                    selectedItem = menuItems.First(i => i.Key.Trim().ToUpper() == choice);
                }
            } while (selectedItem == null);

            return selectedItem;
        }

        public static void LoopMenu(List<MenuItem> menuItems, string Title = "")
        {
            if (menuItems == null || menuItems.Count < 2) throw new ArgumentException(nameof(menuItems));

            MenuItem selectedItem = null;
            while (selectedItem != menuItems.Last())
            {
                ListMenu(menuItems, Title);
                selectedItem = GetChoiceMenu(menuItems, Title);
                selectedItem.Action?.Invoke();
            }
        }

        public static T EnterValue<T>(string Prompt, string[] allowedValues = null)
        {
            var allowedValuesInUse = (allowedValues != null && allowedValues.Length > 0);
            var choices = "";

            if (allowedValuesInUse)
            {
                allowedValues = allowedValues.Select(i => i.ToUpper()).ToArray();
                choices = string.Join("/", allowedValues);
                if (!string.IsNullOrEmpty(choices)) choices = $" [{choices}]?";
            }
            if (string.IsNullOrEmpty(choices)) choices = ":";

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{Prompt}{choices} ");
            Console.ResetColor();
            var valueString = Console.ReadLine().Trim();
            try
            {
                if(allowedValuesInUse)
                {
                    valueString = valueString.ToUpper();
                    if (!allowedValues.Contains(valueString))
                        throw new Exception("Invalid value");
                }

                var valueConverted = (T)Convert.ChangeType(valueString, typeof(T));

                if (typeof(T) == typeof(string) && string.IsNullOrEmpty(valueString))
                    return EnterValue<T>(Prompt, allowedValues);
                return valueConverted;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return EnterValue<T>(Prompt, allowedValues);
            }
        }

        public static void Error(string message)
        {
            //Console.ForegroundColor = ConsoleColor.Red;
            //Console.WriteLine($"[ERROR] {message}");
            //Console.ResetColor();
            ConsoleLog(message, CONSOLE_MESSAGE_TYPE.FAILED);
        }

        public static void Warning(string message)
        {
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine($"{message}");
            //Console.ResetColor();
            ConsoleLog(message, CONSOLE_MESSAGE_TYPE.WARN);
        }

        public static void Info(string message)
        {
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.WriteLine($"{message}");
            //Console.ResetColor();
            ConsoleLog(message, CONSOLE_MESSAGE_TYPE.NONE);
        }

        public static void Ok(string message)
        {
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.WriteLine($"{message}");
            //Console.ResetColor();
            ConsoleLog(message, CONSOLE_MESSAGE_TYPE.OK);
        }

        static void ConsoleLog(
                string txt,
                CONSOLE_MESSAGE_TYPE type = CONSOLE_MESSAGE_TYPE.IDENT,
                ConsoleColor color = ConsoleColor.Gray)
        {
            const string OK = "  OK  ";
            const string FAILED = "FAILED";
            const string WARNING = " WARN ";
            const string EMPTY = "         ";

            if (LogTimeStamp)
                Console.Write($"[{DateTime.Now:HH:mm:ss}] ");

            switch (type)
            {
                case CONSOLE_MESSAGE_TYPE.OK:
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColorOk;
                    Console.Write(OK);
                    Console.ResetColor();
                    Console.Write("] ");
                    break;
                case CONSOLE_MESSAGE_TYPE.FAILED:
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColorError;
                    Console.Write(FAILED);
                    Console.ResetColor();
                    Console.Write("] ");
                    break;
                case CONSOLE_MESSAGE_TYPE.WARN:
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColorWarning;
                    Console.Write(WARNING);
                    Console.ResetColor();
                    Console.Write("] ");
                    break;
                case CONSOLE_MESSAGE_TYPE.NONE:
                    break;
                default:
                    Console.Write(EMPTY);
                    break;
            }
            Console.ForegroundColor = color;
            Console.Write(txt);
            Console.WriteLine();
            Console.ResetColor();
        }

    }
}

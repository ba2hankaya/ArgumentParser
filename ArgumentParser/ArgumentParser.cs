using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace ArgumentParserNS
{
    public enum ParserAction
    {
        positional,
        take_value,
        store_true,
        store_false
    }
    public class ArgumentParser
    {
        public ArgumentBuilder AddArgument(params string[] flags)
        {
            var arg = new Argument(flags);
            args.Add(arg);
            return new ArgumentBuilder(arg);
        }
        internal class Argument
        {
            public string help = "", dest = "";
            public List<string> flags;
            public ParserAction parserAction = ParserAction.take_value;
            public Type? valueType = null;
            public Object? value = null;
            string longFlagPattern = @"(^--)";
            string shortFlagPattern = @"(^-[^-])";

            public Argument(params string[] flags)
            {
                this.flags = flags.ToList();
                if (flags.Length == 1 && flags[0] != "" && !Regex.IsMatch(flags[0], longFlagPattern) && !Regex.IsMatch(flags[0], shortFlagPattern))
                {
                    dest = flags[0];
                    parserAction = ParserAction.positional;
                    return;
                }
                foreach (string flag in flags)
                {
                    if (!Regex.IsMatch(flag, longFlagPattern) && !Regex.IsMatch(flag, shortFlagPattern))
                    {
                        throw new ArgumentException($"Flags must start with a dash or double dash.");
                    }
                }
                foreach (string flag in flags)
                {
                    if (Regex.IsMatch(flag, longFlagPattern))
                    {
                        dest = flag.Remove(0, 2).Replace('-', '_');
                        return;
                    }
                }
                dest = flags[0].Remove(0, 1).Replace('-', '_');
            }
        }

        public class ArgumentBuilder
        {
            private Argument _arg;

            internal ArgumentBuilder(Argument arg)
            {
                _arg = arg;
            }

            public ArgumentBuilder WithParserAction(ParserAction action)
            {
                _arg.parserAction = action;
                return this;
            }

            public ArgumentBuilder WithHelp(string help)
            {
                _arg.help = help;
                return this;
            }

            public ArgumentBuilder WithType(Type type)
            {
                _arg.valueType = type;
                if (_arg.value != null && _arg.valueType != null && _arg.value.GetType() != _arg.valueType)
                {
                    throw new ArgumentException($"Value type and default values don't match.");
                }
                return this;
            }

            public ArgumentBuilder WithDefault(object defaultVal)
            {
                _arg.value = defaultVal;
                if (_arg.value != null && _arg.valueType != null && defaultVal.GetType() != _arg.valueType)
                {
                    throw new ArgumentException($"Value type and default values don't match.");
                }
                return this;
            }
        }

        string prog, desc, epilog;
        List<Argument> args = new();

        IDictionary<string, object> keyValuePairs = new Dictionary<string, object>();
        public ArgumentParser(string prog, string desc, string epilog)
        {
            this.prog = prog;
            this.desc = desc;
            this.epilog = epilog;
        }

        public ExpandoObject ArgParse(in string[] inputArgs)
        {
            try
            {
                List<Argument> argscpy = args.ToList();
                for (int i = 0; i < inputArgs.Length; i++)
                {
                    string currentToken = inputArgs[i].Trim();
                    if (currentToken.StartsWith('-'))
                    {
                        if(currentToken == "-h")
                        {
                            PrintHelp();
                            Environment.Exit(0);
                            return new ExpandoObject();
                        }
                        Argument? current = argscpy.FirstOrDefault(x => x.flags.Contains(currentToken));
                        if (current == null)
                        {
                            throw new FormatException($"There are no flags corresponding to '{currentToken}'.");
                        }
                        switch (current.parserAction)
                        {
                            case ParserAction.take_value:
                                if (i + 1 >= inputArgs.Length)
                                {
                                    throw new FormatException($"There isn't a value after value flag '{currentToken}'.");
                                }
                                string strvalue = inputArgs[i + 1];

                                if (strvalue == "" || strvalue[0] == '-')
                                {
                                    throw new FormatException($"There isn't a value after value flag '{currentToken}'.");
                                }
                                if (current.valueType != null)
                                {
                                    try
                                    {
                                        Convert.ChangeType(strvalue, current.valueType);
                                    }
                                    catch
                                    {
                                        throw new FormatException($"Value type doesn't match entered value for flag {currentToken}");
                                    }
                                }
                                object value = ParseBestGuess(strvalue);
                                keyValuePairs.Add(current.dest, value);
                                i++;
                                argscpy.Remove(current);
                                break;
                            case ParserAction.store_true:
                                keyValuePairs.Add(current.dest, true);
                                argscpy.Remove(current);
                                break;
                            case ParserAction.store_false:
                                keyValuePairs.Add(current.dest, false);
                                argscpy.Remove(current);
                                break;
                        }
                    }
                    else
                    {
                        Argument? current = argscpy.FirstOrDefault(x => x.parserAction == ParserAction.positional);
                        if (current == null)
                        {
                            throw new FormatException($"Couldn't parse argument: '{currentToken}'");
                        }
                        if (currentToken == "")
                        {
                            throw new FormatException($"Couldn't parse arguments, check for whitespaces.");
                        }
                        keyValuePairs.Add(current.dest, currentToken);
                        argscpy.Remove(current);
                    }
                }

                foreach (Argument argument in argscpy)
                {
                    switch (argument.parserAction)
                    {
                        case ParserAction.positional:
                            throw new FormatException($"The positional argument {argument.flags[0]} was never given.");
                            break;
                        case ParserAction.take_value:
                            if (argument.value == null) //if a default value wasn't provided
                            {
                                throw new FormatException($"A value wasn't passed with the {argument.flags[0]} flag.");
                            }
                            keyValuePairs.Add(argument.dest, argument.value);
                            break;
                        case ParserAction.store_true:
                            keyValuePairs.Add(argument.dest, false);
                            break;
                        case ParserAction.store_false:
                            keyValuePairs.Add(argument.dest, true);
                            break;
                    }
                }
                string serialized = JsonConvert.SerializeObject(keyValuePairs);
                dynamic? expando = JsonConvert.DeserializeObject<ExpandoObject>(serialized, new ExpandoObjectConverter());
                if ((object?)expando != null)
                {
                    return expando;
                }
                else
                {
                    throw new Exception("No arguments passed to the arg parser.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                PrintHelp();
                Environment.Exit(1);
                return new ExpandoObject();
            }
        }

        public void PrintHelp()
        {
            Console.WriteLine(ConstructHelpMessage());
        }
        private string ConstructHelpMessage()
        {
            string usage = $"usage: {prog} ";
            string descMessage = $"\n\n{desc}\n";
            string options = "\n\noptions:\n";
            string epilogMessage = $"\n{epilog}\n";
            foreach (Argument arg in args)
            {
                switch (arg.parserAction)
                {
                    case ParserAction.positional:
                        usage += $"{arg.flags[0]} ";
                        break;
                    case ParserAction.take_value:
                        usage += $"[{arg.flags[0]} {arg.dest.ToUpper()}] ";
                        break;
                    default:
                        usage += $"[{arg.flags[0]}] ";
                        break;
                }

                string flags = string.Join(", ", arg.flags);
                if (arg.parserAction == ParserAction.positional)
                {
                    options += $"  {flags}\t{arg.help}\n";
                }
                else
                {
                    options += $"  {flags} {arg.dest.ToUpper()}\t{arg.help}\n";
                }
            }
            string final = usage + descMessage + options + epilogMessage;
            return final;
        }
        private object ParseBestGuess(string s)
        {
            if (int.TryParse(s, out var i)) return i;
            if (double.TryParse(s, out var d)) return d;
            if (bool.TryParse(s, out var b)) return b;
            if (DateTime.TryParse(s, out var dt)) return dt;
            return s; // fallback: keep as string
        }
    }

    
}

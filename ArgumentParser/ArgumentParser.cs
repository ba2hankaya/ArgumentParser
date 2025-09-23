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
            public bool isRequired = false;
            public bool isUserRequired = false;
            public bool nargs = false;
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
                _arg.isRequired = true;
                return this;
            }

            public ArgumentBuilder WithNargs()
            {
                _arg.nargs = true;
                return this;
            }

            public ArgumentBuilder WithRequired()
            {
                _arg.isRequired = true;
                _arg.isUserRequired = true;
                return this;
            }
        }

        string prog, desc, epilog;
        List<Argument> args = new();
        bool implementHelp;
        public ArgumentParser(string prog = "", string desc = "", string epilog = "", bool implementHelp = false)
        {
            this.prog = prog;
            if(prog == "") { this.prog = System.AppDomain.CurrentDomain.FriendlyName; }
            this.desc = desc;
            this.epilog = epilog;
            this.implementHelp = implementHelp;
        }

        public ExpandoObject ArgParse(string[] inputArgs)
        {
            if (implementHelp)
            {
                Argument? helpArg = args.FirstOrDefault(x => x.flags.Contains("-h") || x.flags.Contains("--help"));
                if (helpArg != null)
                {
                    Console.WriteLine(ConstructHelpMessage());
                    Environment.Exit(0);
                }
            }

            try
            {
                ExpandoObject expando = new ExpandoObject();
                List<Argument> argscpy = args.ToList();
                for (int i = 0; i < inputArgs.Length; i++)
                {
                    string currentToken = inputArgs[i].Trim();
                    Argument? current;
                    KeyValuePair<string, object> keyValuePair;

                    if (currentToken == "")
                    {
                        throw new FormatException($"Couldn't parse arguments, check for whitespaces.");
                    }

                    if (currentToken.StartsWith('-'))
                    {
                        current = argscpy.FirstOrDefault(x => x.flags.Contains(currentToken));
                        if(current == null)
                        {
                            throw new FormatException($"Unrecognized flag: {currentToken}");
                        }
                        keyValuePair = HandleFlag(current, inputArgs, ref i, currentToken);
                    }
                    else
                    {
                        current = argscpy.FirstOrDefault(x => x.parserAction == ParserAction.positional);
                        if(current == null)
                        {
                            throw new FormatException($"Unexpected token: {currentToken}");
                        }
                        keyValuePair = new KeyValuePair<string, object>(current.dest, currentToken);
                    }
                    expando.TryAdd(keyValuePair.Key, keyValuePair.Value);
                    argscpy.Remove(current);
                }

                HandleRemaining(argscpy, expando);
                return expando;
            }
            catch (Exception ex)
            {
                ExpandoObject expando = new ExpandoObject();
                expando.TryAdd("err_msg", ex.Message.ToString());
                return expando;
            }
        }

        private KeyValuePair<string, object> HandleFlag(Argument current, string[] inputArgs, ref int i, string currentToken)
        {
            KeyValuePair<string, object> keyValuePair;
            switch (current.parserAction)
            {
                case ParserAction.take_value:
                    if (i + 1 >= inputArgs.Length)
                    {
                        throw new FormatException($"There isn't a value after value flag '{currentToken}'.");
                    }

                    i++;
                    string strvalue = inputArgs[i];

                    if (strvalue == "" || strvalue[0] == '-')
                    {
                        throw new FormatException($"There isn't a value after value flag '{currentToken}'.");
                    }

                    object valueobj;

                    if (current.nargs)
                    {
                        List<string> strvalues = new List<string>();
                        strvalues.Add(strvalue);
                        while (i + 1 < inputArgs.Length && !inputArgs[i + 1].StartsWith("-"))
                        {
                            strvalues.Add(inputArgs[i + 1]);
                            i++;
                        }
                        valueobj = strvalues;
                        keyValuePair = new KeyValuePair<string, object>(current.dest, valueobj);
                        break;
                    }


                    if (current.valueType != null)
                    {
                        try
                        {
                            valueobj = Convert.ChangeType(strvalue, current.valueType);
                        }
                        catch
                        {
                            throw new FormatException($"Value type doesn't match entered value for flag {currentToken}");
                        }
                    }
                    else
                    {
                        valueobj = ParseBestGuess(strvalue);
                    }
                    keyValuePair = new KeyValuePair<string, object>(current.dest, valueobj);
                    break;
                case ParserAction.store_true:
                    keyValuePair = new KeyValuePair<string, object>(current.dest, true);
                    break;
                case ParserAction.store_false:
                    keyValuePair = new KeyValuePair<string, object>(current.dest, false);
                    break;
                default:
                    throw new ArgumentException("Flagged arguments can't be positional.");
            }
            return keyValuePair;
        }

        private void HandleRemaining(List<Argument> remaining, ExpandoObject expando)
        {
            foreach (Argument argument in remaining)
            {
                switch (argument.parserAction)
                {
                    case ParserAction.positional:
                        if (argument.isRequired)
                        {
                            throw new FormatException($"The positional argument {argument.flags[0]} was never given.");
                        }
                        break;
                    case ParserAction.take_value:
                        if (argument.value == null) //if a default value wasn't provided
                        {
                            if (argument.isRequired)
                            {
                                throw new FormatException($"A value wasn't passed with the {argument.flags[0]} flag.");
                            }
                        }
                        else
                        {
                            expando.TryAdd(argument.dest, argument.value);
                        }
                        break;
                    case ParserAction.store_true:
                        expando.TryAdd(argument.dest, false);
                        break;
                    case ParserAction.store_false:
                        expando.TryAdd(argument.dest, true);
                        break;
                }
            }
        }

        public string GetHelpMessage()
        {
            return ConstructHelpMessage();
        }
        private string ConstructHelpMessage()
        {
            string usage = $"usage: {prog} ";
            
            string descMessage = desc == "" ? "\n" : $"\n{desc}\n";
            string requiredOptions = "\nrequired options:\n";
            int requiredOptionsInitialLength = requiredOptions.Length;
            string options = "\noptions:\n";
            string epilogMessage = epilog == "" ? "" : $"\n{epilog}\n";
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
                    if (arg.isUserRequired)
                    {
                        requiredOptions += $"  {flags}\t{arg.help}\n";
                    }
                    else
                    {
                        options += $"  {flags}\t{arg.help}\n";
                    }
                }
                else
                {
                    if (arg.isUserRequired)
                    {
                        switch (arg.parserAction)
                        {
                            case ParserAction.take_value:
                                requiredOptions += $"  {flags} {arg.dest.ToUpper()}\t{arg.help}\n";
                                break;
                            default:
                                requiredOptions += $"  {flags} \t{arg.help}";
                                break;
                        }
                    }
                    else
                    {
                        switch (arg.parserAction)
                        {
                            case ParserAction.take_value:
                                options += $"  {flags} {arg.dest.ToUpper()}\t{arg.help}\n";
                                break;
                            default:
                                options += $"  {flags} \t{arg.help}\n";
                                break;
                        }
                    }
                    
                }
            }
            string final = usage + descMessage + (requiredOptionsInitialLength == requiredOptions.Length ? "" : requiredOptions) + options + epilogMessage;
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
        public static bool HasProperty(ExpandoObject obj, string propertyName)
        {
            return obj != null && ((IDictionary<String, object?>)obj).ContainsKey(propertyName);
        }
    }

    
}

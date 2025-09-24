# ArgumentParser

This project is a library for parsing command line arguments passed to the app. It uses a similar syntax to the python argparse module to make it an easier conversion to c#.

## Usage:

Initialize an ArgumentParser object, then add arguments to it similar to the python argparse module. Then parse a list of strings with the ArgParse method which returns an expando object. A difference in this class is that argument parameters aren't taken in a single constructor method but instead via the builder pattern. I preferred the builder pattern because I think it makes the syntax for both the user and the developer of the library clearer. An example of usage is below:

```

ArgumentParser argparse = new ArgumentParser();

argparse.AddArgument("filename"); //Add a positional flag by providing flag name with no dashes

argparse.AddArgument("-v", "--verbose") //Use the constructor to pass the flag of the argument.
	.WithParserAction(ParserAction.store_true); //when argument has ParserAction.store_true or ParserAction.store_false, it doesn't expect an argument. if it is a store true argument it will have the value false in the final expando
						    //object unless the flag is present in the string array provided(in which case it will have value true of course). the store_false parser action has the exact opposite function.

argparse.AddArgument("-l", "--ipaddr")
	.WithDefault("127.0.0.1");		    //default values to arguments can be provided. If the user doesn't provide the flag, the default value will be returned in the final expando object.

argparse.AddArgument("-p", "--port")
	.WithRequired()				    //if arguments with required flags aren't present in the provided string array, the application will exit and display a message explaining why. Required arguments will also be printed under 'required options:' in the help message instead of the regular 'options:'
	.WithHelp("enter the port from which the server should send requests"); //if a help message is provided, it will be displayed next to the argument flags in the help message.
argparse.AddArgument("--client-list")
	.WithNargs()				    //if an argument has nargs property, it will return a string of all following strings until a string which starts with a dash or the end of the array is reached. (It could be improved to return a list of strings instead.)
	.WithRequired()
	.WithHelp("List of clients to send the request to");

dynamic expando = argparse.ArgParse(args);
if(expando.verbose == true){
	Console.WriteLine("true");
}

//Any values passed with a flag will try to be parsed to int, double, bool, DateTime in order. if non are valid, the value will be returned as a string.

```

> [!IMPORTANT]
> If an error is occured during parsing due to user input, an expando object with only "err_msg" property will be returned. Use `ArgumentParser.HasProperty(expando, "err_msg") == true` to see if user input was valid.

More examples are present in the ArgumentParserTests/ArgumentParserTests.cs

## How it's made

The ArgumentParser uses a builder pattern to define arguments in a chainable way. You can set defaults, mark arguments as required, or specify parser actions.

### Argument processing

#### 1. Positional arguments (take_value): 

Values are taken in order from the input array.

#### 2. Non-positional arguments (take_value): 

The parser finds the flag in the array and takes the following value.

#### 3. Boolean flags (store_true / store_false):

store_true: defaults to false, becomes true if the flag is present.

store_false: defaults to true, becomes false if the flag is present.

#### 4. Arguments with nargs:

Capture all consecutive values after the flag until another flag or the end of the array. Returned as a list of strings unless used with `WithType(<type>)`, in which case it will try to return a List<type>.

### Type conversion

Values are converted in this order:

int → double → bool → DateTime → string

If no conversion succeeds, the value remains a string.

### Storage and output

Parsed values are stored in a ExpandoObject and returned as such for flexible and dynamic access to variables. Also ArgumentParser.HasProperty(ExpandoObject, string) helper method is available for ease of use.

## Lessons Learned

I learned to use the builder pattern to chain argument definitions, work with ExpandoObject for dynamic access, and handle values as object types. I also gained experience in writing unit tests for quick, organized validation and learned how to structure and build a C# library class.

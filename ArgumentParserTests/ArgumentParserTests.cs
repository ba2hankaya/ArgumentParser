using ArgumentParserNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace ArgumentParserTests
{
    [TestClass]
    public sealed class ArgumentParserTests
    {
        [TestMethod]
        public void EmptyInputTest()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f");
            dynamic expando = argparse.ArgParse([]);
            string expected = JsonConvert.SerializeObject(new ExpandoObject());
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void TakeValueFlagSingle()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-a");
            dynamic expando = argparse.ArgParse(["-a", "312"]);

            string expected = "{\"a\":312}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void TakeValueFlagSingleWithLong()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f", "--foo");
            dynamic expando = argparse.ArgParse(["-f", "312"]);

            string expected = "{\"foo\":312}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void TakeValueFlagMultiple()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f");
            argparse.AddArgument("-b");
            dynamic expando = argparse.ArgParse(["-f", "312", "-b", "stringVal"]);

            string expected = "{\"f\":312,\"b\":\"stringVal\"}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void TakeValueFlagMultipleWithLong()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f", "--foo");
            argparse.AddArgument("-b", "--bar");
            dynamic expando = argparse.ArgParse(["--bar", "312", "-f", "stringVal"]);

            string expected = "{\"bar\":312,\"foo\":\"stringVal\"}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void PositionalFlagSingle()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("foo");
            dynamic expando = argparse.ArgParse(["input"]);

            string expected = "{\"foo\":\"input\"}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void PositionalFlagMultiple()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("foo");
            argparse.AddArgument("bar");
            dynamic expando = argparse.ArgParse(["input", "input2"]);

            string expected = "{\"foo\":\"input\",\"bar\":\"input2\"}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void StoreTrueFalse()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f", "--foo")
                .WithParserAction(ParserAction.store_true);
            argparse.AddArgument("-b", "--bar")
                .WithParserAction(ParserAction.store_false);
            dynamic expando = argparse.ArgParse(["-b", "--foo"]);

            string expected = "{\"foo\":true,\"bar\":false}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void StoreTrueFalse2()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f", "--foo")
                .WithParserAction(ParserAction.store_true);
            argparse.AddArgument("-b", "--bar")
                .WithParserAction(ParserAction.store_false);
            dynamic expando = argparse.ArgParse(["-b"]);

            string expected = "{\"foo\":false,\"bar\":false}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }
        
        [TestMethod]
        public void Mixed() 
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-v", "--verbose")
                .WithParserAction(ParserAction.store_true);
            argparse.AddArgument("--make-false")
                .WithParserAction(ParserAction.store_false);
            argparse.AddArgument("ipaddr");
            argparse.AddArgument("-p", "--port");

            dynamic expando = argparse.ArgParse("127.0.0.1 -p 4444 -v --make-false".Split());


            string expected = "{\"ipaddr\":\"127.0.0.1\",\"port\":4444,\"verbose\":true,\"make_false\":false}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void Mixed2()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-v", "--verbose")
                .WithParserAction(ParserAction.store_true);
            argparse.AddArgument("--make-false")
                .WithParserAction(ParserAction.store_false);
            argparse.AddArgument("ipaddr");
            argparse.AddArgument("-p", "--port")
                .WithDefault(4444);

            dynamic expando = argparse.ArgParse("127.0.0.1 -v --make-false".Split());

            string expected = "{\"ipaddr\":\"127.0.0.1\",\"port\":4444,\"verbose\":true,\"make_false\":false}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void Required()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-v", "--verbose")
                .WithParserAction(ParserAction.store_true);
            argparse.AddArgument("-p", "--port").WithRequired();
            argparse.AddArgument("-f", "--fast");

            dynamic expando = argparse.ArgParse("-f 4444 -v".Split());

            Assert.IsTrue(ArgumentParser.HasProperty(expando, "err_msg"));
        }

        [TestMethod]
        public void Nargs1()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f", "--files")
                .WithNargs();

            dynamic expando = argparse.ArgParse("-f file1 file2 file3".Split());


            string expected = "{\"files\":[\"file1\", \"file2\", \"file3\"]}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void Nargs2()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f", "--files")
                .WithNargs();
            argparse.AddArgument("-c");

            dynamic expando = argparse.ArgParse("-f file1 file2 file3 -c 5".Split());


            string expected = "{\"files\":[\"file1\", \"file2\", \"file3\"],\"c\":5}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void Nargs3()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-f", "--files")
                .WithNargs();
            argparse.AddArgument("-c");
            argparse.AddArgument("pos");

            dynamic expando = argparse.ArgParse("posArg -f file1 file2 file3 -c 5".Split());


            string expected = "{\"pos\":\"posArg\",\"files\":[\"file1\", \"file2\", \"file3\"],\"c\":5}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }

        [TestMethod]
        public void Nargs4()
        {
            ArgumentParser argparse = new ArgumentParser();
            argparse.AddArgument("-n", "--numbers")
                .WithNargs()
                .WithType(typeof(int));

            dynamic expando = argparse.ArgParse("-n 1 2 3 4 5".Split());


            string expected = "{\"numbers\":[1,2,3,4,5]}";
            JObject expectedJson = JObject.Parse(expected);

            string received = JsonConvert.SerializeObject(expando);
            JObject receivedJson = JObject.Parse(received);

            Assert.IsTrue(JToken.DeepEquals(expectedJson, receivedJson));
        }
    }
}

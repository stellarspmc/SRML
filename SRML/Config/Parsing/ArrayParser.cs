using System;
using System.Linq;

namespace SRML.Config.Parsing {
    public class ArrayParser : IStringParser {
        public Type ParsedType { get; }
        public ArrayParser(Type array) => ParsedType = array;

        public string EncodeObject(object obj) {
            var array = (Array)obj ?? Array.CreateInstance(ParsedType.GetElementType(), 0);
            var parser = ParserRegistry.GetParser(ParsedType.GetElementType());
            var str = "[";
            for(var i = 0; i < array.Length; i++) {
                str += parser.EncodeObject(array.GetValue(i));
                if (i != array.Length - 1) str += ", ";
            }
            str += ']';
            return str;
        }

        public string GetUsageString() => ParsedType.ToString();

        public object ParseObject(string str) {
            var parser = ParserRegistry.GetParser(ParsedType.GetElementType());
            str = str.Trim(' ', '[', ']');
            var objs = str.Split(',').Select(s => parser.ParseObject(s.Trim())).ToList();

            var array = Array.CreateInstance(ParsedType.GetElementType(), objs.Count);
            for(var i = 0; i < objs.Count; i++) array.SetValue(objs[i], i);
            return array;
        }
    }
}
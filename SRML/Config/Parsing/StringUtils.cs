namespace SRML.Config.Parsing
{
    static class StringUtils
    {
        internal static string ToQuotedString(string str)
        {
            if (str == null) str = "";
            var newStr = "";
            newStr += '\"';
            foreach (var ch in str)
            {
                if (ch == '\"') newStr += "\\\"";
                else newStr += ch;
            }
            newStr += '\"';
            return newStr;
        }

        internal static string FromQuotedString(string str)
        {
            if (str == null) str = "";
            var newStr = "";
            str = str.Trim(' ', '\"');
            var isescaped = false;
            foreach (var v in str)
            {
                if (!isescaped && v == '\\') isescaped = true;
                else if (v == '\\') { newStr += '\\'; isescaped = false; }
                else if (isescaped && v == '\"') { newStr += '\"'; isescaped = false; }
                else
                {
                    isescaped = false;
                    newStr += v;
                }
            }
            return newStr;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Converters
{
    class HTMLCharacterConverter
    {
        private static Dictionary<string, string> Convertions => new Dictionary<string, string>() {
            {"<", "&lt;" },
            { ">", "&gt;" },
            { ". ", ". &nbsp;" }
         };

        public static string Replace(string text)
        {
            foreach (var con in Convertions)
            {
                text = text.Replace(con.Key, con.Value);
            }
            return text;
        }
    }
}

using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Converters
{
    interface IConverter
    {
        public string Convert(DocumentObjectTree Dot, bool fullHtml = false) => string.Empty;
    }
}

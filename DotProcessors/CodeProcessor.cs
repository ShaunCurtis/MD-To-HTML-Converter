using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.DotProcessors
{
    class CodeProcessor : IDOTProcessor
    {
        public string Name => "Code Block Processor";

        public bool Process(IDOTNode node)
        {
            var ok = true;
            return ok;
        }
    }
}

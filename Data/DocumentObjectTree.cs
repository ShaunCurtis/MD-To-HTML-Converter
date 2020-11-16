using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Data
{
    public class DocumentObjectTree
    {
        public string FileName { get; set; }

        public IDOTNode RootNode { get; set; } = default;

    }
}

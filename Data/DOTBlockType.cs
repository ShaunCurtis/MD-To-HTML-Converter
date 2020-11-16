using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Data
{
    public enum DOTBlockType
    {
        None,
        CodeBlock,
        OrderedListBlock,
        UnOrderedListBlock,
        HeadingListBlock,
        QuoteBlock,
        TableBlock,
        TaskBlock
    }
}

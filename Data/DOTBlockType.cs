using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Data
{
    public enum DOTBlockType
    {
        None,
        CodeBlock,
        CodeLine,
        OrderedListBlock,
        UnOrderedListBlock,
        HeadingBlock,
        ListItemBlock,
        QuoteBlock,
        TableBlock,
        TaskBlock,
        TextBlock,
        ParagraphBlock,
        UnderlineBlock,
        BoldBlock,
        ItalicsBlock,
        LinkBlock,
        ImageBlock,
        VariableBlock,
    }
}

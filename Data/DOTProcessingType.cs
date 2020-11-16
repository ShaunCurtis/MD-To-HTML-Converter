using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Data
{
    public enum DOTProcessingType
    {
        None,
        Remove,
        CodeBlock,
        StartCodeBlock,
        EndCodeBlock,
        StartOrderedListBlock,
        OrderedListBlock,
        StartUnOrderedListBlock,
        UnOrderedListBlock,
        HeadingListBlock,
    }
}

using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace MD_To_HTML_Converter.Converters
{
    class ConverterRule
    {
        public DOTBlockType BlockType { get; set; } = DOTBlockType.None;

        public bool IsBlock { get; set; } = true;

        public bool IsLineBlock { get; set; } = false;

        public string Tag(string level = "1") => this.BlockType switch
        {
            DOTBlockType.BoldBlock => "strong",
            DOTBlockType.CodeBlock => "pre",
            DOTBlockType.HeadingBlock => $"h{level}",
            DOTBlockType.ImageBlock => "img",
            DOTBlockType.ItalicsBlock => "i",
            DOTBlockType.LinkBlock => "a",
            DOTBlockType.ListItemBlock => "li",
            DOTBlockType.OrderedListBlock => "ol",
            DOTBlockType.ParagraphBlock => "p",
            DOTBlockType.QuoteBlock => "blockquote",
            DOTBlockType.TextBlock => null,
            DOTBlockType.UnderlineBlock => "u",
            DOTBlockType.UnOrderedListBlock => "ul",
            DOTBlockType.VariableBlock => "code",
            _ => null
        };

        public delegate string NodeConverter(IDOTNode node);

        public NodeConverter ConverterMethod { get; set; }

    }
}

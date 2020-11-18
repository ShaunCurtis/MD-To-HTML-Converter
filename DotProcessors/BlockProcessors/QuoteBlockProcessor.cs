using MD_To_HTML_Converter.Data;
using System.Collections.Generic;

namespace MD_To_HTML_Converter.DotProcessors
{
    class QuoteBlockProcessor : BlockProcessor, IBlockProcessor
    {
        public QuoteBlockProcessor()
        {
            this.Name = $"Quote Block Pre-Processor";
            this.Expressions.Add(@"^\s{0,1}\>\s(.*)");
            this.LineExpressions.Add(@"^\s{0,1}\>\s(.*)");
            this.EndExpressions.Clear();
            this.BlockType = DOTBlockType.QuoteBlock;
            this.LineType = DOTBlockType.TextBlock;
            this.IsMultiline = true;
            this.TextGroup = 1;
            this.MinimumGroups = 1;
            this.LineProcessors = new SortedList<int, ILineProcessor>();
        }
    }
}

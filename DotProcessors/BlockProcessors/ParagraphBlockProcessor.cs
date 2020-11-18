using MD_To_HTML_Converter.Data;
using System.Text.RegularExpressions;

namespace MD_To_HTML_Converter.DotProcessors
{
    class ParagraphBlockProcessor : BlockProcessor, IBlockProcessor
    {
   
        public ParagraphBlockProcessor()
        {
            this.Name = $"Paragraph Block Pre-Processor";
            this.Expressions.Add(@"^(.+)$");
            this.BlockType = DOTBlockType.ParagraphBlock;
            this.TextGroup = 1;
            this.MinimumGroups = 2;
        }

        public override bool Process(IDOTNode rootnode, MatchCollection matches, ref int index)
        {
            return base.Process(rootnode, matches, ref index);
        }

    }
}

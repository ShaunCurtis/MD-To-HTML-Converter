using MD_To_HTML_Converter.Data;
using System.Text.RegularExpressions;

namespace MD_To_HTML_Converter.DotProcessors
{
    class EmptyBlockProcessor : BlockProcessor, IBlockProcessor
    {
   
        /// <summary>
        /// This Pre-Processor removes empty single lines and replaces multiple empty lines with an empty paragraph
        /// </summary>
        public EmptyBlockProcessor()
        {
            this.Name = $"Empty Block Pre-Processor";
            this.Expressions.Add(@"^\s*$()");
            this.LineExpressions.Add(@"^\s*$()");
            this.BlockType = DOTBlockType.ParagraphBlock;
            this.LineType = DOTBlockType.TextBlock;
            this.TextGroup = 1;
            this.MinimumGroups = 1;
        }

        public override bool Process(IDOTNode rootnode, MatchCollection matches, ref int index)
        {
            var ret = base.Process(rootnode, matches, ref index);
            var basenode = rootnode.Nodes[index];

            if (string.IsNullOrEmpty(basenode.Text) && basenode.Nodes.Count == 0) basenode.ProcessingType = DOTProcessingType.Remove;
            return ret;
        }
    }
}

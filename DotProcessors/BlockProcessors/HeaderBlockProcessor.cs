using MD_To_HTML_Converter.Data;
using System.Text.RegularExpressions;

namespace MD_To_HTML_Converter.DotProcessors
{
    class HeaderBlockProcessor : BlockProcessor, IBlockProcessor
    {
   
        public HeaderBlockProcessor()
        {
            this.Name = $"Header Block Pre-Processor";
            this.Expressions.Add(@"^([#]+)\s(.*)");
            this.BlockType = DOTBlockType.HeadingBlock;
            this.TextGroup = 2;
        }

        public override bool Process(IDOTNode rootnode, MatchCollection matches, ref int index)
        {
            var basenode = rootnode.Nodes[index];
            var match = matches[0];

            var ret =  base.Process(rootnode, matches, ref index);
            if (match.Groups.Count > 1 && !string.IsNullOrEmpty(match.Groups[1].Value)) basenode.SetValue("level", match.Groups[1].Value.Length);
            return ret;
        }

    }
}

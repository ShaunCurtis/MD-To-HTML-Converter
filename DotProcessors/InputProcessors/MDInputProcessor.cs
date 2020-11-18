using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MD_To_HTML_Converter.DotProcessors
{
    public class MDInputProcessor : IInputProcessor
    {
        /// <summary>
        /// List of PreProcessors to run
        /// Sorted list so we can order them correctly
        /// </summary>
        public SortedList<int, IBlockProcessor> PreProcessors =>
            new SortedList<int, IBlockProcessor>() {
                { 10, new CodeBlockProcessor() },
                { 20, new HeaderBlockProcessor() },
                { 30, new ListBlockProcessor() },
                { 40, new QuoteBlockProcessor() },
                { 50, new EmptyBlockProcessor() },
                { 60, new ParagraphBlockProcessor() },
            };

        /// <summary>
        /// Processes the Raw DocumentObject line by line
        /// </summary>
        /// <param name="Dot"></param>
        /// <returns></returns>
        public bool Process(DocumentObjectTree Dot)
        {
            if (Dot != null && Dot.RootNode != null)
            {
                var index = 1;

                while (index <= Dot.RootNode.Nodes.Count)
                {
                    var node = Dot.RootNode.Nodes[index];
                    // only process raw nodes
                    if (node.NodeType == DOTNodeType.Raw)
                    {
                        foreach (var proc in PreProcessors)
                        {
                            var ismatch = false;
                            MatchCollection matches = null;
                            foreach (var expression in proc.Value.Expressions)
                            {
                                var reg = new Regex(expression, RegexOptions.ECMAScript);
                                if (reg.IsMatch(node.Text))
                                {
                                    matches = reg.Matches(node.Text);
                                    ismatch = true;
                                    break;
                                    
                                }
                            }
                            if (ismatch)
                            {
                                proc.Value.Process(Dot.RootNode, matches, ref index);
                                break;
                            }
                        }
                    }
                    index++;
                }
            }
            this.CleanUp(Dot);
            return true;
        }

        public void CleanUp(DocumentObjectTree Dot)
        {
            var list = Dot.RootNode.Nodes.Where(item => item.Value.ProcessingType == DOTProcessingType.Remove).ToList();
            list.ForEach(item => Dot.RootNode.DeleteNode(item.Key));
        }

    }
}

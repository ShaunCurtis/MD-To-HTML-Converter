using MD_To_HTML_Converter.Data;
using MD_To_HTML_Converter.DotProcessors;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MD_To_HTML_Converter
{
    class Program
    {

        static  SortedList<int, IDOTPreProcessor> PreProcessors = new SortedList<int, IDOTPreProcessor>();

        static SortedList<int, IDOTProcessor> Processors = new SortedList<int, IDOTProcessor>();

        static DocumentObjectTree Dot;


        static void Main(string[] args)
        {
            Console.WriteLine("Markdown to HTML Coverter");

            //value = string.Empty;
            //Console.Write(string.Concat("Year to process? (default=", year, ")"));
            //value = Console.ReadLine();
            //year = string.IsNullOrEmpty(value) ? year : Convert.ToInt32(value);

            string fileName = @"C:\Users\Shaun.Obsidian\source\repos\MD-To-HTML-Converter\index.md";
            Dot = new DocumentObjectTree() { FileName=fileName };
            Dot.RootNode = new DOTNode() { NodeType = DOTNodeType.RootNode };
            
            PreProcessors.Add(0, new CodePreProcessor());
            PreProcessors.Add(10, new EmptyLinePreProcessor());
            PreProcessors.Add(20,new ListPreProcessor());
            PreProcessors.Add(30, new HeaderPreProcessor());
            PreProcessors.Add(40, new ParagraphPreProcessor());

            Processors.Add(0, new ExpressionProcessor() { BlockType= DOTBlockType.BoldBlock, Expression=@"(.*)[\*]{2}(.*)[\*]{2}(.*)" });
            Processors.Add(10, new ExpressionProcessor() { BlockType = DOTBlockType.ItalicsBlock, Expression = @"(.*)[\*]{1}(.*)[\*]{1}(.*)" });
            Processors.Add(20, new ExpressionProcessor() { BlockType = DOTBlockType.VariableBlock, Expression = @"(.*)[\`]{1}(.+)[\`]{1}(.*)" });
            var eproc = new ExpressionProcessor() { BlockType = DOTBlockType.LinkBlock, Expression = @"(.*)[^\!][\[]{1}(.+)[\]]{1}\s*[\(]{1}(.+)[\)]{1}(.*)" };
            eproc.Attributes.Add(3, "href");
            Processors.Add(30, eproc);
            eproc = new ExpressionProcessor() { BlockType = DOTBlockType.ImageBlock, Expression = @"(.*)[\!][\[]{1}(.+)[\]]{1}\s*[\(]{1}(.+)[\)]{1}(.*)" };
            eproc.Attributes.Add(3, "src");
            Processors.Add(40, eproc);

            Console.WriteLine("Reading File");
            var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var dot = new DOTNode() { NodeType= DOTNodeType.Raw, Text = line};
                Dot.RootNode.AddNode(dot);
            }
            Console.WriteLine("First Read Tree");
            OutputTree();
            foreach (var proc in PreProcessors)
            {
                Console.WriteLine($"Running {proc.Value.Name}");
                proc.Value.Process(Dot);
            }
            foreach (var node in Dot.RootNode.Nodes)
            {
                foreach (var proc in Processors)
                {
                    Console.WriteLine($"Running {proc.Value.Name}");
                    proc.Value.Process(Dot);
                }
            }
            Console.WriteLine("Final Tree");
            Dot.ToConsole();
            Console.WriteLine(Dot.AsHtml());
            // OutputTree();
        }

        static void OutputTree()
        {
            Console.WriteLine($"{Dot.FileName}");
            foreach (var node in Dot.RootNode.Nodes) OutputNode(node, node.Key.ToString(), ">");
        }

        static void OutputNode(KeyValuePair<int, IDOTNode> dotnode, string id, string label)
        {
            Console.WriteLine($"{id}{label}> - {dotnode.Value.NodeType} - {dotnode.Value.BlockType} - {dotnode.Value.Text} - {dotnode.Value.GetAttributeString()}");
            foreach (var node in dotnode.Value.Nodes)
            {
                var sid = $"{id}.{node.Key}"; 
                OutputNode(node, sid, $"--{label}");
            }
        }
    }
}

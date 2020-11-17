using MD_To_HTML_Converter.Data;
using MD_To_HTML_Converter.DotProcessors;
using MD_To_HTML_Converter.Converters;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MD_To_HTML_Converter
{
    class Program
    {
        static SortedList<int, IDOTPreProcessor> PreProcessors => new SortedList<int, IDOTPreProcessor>()
        {
            { 0, new CodePreProcessor()},
            { 10, new EmptyLinePreProcessor()},
            { 20,new ListPreProcessor() },
            { 30, new HeaderPreProcessor() },
            { 40, new ParagraphPreProcessor()}
        };

        static SortedList<int, IDOTProcessor> Processors = new SortedList<int, IDOTProcessor>()
        {
            { 0, new ExpressionProcessor() {
                BlockType = DOTBlockType.BoldBlock,
                Expression = @"(.*)[\*]{2}(.*)[\*]{2}(.*)"
            } },
            { 10, new ExpressionProcessor() { 
                BlockType = DOTBlockType.ItalicsBlock, 
                Expression = @"(.*)[\*]{1}(.*)[\*]{1}(.*)" 
            } },
            { 20, new ExpressionProcessor() { 
                BlockType = DOTBlockType.VariableBlock, 
                Expression = @"(.*)[\`]{1}(.+)[\`]{1}(.*)" 
            } },
            { 30, new ExpressionProcessor() { 
                BlockType = DOTBlockType.ImageBlock, 
                Expression = @"(.*)[\!][\[]{1}(.+)[\]]{1}\s*[\(]{1}(.+)[\)]{1}(.*)", 
                Attributes = new Dictionary<int, string>() { { 3, "src" } }
            } },
            { 40, new ExpressionProcessor() { 
                BlockType = DOTBlockType.LinkBlock, 
                Expression = @"(.*)[\[]{1}(.+)[\]]{1}\s*[\(]{1}(.+)[\)]{1}(.*)", 
                Attributes = new Dictionary<int, string>() { { 3, "href" } }
            } },
        };

        static DocumentObjectTree Dot;

        static List<IConverter> Converters = new List<IConverter>()
        {
            new HTMLConverter(),
        };

        static void Main(string[] args)
        {
            Console.WriteLine("Markdown to HTML Coverter");

            string workingdir = @"C:\Users\Shaun.Obsidian\source\repos\MD-To-HTML-Converter\MD";

            // Get the directory
            var value = string.Empty;
            Console.Write(string.Concat("Directory to process? (default=", workingdir, ")"));
            value = Console.ReadLine();
            workingdir = string.IsNullOrEmpty(value) ? workingdir : value;

            // Get the directory
            value = string.Empty;
            Console.Write(string.Concat("Show Debug Information? (default=", "N", ")"));
            value = Console.ReadLine();
            var debug = value.ToUpper() == "Y";

            // Loop through each MD files in the directory
            foreach (var file in Directory.GetFiles(workingdir, "*.md"))
            {
                Dot = new DocumentObjectTree() { FileName = file };
                Dot.RootNode = new DOTNode() { NodeType = DOTNodeType.RootNode };

                Console.WriteLine($"Reading File: {file}");
                var lines = File.ReadAllLines(file);
                foreach (var line in lines)
                {
                    var dot = new DOTNode() { NodeType = DOTNodeType.Raw, Text = line };
                    Dot.RootNode.AddNode(dot);
                }

                Console.WriteLine("Building Document Object Tree");
                if (debug) OutputTree();
                foreach (var proc in PreProcessors)
                {
                    if (debug) Console.WriteLine($"Running {proc.Value.Name}");
                    proc.Value.Process(Dot);
                }
                foreach (var node in Dot.RootNode.Nodes)
                {
                    foreach (var proc in Processors)
                    {
                        if (debug) Console.WriteLine($"Running {proc.Value.Name}");
                        proc.Value.Process(Dot);
                    }
                }
                if (debug) Dot.ToConsole();
                Console.WriteLine("Running Converters");
                foreach ( var converter in Converters)
                {
                    var outputfilename = file.Replace("md", "html");
                    var html = HTMLConverter.RunConvert(Dot, true);
                    Console.WriteLine($"Writing Output File: {outputfilename}");
                    File.WriteAllText(outputfilename, html);
                    if (debug) Console.WriteLine(html);
                }

            }
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

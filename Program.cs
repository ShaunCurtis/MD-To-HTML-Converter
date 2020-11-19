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

        static DocumentObjectTree Dot;

        static List<IConverter> Converters = new List<IConverter>()
        {
            new HTMLConverter(),
        };

        static IInputProcessor InputProcessor => new MDInputProcessor();

        static void Main(string[] args)
        {
            Console.WriteLine("Markdown to HTML Coverter");

            string workingdir = @"C:\Users\Shaun.Obsidian\source\repos\MD-To-HTML-Converter\MD";

            //// Get the directory
            //var value = string.Empty;
            //Console.Write(string.Concat("Directory to process? (default=", workingdir, ")"));
            //value = Console.ReadLine();
            //workingdir = string.IsNullOrEmpty(value) ? workingdir : value;

            //// Get the directory
            //value = string.Empty;
            //Console.Write(string.Concat("Show Debug Information? (default=", "Y", ")"));
            //value = Console.ReadLine();
            //var debug = value.ToUpper() == "Y";
            var debug = true;

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

                InputProcessor.Process(Dot);

                if (debug) Dot.ToConsole();

                Console.WriteLine("Running Converters");
                foreach (var converter in Converters)
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

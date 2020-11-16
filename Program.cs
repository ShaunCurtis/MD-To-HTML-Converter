using MD_To_HTML_Converter.Data;
using MD_To_HTML_Converter.DotProcessors;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MD_To_HTML_Converter
{
    class Program
    {

        static  SortedList<int, IDOTPreProcessor> PreProcessors = new SortedList<int, IDOTPreProcessor>();

        static List<IDOTProcessor> CodeProcessors = new List<IDOTProcessor>();

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
            Dot.RootNode = new DOTNode() { DOTType = DOTNodeType.RootNode };
            
            PreProcessors.Add(0, new CodePreProcessor());
            PreProcessors.Add(10, new EmptyLinePreProcessor());
            PreProcessors.Add(20,new ListPreProcessor());
            PreProcessors.Add(30, new HeaderPreProcessor());
            PreProcessors.Add(40, new ParagraphPreProcessor());

            Console.WriteLine("Reading File");
            var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var dot = new DOTNode() { DOTType= DOTNodeType.Raw, Text = line, Name="READ-LINE" };
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
                foreach (var proc in CodeProcessors)
                {
                    Console.WriteLine($"Running {proc.Name}");
                    proc.Process(node.Value);
                }
            }
            Console.WriteLine("Final Tree");
            OutputTree();
        }

        static void OutputTree()
        {
            Console.WriteLine($"{Dot.FileName}");
            foreach (var node in Dot.RootNode.Nodes) OutputNode(node, node.Key.ToString(), ">");
        }

        static void OutputNode(KeyValuePair<int, IDOTNode> dotnode, string id, string label)
        {
            Console.WriteLine($"{id}> - {dotnode.Value.DOTType} - {dotnode.Value.Name} - {dotnode.Value.Text}");
            foreach (var node in dotnode.Value.Nodes)
            {
                var sid = $"{id}.{node.Key}"; 
                //Console.WriteLine($"{sid}{label} - {node.Value.DOTType} - {node.Value.Name} - {node.Value.Text}");
                OutputNode(node, sid, $"---{label}");
            }
        }
    }
}

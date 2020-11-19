using MD_To_HTML_Converter.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MD_To_HTML_Converter.Converters
{
    class HTMLConverter : IConverter
    {

        public static string RunConvert(DocumentObjectTree Dot, bool fullHtml = false)
        {
            var converter = new HTMLConverter();
            return converter.Convert(Dot, fullHtml);
        }

        protected DocumentObjectTree Dot { get; set; }

        protected bool FullHtml { get; set; } = false;

        private List<ConverterRule> Rules
        {
            get
            {
                if (_Rules == null)
                {
                    _Rules = new List<ConverterRule>();
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.BoldBlock, IsBlock = false, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.CodeBlock, IsBlock = true, IsLineBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.CodeLine, IsBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.HeadingBlock, IsBlock = true, ConverterMethod = AsHtmlHeader });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.ImageBlock, IsBlock = false, ConverterMethod = AsHtmlImageBlock });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.ItalicsBlock, IsBlock = false, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.LinkBlock, IsBlock = false, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.ListItemBlock, IsBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.OrderedListBlock, IsBlock = true, IsLineBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.ParagraphBlock, IsBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.QuoteBlock, IsBlock = true, IsLineBlock = true, ConverterMethod = AsHtml }); ;
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.TableBlock, IsBlock = false, IsLineBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.TaskBlock, IsBlock = false, IsLineBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.TextBlock, IsBlock = false, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.UnderlineBlock, IsBlock = false, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.UnOrderedListBlock, IsBlock = true, IsLineBlock = true, ConverterMethod = AsHtml });
                    _Rules.Add(new ConverterRule() { BlockType = DOTBlockType.VariableBlock, IsBlock = false, ConverterMethod = AsHtml });
                }
                return _Rules;
            }
        }

        private List<ConverterRule> _Rules { get; set; }

        public string Convert(DocumentObjectTree dot, bool fullHtml = false)
        {
            var html = string.Empty;
            this.Dot = dot;
            this.FullHtml = fullHtml;

            this.IndexTree(dot.RootNode);

            if (Dot.RootNode != null)
            {
                html = this.AddSubNodes(Dot.RootNode);
            }
            if (fullHtml) return HtmlWrapper(html);
            else return html.ToString();
        }

        private void IndexTree(IDOTNode parentnode)
        {
            foreach (var node in parentnode.Nodes)
            {
                node.Value.ParentNode = parentnode;
                if (node.Value.Nodes.Count > 0) this.IndexTree(node.Value);
            }
        }

        private string AsHtml(IDOTNode node)
        {
            var html = new StringBuilder();
            var rule = Rules.FirstOrDefault(item => item.BlockType == node.BlockType);
            if (!string.IsNullOrEmpty(node.Text)) html.Append(HTMLCharacterConverter.Replace(node.Text));
            if (node.BlockType == DOTBlockType.CodeLine) html.AppendLine(AddSubNodes(node));
            else html.Append(AddSubNodes(node));
            return AddTagWrapper(rule, node.GetAttributeString(), html.ToString());
        }

        private string AsHtmlTest(IDOTNode node)
        {
            var html = new StringBuilder();
            var rule = Rules.FirstOrDefault(item => item.BlockType == node.BlockType);
            if (!string.IsNullOrEmpty(node.Text)) html.Append(HTMLCharacterConverter.Replace(node.Text));
            html.Append(AddSubNodes(node));
            return AddTagWrapper(rule, node.GetAttributeString(), html.ToString());
        }


        private string AsHtmlHeader(IDOTNode node)
        {
            var html = new StringBuilder();
            var rule = Rules.FirstOrDefault(item => item.BlockType == node.BlockType);
            if (!string.IsNullOrEmpty(node.Text)) html.Append(HTMLCharacterConverter.Replace(node.Text));
            html.Append(AddSubNodes(node));
            return AddTagWrapper(rule, node.GetAttributeString(), html.ToString(), $"{node.GetValue("level")}");
        }

        private string AsHtmlImageBlock(IDOTNode node)
        {
            var html = new StringBuilder();
            var rule = Rules.FirstOrDefault(item => item.BlockType == node.BlockType);
            if (!string.IsNullOrEmpty(node.Text)) node.SetAttribute("alt", node.Text);
            html.Append(AddSubNodes(node));
            return AddTagWrapper(rule, node.GetAttributeString(), html.ToString());
        }

        private string AsHtmlCodeLine(IDOTNode node)
        {
            var html = new StringBuilder();
            var rule = Rules.FirstOrDefault(item => item.BlockType == node.BlockType);
            if (!string.IsNullOrEmpty(node.Text)) html.Append(HTMLCharacterConverter.Replace(node.Text));
            html.Append(AddSubNodes(node));
            return AddTagWrapper(rule, node.GetAttributeString(), html.ToString());
        }

        private string AddSubNodes(IDOTNode node)
        {
            var html = new StringBuilder();
            if (node.Nodes.Count > 0)
            {
                foreach (var n in node.Nodes)
                {
                    var rule = Rules.FirstOrDefault(item => item.BlockType == n.Value.BlockType);
                    if (rule != null)
                    {
                        if (rule.IsLineBlock) html.AppendLine(rule.ConverterMethod(n.Value));
                        else html.Append(rule.ConverterMethod(n.Value));
                    }
                }
            }
            return html.ToString();
        }

        private string AddTagWrapper(ConverterRule rule, string attributes, string body, string level = "1")
        {
            if (rule.Tag(level) == null) return body;
            else
            {
                var html = new StringBuilder();
                var tag = $"<{$"{rule.Tag(level)} {attributes}".Trim()}>";
                html.Append(tag);
                if (rule.IsBlock) html.Append(Environment.NewLine);
                html.Append(body);
                tag = $"</{rule.Tag(level)}>";
                html.Append(tag);
                if (rule.IsBlock) html.Append(Environment.NewLine);
                return html.ToString();
            }
        }

        private string HtmlWrapper(string body, string head = "")
        {
            var html = new StringBuilder();
            html.AppendLine($"<!DOCTYPE html>");
            html.AppendLine($"<html>");
            html.AppendLine($"<link rel = \"stylesheet\" type = \"text/css\" href = \"https://codeproject.freetls.fastly.net/App_Themes/CodeProject/Css/Main.css?dt=2.8.20201113.1\" />");
            html.AppendLine($"<head>");
            html.AppendLine(head);
            html.AppendLine($"</head>");
            html.AppendLine($"<body>");
            html.AppendLine(body);
            html.AppendLine($"</body>");
            html.AppendLine($"</html>");

            return html.ToString();
        }
    }
}

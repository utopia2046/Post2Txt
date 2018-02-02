using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Post2Txt
{
    internal class PostExtractor
    {
        // .ctor
        public PostExtractor(string postUrlQuery, string postNodeQuery)
        {
            _postUrlQuery = postUrlQuery;
            _postNodeQuery = postNodeQuery;
        }

        // private
        private readonly string _postUrlQuery;
        private readonly string _postNodeQuery;

        // const
        private static readonly string href = "href";

        // Public Methods
        public void ExtractTextFromHtml(string htmlFile, string txtFile, Encoding encHtml, Encoding encText)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(htmlFile, encHtml);

            using (var sw = new StreamWriter(txtFile, false, encText))
            {
                // Find Post Url and output it in txt file
                try
                {
                    var urlQuery = htmlDoc.DocumentNode.SelectNodes(_postUrlQuery);
                    if (urlQuery != null && urlQuery.Any())
                    {
                        var urlNode = urlQuery.FirstOrDefault();
                        if (urlNode.Attributes.Contains(href))
                        {
                            string url = urlNode.Attributes[href].Value;
                            Trace.TraceInformation("Post Url: {0}", url);
                            sw.WriteLine(url);
                            sw.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception when finding post url. Detail: {0}", ex.Message);
                }

                // Find posts and convert each node content to pure text
                try
                {
                    var nodes = htmlDoc.DocumentNode.SelectNodes(_postNodeQuery);
                    if (nodes != null && nodes.Any())
                    {
                        foreach (HtmlNode node in nodes)
                        {
                            string id = node.Attributes["id"].Value;
                            Trace.TraceInformation("Extracting post id = {0}", id);
                            string html = node.InnerHtml;
                            string text = ConvertHtml(html);
                            string[] lines = RemoveRedundantSpaces(text);
                            WriteLines(sw, lines);
                            sw.WriteLine();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Exception when finding posts. Detail: {0}", ex.Message);
                }
            }
        }

        // Private Methods, copied from HtmlAgilityPack sample
        private string ConvertHtml(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            StringWriter sw = new StringWriter();
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
        }

        private string[] RemoveRedundantSpaces(string text)
        {
            var lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var output = new List<string>();
            foreach (string line in lines)
            {
                line.Trim();
                if (!String.IsNullOrEmpty(line))
                {
                    output.Add(line);
                }
            }

            return lines.ToArray();
        }

        private void WriteLines(StreamWriter writer, string[] lines)
        {
            foreach (string line in lines)
            {
                writer.WriteLine(line);
            }
        }

        private void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                ConvertTo(subnode, outText);
            }
        }

        private void ConvertTo(HtmlNode node, TextWriter outText)
        {
            string html, text;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    //if (html.Trim().Length > 0)

                    text = Regex.Replace(HtmlEntity.DeEntitize(html), @"\p{Z}", "");

                    outText.Write(text);
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "p":
                        case "br":
                            // treat paragraphs as crlf
                            outText.Write("\r\n");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }
                    break;
            }
        }
    }
}

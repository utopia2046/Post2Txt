﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Post2Txt.Properties;

namespace Post2Txt
{
    class Program
    {
        private const string DefaultHtmlExt = @"*.html|*.htm|*.HTML|*.HTM";
        private const string DefaultTextExt = @".txt";
        private const string DefaultHtmlEncoding = @"GB18030";
        private const string DefaultTextEncoding = @"GB18030";
        private const string DefaultPostNodeQuery = @"//td[@class='t_f']";
        private const string DefaultUrlNodeQuery = @"//a[contains(@onclick,'return copyThreadUrl')]";
        private const string DefaultRemoveFromFileName = @" - 热门同人区 -  随缘居 -  Powered by Discuz!";

        static void Main(string[] args)
        {
            ShowHelp();
            string sourceFile, targetFile;
            string currentFolder = Environment.CurrentDirectory;

            Encoding encHtml = GetEncoding(Settings.Default.HtmlEncoding ?? DefaultHtmlEncoding);
            Encoding encTxt = GetEncoding(Settings.Default.TextEncoding ?? DefaultTextEncoding);
            if ((encHtml == null) || (encTxt == null))
            {
                Trace.TraceError("Error when reading encoding from config file.");
                return;
            }
            Trace.TraceInformation("Input html file encoding = {0}", encHtml.EncodingName);
            Trace.TraceInformation("Output text file encoding = {0}", encTxt.EncodingName);

            string urlNodeQuery = Settings.Default.UrlNodeQuery ?? DefaultUrlNodeQuery;
            string postNodeQuery = Settings.Default.PostNodeQuery ?? DefaultPostNodeQuery;
            Trace.TraceInformation("Url node query = {0}", urlNodeQuery);
            Trace.TraceInformation("Post node query = {0}", postNodeQuery);
            var extractor = new PostExtractor(urlNodeQuery, postNodeQuery);

            if (args.Length > 0)
            {
                sourceFile = args[0];
                if (!File.Exists(sourceFile))
                {
                    Trace.TraceError("Source file doesn't exist: {0}", sourceFile);
                    return;
                }
                Trace.TraceInformation("Source file: {0}", sourceFile);

                if (args.Length > 1)
                {
                    targetFile = args[1];
                }
                else
                {
                    targetFile = GetOutputFilename(sourceFile);
                }
                Trace.TraceInformation("Target file: {0}", targetFile);
                extractor.ExtractTextFromHtml(sourceFile, targetFile, encHtml, encTxt);
            }
            else
            {
                string filter = Settings.Default.HtmlFileFilter ?? DefaultHtmlExt;
                Trace.TraceInformation("Looking for {0} files under current folder: {1}", filter, currentFolder);
                string[] htmlFiles = filter.Split('|').SelectMany(ext => 
                    Directory.GetFiles(currentFolder, ext, SearchOption.TopDirectoryOnly)).Distinct().ToArray();
                if (htmlFiles.Length < 1)
                {
                    Trace.TraceInformation("No files found under current folder with specifed filter {0}.", filter);
                    return;
                }
                Trace.TraceInformation("{0} .html files found", htmlFiles.Length);
                foreach (var file in htmlFiles)
                {
                    targetFile = GetOutputFilename(file);
                    Trace.TraceInformation("Extracting text file from html post. \n  Source file: {0}, \n  Target file: {1}", file, targetFile);
                    extractor.ExtractTextFromHtml(file, targetFile, encHtml, encTxt);
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: Post2Txt.exe [Source.html [Target.txt]]");
            Console.WriteLine("  1. If source file name is not provided, it will try to ");
            Console.WriteLine("     process all html files under current folder.");
            Console.WriteLine("  2. If target file name is not provided, by default it will");
            Console.WriteLine("     be same as source file.");
            Console.WriteLine();
        }

        private static string GetOutputFilename(string sourceFile)
        {
            string removeString = Settings.Default.RemoveFromFileName ?? DefaultRemoveFromFileName;
            return Path.GetFileNameWithoutExtension(sourceFile).Replace(removeString, String.Empty) + (Settings.Default.TextFileExt ?? DefaultTextExt);
        }

        private static Encoding GetEncoding(string encodingName)
        {
            Trace.TraceInformation("EncodingName = {0}", encodingName);
            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch (System.ArgumentException ex)
            {
                Trace.TraceError("Invalid encoding {0}", encodingName);
                return null;
            }
        }
    }
}

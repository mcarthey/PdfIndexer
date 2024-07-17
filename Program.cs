using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var pdfExtractor = new PdfExtractor();
        var esService = new ElasticSearchService("http://localhost:9200");

        var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");
        var pdfFiles = pdfExtractor.ExtractTextFromDirectory(directoryPath);

        int id = 1;
        foreach (var (fileName, text) in pdfFiles)
        {
            var filePath = Path.Combine(directoryPath, fileName);
            var lastModified = File.GetLastWriteTime(filePath);

            var existingDoc = esService.GetDocumentByFileName(fileName);

            if (existingDoc != null)
            {
                if (existingDoc.LastModified < lastModified)
                {
                    esService.IndexDocument(id++, fileName, text, lastModified);
                    Console.WriteLine($"Re-indexed updated document: {fileName}");
                }
                else
                {
                    Console.WriteLine($"Skipped: {fileName}, no changes detected.");
                }
            }
            else
            {
                esService.IndexDocument(id++, fileName, text, lastModified);
                Console.WriteLine($"Indexed document: {fileName}");
            }
        }

        Console.WriteLine("All PDF files processed successfully!");

        while (true)
        {
            // Perform a search
            Console.Write("Enter search query (or type 'exit' to quit): ");
            var query = Console.ReadLine()?.ToLowerInvariant();
            if (query == "exit")
                break;

            var searchResults = esService.SearchDocuments(query);

            Console.WriteLine("Search Results:");
            if (searchResults != null && searchResults.Hits.Any())
            {
                foreach (var hit in searchResults.Hits)
                {
                    var lines = hit.Source.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var matchingLines = lines.Where(line => line.ToLowerInvariant().Contains(query)).ToList();

                    if (matchingLines.Any())
                    {
                        Console.WriteLine($"FileName: {hit.Source.FileName}");
                        foreach (var line in matchingLines)
                        {
                            Console.WriteLine($"    Text: {line}");
                        }
                        Console.WriteLine();
                    }
                }
            }
            else
            {
                Console.WriteLine("No search results found.");
            }
        }
    }
}

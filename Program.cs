using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Get the API key from configuration
        var apiKey = configuration["OpenAI:ApiKey"];

        // Initialize services
        var pdfExtractor = new PdfExtractor();
        var esService = new ElasticSearchService("http://localhost:9200");
        var nlpService = new NlpQueryService(apiKey);

        var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Files");
        var pdfFiles = pdfExtractor.ExtractTextFromDirectory(directoryPath);

        int id = 1;
        foreach (var (fileName, text) in pdfFiles)
        {
            var filePath = Path.Combine(directoryPath, fileName);
            var lastModified = File.GetLastWriteTime(filePath);
            var textFilePath = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(fileName) + ".txt");

            var existingDoc = esService.GetDocumentByFileName(fileName);

            if (existingDoc != null)
            {
                if (existingDoc.LastModified < lastModified)
                {
                    esService.IndexDocument(id++, fileName, text, lastModified);
                    File.WriteAllText(textFilePath, text);
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
                File.WriteAllText(textFilePath, text);
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

            var documentContexts = new List<string>();
            if (searchResults != null && searchResults.Hits.Any())
            {
                foreach (var hit in searchResults.Hits)
                {
                    var lines = hit.Source.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var matchingLines = lines.Where(line => line.ToLowerInvariant().Contains(query)).ToList();

                    if (matchingLines.Any())
                    {
                        documentContexts.Add($"FileName: {hit.Source.FileName}\n{string.Join("\n", matchingLines)}");
                    }
                }
            }

            if (!documentContexts.Any())
            {
                Console.WriteLine("No search results found.");
                continue;
            }

            var combinedContext = string.Join("\n\n", documentContexts);
            var prompt = $"Based on the following documents, answer the question:\n\n{combinedContext}\n\nQuestion: {query}";

            // NLP Query
            var nlpResponse = await nlpService.QueryModelAsync(prompt);
            Console.WriteLine("NLP Response:");
            Console.WriteLine(nlpResponse);
        }
    }
}

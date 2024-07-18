# PDF Indexer and NLP Query Application

This application indexes text extracted from PDF documents using ElasticSearch and allows users to query the indexed content using natural language processing (NLP) with OpenAI's GPT-3.

## Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download)
- [ElasticSearch](https://www.elastic.co/downloads/elasticsearch)
- [OpenAI API Key](https://platform.openai.com/signup)

## Setup

### 1. Clone the Repository

```bash
git clone https://github.com/your-repo/pdf-indexer-nlp.git
cd pdf-indexer-nlp
```

### 2. Install Dependencies
Ensure you have the following NuGet packages installed:

- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.DependencyInjection`
- `Newtonsoft.Json`

You can install these packages using the .NET CLI:

```bash
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Newtonsoft.Json
```

### 3. Configure appsettings.json
Create a file named appsettings.json in the root of the project with the following content:

```json
{
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY"
  }
}
```

Replace `YOUR_OPENAI_API_KEY` with your actual OpenAI API key.

### 4. 4. Update Program.cs
Ensure your Program.cs reads the API key from appsettings.json and initializes the services correctly.

Program.cs:
```csharp
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
```

### 5. Run the Application

```bash
dotnet run
```

### Usage
1. Place your PDF files in the Files directory.
2. Run the application.
3. Enter search queries to find relevant content in your indexed PDFs.
4. The application will use OpenAI's GPT-4 to respond to queries based on the content of your documents.

### License  

This project is licensed under the MIT License.

### Summary

The `README.md` file provides detailed instructions for setting up and running the project, including how to configure the OpenAI API key in `appsettings.json`. This approach ensures that sensitive information is securely managed and your application is set up correctly. If you need further assistance or have additional questions, feel free to ask!

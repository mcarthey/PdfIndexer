using System;
using Nest;

public class ElasticSearchService
{
    private readonly ElasticClient _client;

    public ElasticSearchService(string uri)
    {
        var settings = new ConnectionSettings(new Uri(uri))
            .DefaultIndex("documents");

        _client = new ElasticClient(settings);

        // Define mapping
        if (!_client.Indices.Exists("documents").Exists)
        {
            var createIndexResponse = _client.Indices.Create("documents", c => c
                .Map<Document>(m => m
                    .Properties(p => p
                        .Text(t => t
                            .Name(n => n.Content)
                            .Analyzer("standard") // Ensure full-text search is enabled
                        )
                        .Text(t => t
                            .Name(n => n.FileName)
                        )
                        .Date(d => d
                            .Name(n => n.LastModified)
                        )
                    )
                )
            );

            if (!createIndexResponse.IsValid)
            {
                Console.WriteLine($"Failed to create index: {createIndexResponse.ServerError}");
            }
        }
    }

    public void IndexDocument(int id, string fileName, string content, DateTime lastModified)
    {
        var document = new Document { Id = id, FileName = fileName, Content = content, LastModified = lastModified };
        var response = _client.IndexDocument(document);
        if (response.IsValid)
        {
            Console.WriteLine($"Indexed document: {fileName}");
        }
        else
        {
            Console.WriteLine($"Failed to index document {fileName}: {response.ServerError}");
        }
    }

    public Document GetDocumentByFileName(string fileName)
    {
        var searchResponse = _client.Search<Document>(s => s
            .Query(q => q
                .Match(m => m
                    .Field(f => f.FileName)
                    .Query(fileName)
                )
            )
        );

        return searchResponse.Hits.FirstOrDefault()?.Source;
    }

    public ISearchResponse<Document> SearchDocuments(string query)
    {
        var searchResponse = _client.Search<Document>(s => s
            .Query(q => q
                .Match(m => m
                    .Field(f => f.Content)
                    .Query(query)
                    .Operator(Operator.And)
                    .Fuzziness(Fuzziness.Auto)
                )
            )
        );
        return searchResponse;
    }
}

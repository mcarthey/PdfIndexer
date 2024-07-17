using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class IndexingStatus
{
    public string FileName { get; set; }
    public DateTime LastModified { get; set; }

    private static string StatusFilePath => Path.Combine(Directory.GetCurrentDirectory(), "indexing_status.json");

    public static List<IndexingStatus> LoadStatus()
    {
        if (!File.Exists(StatusFilePath))
        {
            return new List<IndexingStatus>();
        }

        var json = File.ReadAllText(StatusFilePath);
        return JsonConvert.DeserializeObject<List<IndexingStatus>>(json);
    }

    public static void SaveStatus(List<IndexingStatus> statusList)
    {
        var json = JsonConvert.SerializeObject(statusList, Formatting.Indented);
        File.WriteAllText(StatusFilePath, json);
    }
}

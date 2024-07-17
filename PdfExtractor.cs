using System;
using System.IO;
using System.Collections.Generic;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

public class PdfExtractor
{
    public IEnumerable<(string FileName, string Text)> ExtractTextFromDirectory(string directoryPath)
    {
        var pdfFiles = Directory.GetFiles(directoryPath, "*.pdf");
        foreach (var pdfFile in pdfFiles)
        {
            yield return (Path.GetFileName(pdfFile), ExtractText(pdfFile));
        }
    }

    private string ExtractText(string pdfPath)
    {
        var text = string.Empty;
        using (var document = PdfDocument.Open(pdfPath))
        {
            foreach (var page in document.GetPages())
            {
                text += ContentOrderTextExtractor.GetText(page);
            }
        }
        return text;
    }
}

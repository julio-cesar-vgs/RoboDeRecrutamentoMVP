using System.Text;
using DocumentFormat.OpenXml.Packaging;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace RecrutamentoRobo.Infrastructure.TextExtraction;

public static class DocumentReader
{
    public static string ReadPdf(string filePath)
    {
        var text = new StringBuilder();
        using (var reader = new PdfReader(filePath))
        {
            for (var i = 1; i <= reader.NumberOfPages; i++) text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
        }

        return text.ToString();
    }

    public static string ReadDocx(string filePath)
    {
        using (var doc = WordprocessingDocument.Open(filePath, false))
        {
            return doc.MainDocumentPart.Document.Body.InnerText;
        }
    }
}
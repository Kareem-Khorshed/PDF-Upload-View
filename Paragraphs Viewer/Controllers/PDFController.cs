// Mindmap PDF API with central title and paragraph headings
using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PDFController
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        [HttpPost("upload-pdf")]
        public IActionResult UploadPdf(IFormFile file)
        {
            var paragraphs = ProcessPdfToParagraphs(file);
            return Ok(paragraphs);
        }

        [HttpPost("generate-mindmap")]
        public IActionResult GenerateMindmap(IFormFile file)
        {
            var paragraphs = ProcessPdfToParagraphs(file);

            var root = new MindmapNode
            {
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                Children = paragraphs.Select(p => new MindmapNode
                {
                    Title = p.Title,
                    Children = new List<MindmapNode>
                    {
                        new MindmapNode { Title = ExtractFirstSentence(p.Content.ToString()) }
                    }
                }).ToList()
            };

            return Ok(root);
        }

        private List<ParagraphNode> ProcessPdfToParagraphs(IFormFile file)
        {
            var paragraphs = new List<ParagraphNode>();
            int paragraphCounter = 1;

            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                using var reader = new PdfReader(stream);
                using var pdfDocument = new PdfDocument(reader);

                int numberOfPages = pdfDocument.GetNumberOfPages();

                for (int page = 1; page <= numberOfPages; page++)
                {
                    var strategy = new LocationTextExtractionStrategy();
                    var text = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);

                    var sentences = SplitIntoSentences(text);
                    var group = new List<string>();

                    foreach (var sentence in sentences)
                    {
                        var clean = CleanText(sentence.Trim());
                        if (!string.IsNullOrWhiteSpace(clean))
                        {
                            group.Add(clean);

                            if (group.Count == 7)
                            {
                                paragraphs.Add(new ParagraphNode
                                {
                                    Title = GetParagraphHeading(paragraphCounter, group),
                                    Content = string.Join(" ", group)
                                });
                                paragraphCounter++;
                                group.Clear();
                            }
                        }
                    }

                    if (group.Count > 0)
                    {
                        paragraphs.Add(new ParagraphNode
                        {
                            Title = GetParagraphHeading(paragraphCounter, group),
                            Content = string.Join(" ", group)
                        });
                        paragraphCounter++;
                    }
                }
            }

            return paragraphs;
        }

        private string GetParagraphHeading(int number, List<string> group)
        {
            var heading = group.FirstOrDefault();
            return !string.IsNullOrWhiteSpace(heading) ? $"Paragraph {number}: {heading}" : $"Paragraph {number}";
        }

        private string ExtractFirstSentence(string text)
        {
            var match = Regex.Match(text, @"[^.!؟?!]+[.!؟?!]");
            return match.Success ? match.Value.Trim() : text;
        }

        private List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();
            var matches = Regex.Matches(text, @"[^.!؟?!]+[.!؟?!]");
            foreach (Match match in matches)
            {
                sentences.Add(match.Value.Trim());
            }

            return sentences;
        }

        private string CleanText(string text)
        {
            return Regex.Replace(text, @"www\\.alkottob\\.com", string.Empty);
        }
    }

    public class ParagraphNode
    {
        public string Title { get; set; }
        public object Content { get; set; }
    }

    public class MindmapNode
    {
        public string Title { get; set; }
        public List<MindmapNode> Children { get; set; } = new();
    }
}

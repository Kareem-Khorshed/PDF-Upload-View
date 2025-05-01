using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PDFController
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        [HttpPost("upload-pdf")]
        public IActionResult UploadPdf(IFormFile file)
        {
            var nestedParagraphs = new List<ParagraphNode>();

            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                using var reader = new PdfReader(stream);
                using var pdfDocument = new PdfDocument(reader);

                int numberOfPages = pdfDocument.GetNumberOfPages();

                ParagraphNode currentMain = null;
                ParagraphNode currentSub = null;

                for (int page = 1; page <= numberOfPages; page++)
                {
                    var strategy = new LocationTextExtractionStrategy();
                    var text = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(page), strategy);
                    var paragraphList = text.Split(new[] { "\n" }, StringSplitOptions.None);

                    foreach (var raw in paragraphList)
                    {
                        if (string.IsNullOrWhiteSpace(raw)) continue;
                        var paragraph = CleanText(raw.Trim());

                        if (IsMainTitle(paragraph))
                        {
                            currentMain = new ParagraphNode
                            {
                                Title = paragraph,
                                Content = new List<ParagraphNode>()
                            };
                            nestedParagraphs.Add(currentMain);
                            currentSub = null;
                        }
                        else if (IsSubTitle(paragraph))
                        {
                            currentSub = new ParagraphNode
                            {
                                Title = paragraph,
                                Content = new List<ParagraphNode>()
                            };
                            if (currentMain != null)
                                ((List<ParagraphNode>)currentMain.Content).Add(currentSub);
                        }
                        else
                        {
                            var contentNode = new ParagraphNode
                            {
                                Title = null,
                                Content = paragraph
                            };

                            if (currentSub != null)
                                ((List<ParagraphNode>)currentSub.Content).Add(contentNode);
                            else if (currentMain != null)
                                ((List<ParagraphNode>)currentMain.Content).Add(contentNode);
                            else
                                nestedParagraphs.Add(new ParagraphNode
                                {
                                    Title = "Uncategorized",
                                    Content = paragraph
                                });
                        }
                    }
                }
            }

            return Ok(nestedParagraphs);
        }

        private string CleanText(string text)
        {
            return Regex.Replace(text, @"www\.alkottob\.com", string.Empty);
        }

        private bool IsMainTitle(string text)
        {
            return Regex.IsMatch(text, @"(?i)^(introduction|chapter|overview|section \d+)");
        }

        private bool IsSubTitle(string text)
        {
            return Regex.IsMatch(text, @"(?i)(definition|types|applications|advantages|disadvantages)");
        }
    }

    public class ParagraphNode
    {
        public string Title { get; set; }
        public object Content { get; set; } // ممكن تكون string أو List<ParagraphNode>
    }
}

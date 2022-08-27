using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.AcroForms.Fields;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.XObjects;

namespace CIAPI.Helpers
{
    public class PDFHelper
    {
        public static List<string> GetAllWords(string physicalPath)
        {
            var texto = "";
            var vtexto = new List<string>();
            using (PdfDocument document = PdfDocument.Open(physicalPath))
            {
                foreach (Page page in document.GetPages())
                {
                    string pageText = page.Text;


                    foreach (Word word in page.GetWords())
                    {
                        texto += word.Text + ";";
                        vtexto.Add(word.Text);
                        //Console.WriteLine(word.Text);
                    }



                    /*
                    //Extracción  de Imagenes
                    byte[] myArryByte = b.ToArray();
                    var images = page.GetImages();
                    foreach (var image in page.GetImages())
                    {
                        if (!image.TryGetBytes(out var b))
                        {
                            b = image.RawBytes;

                        }
                    }

                    


                    var physicalPath2 = "";
                    using (MemoryStream mStream = new MemoryStream(myArryByte))
                    {
                        i++;
                        Image img = Image.FromStream(mStream);
                        fileName = "img" + i.ToString().Trim() + ".jpg";

                        physicalPath2 = Path.Combine(WebHostEnvironment.WebRootPath, "upload", fileName);
                        img.Save(physicalPath2);
                    }
                    */
                }

            }
            return vtexto;
        }
        public static List<string> ExtractTextWithNewLines(string physicalPath)
        {
            var vtexto = new List<string>();
            using (var document = PdfDocument.Open(physicalPath))
                {
                    foreach (var page in document.GetPages())
                    {
                        var text = ContentOrderTextExtractor.GetText(page, true);
                        vtexto.Add(text);
                        //Console.WriteLine(text);
                    }
                }
            return vtexto;
            

        }

        public static string AdvancedExtract(string physicalPath)
        {
            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(physicalPath))
            {
                foreach (var page in document.GetPages())
                {
                    // 0. Preprocessing
                    var letters = page.Letters; // no preprocessing

                    // 1. Extract words
                    var wordExtractor = NearestNeighbourWordExtractor.Instance;
                    var wordExtractorOptions = new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions()
                    {
                        Filter = (pivot, candidate) =>
                        {
                            // check if white space (default implementation of 'Filter')
                            if (string.IsNullOrWhiteSpace(candidate.Value))
                            {
                                // pivot and candidate letters cannot belong to the same word 
                                // if candidate letter is null or white space.
                                // ('FilterPivot' already checks if the pivot is null or white space by default)
                                return false;
                            }

                            // check for height difference
                            var maxHeight = Math.Max(pivot.PointSize, candidate.PointSize);
                            var minHeight = Math.Min(pivot.PointSize, candidate.PointSize);
                            if (minHeight != 0 && maxHeight / minHeight > 2.0)
                            {
                                // pivot and candidate letters cannot belong to the same word 
                                // if one letter is more than twice the size of the other.
                                return false;
                            }

                            // check for colour difference
                            var pivotRgb = pivot.Color.ToRGBValues();
                            var candidateRgb = candidate.Color.ToRGBValues();
                            if (!pivotRgb.Equals(candidateRgb))
                            {
                                // pivot and candidate letters cannot belong to the same word 
                                // if they don't have the same colour.
                                return false;
                            }

                            return true;
                        }
                    };

                    var words = wordExtractor.GetWords(letters);

                    // 2. Segment page
                    var pageSegmenter = DocstrumBoundingBoxes.Instance;
                    var pageSegmenterOptions = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions()
                    {

                    };

                    var textBlocks = pageSegmenter.GetBlocks(words);

                    // 3. Postprocessing
                    var readingOrder = UnsupervisedReadingOrderDetector.Instance;
                    var orderedTextBlocks = readingOrder.Get(textBlocks);

                    // 4. Extract text
                    foreach (var block in orderedTextBlocks)
                    {
                        sb.Append(block.Text.Normalize(NormalizationForm.FormKC)); // normalise text
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    
        public static string GetFormData(string physicalPath)
        {
            var sb = new StringBuilder();
            
            using (var document = PdfDocument.Open(physicalPath))
            {
                if (!document.TryGetForm(out var form))
                {
                    
                    return "";
                }

                var page1Fields = form.GetFieldsForPage(1);

                foreach (var field in page1Fields)
                {
                    switch (field)
                    {
                        case AcroTextField text:
                            //Console.WriteLine($"Found text field on page 1 with text: {text.Value}.");
                            sb.Append($"Found text field on page 1 with text: {text.Value}."); // normalise text


                            break;
                        case AcroCheckboxesField cboxes:
                            //Console.WriteLine($"Found checkboxes field on page 1 with {cboxes.Children.Count} checkboxes.");
                            sb.Append($"Found checkboxes field on page 1 with {cboxes.Children.Count} checkboxes.");
                            break;
                        case AcroListBoxField listbox:
                            var opts = string.Join(", ", listbox.Options.Select(x => x.Name));
                            //Console.WriteLine($"Found listbox field on page 1 with options: {opts}.");
                            sb.Append($"Found listbox field on page 1 with options: {opts}.");
                            break;
                    }
                    sb.AppendLine();
                }
                return sb.ToString();
                
            }

        }
        public static void CrearPDFTest(string trueTypeFontPath, string jpgImagePath)
        {
            var builder = new PdfDocumentBuilder
            {
                ArchiveStandard = PdfAStandard.A2A
            };

            var font = builder.AddTrueTypeFont(File.ReadAllBytes(trueTypeFontPath));

            var page = builder.AddPage(PageSize.A4);
            var pageTop = new PdfPoint(0, page.PageSize.Top);

            var letters = page.AddText("This is some text added to the output file near the top of the page.",
                12,
                pageTop.Translate(20, -25),
                font);

            var bottomOfText = letters.Min(x => x.GlyphRectangle.Bottom);

            var imagePlacement = new PdfRectangle(new PdfPoint(50, bottomOfText - 200),
                new PdfPoint(150, bottomOfText));
            page.AddJpeg(File.ReadAllBytes(jpgImagePath), imagePlacement);

            var fileBytes = builder.Build();

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, "outputOfPdfA2A.pdf");
                File.WriteAllBytes(output, fileBytes);
                Console.WriteLine($"File output to: {output}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write output to file due to error: {ex}.");
            }
        }
        public static void Merge3(string filePath1, string filePath2, string filePath3, string filenameOutput)
        {
            var fileBytes = new[] { filePath1, filePath2, filePath3 }
                .Select(File.ReadAllBytes).ToList();

            var resultFileBytes = PdfMerger.Merge(fileBytes);

            try
            {
                var location = AppDomain.CurrentDomain.BaseDirectory;
                var output = Path.Combine(location, filenameOutput);
                File.WriteAllBytes(output, resultFileBytes);
                Console.WriteLine($"File output to: {output}");

                using (var doc = PdfDocument.Open(resultFileBytes))
                {
                    Console.WriteLine($"Generated document with {doc.NumberOfPages} pages.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write output to file due to error: {ex}.");
            }
        }
    }
}

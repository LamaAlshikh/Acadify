using System.Text;
using Acadify.Services.AcademicCalendar.Interfaces;
<<<<<<< HEAD
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;
=======
>>>>>>> origin_second/لما2

namespace Acadify.Services.AcademicCalendar
{
    public class PdfOcrService : IPdfOcrService
    {
<<<<<<< HEAD
        private readonly IWebHostEnvironment _env;

        public PdfOcrService(IWebHostEnvironment env)
        {
            _env = env;
=======
        private readonly OpenAiVisionClient _vision;

        public PdfOcrService(OpenAiVisionClient vision)
        {
            _vision = vision;
>>>>>>> origin_second/لما2
        }

        public async Task<string> ExtractTextByOcrAsync(string pdfPath)
        {
<<<<<<< HEAD
            var tessDataPath = Path.Combine(_env.WebRootPath, "tessdata");

            if (!Directory.Exists(tessDataPath))
                throw new DirectoryNotFoundException($"tessdata not found: {tessDataPath}");

            using var engine = new TesseractEngine(tessDataPath, "ara+eng", EngineMode.Default);

            var sb = new StringBuilder();

            using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(0, 0));
            var pageCount = docReader.GetPageCount();

            for (int i = 0; i < pageCount; i++)
            {
                using var pageReader = docReader.GetPageReader(i);

                var rawBytes = pageReader.GetImage();
                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                using var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height);

                await using var ms = new MemoryStream();
                await image.SaveAsPngAsync(ms);
                var pngBytes = ms.ToArray();

                using var pix = Pix.LoadFromMemory(pngBytes);
                using var page = engine.Process(pix);

                sb.AppendLine(page.GetText());
                sb.AppendLine("-----PAGE_BREAK-----");
            }

            return sb.ToString();
        }
    }
}
=======
            var images = await PdfToImages.RenderAllPagesAsPngAsync(pdfPath);

            if (images == null || images.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            for (int i = 0; i < images.Count; i++)
            {
                var pageText = await ExtractTextFromSingleImageAsync(images[i]);

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.AppendLine($"[Page {i + 1}]");
                    sb.AppendLine(pageText);
                    sb.AppendLine();
                }
            }

            return sb.ToString().Trim();
        }

        public async Task<string> ExtractPageTextByOcrAsync(string pdfPath, int pageNumber)
        {
            var images = await PdfToImages.RenderAllPagesAsPngAsync(pdfPath);

            if (images == null || images.Count == 0)
                return string.Empty;

            if (pageNumber < 1 || pageNumber > images.Count)
                return string.Empty;

            var singlePageImage = images[pageNumber - 1];

            return await ExtractTextFromSingleImageAsync(singlePageImage);
        }

        private async Task<string> ExtractTextFromSingleImageAsync(byte[] pngImage)
        {
            var prompt = """
اقرأ النص العربي الموجود في الصورة كما هو.
هذه صفحة من تقويم أكاديمي.
استخرج النص فقط.
لا تشرح.
لا تلخص.
لا تعيده بصيغة JSON.
أعد النص الخام فقط مع الحفاظ قدر الإمكان على الكلمات والتواريخ.
""";

            var text = await _vision.GetJsonFromImagesAsync(
      prompt,
      new List<byte[]> { pngImage });

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("OCR returned empty text from image.");

            return text.Trim();
        }
}
}

>>>>>>> origin_second/لما2

using System.Text;
using Acadify.Services.AcademicCalendar.Interfaces;

namespace Acadify.Services.AcademicCalendar
{
    public class PdfOcrService : IPdfOcrService
    {
        private readonly OpenAiVisionClient _vision;

        public PdfOcrService(OpenAiVisionClient vision)
        {
            _vision = vision;
        }

        public async Task<string> ExtractTextByOcrAsync(string pdfPath)
        {
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


using System.Text;
using Acadify.Services.AcademicCalendar.Interfaces;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.AspNetCore.Hosting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;

namespace Acadify.Services.AcademicCalendar
{
    public class PdfOcrService : IPdfOcrService
    {
        private readonly IWebHostEnvironment _env;

        public PdfOcrService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> ExtractTextByOcrAsync(string pdfPath)
        {
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
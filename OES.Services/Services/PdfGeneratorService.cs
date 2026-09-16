using Microsoft.AspNetCore.Hosting;
using OES.Interface.Interfaces;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace OES.Services.Services
{
    public class PdfGeneratorService : IPdfGeneratorService
    {
        private const string FontsFolderName = "fonts";
        private const string WebRootFolderName = "wwwroot";

        public PdfGeneratorService(IWebHostEnvironment webHostEnvironment)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var fontsDirectoryPath = Path.Combine(
                webHostEnvironment.ContentRootPath,
                WebRootFolderName,
                FontsFolderName
            );

            RegisterFont(fontsDirectoryPath, "Cairo-Bold.ttf");
            RegisterFont(fontsDirectoryPath, "Cairo-Medium.ttf");
            RegisterFont(fontsDirectoryPath, "Cairo-Regular.ttf");
            RegisterFont(fontsDirectoryPath, "Cairo-SemiBold.ttf");
            RegisterFont(fontsDirectoryPath, "NotoColorEmoji-Regular.ttf");
        }

        public Task<byte[]> GeneratePdfAsync(IDocument documentModel)
        {
            var pdfBytes = documentModel.GeneratePdf();

            return Task.FromResult(pdfBytes);
        }

        private static void RegisterFont(string directoryPath, string fileName)
        {
            using var fontStream = File.OpenRead(Path.Combine(directoryPath, fileName));

            FontManager.RegisterFont(fontStream);
        }
    }
}

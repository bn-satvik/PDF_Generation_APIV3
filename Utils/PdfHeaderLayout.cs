using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;

namespace Proj.Utils
{
    public static class PdfHeaderLayout
    {
        // ===== Constants =====
        private const string TopMarginCm = "5cm";
        private const string RightFrameWidth = "7cm";
        private const string RightFrameHeight = "4cm";
        private const double RightFrameTopCm = 2.1;
        private const double RightFrameWidthCm = 6;
        private const double RightMarginCm = 2.5;
        private const int CompanyFontSize = 14;
        private const int InspectorFontSize = 12;
        private const int DateRangeFontSize = 12;

        private const string RightParagraphSpaceAfterCm = "0.45cm";
        private const string LeftFrameWidth = "10cm";
        private const string LeftFrameHeight = "4cm";
        private const string LeftFrameTop = "1.5cm";
        private const string LeftFrameLeft = "1.5cm";
        private const string LogoWidth = "4cm";

        private const int TitleFontSize = 16;
        private const string TitleSpaceBefore = "0.3cm";
        private const string TitleSpaceAfter = "0.3cm";
        private const int GeneratedDateFontSize = 12;

        // Builds both left and right header frames
        public static void BuildHeader(Section section, PdfHeaderModel headerModel)
        {
            section.PageSetup.TopMargin = TopMarginCm;
            CreateLeftHeader(section, headerModel);
            CreateRightHeader(section, headerModel);
        }

        // Adds company name, inspector, and date range to the top-right
        private static void CreateRightHeader(Section section, PdfHeaderModel headerModel)
        {
            var rightFrame = section.Headers.Primary.AddTextFrame();
            rightFrame.Width = RightFrameWidth;
            rightFrame.Height = RightFrameHeight;
            rightFrame.RelativeVertical = RelativeVertical.Page;
            rightFrame.RelativeHorizontal = RelativeHorizontal.Page;

            var pageWidth = section.PageSetup.PageWidth;
            var frameWidth = Unit.FromCentimeter(RightFrameWidthCm);
            var rightMargin = Unit.FromCentimeter(RightMarginCm);

            rightFrame.Top = $"{RightFrameTopCm}cm";
            rightFrame.Left = (pageWidth - frameWidth - rightMargin).ToString();

            var companyParagraph = rightFrame.AddParagraph(headerModel.CompanyName);
            companyParagraph.Format.Font.Bold = true;
            companyParagraph.Format.Font.Size = CompanyFontSize;
            companyParagraph.Format.Alignment = ParagraphAlignment.Right;
            companyParagraph.Format.SpaceAfter = RightParagraphSpaceAfterCm;

            var inspectorParagraph = rightFrame.AddParagraph(headerModel.ProuductName);
            inspectorParagraph.Format.Font.Size = InspectorFontSize;
            inspectorParagraph.Format.Alignment = ParagraphAlignment.Right;
            inspectorParagraph.Format.SpaceAfter = RightParagraphSpaceAfterCm;

            var dateRangeParagraph = rightFrame.AddParagraph($"Data from {headerModel.DateRange}");
            dateRangeParagraph.Format.Font.Size = DateRangeFontSize;
            dateRangeParagraph.Format.Alignment = ParagraphAlignment.Right;
        }

        private static void CreateLeftHeader(Section section, PdfHeaderModel headerModel)
        {
            var leftFrame = section.Headers.Primary.AddTextFrame();
            leftFrame.Width = LeftFrameWidth;
            leftFrame.Height = LeftFrameHeight;
            leftFrame.RelativeVertical = RelativeVertical.Page;
            leftFrame.RelativeHorizontal = RelativeHorizontal.Page;

            leftFrame.Top = LeftFrameTop;
            leftFrame.Left = LeftFrameLeft;

            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), headerModel.LogoPath);
            if (File.Exists(logoPath))
            {
                var logo = leftFrame.AddImage(logoPath);
                logo.Width = LogoWidth;
                logo.LockAspectRatio = true;
            }
            else
            {
                leftFrame.AddParagraph("Logo Not Found");
            }

            var titleParagraph = leftFrame.AddParagraph(headerModel.Title);
            titleParagraph.Format.Font.Size = TitleFontSize;
            titleParagraph.Format.Font.Bold = true;
            titleParagraph.Format.SpaceBefore = TitleSpaceBefore;
            titleParagraph.Format.SpaceAfter = TitleSpaceAfter;
            titleParagraph.Format.Alignment = ParagraphAlignment.Left;

            var genLine = leftFrame.AddParagraph();
            genLine.Format.Font.Size = GeneratedDateFontSize;
            genLine.Format.Alignment = ParagraphAlignment.Left;

            var boldText = genLine.AddFormattedText("Generated on ", TextFormat.Bold);
            genLine.AddText(headerModel.GeneratedDate);
        }
    }
}

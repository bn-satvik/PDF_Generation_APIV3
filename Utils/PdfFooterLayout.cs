using MigraDoc.DocumentObjectModel;


namespace Proj.Utils
{
    public static class PdfFooterLayout
    {
        // Builds the footer content for the PDF
        public static void BuildFooter(PdfFooterModel footerModel, Section section)
        {
            // Get the footer section
            var footer = section.Footers.Primary;

            // Create a new paragraph in the footer
            Paragraph paragraph = footer.AddParagraph();
            paragraph.Format.Font.Size = 12;
            paragraph.Format.Alignment = ParagraphAlignment.Right;

            
            // Add page number info if enabled
            if (footerModel.ShowPageNumbers)
            {
                if (!string.IsNullOrEmpty(footerModel.RightText))
                {
                    paragraph.AddText(footerModel.RightText);
                    paragraph.AddText(" | ");
                }

                paragraph.AddText("Page ");
                paragraph.AddPageField();        // Adds current page number
                paragraph.AddText(" of ");
                paragraph.AddNumPagesField();    // Adds total page count
            }

            // Re-add right text if combined with other info
            if (!string.IsNullOrEmpty(footerModel.RightText))
            {
                

                paragraph.AddText(footerModel.RightText);
            }
        }
    }
}

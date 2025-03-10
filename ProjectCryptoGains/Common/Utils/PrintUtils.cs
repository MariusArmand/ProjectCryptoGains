using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ProjectCryptoGains.Common.Utils
{
    public static class PrintUtils
    {
        /// <summary>
        /// Asynchronously creates and prints a FlowDocument with the specified data
        /// Runs on a background thread with pagination support and footer margin, including page numbering
        /// The caller must provide a PrintDialog instance created on the UI thread
        /// </summary>
        /// <typeparam name="T">Type of items in the data collection</typeparam>
        /// <param name="columnHeaders">Array of column header names</param>
        /// <param name="dataItems">Collection of data items for the table</param>
        /// <param name="dataExtractor">Function to extract values, alignments, and optional column spans from each item</param>
        /// <param name="printDlg">PrintDialog instance for printing, created on the UI thread</param>
        /// <param name="titlePage">If true, places title and subtitle on a separate first page (default: false)</param>
        /// <param name="title">Document title, displayed at the top</param>
        /// <param name="subtitle">Subtitle, displayed below the title (optional)</param>
        /// <param name="summaryText">Text for a summary section below the table (optional)</param>
        /// <param name="pageWidth">Page width in device-independent units (default: 793, A4 width)</param>
        /// <param name="pageHeight">Page height in device-independent units (default: 1123, A4 height)</param>
        /// <param name="footerHeight">Height reserved for the footer with page numbers (default: 60)</param>
        /// <param name="fontFamily">Font family for the document (default: Fixedsys)</param>
        /// <param name="fontSize">Font size for the document (default: 8)</param>
        /// <param name="maxColumnsPerRow">Maximum number of columns per row before splitting (default: 7), based on total spanned slots rather than individual columns</param>
        /// <param name="repeatHeadersPerItem">Repeat headers per item if true, otherwise once at the top</param>
        /// <param name="itemsPerPage">Number of items per page (default: 100)</param>
        public static async Task PrintFlowDocumentAsync<T>(
            string[] columnHeaders,
            IEnumerable<T> dataItems,
            Func<T, (string Value, TextAlignment Alignment, int ColumnSpan)[]> dataExtractor,
            PrintDialog printDlg,
            bool titlePage = false,
            string title = "",
            string? subtitle = null,
            string? summaryText = null,
            double pageWidth = 793,
            double pageHeight = 1123,
            double footerHeight = 50,
            string fontFamily = "Fixedsys",
            double fontSize = 8,
            int maxColumnsPerRow = 7,
            bool repeatHeadersPerItem = false,
            int itemsPerPage = 100
            )
        {
            await Task.Run(() =>
            {
                // Calculate the effective page height by subtracting the footer height from the total page height
                double effectivePageHeight = pageHeight - footerHeight;
                // Define page padding with extra space at the bottom to accommodate the footer
                Thickness pagePadding = new Thickness(20, 20, 20, 20 + footerHeight);

                // Create a new FlowDocument with the specified dimensions and styling
                FlowDocument flowDocument = new()
                {
                    PageWidth = pageWidth,
                    PageHeight = effectivePageHeight, // Adjust height to leave space for footer
                    ColumnWidth = pageWidth,
                    PagePadding = pagePadding, // Apply padding including footer height
                    FontFamily = new FontFamily(fontFamily),
                    FontSize = fontSize
                };

                // Create a title paragraph with larger, bold, centered text
                Paragraph titleParagraph = new Paragraph(new Run(title))
                {
                    FontSize = fontSize * 2,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                };

                // Handle title and subtitle placement based on titlePage parameter
                if (titlePage)
                {
                    // Create a separate section for the title page when titlePage is true
                    Section titleSection = new Section();

                    // Add empty paragraphs to push the title down further on the page
                    Paragraph spacer = new Paragraph(new Run("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n"))
                    {
                        TextAlignment = TextAlignment.Center // Match title alignment
                    };
                    titleSection.Blocks.Add(spacer);

                    titleSection.Blocks.Add(titleParagraph);

                    // Add subtitle with reduced spacing and aligned to title's left edge if provided
                    if (subtitle != null)
                    {
                        // Measure the title's width to calculate its left edge
                        FormattedText titleText = new FormattedText(
                            title,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(flowDocument.FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                            fontSize * 2, // Title font size
                            Brushes.Black,
                            1.0
                        );

                        // Calculate the usable width for the title page
                        double titleUsableWidth = pageWidth - (flowDocument.PagePadding.Left + flowDocument.PagePadding.Right);
                        // Calculate the left offset: (usable width - title width) / 2
                        double titleLeftOffset = (titleUsableWidth - titleText.WidthIncludingTrailingWhitespace) / 2;

                        // Create subtitle paragraph with a left margin to match the title's left edge
                        Paragraph subtitleParagraph = new Paragraph(new Run(subtitle))
                        {
                            TextAlignment = TextAlignment.Left,
                            Margin = new Thickness(titleLeftOffset, 0, 0, 0) // Shift subtitle to align with title's left edge
                        };
                        titleSection.Blocks.Add(subtitleParagraph);
                    }

                    flowDocument.Blocks.Add(titleSection);
                    // No page break before the first section (title page)
                    titleSection.BreakPageBefore = false;
                }
                else
                {
                    // Add title and subtitle to the main document flow when titlePage is false
                    flowDocument.Blocks.Add(titleParagraph);
                    flowDocument.Blocks.Add(new Paragraph(new Run("\n")));

                    // Set up the initial table structure for subtitle or spacing
                    Table initialTable = new();
                    TableRowGroup initialRowGroup = new TableRowGroup();
                    initialTable.RowGroups.Add(initialRowGroup);

                    // Check subtitle status and add appropriate rows to the document
                    if (subtitle == null)
                    {
                        initialRowGroup.Rows.Add(new TableRow { Cells = { new TableCell(new Paragraph(new Run("\n"))) } });
                    }
                    else
                    {
                        TableRow subtitleRow = new();
                        TableCell subtitleCell = new(new Paragraph(new Run(subtitle)))
                        {
                            ColumnSpan = Math.Min(maxColumnsPerRow, columnHeaders.Length), // Limit span based on column constraints
                            TextAlignment = TextAlignment.Left
                        };
                        subtitleRow.Cells.Add(subtitleCell);
                        initialRowGroup.Rows.Add(subtitleRow);
                        initialRowGroup.Rows.Add(new TableRow { Cells = { new TableCell(new Paragraph(new Run("\n"))) } });
                    }
                    flowDocument.Blocks.Add(initialTable);
                }

                // Perform calculations for column layout
                int totalColumns = columnHeaders.Length; // Total number of actual data columns
                double usableWidth = pageWidth - (flowDocument.PagePadding.Left + flowDocument.PagePadding.Right); // Width available for content after accounting for padding

                // Calculate the effective number of columns for width distribution based on the maximum columns per row
                // This ensures consistent column widths across prints, regardless of the total number of columns
                int effectiveColumnCount = maxColumnsPerRow;

                // Calculate the total spanned slots to determine the minimum number of TableColumn objects needed,
                // accounting for ColumnSpan values to prevent layout issues; Use a safe default if dataItems is empty
                int totalSpannedSlots = totalColumns; // Default to totalColumns assuming ColumnSpan = 1 for each if no data
                foreach (var item in dataItems)
                {
                    if (item is T validItem)
                    {
                        totalSpannedSlots = dataExtractor(validItem).Sum(v => v.ColumnSpan);
                        break; // Use the first valid item
                    }
                }
                int actualColumnCount = Math.Max(totalSpannedSlots, effectiveColumnCount); // Ensure enough columns for all spanned slots

                // Add a minimum padding between columns to improve readability
                double minPaddingPerColumn = 5.0; // Padding between columns
                double adjustedUsableWidth = usableWidth - (minPaddingPerColumn * (effectiveColumnCount - 1)); // Use effectiveColumnCount for padding consistency
                double columnWeight = 1.0 / effectiveColumnCount; // Proportional width of each column, dividing adjusted width by effectiveColumnCount
                double columnWidth = adjustedUsableWidth * columnWeight; // Width of a single column in the table

                // Split dataItems into chunks of itemsPerPage, adjusting for title on first page or separate title page
                var dataList = dataItems.ToList();
                int totalItems = dataList.Count;
                int initialPageItemCount = titlePage ? 0 : Math.Max(1, itemsPerPage - 2); // No items on first page if titlePage is true, otherwise adjust for title/subtitle space
                int remainingItems = totalItems - initialPageItemCount;

                // Calculate how many additional pages are needed after the first page
                // If there are remaining items (> 0), divide them by itemsPerPage and round up; otherwise, set to 0
                int subsequentPages = remainingItems > 0 ? (int)Math.Ceiling((double)remainingItems / itemsPerPage) : 0;

                // Total pages includes a title page (if titlePage is true) plus content pages
                int pageCount = (titlePage ? 1 : 0) + (initialPageItemCount > 0 || remainingItems > 0 ? 1 + subsequentPages : 0);

                for (int page = 0; page < pageCount; page++)
                {
                    // Skip content generation for the first page if it’s a title page
                    if (titlePage && page == 0) continue;

                    // Adjust page index for content pages when titlePage is true
                    int adjustedPageIndex = titlePage ? page - 1 : page;
                    // Decide how many items this page gets based on whether it’s the first content page
                    int itemsPerPageThisPage = (adjustedPageIndex == 0 && !titlePage) ? initialPageItemCount : itemsPerPage;

                    // Calculate how many items to skip based on the adjusted page number
                    int skipCount = (adjustedPageIndex == 0 && !titlePage) ? 0 : initialPageItemCount + (adjustedPageIndex - 1) * itemsPerPage;
                    // Get the items for the current page by skipping and taking
                    var pageItems = dataList.Skip(skipCount)
                                            .Take(itemsPerPageThisPage);

                    // Create a new Section for this page's chunk
                    Section pageSection = new Section();
                    // Force page break before all content sections when titlePage is true, or before all but the first section otherwise
                    if (titlePage ? page > 0 : page > 0)
                    {
                        pageSection.BreakPageBefore = true;
                    }

                    // Set up the table structure for this page
                    Table table = new();
                    TableRowGroup rowGroup = new TableRowGroup();
                    table.RowGroups.Add(rowGroup);

                    // Add columns to the table, ensuring enough columns to match actualColumnCount for proper layout
                    for (int i = 0; i < actualColumnCount; i++)
                    {
                        table.Columns.Add(new TableColumn { Width = new GridLength(columnWeight, GridUnitType.Star) });
                    }

                    int rowsNeeded = (int)Math.Ceiling((double)totalColumns / Math.Min(maxColumnsPerRow, totalColumns)); // Calculate the number of rows required (used when headers are not repeated per item)

                    // Add a single header row if headers are not set to repeat for each item
                    // (repeating headers are handled per item in the data loop below)
                    if (!repeatHeadersPerItem)
                    {
                        TableRow headerRow = new() { FontWeight = FontWeights.Bold };
                        var firstItem = pageItems.FirstOrDefault() ?? dataItems.FirstOrDefault();
                        (string Value, TextAlignment Alignment, int ColumnSpan)[] headerSampleValues = firstItem != null
                            ? dataExtractor(firstItem)
                            : Enumerable.Range(0, totalColumns).Select(_ => ("", TextAlignment.Left, 1)).ToArray(); // Default if no item available
                        for (int i = 0; i < totalColumns; i++)
                        {
                            var cell = new TableCell(new Paragraph(new Run(columnHeaders[i])))
                            {
                                TextAlignment = i < headerSampleValues.Length ? headerSampleValues[i].Alignment : TextAlignment.Left,
                                ColumnSpan = i < headerSampleValues.Length ? headerSampleValues[i].ColumnSpan : 1 // Apply ColumnSpan to headers
                            };
                            headerRow.Cells.Add(cell);
                        }
                        // Fill remaining columns with empty cells to match actualColumnCount
                        for (int i = totalColumns; i < actualColumnCount; i++)
                        {
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
                        }
                        rowGroup.Rows.Add(headerRow);
                    }

                    // Iterate over each data item in this page's chunk
                    foreach (var item in pageItems)
                    {
                        if (item == null) continue; // Skip null items to avoid null reference issues in dataExtractor

                        (string Value, TextAlignment Alignment, int ColumnSpan)[] values = dataExtractor(item);

                        // Calculate the total spanned width for this item
                        int totalSpan = values.Sum(v => v.ColumnSpan);
                        int rowsNeededDynamic = (int)Math.Ceiling((double)totalSpan / maxColumnsPerRow); // Dynamically calculate rows based on total span

                        TableRowGroup itemGroup = new TableRowGroup();
                        table.RowGroups.Add(itemGroup);

                        int currentColumnIndex = 0; // Track the current column index across rows

                        for (int rowIndex = 0; rowIndex < rowsNeededDynamic; rowIndex++)
                        {
                            int startColumnIndex = currentColumnIndex; // Starting column for this row

                            // Check if headers should repeat or columns span multiple rows
                            if (repeatHeadersPerItem || totalSpan > maxColumnsPerRow)
                            {
                                TableRow headerRow = new() { FontWeight = FontWeights.Bold };
                                int headerIndex = startColumnIndex;
                                int headerSpanFilled = 0;
                                while (headerIndex < columnHeaders.Length && headerSpanFilled < maxColumnsPerRow)
                                {
                                    int span = headerIndex < values.Length ? values[headerIndex].ColumnSpan : 1;
                                    if (headerSpanFilled + span <= maxColumnsPerRow)
                                    {
                                        var cell = new TableCell(new Paragraph(new Run(columnHeaders[headerIndex])))
                                        {
                                            TextAlignment = headerIndex < values.Length ? values[headerIndex].Alignment : TextAlignment.Left,
                                            ColumnSpan = span
                                        };
                                        headerRow.Cells.Add(cell);
                                        headerSpanFilled += span;
                                        headerIndex++;
                                    }
                                    else
                                    {
                                        break; // Move to the next row
                                    }
                                }
                                // Fill remaining columns with empty cells up to maxColumnsPerRow
                                for (int i = headerSpanFilled; i < maxColumnsPerRow; i++)
                                {
                                    headerRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
                                }
                                itemGroup.Rows.Add(headerRow);
                                currentColumnIndex = headerIndex; // Update the column index for the next row
                            }

                            // Add data row
                            TableRow dataRow = new();
                            int valueIndex = startColumnIndex;
                            int dataSpanFilled = 0;
                            while (valueIndex < values.Length && dataSpanFilled < maxColumnsPerRow)
                            {
                                int span = values[valueIndex].ColumnSpan;
                                if (dataSpanFilled + span <= maxColumnsPerRow)
                                {
                                    string cellText = values[valueIndex].Value;
                                    double adjustmentFactor = 1 / maxColumnsPerRow + 1.0; // Dynamic factor based on maxColumnsPerRow
                                    double availableWidth = columnWidth * span * adjustmentFactor; // Adjust available width dynamically

                                    // Measure the text width using FormattedText
                                    FormattedText formattedText = new FormattedText(
                                        cellText,
                                        System.Globalization.CultureInfo.CurrentCulture,
                                        FlowDirection.LeftToRight,
                                        new Typeface(flowDocument.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                                        fontSize,
                                        Brushes.Black,
                                        1.0
                                    );

                                    // If the text width exceeds the available width, trim and append "..." to prevent line breaks
                                    // This ensures data cells fit within their columns, enhancing readability and layout consistency
                                    if (formattedText.WidthIncludingTrailingWhitespace > availableWidth)
                                    {
                                        const string ellipsis = "...";
                                        // Measure the width of the ellipsis to ensure it fits
                                        FormattedText ellipsisText = new FormattedText(
                                            ellipsis,
                                            System.Globalization.CultureInfo.CurrentCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface(flowDocument.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                                            fontSize,
                                            Brushes.Black,
                                            1.0
                                        );
                                        double ellipsisWidth = ellipsisText.WidthIncludingTrailingWhitespace;
                                        double targetWidth = availableWidth - ellipsisWidth;

                                        // Iteratively trim the text until it fits within the target width
                                        string trimmedText = cellText;
                                        while (trimmedText.Length > 0)
                                        {
                                            formattedText = new FormattedText(
                                                trimmedText + ellipsis,
                                                System.Globalization.CultureInfo.CurrentCulture,
                                                FlowDirection.LeftToRight,
                                                new Typeface(flowDocument.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                                                fontSize,
                                                Brushes.Black,
                                                1.0
                                            );
                                            if (formattedText.WidthIncludingTrailingWhitespace <= availableWidth)
                                            {
                                                break;
                                            }
                                            trimmedText = trimmedText.Substring(0, trimmedText.Length - 1);
                                        }
                                        cellText = trimmedText + ellipsis;
                                    }

                                    var cell = new TableCell(new Paragraph(new Run(cellText)))
                                    {
                                        TextAlignment = values[valueIndex].Alignment,
                                        ColumnSpan = span
                                    };
                                    dataRow.Cells.Add(cell);
                                    dataSpanFilled += span;
                                    valueIndex++;
                                }
                                else
                                {
                                    break; // Move to the next row
                                }
                            }

                            // Fill remaining columns with empty cells up to maxColumnsPerRow
                            for (int i = dataSpanFilled; i < maxColumnsPerRow; i++)
                            {
                                dataRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
                            }
                            itemGroup.Rows.Add(dataRow);
                            currentColumnIndex = valueIndex; // Update the column index for the next row
                        }

                        if (repeatHeadersPerItem)
                        {
                            itemGroup.Rows.Add(new TableRow { Cells = { new TableCell(new Paragraph(new Run("\n"))) } });
                        }
                    }

                    // Add this page's table to the section, and the section to the document
                    pageSection.Blocks.Add(table);
                    flowDocument.Blocks.Add(pageSection);
                }

                // Check if summary text is provided and add it to the document
                if (!string.IsNullOrEmpty(summaryText))
                {
                    Table summaryTable = new();
                    TableRowGroup summaryGroup = new TableRowGroup();
                    summaryTable.RowGroups.Add(summaryGroup);

                    // Add columns to match earlier tables, using totalColumns since summary doesn't repeat per item
                    for (int i = 0; i < totalColumns; i++)
                    {
                        summaryTable.Columns.Add(new TableColumn { Width = new GridLength(columnWeight, GridUnitType.Star) }); // Use consistent column weight
                    }

                    if (!repeatHeadersPerItem)
                    {
                        summaryGroup.Rows.Add(new TableRow { Cells = { new TableCell(new Paragraph(new Run("\n"))) } });
                    }
                    TableRow summaryRow = new();
                    TableCell summaryCell = new(new Paragraph(new Run(summaryText)))
                    {
                        ColumnSpan = Math.Min(maxColumnsPerRow, totalColumns),
                        TextAlignment = TextAlignment.Left
                    };
                    summaryRow.Cells.Add(summaryCell);
                    summaryGroup.Rows.Add(summaryRow);
                    flowDocument.Blocks.Add(summaryTable);
                }

                // Create a custom paginator to add page numbers
                IDocumentPaginatorSource idpSource = flowDocument;
                DocumentPaginator paginator = idpSource.DocumentPaginator;
                int totalPages = GetTotalPageCount(paginator); // Calculate the total number of pages
                CustomPaginator customPaginator = new CustomPaginator(paginator, footerHeight, fontSize, pageWidth, flowDocument.FontFamily, totalPages);

                // Attempt to print the document, handling any exceptions
                try
                {
                    flowDocument.Name = "FlowDoc";
                    // Tell the printer to use our CustomPaginator, which adds "Page X of Y" to each page
                    // This pulls pages one by one through our overridden GetPage, so we get content with numbers on every printed page (e.g., "Page 1 of 12" for a 12-page document)
                    printDlg.PrintDocument(customPaginator, title);
                }
                catch (Exception ex)
                {
                    // Handle printing exceptions by showing an error message
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxResult result = CustomMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        // Determines the total page count by iterating through pages with GetPage when IsPageCountValid is false
        // Relies on WPF's validation (IsPageCountValid) or manual counting up to a 1000-page limit to prevent infinite loops
        // Uses GetPage to compute the count as needed
        private static int GetTotalPageCount(DocumentPaginator paginator)
        {
            int pageCount = 0;
            while (!paginator.IsPageCountValid && pageCount < 1000) // Continue counting pages up to a maximum of 1000 to prevent infinite loops
            {
                try
                {
                    paginator.GetPage(pageCount);
                    pageCount++;
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
            }
            // Returns the page count based on reliability:
            // - If paginator.IsPageCountValid is true, use paginator.PageCount (the official count)
            // - If false, and pageCount > 0 (we manually found pages), use that count
            // - Otherwise, default to 1 as a fallback to ensure at least one page
            return paginator.IsPageCountValid ? paginator.PageCount : pageCount > 0 ? pageCount : 1;
        }

        // CustomPaginator adds page numbers (e.g., "Page X of Y") to each page for printing
        // We override GetPage because the default paginator does not include page numbers
        // PrintDialog uses this to render numbered pages (e.g., "Page 1 of 12" for a 12-page document)
        private class CustomPaginator : DocumentPaginator
        {
            private readonly DocumentPaginator _inner;
            private readonly double _footerHeight;
            private readonly double _fontSize;
            private readonly double _pageWidth;
            private readonly FontFamily _fontFamily;
            private readonly int _totalPages;

            public CustomPaginator(DocumentPaginator inner, double footerHeight, double fontSize, double pageWidth, FontFamily fontFamily, int totalPages)
            {
                _inner = inner;
                _footerHeight = footerHeight;
                _fontSize = fontSize;
                _pageWidth = pageWidth;
                _fontFamily = fontFamily;
                _totalPages = totalPages;
            }

            public override DocumentPage GetPage(int pageNumber)
            {
                // Get the original page from the inner paginator (the raw content without our customizations)
                DocumentPage originalPage = _inner.GetPage(pageNumber);

                // If the page has no visual content (rare edge case), return it unchanged
                if (originalPage.Visual == null)
                {
                    return originalPage;
                }

                // Create a new visual canvas to draw our customized page
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    // Draw a transparent background to match the page size (sets the drawing area)
                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, _pageWidth, originalPage.Size.Height));

                    // Add the original page content (e.g., table data) to our new canvas
                    visual.Children.Add(originalPage.Visual);

                    // Create the page number text (e.g., "Page 1 of 5") with specified font settings
                    FormattedText formattedText = new FormattedText(
                        $"Page {pageNumber + 1} of {_totalPages}", // Page numbers start at 1, not 0
                        System.Globalization.CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(_fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                        _fontSize,
                        Brushes.Black,
                        1.0 // Pixels per dip, standard scaling
                    );

                    // Calculate centering: x for horizontal, y for vertical within the footer area
                    double x = (_pageWidth - formattedText.Width) / 2; // Center horizontally
                    double y = originalPage.Size.Height - _footerHeight + (_footerHeight - formattedText.Height) / 2; // Center vertically in footer

                    // Draw the page number text onto the page
                    dc.DrawText(formattedText, new Point(x, y));
                }

                // Return the new page with original content plus the page number, preserving size and layout
                return new DocumentPage(visual, originalPage.Size, new Rect(0, 0, originalPage.Size.Width, originalPage.Size.Height), new Rect(0, 0, originalPage.Size.Width, originalPage.Size.Height));
            }

            public override bool IsPageCountValid => _inner.IsPageCountValid;
            public override int PageCount => _totalPages;
            public override Size PageSize
            {
                get => _inner.PageSize;
                set => _inner.PageSize = value;
            }
            public override IDocumentPaginatorSource Source => _inner.Source;
        }
    }
}
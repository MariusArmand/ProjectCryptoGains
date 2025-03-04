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
        /// <param name="title">Document title, displayed at the top</param>
        /// <param name="subtitle">Subtitle, displayed below the title (optional)</param>
        /// <param name="summaryText">Text for a summary section below the table (optional)</param>
        /// <param name="pageWidth">Page width in device-independent units (default: 793, A4 width)</param>
        /// <param name="pageHeight">Page height in device-independent units (default: 1123, A4 height)</param>
        /// <param name="footerHeight">Height reserved for the footer with page numbers (default: 60)</param>
        /// <param name="fontFamily">Font family for the document (default: Fixedsys)</param>
        /// <param name="fontSize">Font size for the document (default: 8)</param>
        /// <param name="maxColumnsPerRow">Maximum number of columns per row before splitting(default: 7), based on column count rather than spanned slots</param>
        /// <param name="repeatHeadersPerItem">Repeat headers per item if true, otherwise once at the top</param>
        /// <param name="virtualColumnCount">Number of virtual columns for width distribution (defaults to maxColumnsPerRow or adjusted dynamically)</param>
        /// <param name="itemsPerPage">Number of items per page (default: 100)</param>
        public static async Task PrintFlowDocumentAsync<T>(
            string[] columnHeaders,
            IEnumerable<T> dataItems,
            Func<T, (string Value, TextAlignment Alignment, int ColumnSpan)[]> dataExtractor,
            PrintDialog printDlg,
            string title,
            string? subtitle = null,
            string? summaryText = null,
            double pageWidth = 793,
            double pageHeight = 1123,
            double footerHeight = 60,
            string fontFamily = "Fixedsys",
            double fontSize = 8,
            int maxColumnsPerRow = 7,
            bool repeatHeadersPerItem = false,
            int? virtualColumnCount = null,
            int itemsPerPage = 100)
        {
            await Task.Run(() =>
            {
                // Calculate the effective page height by subtracting the footer height from the total page height
                double effectivePageHeight = pageHeight - footerHeight;
                // Define page padding with extra space at the bottom to accommodate the footer
                Thickness pagePadding = new Thickness(20, 20, 20, 20 + footerHeight);

                // Create a new FlowDocument with the specified dimensions and styling
                FlowDocument flowDoc = new()
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
                flowDoc.Blocks.Add(titleParagraph);
                flowDoc.Blocks.Add(new Paragraph(new Run("\n")));

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
                flowDoc.Blocks.Add(initialTable);

                // Perform calculations for column layout
                int totalColumns = columnHeaders.Length; // Total number of actual data columns
                int adjustedVirtualColumnCount; // Adjusted number of virtual columns to handle span overflow and prevent margin issues
                if (virtualColumnCount.HasValue)
                {
                    adjustedVirtualColumnCount = virtualColumnCount.Value;
                }
                else
                {
                    // Calculate total effective column span count from the first item's extractor (assumes consistent spans across items)
                    int totalColumnSpanCount = dataItems.Any() ? dataExtractor(dataItems.First()).Sum(x => x.ColumnSpan) : 0;

                    // Adjust adjustedVirtualColumnCount based on total span to prevent margin overflow
                    if (totalColumnSpanCount > totalColumns)
                    {
                        adjustedVirtualColumnCount = Math.Max(1, maxColumnsPerRow - (totalColumnSpanCount - totalColumns)); // Reduce virtual columns to compensate for extra span width
                    }
                    else
                    {
                        adjustedVirtualColumnCount = maxColumnsPerRow;
                    }
                }
                double usableWidth = pageWidth - (flowDoc.PagePadding.Left + flowDoc.PagePadding.Right); // Width available for content after accounting for padding
                int maxVirtualColumns = Math.Max(totalColumns, adjustedVirtualColumnCount); // Maximum number of virtual columns used for width distribution
                double columnWeight = 1.0 / maxVirtualColumns; // Proportional weight for each virtual column's width

                // Split dataItems into chunks of itemsPerPage, adjusting for title on first page
                var dataList = dataItems.ToList();
                int totalItems = dataList.Count;
                int initialPageItemCount = Math.Max(1, itemsPerPage - 2); // Adjusted item count for first page, accounting for title/subtitle space
                int remainingItems = totalItems - initialPageItemCount;

                // Calculate how many additional pages are needed after the first page
                // If there are remaining items (> 0), divide them by itemsPerPage and round up; otherwise, set to 0
                int subsequentPages = remainingItems > 0 ? (int)Math.Ceiling((double)remainingItems / itemsPerPage) : 0;

                // Total pages is 1 (first page with reduced items) plus any subsequent full pages
                int pageCount = 1 + subsequentPages;

                for (int page = 0; page < pageCount; page++)
                {
                    // Decide how many items this page gets:
                    // If it’s the first page (page == 0), use the reduced count (firstPageItems); otherwise, use full itemsPerPage
                    int itemsPerPageThisPage = (page == 0) ? initialPageItemCount : itemsPerPage;

                    // Calculate how many items to skip based on the page number
                    int skipCount;
                    if (page == 0)
                    {
                        skipCount = 0; // No skipping for the first page
                    }
                    else
                    {
                        skipCount = initialPageItemCount + (page - 1) * itemsPerPage; // Skip past first page and prior pages' items
                    }
                    // Get the items for the current page by skipping and taking
                    var pageItems = dataList.Skip(skipCount)
                                            .Take(itemsPerPageThisPage);

                    // Create a new Section for this page's chunk
                    Section pageSection = new Section();
                    if (page > 0) // Force page break before all but the first section
                    {
                        pageSection.BreakPageBefore = true;
                    }

                    // Set up the table structure for this page
                    Table table = new();
                    TableRowGroup rowGroup = new TableRowGroup();
                    table.RowGroups.Add(rowGroup);

                    // Add columns to the table
                    for (int i = 0; i < totalColumns; i++)
                    {
                        table.Columns.Add(new TableColumn { Width = new GridLength(columnWeight, GridUnitType.Star) });
                    }
                    if (adjustedVirtualColumnCount > totalColumns)
                    {
                        for (int i = totalColumns; i < adjustedVirtualColumnCount; i++)
                        {
                            table.Columns.Add(new TableColumn { Width = new GridLength(columnWeight, GridUnitType.Star) });
                        }
                    }

                    int rowsNeeded = (int)Math.Ceiling((double)totalColumns / Math.Min(maxColumnsPerRow, totalColumns)); // Calculate the number of rows required

                    // Add headers if not repeating per item and columns fit in one row
                    if (!repeatHeadersPerItem && totalColumns <= Math.Min(maxColumnsPerRow, totalColumns))
                    {
                        TableRow headerRow = new() { FontWeight = FontWeights.Bold };
                        var firstItem = pageItems.FirstOrDefault() ?? dataItems.FirstOrDefault();
                        (string Value, TextAlignment Alignment, int ColumnSpan)[] sampleValues = firstItem != null ? dataExtractor(firstItem) : new (string, TextAlignment, int)[totalColumns];
                        for (int i = 0; i < totalColumns; i++)
                        {
                            var cell = new TableCell(new Paragraph(new Run(columnHeaders[i])))
                            {
                                TextAlignment = i < sampleValues.Length ? sampleValues[i].Alignment : TextAlignment.Left,
                                ColumnSpan = i < sampleValues.Length ? sampleValues[i].ColumnSpan : 1 // Apply ColumnSpan to headers
                            };
                            headerRow.Cells.Add(cell);
                        }
                        for (int i = totalColumns; i < adjustedVirtualColumnCount; i++)
                        {
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
                        }
                        rowGroup.Rows.Add(headerRow);
                    }

                    // Iterate over each data item in this page's chunk
                    foreach (var item in pageItems)
                    {
                        (string Value, TextAlignment Alignment, int ColumnSpan)[] values = dataExtractor(item);

                        // If columns fit in one row and headers should not repeat
                        if (totalColumns <= Math.Min(maxColumnsPerRow, totalColumns) && !repeatHeadersPerItem)
                        {
                            TableRow dataRow = new();
                            for (int i = 0; i < totalColumns; i++)
                            {
                                var cell = new TableCell(new Paragraph(new Run(values[i].Value)))
                                {
                                    TextAlignment = values[i].Alignment,
                                    ColumnSpan = values[i].ColumnSpan // Apply ColumnSpan from tuple
                                };
                                dataRow.Cells.Add(cell);
                            }
                            for (int i = totalColumns; i < adjustedVirtualColumnCount; i++)
                            {
                                dataRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
                            }
                            rowGroup.Rows.Add(dataRow);
                        }
                        // Handle case where columns span multiple rows or headers should repeat
                        else
                        {
                            TableRowGroup itemGroup = new TableRowGroup();
                            table.RowGroups.Add(itemGroup);

                            for (int rowIndex = 0; rowIndex < rowsNeeded; rowIndex++)
                            {
                                int startIndex = rowIndex * Math.Min(maxColumnsPerRow, totalColumns); // Calculate the starting index for the current row
                                int columnsInThisRow = Math.Min(Math.Min(maxColumnsPerRow, totalColumns), totalColumns - startIndex); // Determine the number of columns for this row

                                // Check if headers should repeat or columns span multiple rows
                                if (repeatHeadersPerItem || totalColumns > Math.Min(maxColumnsPerRow, totalColumns))
                                {
                                    TableRow headerRow = new() { FontWeight = FontWeights.Bold };
                                    for (int i = 0; i < columnsInThisRow; i++)
                                    {
                                        int headerIndex = startIndex + i;
                                        var cell = new TableCell(new Paragraph(new Run(columnHeaders[headerIndex])))
                                        {
                                            TextAlignment = headerIndex < values.Length ? values[headerIndex].Alignment : TextAlignment.Left,
                                            ColumnSpan = headerIndex < values.Length ? values[headerIndex].ColumnSpan : 1 // Apply ColumnSpan to headers
                                        };
                                        headerRow.Cells.Add(cell);
                                    }
                                    for (int i = columnsInThisRow; i < adjustedVirtualColumnCount; i++)
                                    {
                                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
                                    }
                                    itemGroup.Rows.Add(headerRow);
                                }

                                TableRow dataRow = new();
                                for (int i = 0; i < columnsInThisRow; i++)
                                {
                                    int valueIndex = startIndex + i;
                                    var cell = new TableCell(new Paragraph(new Run(values[valueIndex].Value)))
                                    {
                                        TextAlignment = values[valueIndex].Alignment,
                                        ColumnSpan = values[valueIndex].ColumnSpan // Apply ColumnSpan from tuple
                                    };
                                    dataRow.Cells.Add(cell);
                                }
                                for (int i = columnsInThisRow; i < adjustedVirtualColumnCount; i++)
                                {
                                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(""))));
                                }
                                itemGroup.Rows.Add(dataRow);
                            }

                            if (repeatHeadersPerItem)
                            {
                                itemGroup.Rows.Add(new TableRow { Cells = { new TableCell(new Paragraph(new Run("\n"))) } });
                            }
                        }
                    }

                    // Add this page's table to the section, and the section to the document
                    pageSection.Blocks.Add(table);
                    flowDoc.Blocks.Add(pageSection);
                }

                // Check if summary text is provided and add it to the document
                if (!string.IsNullOrEmpty(summaryText))
                {
                    Table summaryTable = new();
                    TableRowGroup summaryGroup = new TableRowGroup();
                    summaryTable.RowGroups.Add(summaryGroup);

                    // Add columns to match earlier tables
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
                    flowDoc.Blocks.Add(summaryTable);
                }

                // Create a custom paginator to add page numbers
                IDocumentPaginatorSource idpSource = flowDoc;
                DocumentPaginator paginator = idpSource.DocumentPaginator;
                int totalPages = GetTotalPageCount(paginator); // Calculate the total number of pages
                if (totalPages == 0) totalPages = 1; // Set to 1 as a fallback if no pages are detected
                CustomPaginator customPaginator = new CustomPaginator(paginator, footerHeight, fontSize, pageWidth, flowDoc.FontFamily, totalPages);

                // Attempt to print the document, handling any exceptions
                try
                {
                    flowDoc.Name = "FlowDoc";
                    // Tell the printer to use our CustomPaginator, which adds "Page X of Y" to each page.
                    // This pulls pages one by one through our overridden GetPage, so we get trades with
                    // numbers on every printed page (like "Page 1 of 12" for our 12 pages).
                    printDlg.PrintDocument(customPaginator, title);
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxResult result = CustomMessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        // Forces WPF to count pages by asking for each one (GetPage) until it either knows the total
        // (IsPageCountValid turns true) or we count them ourselves. Stops at 1000 max, but for our
        // 12 pages, it’ll stop at 12 when it runs out (error). Works because GetPage waits to cut each
        // page right, waking up the paginator as we go.
        private static int GetTotalPageCount(DocumentPaginator paginator)
        {
            int count = 0;
            while (!paginator.IsPageCountValid && count < 1000) // Continue counting pages up to a maximum of 1000 to prevent infinite loops
            {
                try
                {
                    paginator.GetPage(count);
                    count++;
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
            }
            // Returns the page count based on reliability:
            // - If paginator.IsPageCountValid is true, use paginator.PageCount (the official count).
            // - If false, and count > 0 (we manually found pages), use that count.
            // - Otherwise, default to 1 as a fallback to ensure at least one page.
            return paginator.IsPageCountValid ? paginator.PageCount : count > 0 ? count : 1;
        }

        // CustomPaginator adds page numbers (like "Page X of Y") to each page for printing.
        // We override GetPage because the default only gives plain trades pages—no numbers.
        // PrintDialog uses this to pull numbered pages, so our 12-page doc prints with "Page 1 of 12" etc.
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

                    // Add the original page content (e.g., trades table) to our new canvas
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
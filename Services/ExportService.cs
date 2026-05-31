using ClosedXML.Excel;
using AzubiHilfer.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Document = AzubiHilfer.Models.Document;

namespace AzubiHilfer.Services;

public static class ExportService
{
    static ExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static void ExportToExcel(List<Models.Task> tasks, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Aufgaben");

        // Header
        var headers = new[] { "ID", "Titel", "Gruppe", "Abteilung", "Schwierigkeit", "Autor", "Tags", "Status", "Erstellt" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Data
        int row = 2;
        foreach (var t in tasks)
        {
            var diffNames = new[] { "Anfänger", "Mittel", "Fortgeschritten" };
            ws.Cell(row, 1).Value = t.Id;
            ws.Cell(row, 2).Value = t.Title;
            ws.Cell(row, 3).Value = t.Group == AzubiGroup.FachinformatikerAnwendungsentwicklung
                ? "FI Anwendungsentwicklung" : "Kaufmann Digitalisierung";
            ws.Cell(row, 4).Value = t.Department?.ToString() ?? "-";
            ws.Cell(row, 5).Value = diffNames[(int)t.Difficulty];
            ws.Cell(row, 6).Value = t.AuthorName;
            ws.Cell(row, 7).Value = string.Join(", ", t.Tags);
            ws.Cell(row, 8).Value = t.IsCompleted ? "✅ Erledigt" : "⬜ Offen";

            // Color completed rows
            if (t.IsCompleted)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#F0FFF4");

            row++;
        }

        // Stats sheet
        var ws2 = wb.Worksheets.Add("Statistik");
        ws2.Cell(1, 1).Value = "Statistik"; ws2.Cell(1, 1).Style.Font.Bold = true; ws2.Cell(1, 1).Style.Font.FontSize = 14;
        ws2.Cell(3, 1).Value = "Gesamt Aufgaben"; ws2.Cell(3, 2).Value = tasks.Count;
        ws2.Cell(4, 1).Value = "Abgeschlossen";   ws2.Cell(4, 2).Value = tasks.Count(t => t.IsCompleted);
        ws2.Cell(5, 1).Value = "Noch offen";      ws2.Cell(5, 2).Value = tasks.Count(t => !t.IsCompleted);
        ws2.Cell(6, 1).Value = "Fortschritt";
        ws2.Cell(6, 2).Value = tasks.Count == 0 ? "0%" : $"{(double)tasks.Count(t => t.IsCompleted) / tasks.Count:P0}";

        ws2.Cell(8, 1).Value = "Nach Abteilung"; ws2.Cell(8, 1).Style.Font.Bold = true;
        int r2 = 9;
        foreach (Department d in Enum.GetValues<Department>())
        {
            int cnt = tasks.Count(t => t.Department == d);
            if (cnt > 0) { ws2.Cell(r2, 1).Value = d.ToString(); ws2.Cell(r2, 2).Value = cnt; r2++; }
        }

        ws.Columns().AdjustToContents();
        ws2.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    public static void ExportToPdf(List<Models.Task> tasks, string filePath)
    {
        int total = tasks.Count;
        int done  = tasks.Count(t => t.IsCompleted);

        QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("🎓 Azubi-Hilfer — Aufgabenübersicht")
                            .FontSize(20).Bold().FontColor(Color.FromHex("1E3A5F"));
                        row.ConstantItem(120).AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy"))
                            .FontSize(10).FontColor(Color.FromHex("6B7280"));
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Color.FromHex("E5E7EB"));
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    // Stats summary
                    col.Item().Background(Color.FromHex("F0F9FF")).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Text($"📚 Gesamt: {total}").Bold();
                        row.RelativeItem().Text($"✅ Erledigt: {done}").Bold().FontColor(Color.FromHex("10B981"));
                        row.RelativeItem().Text($"⏳ Offen: {total - done}").Bold().FontColor(Color.FromHex("F59E0B"));
                        row.RelativeItem().Text($"🎯 Fortschritt: {(total == 0 ? 0 : done * 100 / total)}%").Bold().FontColor(Color.FromHex("8B5CF6"));
                    });
                    col.Item().PaddingTop(16);

                    // Group tasks
                    var groups = tasks.GroupBy(t => t.Group);
                    foreach (var grp in groups)
                    {
                        string grpName = grp.Key == AzubiGroup.FachinformatikerAnwendungsentwicklung
                            ? "FI Anwendungsentwicklung" : "Kaufmann Digitalisierungsmanagement";
                        col.Item().PaddingBottom(8).Text(grpName).FontSize(13).Bold().FontColor(Color.FromHex("1E3A5F"));

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });

                            // Table header
                            static IContainer HeaderCell(IContainer c) =>
                                c.Background(Color.FromHex("1E3A5F")).Padding(6)
                                 .DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(9));

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderCell).Text("Aufgabe");
                                h.Cell().Element(HeaderCell).Text("Abteilung");
                                h.Cell().Element(HeaderCell).Text("Autor");
                                h.Cell().Element(HeaderCell).Text("Schwierigkeit");
                                h.Cell().Element(HeaderCell).Text("Status");
                            });

                            var diffNames = new[] { "Anfänger", "Mittel", "Fortgeschr." };
                            bool even = false;
                            foreach (var t in grp.OrderBy(t => t.IsCompleted))
                            {
                                var bg = t.IsCompleted ? Color.FromHex("F0FFF4") :
                                         (even ? Color.FromHex("FAFAFA") : Colors.White);
                                even = !even;

                                static IContainer DataCell(IContainer c, Color bg) =>
                                    c.Background(bg).BorderBottom(1).BorderColor(Color.FromHex("E5E7EB")).Padding(5);

                                table.Cell().Element(c => DataCell(c, bg)).Text(t.Title).FontSize(9);
                                table.Cell().Element(c => DataCell(c, bg)).Text(t.Department?.ToString() ?? "-").FontSize(9);
                                table.Cell().Element(c => DataCell(c, bg)).Text(t.AuthorName).FontSize(9);
                                table.Cell().Element(c => DataCell(c, bg)).Text(diffNames[(int)t.Difficulty]).FontSize(9);
                                table.Cell().Element(c => DataCell(c, bg))
                                    .Text(t.IsCompleted ? "✅ Erledigt" : "⬜ Offen").FontSize(9);
                            }
                        });

                        col.Item().PaddingBottom(16);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Azubi-Hilfer — Seite ").FontSize(9).FontColor(Color.FromHex("9CA3AF"));
                    x.CurrentPageNumber().FontSize(9).FontColor(Color.FromHex("9CA3AF"));
                    x.Span(" von ").FontSize(9).FontColor(Color.FromHex("9CA3AF"));
                    x.TotalPages().FontSize(9).FontColor(Color.FromHex("9CA3AF"));
                });
            });
        }).GeneratePdf(filePath);
    }
}

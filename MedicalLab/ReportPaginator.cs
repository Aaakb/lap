using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MedicalLab;

// Physical printing and PDF export use exactly the same page decoration.
internal sealed class ReportPaginator(DocumentPaginator source, LabSettings settings, Thickness margins, bool repeatColumns) : DocumentPaginator
{
    public override bool IsPageCountValid => source.IsPageCountValid;
    public override int PageCount => source.PageCount;
    public override Size PageSize { get => source.PageSize; set => source.PageSize = value; }
    public override IDocumentPaginatorSource Source => source.Source;
    public override void ComputePageCount() => source.ComputePageCount();

    public override DocumentPage GetPage(int pageNumber)
    {
        var page = source.GetPage(pageNumber);
        if (page == DocumentPage.Missing) return page;
        var visual = new DrawingVisual();
        var width = page.Size.Width; var height = page.Size.Height;
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
            DrawWaves(dc, width, height);
            dc.DrawRectangle(new VisualBrush(page.Visual), null, new Rect(new Point(), page.Size));
            if (repeatColumns && pageNumber > 0) DrawColumnHeader(dc, width);
            DrawFooter(dc, width, height, pageNumber);
        }
        return new DocumentPage(visual, page.Size, page.BleedBox, page.ContentBox);
    }

    private static void DrawWaves(DrawingContext dc, double width, double height)
    {
        var top = new StreamGeometry();
        using (var g = top.Open())
        {
            g.BeginFigure(new Point(width * .57, 0), true, true);
            g.BezierTo(new Point(width * .77, 64), new Point(width * .91, 7), new Point(width, 78), true, false);
            g.LineTo(new Point(width, 0), true, false);
        }
        dc.DrawGeometry(new LinearGradientBrush(Color.FromRgb(240, 247, 252), Color.FromRgb(120, 177, 210), 0), null, top);
        var bottom = new StreamGeometry();
        using (var g = bottom.Open())
        {
            g.BeginFigure(new Point(0, height - 54), true, true);
            g.BezierTo(new Point(width * .24, height - 100), new Point(width * .48, height + 34), new Point(width, height - 72), true, false);
            g.LineTo(new Point(width, height), true, false); g.LineTo(new Point(0, height), true, false);
        }
        dc.DrawGeometry(new LinearGradientBrush(Color.FromRgb(207, 227, 242), Color.FromRgb(234, 244, 250), 0), null, bottom);
        var ribbon = new StreamGeometry();
        using (var g = ribbon.Open())
        {
            g.BeginFigure(new Point(0, height - 30), true, true);
            g.BezierTo(new Point(width * .29, height - 74), new Point(width * .51, height + 62), new Point(width, height - 28), true, false);
            g.LineTo(new Point(width, height), true, false); g.LineTo(new Point(0, height), true, false);
        }
        dc.DrawGeometry(new LinearGradientBrush(Color.FromRgb(151, 195, 221), Color.FromRgb(214, 234, 246), 0), null, ribbon);
    }

    private void DrawFooter(DrawingContext dc, double width, double height, int pageNumber)
    {
        var left = margins.Left; var available = Math.Max(1, width - margins.Left - margins.Right);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(176, 200, 219)), 1), new Point(left, height - 85), new Point(width - margins.Right, height - 85));
        var contact = string.Join("   |   ", new[] { settings.Address, settings.Phone }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (contact.Length > 0) DrawText(dc, contact, left, height - 76, available, 14, FlowDirection.RightToLeft, TextAlignment.Center);
        DrawText(dc, $"Page {pageNumber + 1} of {PageCount}", left, height - 32, available, 11, FlowDirection.LeftToRight, TextAlignment.Center);
    }

    private void DrawColumnHeader(DrawingContext dc, double width)
    {
        var available = width - margins.Left - margins.Right;
        var left = margins.Left;
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(223, 237, 247)), null, new Rect(left, 5, available, 25));
        var total = LabReport.ColumnWeights.Sum();
        for (int i = 0; i < LabReport.Headings.Length; i++)
        {
            var cell = available * LabReport.ColumnWeights[i] / total;
            DrawText(dc, LabReport.Headings[i], left + 9, 8, Math.Max(1, cell - 18), 13, FlowDirection.LeftToRight, i == 0 ? TextAlignment.Left : TextAlignment.Center);
            left += cell;
        }
    }

    private static void DrawText(DrawingContext dc, string text, double x, double y, double width, double size, FlowDirection direction, TextAlignment alignment)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, direction, new Typeface("Segoe UI"), size, new SolidColorBrush(Color.FromRgb(31, 62, 89)), 2)
        { MaxTextWidth = width, TextAlignment = alignment, MaxLineCount = 2, Trimming = TextTrimming.CharacterEllipsis };
        dc.DrawText(formatted, new Point(x, y));
    }
}

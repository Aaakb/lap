using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MedicalLab;

public static class LabReport
{
    internal static readonly double[] ColumnWeights = { 2.8, 1.0, 1.2, 1.8, 0.8 };
    internal static readonly string[] Headings = { "Test", "Result", "Unit", "Reference Range", "Flag" };
    private static readonly Brush Ink = ColorBrush("#142F4C");
    private static readonly Brush Blue = ColorBrush("#286B9C");
    private static readonly Brush Line = ColorBrush("#CEDDE9");
    private static readonly Lazy<BitmapSource> Letterhead = new(LoadLetterhead);

    private sealed class ReportDocument : FlowDocument
    {
        public required LabSettings Settings { get; init; }
        public bool RepeatColumns { get; init; }
    }

    public static FlowDocument Build(OrderReportData report) => Build(report.Settings, report.Order, report.Results, report.Pending);

    public static FlowDocument Build(LabSettings settings, LabOrder order, IReadOnlyCollection<LabResult> rows, IReadOnlyCollection<string>? pending = null)
    {
        ReportSelection.Validate(rows);
        var first = rows.First();
        var doc = CreateDocument(settings, true);
        doc.Blocks.Add(Header(settings, order));
        doc.Blocks.Add(PatientCard(first.PatientId, first.PatientName, first.PatientSex, first.BirthDate, order.CreatedAt));
        doc.Blocks.Add(SectionHeading("Laboratory Test Results"));
        if (pending is { Count: > 0 }) AddPending(doc, pending);
        doc.Blocks.Add(ResultsTable(rows));
        AddReferenceNotice(doc);
        return doc;
    }

    public static FlowDocument BuildHistories(LabSettings settings, IReadOnlyCollection<PatientHistory> histories)
    {
        if (histories.Count == 0) throw new ArgumentException("اختر مراجعاً واحداً على الأقل.");
        var doc = CreateDocument(settings, false);
        bool first = true;
        foreach (var history in histories)
        {
            var header = Header(settings, null);
            header.BreakPageBefore = !first;
            first = false;
            doc.Blocks.Add(header);
            doc.Blocks.Add(new Paragraph(new Run("سجل المراجع")) { FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Blue, KeepWithNext = true });
            var patient = history.Patient;
            doc.Blocks.Add(PatientCard(patient.Id, patient.Name, patient.Sex, patient.BirthDate, DateTime.Today));
            if (history.Orders.Count == 0) doc.Blocks.Add(new Paragraph(new Run("لا توجد طلبات مسجلة.")));
            foreach (var visit in history.Orders)
            {
                doc.Blocks.Add(SectionHeading($"Order {visit.Order.PatientOrderNumber}  |  {visit.Order.CreatedAt:yyyy-MM-dd}"));
                if (visit.Results.Count == 0) doc.Blocks.Add(new Paragraph(new Run("لا توجد نتائج محفوظة.")));
                else doc.Blocks.Add(ResultsTable(visit.Results));
                if (visit.Pending.Count > 0) AddPending(doc, visit.Pending);
            }
            AddReferenceNotice(doc);
        }
        return doc;
    }

    private static ReportDocument CreateDocument(LabSettings settings, bool repeatColumns) => new()
    {
        Settings = settings, RepeatColumns = repeatColumns, FlowDirection = FlowDirection.RightToLeft,
        FontFamily = new FontFamily("Segoe UI"), FontSize = 17, Foreground = Ink,
        PageWidth = 793.700787, PageHeight = 1122.519685,
        PagePadding = new Thickness(30, 36, 30, 100), ColumnWidth = double.PositiveInfinity
    };

    private static BlockUIContainer Header(LabSettings settings, LabOrder? order)
    {
        var content = new StackPanel { FlowDirection = FlowDirection.LeftToRight };
        if (settings.Logo is not { Length: > 0 } && settings.Name == new LabSettings().Name)
        {
            content.Children.Add(new Image { Source = Letterhead.Value, Height = 195, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Stretch });
        }
        else
        {
            var brand = new Grid { Margin = new Thickness(0, 8, 0, 18) };
            brand.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.Logo is null ? 0 : 95) });
            brand.ColumnDefinitions.Add(new ColumnDefinition());
            if (settings.Logo is { Length: > 0 }) brand.Children.Add(new Image { Source = LabLogo.Decode(settings.Logo), Width = 78, Height = 84, Stretch = Stretch.Uniform });
            var name = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            name.Children.Add(new TextBlock { Text = settings.Name, FontSize = 32, FontWeight = FontWeights.Bold, Foreground = Ink, FlowDirection = FlowDirection.RightToLeft, TextWrapping = TextWrapping.Wrap });
            name.Children.Add(new TextBlock { Text = "Clinical Analysis Report", FontSize = 18, Foreground = Blue, Margin = new Thickness(0, 5, 0, 0) });
            Grid.SetColumn(name, 1); brand.Children.Add(name); content.Children.Add(brand);
        }
        var info = new Grid { Margin = new Thickness(0, 3, 0, 14) };
        info.ColumnDefinitions.Add(new ColumnDefinition()); info.ColumnDefinitions.Add(new ColumnDefinition());
        info.Children.Add(Field(order is null ? "Report:" : "Order No.:", order is null ? "Patient History" : order.PatientOrderNumber.ToString(CultureInfo.InvariantCulture)));
        var date = Field("Date:", (order?.CreatedAt ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        date.TextAlignment = TextAlignment.Right; Grid.SetColumn(date, 1); info.Children.Add(date); content.Children.Add(info);
        return new BlockUIContainer(content) { Margin = new Thickness(0) };
    }

    private static BlockUIContainer PatientCard(int id, string name, string sex, DateTime? birth, DateTime date)
    {
        var content = new StackPanel();
        content.Children.Add(new Border
        {
            Background = HeaderFill(), CornerRadius = new CornerRadius(10, 10, 0, 0), Padding = new Thickness(14, 9, 14, 9),
            Child = new TextBlock { Text = "بيانات المراجع  |  Patient Information", FontSize = 19, FontWeight = FontWeights.SemiBold, Foreground = Ink }
        });
        var fields = new Grid { FlowDirection = FlowDirection.LeftToRight, Margin = new Thickness(14, 9, 14, 10) };
        fields.ColumnDefinitions.Add(new ColumnDefinition()); fields.ColumnDefinitions.Add(new ColumnDefinition());
        fields.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); fields.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        void Add(TextBlock field, int column, int row) { field.Margin = new Thickness(4, 4, 4, 6); Grid.SetColumn(field, column); Grid.SetRow(field, row); fields.Children.Add(field); }
        Add(Field("الاسم:", name, true), 1, 0);
        Add(Field("Age / Gender:", AgeAt(birth, date) + " / " + Patient.SexInEnglish(sex)), 0, 0);
        Add(Field("رقم المراجع:", id.ToString("D6", CultureInfo.InvariantCulture), true), 1, 1);
        Add(Field("Date of Birth:", birth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—"), 0, 1);
        content.Children.Add(fields);
        return new BlockUIContainer(new Border
        {
            BorderBrush = Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10),
            Background = ColorBrush("#F8FBFE"), Child = content
        }) { Margin = new Thickness(0, 0, 0, 5) };
    }

    private static TextBlock Field(string label, string value, bool rtl = false)
    {
        var text = new TextBlock { FontSize = 16, Foreground = Ink, TextWrapping = TextWrapping.Wrap, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Margin = new Thickness(0, 3, 0, 3) };
        text.Inlines.Add(new Run(label + "  ") { FontWeight = FontWeights.SemiBold }); text.Inlines.Add(new Run(value));
        return text;
    }

    private static string AgeAt(DateTime? birth, DateTime date)
    {
        if (birth is null || birth.Value.Date > date.Date) return "—";
        var years = date.Year - birth.Value.Year;
        if (birth.Value.Date > date.Date.AddYears(-years)) years--;
        return years.ToString(CultureInfo.InvariantCulture) + " Years";
    }

    private static Paragraph SectionHeading(string text) => new(new Run(text))
    {
        FlowDirection = FlowDirection.LeftToRight, TextAlignment = TextAlignment.Left, FontSize = 21,
        FontWeight = FontWeights.SemiBold, Foreground = Ink, Background = HeaderFill(),
        Padding = new Thickness(13, 9, 13, 10), Margin = new Thickness(0, 17, 0, 14), KeepWithNext = true
    };

    private static void AddPending(FlowDocument doc, IReadOnlyCollection<string> pending)
    {
        doc.Blocks.Add(new Paragraph(new Run($"تقرير جزئي — تحاليل بانتظار النتيجة: {pending.Count}")) { FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = ColorBrush("#955B12"), Margin = new Thickness(0, 0, 0, 5), KeepWithNext = true });
        doc.Blocks.Add(new Paragraph(new Run("Pending: " + string.Join(", ", pending))) { FontSize = 15, FlowDirection = FlowDirection.LeftToRight, TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 0, 0, 12), KeepWithNext = true });
    }

    private static void AddReferenceNotice(FlowDocument doc) => doc.Blocks.Add(new Paragraph(new Run("القيم المرجعية الحالية غير معتمدة - لا يستخدم لاتخاذ قرارات طبية"))
    { FontSize = 12, Foreground = ColorBrush("#8C4C4C"), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 12, 0, 0) });

    private static Table ResultsTable(IEnumerable<LabResult> rows)
    {
        var table = new Table { CellSpacing = 0, FlowDirection = FlowDirection.LeftToRight, TextAlignment = TextAlignment.Left };
        foreach (var width in ColumnWeights) table.Columns.Add(new TableColumn { Width = new GridLength(width, GridUnitType.Star) });
        var group = new TableRowGroup(); table.RowGroups.Add(group); AddRow(group, Headings, true, false);
        foreach (var r in rows)
        {
            var flag = r.FlagDisplay; var abnormal = flag is "High" or "Low" or "Outside reference";
            if (flag is "Within range" or "Matches reference" or "Descriptive" || string.IsNullOrWhiteSpace(flag)) flag = "—";
            var value = r.IsText ? r.TextValue : r.Value.ToString(CultureInfo.InvariantCulture);
            var reference = r.IsText ? r.ExpectedText : r.Low.ToString(CultureInfo.InvariantCulture) + " – " + r.High.ToString(CultureInfo.InvariantCulture);
            AddRow(group, new[] { r.TestName, value, r.Unit, reference, flag }, false, abnormal);
        }
        return table;
    }

    private static void AddRow(TableRowGroup group, string[] values, bool header, bool abnormal)
    {
        var row = new TableRow();
        for (int i = 0; i < values.Length; i++)
        {
            var paragraph = new Paragraph(new Run(values[i])) { Margin = new Thickness(0), TextAlignment = i == 0 ? TextAlignment.Left : TextAlignment.Center };
            row.Cells.Add(new TableCell(paragraph)
            {
                FontSize = header ? 16 : 17, FontWeight = header || i == 1 ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = abnormal && (i == 1 || i == 4) ? ColorBrush("#AF2839") : Ink,
                Padding = new Thickness(9, header ? 11 : 8, 9, header ? 11 : 8),
                BorderBrush = Line, BorderThickness = new Thickness(i == 0 ? 1 : 0, header ? 1 : 0, 1, 1),
                Background = header ? HeaderFill() : group.Rows.Count % 2 == 0 ? ColorBrush("#F7FAFD") : Brushes.White
            });
        }
        group.Rows.Add(row);
    }

    private static Brush HeaderFill() => new LinearGradientBrush(Color.FromRgb(230, 240, 248), Color.FromRgb(201, 222, 238), 0);
    private static SolidColorBrush ColorBrush(string color) { var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); return brush; }

    private static BitmapSource LoadLetterhead()
    {
        using var stream = typeof(LabReport).Assembly.GetManifestResourceStream("MedicalLab.Assets.letterhead.png")
            ?? throw new InvalidOperationException("ملف ترويسة المختبر غير متوفر.");
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream; image.EndInit(); image.Freeze();
        return image;
    }

    public static DocumentPaginator GetPaginator(FlowDocument document)
    {
        var source = ((IDocumentPaginatorSource)document).DocumentPaginator; source.ComputePageCount();
        var report = document as ReportDocument;
        return new ReportPaginator(source, report?.Settings ?? new LabSettings(), document.PagePadding, report?.RepeatColumns ?? false);
    }

    public static void SavePdf(FlowDocument document, string path)
    {
        var paginator = GetPaginator(document);
        using var pdf = new PdfDocument(); pdf.Info.Title = "Clinical Analysis Report";
        for (int i = 0; i < paginator.PageCount; i++)
        {
            using var page = paginator.GetPage(i); var visual = new DrawingVisual();
            using (var drawing = visual.RenderOpen()) drawing.DrawRectangle(new VisualBrush(page.Visual), null, new Rect(new Point(), page.Size));
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(page.Size.Width * 2), (int)Math.Ceiling(page.Size.Height * 2), 192, 192, PixelFormats.Pbgra32); bitmap.Render(visual);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = new MemoryStream(); encoder.Save(stream); stream.Position = 0;
            using var image = XImage.FromStream(stream); var pdfPage = pdf.AddPage(); pdfPage.Width = XUnit.FromPoint(page.Size.Width * 0.75); pdfPage.Height = XUnit.FromPoint(page.Size.Height * 0.75);
            using var graphics = XGraphics.FromPdfPage(pdfPage); graphics.DrawImage(image, 0, 0, pdfPage.Width.Point, pdfPage.Height.Point);
        }
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { pdf.Save(temporary); File.Move(temporary, path, true); } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

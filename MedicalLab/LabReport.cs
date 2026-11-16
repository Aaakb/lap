using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
namespace MedicalLab;
public static class LabReport
{
 public static FlowDocument Build(LabSettings settings,LabOrder order,IReadOnlyCollection<LabResult> rows)
 {
  ReportSelection.Validate(rows); var first=rows.First();
  var doc=new FlowDocument {FlowDirection=FlowDirection.RightToLeft,FontFamily=new FontFamily("Segoe UI"),FontSize=16,PageWidth=793.7,PageHeight=1122.5,PagePadding=new Thickness(42,42,42,55),ColumnWidth=double.PositiveInfinity};
  if(settings.Logo is { Length: > 0 }) {
   var image=new BitmapImage(); using var source=new MemoryStream(settings.Logo); image.BeginInit(); image.CacheOption=BitmapCacheOption.OnLoad; image.StreamSource=source; image.EndInit(); image.Freeze();
   var logo=new System.Windows.Controls.Image {Source=image,Width=95,Height=70,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Right};
   doc.Blocks.Add(new BlockUIContainer(logo) {TextAlignment=TextAlignment.Right});
  }
  doc.Blocks.Add(new Paragraph(new Run(settings.Name)) {FontSize=28,FontWeight=FontWeights.Bold,Foreground=new SolidColorBrush(Color.FromRgb(21,86,90))});
  doc.Blocks.Add(new Paragraph(new Run(settings.Address+"   "+settings.Phone)) {FontSize=15,Foreground=Brushes.DimGray,Margin=new Thickness(0,0,0,12)});
  doc.Blocks.Add(new Paragraph(new Run("تقرير تجريبي - لا يستخدم لاتخاذ قرارات طبية")) {Foreground=Brushes.DarkRed,FontWeight=FontWeights.Bold});
  doc.Blocks.Add(new Paragraph(new Run($"طلب {order.PatientOrderNumber} | {order.CreatedAt:yyyy-MM-dd}\nالمراجع: {first.PatientName} | الجنس: {Patient.SexInEnglish(first.PatientSex)} | الميلاد: {first.BirthDate:yyyy-MM-dd}")) { FontSize=16, LineHeight=27, Background=new SolidColorBrush(Color.FromRgb(245,249,249)), Padding=new Thickness(12), Margin=new Thickness(0,12,0,18) });
  var table=new Table {CellSpacing=0,FlowDirection=FlowDirection.LeftToRight,TextAlignment=TextAlignment.Left}; foreach(var width in new[]{2.5,1.3,1.1,1.8,1.7}) table.Columns.Add(new TableColumn {Width=new GridLength(width,GridUnitType.Star)});
  var group=new TableRowGroup(); table.RowGroups.Add(group); AddRow(group,new[]{"Test","Result","Unit","Reference","Flag"},true);
  foreach(var r in rows) AddRow(group,new[]{r.TestName,r.ValueDisplay,r.Unit,r.ReferenceDisplay,r.FlagDisplay},false);
  doc.Blocks.Add(table); return doc;
 }
 private static void AddRow(TableRowGroup group,string[] values,bool header)
 {
  var row=new TableRow(); foreach(var value in values) row.Cells.Add(new TableCell(new Paragraph(new Run(value)) {Margin=new Thickness(0)}) {Padding=new Thickness(7,11,7,11),BorderBrush=Brushes.LightGray,BorderThickness=new Thickness(0,0,0,1),Background=header?new SolidColorBrush(Color.FromRgb(230,240,240)):Brushes.White,FontWeight=header?FontWeights.Bold:FontWeights.Normal}); group.Rows.Add(row);
 }
 public static void SavePdf(FlowDocument document,string path)
 {
  var paginator=((IDocumentPaginatorSource)document).DocumentPaginator; paginator.ComputePageCount();
  using var pdf=new PdfDocument(); pdf.Info.Title="Laboratory demo report";
  for(int i=0;i<paginator.PageCount;i++) {
   using var page=paginator.GetPage(i); var visual=new DrawingVisual();
   using(var drawing=visual.RenderOpen()) { drawing.DrawRectangle(Brushes.White,null,new Rect(new Point(),page.Size)); drawing.DrawRectangle(new VisualBrush(page.Visual),null,new Rect(new Point(),page.Size)); var footer=new FormattedText($"Page {i+1} of {paginator.PageCount}",System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),10,Brushes.Gray,2); drawing.DrawText(footer,new Point(48,page.Size.Height-28)); }
   var bitmap=new RenderTargetBitmap((int)Math.Ceiling(page.Size.Width*2),(int)Math.Ceiling(page.Size.Height*2),192,192,PixelFormats.Pbgra32); bitmap.Render(visual);
   var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream=new MemoryStream(); encoder.Save(stream); stream.Position=0;
   using var image=XImage.FromStream(stream); var pdfPage=pdf.AddPage(); pdfPage.Width=XUnit.FromPoint(page.Size.Width*0.75); pdfPage.Height=XUnit.FromPoint(page.Size.Height*0.75);
   using var graphics=XGraphics.FromPdfPage(pdfPage); graphics.DrawImage(image,0,0,pdfPage.Width.Point,pdfPage.Height.Point);
  }
  // Write completely before replacing an existing report.
  var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try { pdf.Save(temporary); File.Move(temporary,path,true); } finally { if(File.Exists(temporary)) File.Delete(temporary); }
 }
}

using MedicalLab;
using System.Windows;
using System.Windows.Documents;
namespace MedicalLab.Tests;
public class PdfExportTests
{
 [Fact] public void ArabicIdentityAndEnglishTableProduceA4Pdf()
 {
  Exception? error=null; var thread=new Thread(()=> {
   try {
    var rows=new[]{new LabResult { PatientId=1,PatientName="مريض تجريبي",PatientSex="ذكر",BirthDate=new DateTime(2000,1,1),TestName="Glucose (DEMO)",Value=22,Low=20,High=30,Unit="demo unit",Status="ضمن النطاق"},new LabResult {PatientId=1,PatientName="مريض تجريبي",PatientSex="ذكر",BirthDate=new DateTime(2000,1,1),IsText=true,TestName="Screening (DEMO)",TextValue="Negative",ExpectedText="Negative",Status="مطابق للمرجع"}};
    var doc=LabReport.Build(new LabSettings {Name="مختبر الأصمعي",Address="بغداد",Phone="0000000000"},new LabOrder {Id=1,CreatedAt=DateTime.Now,Approved=true},rows);
    var table=doc.Blocks.OfType<Table>().Single(); Assert.Equal(FlowDirection.LeftToRight,table.FlowDirection);
    var path=Path.Combine(AppContext.BaseDirectory,"report-preview.pdf"); LabReport.SavePdf(doc,path);
    using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(path); Assert.Single(pdf.Pages.Cast<PdfSharp.Pdf.PdfPage>()); Assert.InRange(pdf.Pages[0].Width.Point,595,596);
    var longDoc=LabReport.Build(new LabSettings(),new LabOrder(),Enumerable.Range(0,100).Select(i=>new LabResult {PatientId=1,TestName="Long test name "+i,Value=i,Unit="unit"}).ToArray()); var longPath=Path.Combine(AppContext.BaseDirectory,"report-long-preview.pdf"); LabReport.SavePdf(longDoc,longPath); using var longPdf=PdfSharp.Pdf.IO.PdfReader.Open(longPath); Assert.True(longPdf.PageCount>1);
   } catch(Exception ex) {error=ex;}
  }); thread.SetApartmentState(ApartmentState.STA); thread.Start(); Assert.True(thread.Join(TimeSpan.FromSeconds(30))); if(error is not null) throw error;
 }
}

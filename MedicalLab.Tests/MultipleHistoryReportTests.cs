using MedicalLab;
using System.Windows.Documents;
namespace MedicalLab.Tests;
public class MultipleHistoryReportTests
{
 [Fact] public void EachSelectedPersonStartsSeparatePage()
 {
  Exception? failure=null;var worker=new Thread(()=>{try{var a=new PatientHistory(new Patient{Id=1,Name="أول"},new());var b=new PatientHistory(new Patient{Id=2,Name="ثان"},new());var doc=LabReport.BuildHistories(new LabSettings(),new[]{a,b});Assert.Equal(2,doc.Blocks.OfType<Paragraph>().Count(p=>new TextRange(p.ContentStart,p.ContentEnd).Text.Contains("سجل المراجع")));var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".pdf");try{LabReport.SavePdf(doc,path);using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(path);Assert.Equal(2,pdf.PageCount);}finally{File.Delete(path);}}catch(Exception ex){failure=ex;}});worker.SetApartmentState(ApartmentState.STA);worker.Start();Assert.True(worker.Join(TimeSpan.FromSeconds(30)));if(failure is not null)throw failure;
 }
}

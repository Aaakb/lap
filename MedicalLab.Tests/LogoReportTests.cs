using MedicalLab;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MedicalLab.Tests;
public class LogoReportTests
{
 [Fact] public void ReportEmbedsLogoAndExportsPdf()
 {
  Exception? failure=null; var thread=new Thread(()=> {try {
   var bitmap=BitmapSource.Create(2,2,96,96,PixelFormats.Bgra32,null,new byte[]{0,0,0,255,0,0,0,255,0,0,0,255,0,0,0,255},8);
   var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var bytes=new MemoryStream();encoder.Save(bytes);
   var doc=LabReport.Build(new LabSettings{Logo=bytes.ToArray()},new LabOrder(),new[]{new LabResult{PatientId=1,TestName="Demo"}});
   Assert.Single(doc.Blocks.OfType<BlockUIContainer>());var output=Path.Combine(Path.GetTempPath(),$"logo-{Guid.NewGuid()}.pdf");try{LabReport.SavePdf(doc,output);Assert.True(new FileInfo(output).Length>1000);}finally{File.Delete(output);}
  }catch(Exception ex){failure=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(30)));if(failure!=null)throw failure;
 }
}

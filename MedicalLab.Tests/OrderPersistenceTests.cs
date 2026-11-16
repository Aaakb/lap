using MedicalLab;
namespace MedicalLab.Tests;
public class OrderPersistenceTests
{
 [Fact] public void RequestsKeepTheirOwnResultsAcrossReopening()
 {
  var path=Path.Combine(Path.GetTempPath(), $"lab-orders-{Guid.NewGuid()}.db");
  try {
   int firstId;
   using(var db=new LabContext(path)) {
    db.Initialize(); var p=Patient.Create("مريض",new DateTime(2000,1,1),"ذكر"); db.Patients.Add(p); db.SaveChanges();
    var first=new LabOrder { PatientId=p.Id, PatientName=p.Name, CreatedAt=DateTime.Now }; var second=new LabOrder {PatientId=p.Id,PatientName=p.Name,CreatedAt=DateTime.Now}; db.LabOrders.AddRange(first,second); db.SaveChanges(); firstId=first.Id;
    var a=new LabResult { PatientId=p.Id,TestName="A" }; var b=new LabResult { PatientId=p.Id,TestName="B" }; db.LabResults.AddRange(a,b); db.SaveChanges(); db.OrderResults.AddRange(new OrderResult {LabOrderId=first.Id,LabResultId=a.Id},new OrderResult {LabOrderId=second.Id,LabResultId=b.Id}); db.SaveChanges();
   }
   using(var db=new LabContext(path)) { db.Initialize(); var ids=db.OrderResults.Where(x=>x.LabOrderId==firstId).Select(x=>x.LabResultId); var rows=db.LabResults.Where(x=>ids.Contains(x.Id)).ToList(); Assert.Single(rows); Assert.Equal("A",rows[0].TestName); }
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
 }
}

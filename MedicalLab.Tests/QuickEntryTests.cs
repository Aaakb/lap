using MedicalLab;
namespace MedicalLab.Tests;
public class QuickEntryTests
{
 [Fact] public void FirstResultCreatesOrderButInvalidResultDoesNot()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");try { using var db=new LabContext(path);db.Initialize();var p=Patient.Create("تجربة",new DateTime(2000,1,1),"ذكر");db.Patients.Add(p);db.SaveChanges();Assert.Throws<ArgumentException>(()=>OrderWorkflow.CreateOrderWithResult(db,p.Id,"Glucose (DEMO)","invalid"));Assert.Empty(db.LabOrders);var id=OrderWorkflow.CreateOrderWithResult(db,p.Id,"Glucose (DEMO)","22");Assert.Equal(p.Id,db.LabOrders.Single(o=>o.Id==id).PatientId);Assert.Single(db.LabResults); } finally {Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

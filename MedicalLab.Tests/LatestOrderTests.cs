using MedicalLab;
namespace MedicalLab.Tests;
public class LatestOrderTests
{
 [Fact] public void LatestBelongsToSelectedPatientAndEmptyIsNull()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");try{using var db=new LabContext(path);db.Initialize();var first=new LabOrder{PatientId=1,PatientName="A"};db.LabOrders.Add(first);db.SaveChanges();var latest=new LabOrder{PatientId=1,PatientName="A"};db.LabOrders.Add(latest);db.SaveChanges();db.LabOrders.Add(new LabOrder{PatientId=2,PatientName="B"});db.SaveChanges();Assert.Equal(latest.Id,OrderWorkflow.LatestOrderForPatient(db,1)?.Id);Assert.Null(OrderWorkflow.LatestOrderForPatient(db,3));Assert.Null(OrderWorkflow.LatestOrderForPatient(db,null));}finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

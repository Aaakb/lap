using MedicalLab;
namespace MedicalLab.Tests;
public class PlannedSelectionTests
{
 [Fact] public void AddingTestsIsIdempotentAndBlockedAfterApproval()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");
  try { using var db=new LabContext(path); db.Initialize(); var patient=Patient.Create("مريض",new DateTime(2000,1,1),"ذكر"); db.Patients.Add(patient); db.SaveChanges(); var order=new LabOrder{PatientId=patient.Id,PatientName=patient.Name,CreatedAt=DateTime.Now}; db.LabOrders.Add(order); db.SaveChanges(); OrderWorkflow.PlanTests(db,order.Id,new[]{"Glucose (DEMO)","Glucose (DEMO)"}); OrderWorkflow.PlanTests(db,order.Id,new[]{"Glucose (DEMO)"}); Assert.Single(db.PlannedTests); Assert.Throws<ArgumentException>(()=>OrderWorkflow.PlanTests(db,order.Id,new[]{"missing"})); OrderWorkflow.SaveResult(db,order.Id,"Glucose (DEMO)","22"); OrderWorkflow.SetApproval(db,order.Id,true); Assert.Throws<InvalidOperationException>(()=>OrderWorkflow.PlanTests(db,order.Id,new[]{"Hemoglobin (DEMO)"})); }
  finally {Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

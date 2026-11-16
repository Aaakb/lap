using MedicalLab;
namespace MedicalLab.Tests;
public class WorkflowIntegrationTests
{
 [Fact] public void PanelApprovalCorrectionAndAuditWorkAcrossReopening()
 {
  var path=Path.Combine(Path.GetTempPath(),$"workflow-{Guid.NewGuid()}.db");
  try {
   int orderId;
   using(var db=new LabContext(path)) {
    db.Initialize(); var patient=Patient.Create("تجربة",new DateTime(2000,1,1),"ذكر"); db.Patients.Add(patient); db.SaveChanges();
    var order=new LabOrder {PatientId=patient.Id,PatientName=patient.Name,CreatedAt=DateTime.Now}; var panel=new PanelDefinition {Name="مجموعة",TestNames="Glucose (DEMO)\nHemoglobin (DEMO)"}; db.LabOrders.Add(order); db.Panels.Add(panel); db.SaveChanges(); orderId=order.Id;
    OrderWorkflow.ApplyPanel(db,orderId,panel.Id); OrderWorkflow.ApplyPanel(db,orderId,panel.Id); Assert.Equal(2,db.PlannedTests.Count());
    OrderWorkflow.SaveResult(db,orderId,"Glucose (DEMO)","22"); Assert.Throws<ArgumentException>(()=>OrderWorkflow.SetApproval(db,orderId,true));
    OrderWorkflow.SaveResult(db,orderId,"Hemoglobin (DEMO)","10"); OrderWorkflow.SetApproval(db,orderId,true);
    Assert.Throws<InvalidOperationException>(()=>OrderWorkflow.SaveResult(db,orderId,"Glucose (DEMO)","25"));
    OrderWorkflow.SetApproval(db,orderId,false); OrderWorkflow.DeleteResult(db,orderId,db.LabResults.First().Id); Assert.Single(db.LabResults); Assert.Equal(2,db.PlannedTests.Count());
   }
   using(var db=new LabContext(path)) { db.Initialize(); Assert.False(db.LabOrders.Single().Approved); Assert.True(db.AuditEntries.Count()>=7); }
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
 }
}

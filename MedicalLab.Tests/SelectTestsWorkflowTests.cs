using MedicalLab;
namespace MedicalLab.Tests;
public class SelectTestsWorkflowTests
{
 [Fact] public void NewOrderAndTestsAreCreatedTogetherAndInvalidSelectionRollsBack()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");try{using var db=new LabContext(path);db.Initialize();var p=Patient.Create("تجربة",new DateTime(2000,1,1),"ذكر");db.Patients.Add(p);db.SaveChanges();Assert.Throws<ArgumentException>(()=>OrderWorkflow.CreateOrderWithTests(db,p.Id,new[]{"missing"}));Assert.Empty(db.LabOrders);var id=OrderWorkflow.CreateOrderWithTests(db,p.Id,new[]{"Glucose (DEMO)","Hemoglobin (DEMO)"});Assert.Equal(2,db.PlannedTests.Count(t=>t.LabOrderId==id));}finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

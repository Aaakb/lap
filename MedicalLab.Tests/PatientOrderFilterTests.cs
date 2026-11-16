using MedicalLab;
namespace MedicalLab.Tests;
public class PatientOrderFilterTests
{
 [Fact] public void ListsOnlySelectedPatientsOrdersAndNoneWithoutPatient()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");
  try {using var db=new LabContext(path);db.Initialize();db.LabOrders.AddRange(new LabOrder{PatientId=1,PatientName="A"},new LabOrder{PatientId=2,PatientName="B"},new LabOrder{PatientId=1,PatientName="A"});db.SaveChanges();Assert.Equal(2,OrderWorkflow.OrdersForPatient(db,1).Count);Assert.Single(OrderWorkflow.OrdersForPatient(db,2));Assert.Empty(OrderWorkflow.OrdersForPatient(db,null));}
  finally {Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

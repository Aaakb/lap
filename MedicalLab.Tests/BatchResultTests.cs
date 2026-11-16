using MedicalLab;
namespace MedicalLab.Tests;
public class BatchResultTests
{
 [Fact] public void InvalidEntryRollsBackEveryResult()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");
  try {using var db=new LabContext(path);db.Initialize();var p=Patient.Create("تجربة",new DateTime(2000,1,1),"ذكر");db.Patients.Add(p);db.SaveChanges();var o=new LabOrder{PatientId=p.Id,PatientName=p.Name,CreatedAt=DateTime.Now};db.LabOrders.Add(o);db.SaveChanges();OrderWorkflow.PlanTests(db,o.Id,new[]{"Glucose (DEMO)","Hemoglobin (DEMO)"});Assert.Throws<ArgumentException>(()=>OrderWorkflow.SaveResults(db,o.Id,new Dictionary<string,string>{{"Glucose (DEMO)","22"},{"Hemoglobin (DEMO)","invalid"}}));Assert.Empty(db.LabResults);OrderWorkflow.SaveResults(db,o.Id,new Dictionary<string,string>{{"Glucose (DEMO)","22"},{"Hemoglobin (DEMO)","10"}});Assert.Equal(2,db.LabResults.Count());}
  finally {Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

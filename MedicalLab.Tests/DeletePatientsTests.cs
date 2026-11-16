using MedicalLab;
namespace MedicalLab.Tests;
public class DeletePatientsTests
{
 [Fact] public void DeletesSelectedPatientsAndRelatedRecordsOnly()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");try{using var db=new LabContext(path);db.Initialize();var a=Patient.Create("أ",new DateTime(2000,1,1),"ذكر");var b=Patient.Create("ب",new DateTime(2000,1,1),"ذكر");db.Patients.AddRange(a,b);db.SaveChanges();var oa=OrderWorkflow.CreateOrderWithResult(db,a.Id,"Glucose (DEMO)","22");var ob=OrderWorkflow.CreateOrderWithResult(db,b.Id,"Glucose (DEMO)","23");PatientDeletion.Delete(db,new[]{a.Id});Assert.Null(db.Patients.Find(a.Id));Assert.NotNull(db.Patients.Find(b.Id));Assert.DoesNotContain(db.LabOrders,x=>x.Id==oa);Assert.Contains(db.LabOrders,x=>x.Id==ob);Assert.DoesNotContain(db.LabResults,x=>x.PatientId==a.Id);Assert.Contains(db.LabResults,x=>x.PatientId==b.Id);Assert.DoesNotContain(db.PlannedTests,x=>x.LabOrderId==oa);Assert.DoesNotContain(db.OrderResults,x=>x.LabOrderId==oa);}
  finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

using MedicalLab;
using Microsoft.EntityFrameworkCore;
namespace MedicalLab.Tests;
public class PatientHistoryTests
{
 [Fact] public void HistoryContainsOnlySelectedPatientsOrdersAndResults()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");try{using var db=new LabContext(path);db.Initialize();var a=Patient.Create("أول",new DateTime(2000,1,1),"ذكر");var b=Patient.Create("ثان",new DateTime(2000,1,1),"ذكر");db.Patients.AddRange(a,b);db.SaveChanges();var id=OrderWorkflow.CreateOrderWithResult(db,a.Id,"Glucose (DEMO)","22");OrderWorkflow.CreateOrderWithResult(db,b.Id,"Glucose (DEMO)","23");var history=PatientHistory.Load(db,a.Id);Assert.Equal(a.Id,history.Patient.Id);Assert.Single(history.Orders);Assert.Equal(id,history.Orders[0].Order.Id);Assert.Single(history.Orders[0].Results);Assert.Equal(22,history.Orders[0].Results[0].Value);}
  finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

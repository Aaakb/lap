using MedicalLab;
namespace MedicalLab.Tests;
public class PatientOrderNumberTests
{
 [Fact] public void EachPatientHasIndependentStableNumbers()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");try {using(var db=new LabContext(path)){db.Initialize();db.LabOrders.AddRange(new LabOrder{PatientId=1,CreatedAt=new DateTime(2026,1,2,15,30,0)},new LabOrder{PatientId=2},new LabOrder{PatientId=1});db.SaveChanges();var orders=db.LabOrders.OrderBy(x=>x.Id).ToList();Assert.Equal(new[]{1,2},orders.Where(x=>x.PatientId==1).Select(x=>x.PatientOrderNumber).Order().ToArray());Assert.Equal(1,orders.Single(x=>x.PatientId==2).PatientOrderNumber);var dated=orders.Single(x=>x.CreatedAt.Year==2026);Assert.Equal($"طلب {dated.PatientOrderNumber} — 2026-01-02",dated.Label);}using(var db=new LabContext(path)){db.Initialize();Assert.Equal(new[]{1,2},db.LabOrders.Where(x=>x.PatientId==1).OrderBy(x=>x.PatientOrderNumber).Select(x=>x.PatientOrderNumber).ToArray());}}finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

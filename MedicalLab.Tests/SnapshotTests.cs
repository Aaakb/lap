using MedicalLab;
namespace MedicalLab.Tests;
public class SnapshotTests
{
 [Fact] public void SavedResultKeepsItsRangeWhenCatalogChanges()
 {
  var path=Path.Combine(Path.GetTempPath(), $"lab-snapshot-{Guid.NewGuid()}.db");
  try {
   using(var db=new LabContext(path)) { db.Initialize(); var t=db.LabTests.First(); db.LabResults.Add(new LabResult { PatientName="تجربة", TestName=t.Name, Unit=t.Unit, Low=t.Low, High=t.High, PatientSex="ذكر", CreatedAt=DateTime.Now }); db.SaveChanges(); t.Low=999; db.SaveChanges(); }
   using(var db=new LabContext(path)) { Assert.NotEqual(999, db.LabResults.Single().Low); }
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
 }
}

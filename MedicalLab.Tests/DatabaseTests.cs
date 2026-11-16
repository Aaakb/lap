using MedicalLab;
using Microsoft.EntityFrameworkCore;
namespace MedicalLab.Tests;
public class DatabaseTests
{
 [Fact] public void InitializesExistingPatientDatabaseWithoutLosingDataAndSeedsOnce()
 {
  var path=Path.Combine(Path.GetTempPath(), $"lab-test-{Guid.NewGuid()}.db");
  try {
   using (var db=new LabContext(path)) { db.Database.OpenConnection(); db.Database.ExecuteSqlRaw("CREATE TABLE Patients (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, BirthDate TEXT NULL, Sex TEXT NOT NULL)"); db.Patients.Add(Patient.Create("مريض تجريبي", null, "ذكر")); db.SaveChanges(); }
   using (var db=new LabContext(path)) { db.Initialize(); Assert.Single(db.Patients); Assert.Equal(3, db.LabTests.Count()); db.LabTests.RemoveRange(db.LabTests); db.SaveChanges(); }
   using (var db=new LabContext(path)) { db.Initialize(); Assert.Empty(db.LabTests); Assert.Single(db.Patients); }
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
 }
 [Fact] public void OverlappingRangesAreNotSelected()
 {
  var ranges=new[] { new LabTest { MinAge=0,MaxAge=120,Sex="الكل" },new LabTest { MinAge=0,MaxAge=120,Sex="ذكر" } };
  Assert.Null(RangeSelector.Find(ranges, new DateTime(2000,1,1), "ذكر", new DateTime(2026,1,1)));
 }
}

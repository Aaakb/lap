using MedicalLab;
using Microsoft.EntityFrameworkCore;
namespace MedicalLab.Tests;
public class TextPersistenceTests
{
 [Fact] public void ExistingNumericSchemaUpgradesAndTextSnapshotsSurviveReopening()
 {
  var path=Path.Combine(Path.GetTempPath(),$"text-results-{Guid.NewGuid()}.db");
  try {
   using(var db=new LabContext(path)) {
    db.Database.OpenConnection();
    db.Database.ExecuteSqlRaw("CREATE TABLE LabTests (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Unit TEXT NOT NULL, MinAge INTEGER NOT NULL, MaxAge INTEGER NOT NULL, Sex TEXT NOT NULL, Low REAL NOT NULL, High REAL NOT NULL, Active INTEGER NOT NULL)");
    db.Database.ExecuteSqlRaw("INSERT INTO LabTests VALUES (100,'Old','unit',0,120,'الكل',1,5,1)");
    db.Database.ExecuteSqlRaw("CREATE TABLE Patients (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, BirthDate TEXT, Sex TEXT NOT NULL)");
    db.Initialize(); Assert.False(db.LabTests.Single(t=>t.Id==100).IsText);
    var test=new LabTest {Name="Text demo",IsText=true,ExpectedText="Negative",MinAge=0,MaxAge=120}; db.LabTests.Add(test); db.SaveChanges();
    db.LabResults.Add(ResultEntry.Create(test,"Positive")); db.SaveChanges(); test.ExpectedText="Changed"; db.SaveChanges();
   }
   using(var db=new LabContext(path)) { db.Initialize(); var result=db.LabResults.Single(); Assert.True(result.IsText); Assert.Equal("Positive",result.ValueDisplay); Assert.Equal("Negative",result.ReferenceDisplay); }
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
 }
}

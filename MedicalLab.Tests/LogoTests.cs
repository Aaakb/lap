using MedicalLab;
namespace MedicalLab.Tests;
public class LogoTests
{
 [Fact] public void LogoSurvivesDatabaseReopenAndBackup()
 {
  var folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());Directory.CreateDirectory(folder);
  var dbPath=Path.Combine(folder,"live.db");var backup=Path.Combine(folder,"backup.db");
  try {
   using(var db=new LabContext(dbPath)) {db.Initialize();db.Settings.Single().Logo=new byte[]{1,2,3};db.SaveChanges();}
   DatabaseBackup.Create(dbPath,backup);
   using(var db=new LabContext(backup)) {db.Initialize();Assert.Equal(new byte[]{1,2,3},db.Settings.Single().Logo);}
  } finally {Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(folder,true);}
 }
}

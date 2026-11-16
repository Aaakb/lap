using MedicalLab;
namespace MedicalLab.Tests;
public class BackupTests
{
 [Fact] public void BackupAndRestoreRecoverSavedPatient()
 {
  var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()); Directory.CreateDirectory(root);
  var live=Path.Combine(root,"live.db"); var backup=Path.Combine(root,"backup.db");
  try {
   using(var db=new LabContext(live)) { db.Initialize(); db.Patients.Add(Patient.Create("قبل النسخ",null,"ذكر")); db.SaveChanges(); }
   DatabaseBackup.Create(live,backup);
   using(var db=new LabContext(live)) { db.Patients.RemoveRange(db.Patients); db.SaveChanges(); }
   DatabaseBackup.Restore(backup,live);
   using(var db=new LabContext(live)) Assert.Equal("قبل النسخ",db.Patients.Single().Name);
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root,true); }
 }
 [Fact] public void InvalidBackupLeavesLiveDataUntouched()
 {
  var root=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString()); Directory.CreateDirectory(root);
  var live=Path.Combine(root,"live.db"); var invalid=Path.Combine(root,"bad.db");
  try {
   using(var db=new LabContext(live)) { db.Initialize(); db.Patients.Add(Patient.Create("محفوظ",null,"ذكر")); db.SaveChanges(); }
   File.WriteAllText(invalid,"invalid");
   Assert.ThrowsAny<Exception>(()=>DatabaseBackup.Restore(invalid,live));
   using(var db=new LabContext(live)) Assert.Equal("محفوظ",db.Patients.Single().Name);
  } finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root,true); }
 }
}

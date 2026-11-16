using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
namespace MedicalLab;
public static class DatabaseBackup
{
 public static string LivePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AlAsmaiLab","lab.db");
 private static SqliteConnection Open(string path,SqliteOpenMode mode)
 {
  var connection=new SqliteConnection(new SqliteConnectionStringBuilder {DataSource=path,Mode=mode,Pooling=false}.ToString()); connection.Open(); return connection;
 }
 public static void Create(string source,string destination)
 {
  if(Path.GetFullPath(source).Equals(Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("اختر ملفاً مختلفاً عن القاعدة الحالية.");
  if(File.Exists(destination)) throw new IOException("اختر اسماً جديداً للنسخة الاحتياطية.");
  using var input=Open(source,SqliteOpenMode.ReadOnly);
  using var output=Open(destination,SqliteOpenMode.ReadWriteCreate);
  input.BackupDatabase(output);
 }
 public static void Restore(string source,string destination)
 {
  if(Path.GetFullPath(source).Equals(Path.GetFullPath(destination),StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("ملف النسخة هو القاعدة الحالية.");
  var staging=Path.GetFullPath(destination)+".restore-"+Guid.NewGuid().ToString("N")+".db";
  try
  {
   Create(source,staging);
   ValidateAndUpgrade(staging);
   if(File.Exists(destination)) Create(destination,destination+".before-restore-"+Guid.NewGuid().ToString("N")+".db");
   SqliteConnection.ClearAllPools();
   using var input=Open(staging,SqliteOpenMode.ReadOnly);
   using var output=Open(destination,SqliteOpenMode.ReadWriteCreate);
   // SQLite commits the destination write transaction only when the backup completes.
   input.BackupDatabase(output);
  }
  finally
  {
   SqliteConnection.ClearAllPools();
   foreach(var suffix in new[]{"","-wal","-shm","-journal"})
    if(File.Exists(staging+suffix)) File.Delete(staging+suffix);
  }
 }
 private static void ValidateAndUpgrade(string path)
 {
  // All checks and migrations operate on an isolated copy, never the selected backup.
  using(var input=Open(path,SqliteOpenMode.ReadOnly))
  {
  using(var check=input.CreateCommand()) { check.CommandText="PRAGMA quick_check"; if((string?)check.ExecuteScalar()!="ok") throw new InvalidDataException("النسخة غير سليمة."); }
  foreach(var table in new[] {"Patients","LabTests","LabResults","LabOrders","OrderResults","AppMetadata"})
  {
   using var check=input.CreateCommand(); check.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name"; check.Parameters.AddWithValue("$name",table);
   if(Convert.ToInt32(check.ExecuteScalar())!=1) throw new InvalidDataException("الملف ليس نسخة متوافقة مع البرنامج.");
  }
  foreach(var query in new[] { "SELECT Id,Name,BirthDate,Sex FROM Patients LIMIT 0", "SELECT Id,Name,Unit,MinAge,MaxAge,Sex,Low,High,Active FROM LabTests LIMIT 0", "SELECT Id,PatientId,PatientName,PatientSex,BirthDate,CreatedAt,TestName,Unit,Value,Low,High,Status FROM LabResults LIMIT 0", "SELECT Id,PatientId,PatientName,CreatedAt FROM LabOrders LIMIT 0", "SELECT Id,LabOrderId,LabResultId FROM OrderResults LIMIT 0", "SELECT Key,Value FROM AppMetadata LIMIT 0" }) { using var schemaCheck=input.CreateCommand(); schemaCheck.CommandText=query; using var reader=schemaCheck.ExecuteReader(); }
  }
  using var db=new LabContext(path);
  db.Initialize();
  var settings=db.Settings.AsNoTracking().Single();
  _=LabLogo.Decode(settings.Logo);
  ValidateRows(db.Patients); ValidateRows(db.LabTests); ValidateRows(db.LabResults);
  ValidateRows(db.LabOrders); ValidateRows(db.OrderResults); ValidateRows(db.Panels);
  ValidateRows(db.PlannedTests); ValidateRows(db.AuditEntries);
 }
 private static void ValidateRows<T>(IQueryable<T> rows) where T:class
 {
  // Streaming verifies every stored value without retaining the entire database in memory.
  foreach(var row in rows.AsNoTracking()) { }
 }
}
public class LabSettings
{
 public int Id {get;set;}=1;
 public string Name {get;set;}="مختبر الأصمعي";
 public string Address {get;set;}="";
 public string Phone {get;set;}="";
 public byte[]? Logo {get;set;}
}

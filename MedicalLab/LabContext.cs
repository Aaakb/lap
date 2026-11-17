using Microsoft.EntityFrameworkCore;
using System.IO;
namespace MedicalLab;
public class LabContext : DbContext
{
 private readonly string? databasePath;
 public LabContext(string? databasePath = null) { this.databasePath = databasePath; }
 public DbSet<PanelDefinition> Panels => Set<PanelDefinition>();
 public DbSet<PlannedTest> PlannedTests => Set<PlannedTest>();
 public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
 public DbSet<LabSettings> Settings => Set<LabSettings>();
 public DbSet<LabOrder> LabOrders => Set<LabOrder>();
 public DbSet<OrderResult> OrderResults => Set<OrderResult>();
 public DbSet<Patient> Patients => Set<Patient>();
 public DbSet<LabTest> LabTests => Set<LabTest>();
 public DbSet<LabResult> LabResults => Set<LabResult>();
 protected override void OnConfiguring(DbContextOptionsBuilder options)
 {
  var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AlAsmaiLab");
  Directory.CreateDirectory(directory);
  options.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = databasePath ?? Path.Combine(directory, "lab.db") }.ToString());
 }
 public override int SaveChanges()
 {
  foreach(var entry in ChangeTracker.Entries<LabOrder>().Where(e=>e.State==EntityState.Added && e.Entity.PatientAgeYears==null).ToList()) {
   var patient=Patients.Local.FirstOrDefault(p=>p.Id==entry.Entity.PatientId) ?? Patients.AsNoTracking().SingleOrDefault(p=>p.Id==entry.Entity.PatientId);
   if(patient is not null) entry.Entity.PatientAgeYears=PatientAge.At(patient,entry.Entity.CreatedAt.Date);
  }
  var additions=ChangeTracker.Entries<LabOrder>().Where(e=>e.State==EntityState.Added && e.Entity.PatientOrderNumber==0).Select(e=>e.Entity).ToList();
  foreach(var group in additions.GroupBy(o=>o.PatientId)) {
   var last=LabOrders.Where(o=>o.PatientId==group.Key).Select(o=>(int?)o.PatientOrderNumber).Max() ?? 0;
   foreach(var order in group) order.PatientOrderNumber=++last;
  }
  return base.SaveChanges();
 }
 public void Initialize()
 {
  Database.EnsureCreated();
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Patients (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, BirthDate TEXT NULL, Sex TEXT NOT NULL)");
  AddColumnIfMissing("Patients","AgeYears","INTEGER NULL");
  AddColumnIfMissing("Patients","RegisteredAt","TEXT NULL");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Settings (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Address TEXT NOT NULL, Phone TEXT NOT NULL)");
  AddColumnIfMissing("Settings","Logo","BLOB NULL");
  if(!Settings.Any()) { Settings.Add(new LabSettings()); SaveChanges(); }
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS LabOrders (Id INTEGER PRIMARY KEY AUTOINCREMENT, PatientId INTEGER NOT NULL, PatientName TEXT NOT NULL, CreatedAt TEXT NOT NULL)");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS OrderResults (Id INTEGER PRIMARY KEY AUTOINCREMENT, LabOrderId INTEGER NOT NULL, LabResultId INTEGER NOT NULL)");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS LabTests (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Unit TEXT NOT NULL, MinAge INTEGER NOT NULL, MaxAge INTEGER NOT NULL, Sex TEXT NOT NULL, Low REAL NOT NULL, High REAL NOT NULL, Active INTEGER NOT NULL)");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS LabResults (Id INTEGER PRIMARY KEY AUTOINCREMENT, PatientId INTEGER NOT NULL, PatientName TEXT NOT NULL, PatientSex TEXT NOT NULL, BirthDate TEXT NULL, CreatedAt TEXT NOT NULL, TestName TEXT NOT NULL, Unit TEXT NOT NULL, Value REAL NOT NULL, Low REAL NOT NULL, High REAL NOT NULL, Status TEXT NOT NULL)");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS AppMetadata (Key TEXT PRIMARY KEY, Value TEXT NOT NULL)");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Panels (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, TestNames TEXT NOT NULL)");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS PlannedTests (Id INTEGER PRIMARY KEY AUTOINCREMENT, LabOrderId INTEGER NOT NULL, TestName TEXT NOT NULL)");
  Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS AuditEntries (Id INTEGER PRIMARY KEY AUTOINCREMENT, At TEXT NOT NULL, Action TEXT NOT NULL, OrderId INTEGER NOT NULL)");
  AddColumnIfMissing("LabOrders","Approved","INTEGER NOT NULL DEFAULT 0");
  AddColumnIfMissing("LabOrders","PatientOrderNumber","INTEGER NOT NULL DEFAULT 0");
  Database.ExecuteSqlRaw("UPDATE LabOrders SET PatientOrderNumber=(SELECT COUNT(*) FROM LabOrders AS prior WHERE prior.PatientId=LabOrders.PatientId AND prior.Id<=LabOrders.Id) WHERE PatientOrderNumber=0");
  Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_LabOrders_Patient_Number ON LabOrders(PatientId,PatientOrderNumber)");
  AddColumnIfMissing("LabTests","IsText","INTEGER NOT NULL DEFAULT 0");
  AddColumnIfMissing("LabTests","ExpectedText","TEXT NOT NULL DEFAULT ''");
  AddColumnIfMissing("LabResults","IsText","INTEGER NOT NULL DEFAULT 0");
  AddColumnIfMissing("LabResults","TextValue","TEXT NOT NULL DEFAULT ''");
  AddColumnIfMissing("LabResults","ExpectedText","TEXT NOT NULL DEFAULT ''");
  AddColumnIfMissing("LabTests","TestKey","TEXT NOT NULL DEFAULT ''");
  AddColumnIfMissing("LabResults","TestKey","TEXT NOT NULL DEFAULT ''");
  AddColumnIfMissing("PlannedTests","TestKey","TEXT NOT NULL DEFAULT ''");
  AddColumnIfMissing("LabOrders","PatientAgeYears","INTEGER NULL");
  AddColumnIfMissing("LabResults","AgeYears","INTEGER NULL");
  TranslateDemoNames();
  if (Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM AppMetadata WHERE Key = 'DemoSeedV1'").Single() == 0)
  {
   using var transaction = Database.BeginTransaction();
   LabTests.AddRange(new LabTest { Name="Glucose (DEMO)", Unit="demo unit", MinAge=0, MaxAge=17, Sex="الكل", Low=10, High=20 }, new LabTest { Name="Glucose (DEMO)", Unit="demo unit", MinAge=18, MaxAge=120, Sex="الكل", Low=20, High=30 }, new LabTest { Name="Hemoglobin (DEMO)", Unit="demo unit", MinAge=0, MaxAge=120, Sex="الكل", Low=5, High=15 });
   SaveChanges(); Database.ExecuteSqlRaw("INSERT INTO AppMetadata (Key, Value) VALUES ('DemoSeedV1','1')"); transaction.Commit();
  }
   // Backfill once per record; a subsequent rename must never change its identity.
   Database.ExecuteSqlRaw("UPDATE LabTests SET TestKey='name:' || Name WHERE TestKey=''");
   Database.ExecuteSqlRaw("UPDATE LabResults SET TestKey='name:' || TestName WHERE TestKey=''");
   Database.ExecuteSqlRaw("UPDATE PlannedTests SET TestKey='name:' || TestName WHERE TestKey=''");
   MigrateAges();
 }
 private void MigrateAges()
 {
  if(Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM AppMetadata WHERE Key='AgeYearsV1'").Single()!=0) return;
  using var transaction=Database.BeginTransaction();
  Database.ExecuteSqlRaw("""
   UPDATE LabOrders AS o SET PatientAgeYears=(
    SELECT COALESCE(p.AgeYears,CASE WHEN date(p.BirthDate)<=date(o.CreatedAt) THEN
     CAST(strftime('%Y',o.CreatedAt) AS INTEGER)-CAST(strftime('%Y',p.BirthDate) AS INTEGER)
     -(strftime('%m-%d',o.CreatedAt)<strftime('%m-%d',p.BirthDate)) END)
    FROM Patients AS p WHERE p.Id=o.PatientId) WHERE PatientAgeYears IS NULL
   """);
  Database.ExecuteSqlRaw("""
   UPDATE LabResults AS r SET AgeYears=COALESCE(
    (SELECT o.PatientAgeYears FROM OrderResults AS link JOIN LabOrders AS o ON o.Id=link.LabOrderId
     WHERE link.LabResultId=r.Id ORDER BY link.Id LIMIT 1),
    CASE WHEN date(r.BirthDate)<=date(r.CreatedAt) THEN
     CAST(strftime('%Y',r.CreatedAt) AS INTEGER)-CAST(strftime('%Y',r.BirthDate) AS INTEGER)
     -(strftime('%m-%d',r.CreatedAt)<strftime('%m-%d',r.BirthDate)) END) WHERE AgeYears IS NULL
   """);
  Database.ExecuteSqlRaw("INSERT INTO AppMetadata(Key,Value) VALUES('AgeYearsV1','1')");
  transaction.Commit();
 }
 private void AddColumnIfMissing(string table,string column,string definition)
 {
  Database.OpenConnection();
  try {
   using var command=Database.GetDbConnection().CreateCommand(); command.CommandText=$"PRAGMA table_info({table})";
   bool exists=false; using(var reader=command.ExecuteReader()) { while(reader.Read()) if(reader.GetString(1)==column) exists=true; }
   if(!exists) { using var alter=Database.GetDbConnection().CreateCommand(); alter.CommandText=$"ALTER TABLE {table} ADD COLUMN {column} {definition}"; alter.ExecuteNonQuery(); }
  } finally { Database.CloseConnection(); }
 } private void TranslateDemoNames()
 {
  using var transaction=Database.BeginTransaction();
  foreach(var pair in new[] {("سكر الدم (تجريبي)","Glucose (DEMO)"),("هيموغلوبين (تجريبي)","Hemoglobin (DEMO)")})
  {
   Database.ExecuteSqlInterpolated($"UPDATE LabTests SET Name={pair.Item2}, Unit='demo unit' WHERE Name={pair.Item1}");
   Database.ExecuteSqlInterpolated($"UPDATE LabResults SET TestName={pair.Item2}, Unit='demo unit' WHERE TestName={pair.Item1}");
   Database.ExecuteSqlInterpolated($"UPDATE PlannedTests SET TestName={pair.Item2} WHERE TestName={pair.Item1}");
   foreach(var panel in Panels.Where(p=>p.TestNames.Contains(pair.Item1)).ToList()) panel.TestNames=string.Join("\n",PanelDefinition.Parse(panel.TestNames).Select(n=>n==pair.Item1?pair.Item2:n));
  }
  SaveChanges(); transaction.Commit();
 }}

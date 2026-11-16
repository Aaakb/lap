using MedicalLab;
using Microsoft.EntityFrameworkCore;
namespace MedicalLab.Tests;
public class OldOrderMigrationTests
{
 [Fact] public void ExistingOrdersReceivePerPatientNumbersWithoutChangingTheirIds()
 {
  var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".db");try{using(var db=new LabContext(path)){db.Database.OpenConnection();db.Database.ExecuteSqlRaw("CREATE TABLE LabOrders (Id INTEGER PRIMARY KEY,PatientId INTEGER NOT NULL,PatientName TEXT NOT NULL,CreatedAt TEXT NOT NULL,Approved INTEGER NOT NULL)");db.Database.ExecuteSqlRaw("INSERT INTO LabOrders VALUES (7,1,'A','2026-01-01',0),(8,2,'B','2026-01-01',0),(9,1,'A','2026-01-02',1)");db.Initialize();Assert.Equal(new[]{1,1,2},db.LabOrders.OrderBy(o=>o.Id).Select(o=>o.PatientOrderNumber).ToArray());Assert.Equal(new[]{7,8,9},db.LabOrders.OrderBy(o=>o.Id).Select(o=>o.Id).ToArray());}using(var db=new LabContext(path)){db.Initialize();Assert.Equal(2,db.LabOrders.Single(o=>o.Id==9).PatientOrderNumber);}}finally{Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();File.Delete(path);}
 }
}

using MedicalLab;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MedicalLab.Tests;

public class RestoreSafetyTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "restore-safety-" + Guid.NewGuid());
    private string Live => Path.Combine(root, "live.db");
    private string Backup => Path.Combine(root, "backup.db");

    public RestoreSafetyTests()
    {
        Directory.CreateDirectory(root);
        using var db = new LabContext(Live);
        db.Initialize();
        db.Patients.Add(Patient.Create("البيانات الأصلية", new DateTime(2000, 1, 1), "ذكر"));
        db.SaveChanges();
        DatabaseBackup.Create(Live, Backup);
    }

    [Theory]
    [InlineData("DROP TABLE Settings; CREATE TABLE Settings (Id INTEGER PRIMARY KEY)")]
    [InlineData("DROP TABLE Panels; CREATE TABLE Panels (Id INTEGER PRIMARY KEY, Name TEXT)")]
    [InlineData("ALTER TABLE LabResults DROP COLUMN Status")]
    public void IncompatibleSchemaIsRejectedBeforeLiveReplacement(string sql)
    {
        using (var db = new LabContext(Backup)) db.Database.ExecuteSqlRaw(sql);

        Assert.ThrowsAny<Exception>(() => DatabaseBackup.Restore(Backup, Live));

        using var current = new LabContext(Live);
        Assert.Equal("البيانات الأصلية", current.Patients.Single().Name);
        Assert.Single(current.Settings);
        Assert.Empty(Directory.GetFiles(root, "*.restore-*.db*"));
    }

    [Fact]
    public void InvalidStoredLogoCannotReplaceLiveData()
    {
        using (var db = new LabContext(Backup))
        {
            db.Settings.Single().Logo = new byte[] { 1, 2, 3 };
            db.Patients.Single().Name = "من ملف غير صالح";
            db.SaveChanges();
        }
        Assert.ThrowsAny<Exception>(() => DatabaseBackup.Restore(Backup, Live));
        using var current = new LabContext(Live);
        Assert.Equal("البيانات الأصلية", current.Patients.Single().Name);
    }

    [Fact]
    public void RestoreMigratesAStagedCopyAndPreservesOriginalBackup()
    {
        using (var db = new LabContext(Backup))
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE LabOrders DROP COLUMN Approved");
            db.Patients.Single().Name = "من النسخة";
            db.SaveChanges();
        }

        DatabaseBackup.Restore(Backup, Live);

        using (var current = new LabContext(Live))
        {
            Assert.Equal("من النسخة", current.Patients.Single().Name);
            _ = current.LabOrders.ToList(); // All current columns must already be available.
        }
        using var original = new SqliteConnection($"Data Source={Backup};Pooling=False");
        original.Open();
        using var command = original.CreateCommand();
        command.CommandText = "SELECT Approved FROM LabOrders LIMIT 0";
        Assert.Throws<SqliteException>(() => command.ExecuteScalar());
        var beforeRestore = Assert.Single(Directory.GetFiles(root, "*.before-restore-*.db"));
        using var previous = new LabContext(beforeRestore);
        Assert.Equal("البيانات الأصلية", previous.Patients.Single().Name);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(root, true);
    }
}

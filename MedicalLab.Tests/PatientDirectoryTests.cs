using MedicalLab;

namespace MedicalLab.Tests;

public class PatientDirectoryTests
{
    [Fact]
    public void MidnightChangesTheDailyListWhilePriorPatientsRemainSearchable()
    {
        var path = Path.Combine(Path.GetTempPath(), $"patient-directory-{Guid.NewGuid()}.db");
        try
        {
            using var db = new LabContext(path);
            db.Initialize();
            var day = new DateTime(2026, 11, 17);
            var returning = new Patient { Name = "مراجع سابق" };
            var registered = new Patient { Name = "مراجع جديد", RegisteredAt = day.AddHours(10) };
            var midnight = new Patient { Name = "مراجع منتصف الليل" };
            db.Patients.AddRange(returning, registered, midnight);
            db.SaveChanges();
            db.LabOrders.AddRange(
                new LabOrder { PatientId = returning.Id, CreatedAt = day.AddHours(11) },
                new LabOrder { PatientId = midnight.Id, CreatedAt = day.AddDays(1) });
            db.SaveChanges();

            Assert.Equal(new[] { registered.Id, returning.Id }, PatientDirectory.Load(db, day, "").Select(p => p.Id));
            Assert.Equal(midnight.Id, Assert.Single(PatientDirectory.Load(db, day.AddDays(1), "")).Id);
            Assert.Equal(returning.Id, Assert.Single(PatientDirectory.Load(db, day.AddDays(1), "سابق")).Id);
            Assert.Equal(registered.Id, Assert.Single(PatientDirectory.Load(db, day.AddDays(1), registered.Id.ToString())).Id);
            Assert.Single(OrderWorkflow.OrdersForPatient(db, returning.Id));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}

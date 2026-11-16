using MedicalLab;
using Microsoft.EntityFrameworkCore;

namespace MedicalLab.Tests;

public class TestRenameTests
{
    [Fact]
    public void RenameKeepsPendingOrdersPanelsAndSavedSnapshotsConsistent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rename-{Guid.NewGuid()}.db");
        try
        {
            using var db = new LabContext(path);
            db.Initialize();
            var patient = Patient.Create("مراجع", new DateTime(2000, 1, 1), "ذكر");
            db.Patients.Add(patient);
            var panel = new PanelDefinition { Name = "Panel", TestNames = "Glucose (DEMO)\nHemoglobin (DEMO)" };
            db.Panels.Add(panel);
            db.SaveChanges();
            var pending = OrderWorkflow.CreateOrderWithTests(db, patient.Id, new[] { "Glucose (DEMO)" });
            var saved = OrderWorkflow.CreateOrderWithResult(db, patient.Id, "Glucose (DEMO)", "22");
            var approved = OrderWorkflow.CreateOrderWithResult(db, patient.Id, "Glucose (DEMO)", "23");
            OrderWorkflow.SetApproval(db, approved, true);
            var range = db.LabTests.AsNoTracking().First(t => t.Name == "Glucose (DEMO)" && t.MinAge == 18);
            range.Name = "Glucose";

            TestCatalog.Save(db, range.Id, range);

            Assert.Equal(2, db.LabTests.Count(t => t.Name == "Glucose"));
            Assert.DoesNotContain("Glucose (DEMO)", db.Panels.Single().TestNames);
            Assert.Equal("Glucose", db.PlannedTests.Single(t => t.LabOrderId == pending).TestName);
            OrderWorkflow.SaveResult(db, pending, "Glucose", "24");
            Assert.Throws<ArgumentException>(() => OrderWorkflow.SaveResult(db, saved, "Glucose", "25"));
            OrderWorkflow.SetApproval(db, saved, true);
            Assert.Empty(PatientHistory.Load(db, patient.Id).Orders.Single(v => v.Order.Id == saved).Pending);
            Assert.Equal("Glucose (DEMO)", db.LabResults.Single(r => r.Value == 22).TestName);
            Assert.True(db.LabOrders.Find(approved)!.Approved);
            OrderWorkflow.SetApproval(db, approved, false);
            OrderWorkflow.DeleteResult(db, approved, db.LabResults.Single(r => r.Value == 23).Id);
            Assert.Equal("Glucose", Assert.Single(OrderWorkflow.PendingTests(db, approved, Array.Empty<LabResult>())).TestName);
            OrderWorkflow.SaveResult(db, approved, "Glucose", "23");
            var second = OrderWorkflow.CreateOrderWithTests(db, patient.Id, new[] { "Hemoglobin (DEMO)" });
            OrderWorkflow.ApplyPanel(db, second, panel.Id);
            Assert.Equal(2, db.PlannedTests.Count(t => t.LabOrderId == second));
            db.ChangeTracker.Clear();
            db.Initialize();
            Assert.Throws<ArgumentException>(() => OrderWorkflow.SaveResult(db, pending, "Glucose", "26"));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public void LegacyRecordsReceiveStableKeysBeforeRename()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rename-legacy-{Guid.NewGuid()}.db");
        try
        {
            int order;
            using (var db = new LabContext(path))
            {
                db.Initialize();
                var patient = Patient.Create("مراجع", new DateTime(2000, 1, 1), "ذكر");
                db.Patients.Add(patient);
                db.SaveChanges();
                order = OrderWorkflow.CreateOrderWithResult(db, patient.Id, "Glucose (DEMO)", "22");
                db.Database.ExecuteSqlRaw("ALTER TABLE LabTests DROP COLUMN TestKey");
                db.Database.ExecuteSqlRaw("ALTER TABLE LabResults DROP COLUMN TestKey");
                db.Database.ExecuteSqlRaw("ALTER TABLE PlannedTests DROP COLUMN TestKey");
            }
            using (var db = new LabContext(path))
            {
                db.Initialize();
                var range = db.LabTests.AsNoTracking().First(t => t.Name == "Glucose (DEMO)");
                range.Name = "Glucose";
                TestCatalog.Save(db, range.Id, range);
                Assert.Throws<ArgumentException>(() => OrderWorkflow.SaveResult(db, order, "Glucose", "24"));
                OrderWorkflow.SetApproval(db, order, true);
                Assert.Equal("Glucose (DEMO)", db.LabResults.Single().TestName);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public void RenameToAnotherTestsNameIsRejectedWithoutChangingReferences()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rename-conflict-{Guid.NewGuid()}.db");
        try
        {
            using var db = new LabContext(path);
            db.Initialize();
            var range = db.LabTests.AsNoTracking().First(t => t.Name == "Glucose (DEMO)");
            range.Name = "Hemoglobin (DEMO)";
            Assert.Throws<ArgumentException>(() => TestCatalog.Save(db, range.Id, range));
            Assert.Equal(2, db.LabTests.Count(t => t.Name == "Glucose (DEMO)"));
            Assert.Single(db.LabTests.Where(t => t.Name == "Hemoglobin (DEMO)"));
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}

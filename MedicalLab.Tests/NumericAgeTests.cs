using MedicalLab;
using Microsoft.EntityFrameworkCore;

namespace MedicalLab.Tests;

public class NumericAgeTests
{
    [Theory]
    [InlineData("28", 28)]
    [InlineData("٢٨", 28)]
    [InlineData("0", 0)]
    [InlineData("150", 150)]
    public void AcceptsOnlyWholeYearsWithoutCreatingADate(string input, int expected)
    {
        var patient = Patient.CreateWithAge("مراجع", Patient.ParseAge(input), "Male");
        Assert.Equal(expected, patient.AgeYears);
        Assert.Null(patient.BirthDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("151")]
    [InlineData("28.5")]
    [InlineData("abc")]
    public void RejectsMissingOrInvalidAge(string input) => Assert.Throws<ArgumentException>(() => Patient.ParseAge(input));

    [Fact]
    public void RangeSelectionUsesTheEnteredNumberAtTheBoundary()
    {
        var ranges = new[] { new LabTest { Id = 1, MinAge = 0, MaxAge = 17 }, new LabTest { Id = 2, MinAge = 18, MaxAge = 150 } };
        Assert.Equal(1, RangeSelector.FindForAge(ranges, 17, "Male")!.Id);
        Assert.Equal(2, RangeSelector.FindForAge(ranges, 18, "Male")!.Id);
        Assert.Null(RangeSelector.FindForAge(ranges, null, "Male"));
    }

    [Fact]
    public void SavedOrdersAndResultsKeepTheirAgeWhenThePatientsAgeChanges()
    {
        var path = Path.Combine(Path.GetTempPath(), $"numeric-age-{Guid.NewGuid()}.db");
        try
        {
            using var db = new LabContext(path);
            db.Initialize();
            var patient = Patient.CreateWithAge("مراجع", 17, "Male");
            db.Patients.Add(patient);
            db.SaveChanges();
            var first = OrderWorkflow.CreateOrderWithTests(db, patient.Id, new[] { "Glucose (DEMO)" });
            patient.AgeYears = 18;
            db.SaveChanges();
            OrderWorkflow.SaveResult(db, first, "Glucose (DEMO)", "15");
            Assert.Equal(17, db.LabOrders.Find(first)!.PatientAgeYears);
            Assert.Equal(17, db.LabResults.Single().AgeYears);
            Assert.Equal(10, db.LabResults.Single().Low);
            var second = OrderWorkflow.CreateOrderWithResult(db, patient.Id, "Glucose (DEMO)", "25");
            Assert.Equal(18, db.LabOrders.Find(second)!.PatientAgeYears);
            Assert.Equal(20, db.LabResults.Single(r => r.AgeYears == 18).Low);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }

    [Fact]
    public void LegacyRecordsKeepTheAgeAtTheVisitRatherThanTodaysAge()
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy-age-{Guid.NewGuid()}.db");
        try
        {
            using (var db = new LabContext(path))
            {
                db.Initialize();
                var patient = Patient.Create("مراجع سابق", new DateTime(2000, 6, 1), "Male");
                db.Patients.Add(patient);
                db.SaveChanges();
                var order = new LabOrder { PatientId = patient.Id, PatientName = patient.Name, CreatedAt = new DateTime(2020, 5, 31) };
                db.LabOrders.Add(order); db.SaveChanges();
                OrderWorkflow.SaveResult(db, order.Id, "Glucose (DEMO)", "22");
                db.Database.ExecuteSqlRaw("ALTER TABLE Patients DROP COLUMN AgeYears");
                db.Database.ExecuteSqlRaw("ALTER TABLE LabOrders DROP COLUMN PatientAgeYears");
                db.Database.ExecuteSqlRaw("ALTER TABLE LabResults DROP COLUMN AgeYears");
                db.Database.ExecuteSqlRaw("DELETE FROM AppMetadata WHERE Key='AgeYearsV1'");
            }
            using (var db = new LabContext(path))
            {
                db.Initialize();
                Assert.Equal(19, db.LabOrders.Single().PatientAgeYears);
                Assert.Equal(19, db.LabResults.Single().AgeYears);
                Assert.Equal(new DateTime(2000, 6, 1), db.Patients.Single().BirthDate);
            }
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}

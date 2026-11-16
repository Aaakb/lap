using MedicalLab;
using System.Windows.Documents;

namespace MedicalLab.Tests;

public class PartialReportTests
{
    [Fact]
    public void ReportListsPendingTestsAndUsesTheStoredOrder()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            var path = Path.Combine(Path.GetTempPath(), $"partial-report-{Guid.NewGuid()}.db");
            try
            {
                using var db = new LabContext(path);
                db.Initialize();
                var patient = Patient.Create("مراجع", new DateTime(2000, 1, 1), "ذكر");
                db.Patients.Add(patient);
                db.SaveChanges();
                var id = OrderWorkflow.CreateOrderWithTests(db, patient.Id, new[] { "Glucose (DEMO)", "Hemoglobin (DEMO)" });
                OrderWorkflow.SaveResult(db, id, "Glucose (DEMO)", "22");

                var report = OrderReportData.Load(db, id);
                Assert.Equal("Hemoglobin (DEMO)", Assert.Single(report.Pending));
                var document = LabReport.Build(report);
                var text = new TextRange(document.ContentStart, document.ContentEnd).Text;
                Assert.Contains("تقرير جزئي", text);
                Assert.Contains("Pending: Hemoglobin (DEMO)", text);
                Assert.Single(document.Blocks.OfType<Table>());
                LabReport.SavePdf(document, Path.Combine(AppContext.BaseDirectory, "report-partial-preview.pdf"));

                OrderWorkflow.SaveResult(db, id, "Hemoglobin (DEMO)", "10");
                report = OrderReportData.Load(db, id);
                Assert.Empty(report.Pending);
                document = LabReport.Build(report);
                Assert.DoesNotContain("تقرير جزئي", new TextRange(document.ContentStart, document.ContentEnd).Text);
            }
            catch (Exception ex) { failure = ex; }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                File.Delete(path);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        if (failure is not null) throw failure;
    }
}

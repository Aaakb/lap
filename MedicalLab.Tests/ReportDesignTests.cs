using MedicalLab;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MedicalLab.Tests;

public class ReportDesignTests
{
    [Fact]
    public void A4TemplateShowsIdentityAndResultsWithoutInterpretationOrStaffNames()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var names = new[] { "WBC (White Blood Cells)", "RBC (Red Blood Cells)", "Hemoglobin (Hb)", "Hematocrit (Hct)", "MCV", "MCH", "MCHC", "Platelets", "RDW" };
                var values = new[] { 6.8, 5.12, 15.1, 44.8, 87.5, 29.5, 33.7, 238, 13.2 };
                var units = new[] { "×10³/µL", "×10⁶/µL", "g/dL", "%", "fL", "pg", "g/dL", "×10³/µL", "%" };
                var lows = new[] { 4.0, 4.5, 13, 40, 80, 27, 32, 150, 11.5 };
                var highs = new[] { 10.0, 6.0, 17, 50, 96, 33, 36, 400, 14.5 };
                var rows = names.Select((name, i) => new LabResult
                {
                    PatientId = 123, PatientName = "اسم المراجع", PatientSex = "ذكر", BirthDate = new DateTime(1998, 1, 1),
                    TestName = name, Value = values[i], Unit = units[i], Low = lows[i], High = highs[i], Status = "ضمن النطاق"
                }).ToList();
                var doc = LabReport.Build(new LabSettings { Address = "بغداد", Phone = "07721280623" },
                    new LabOrder { PatientId = 123, PatientOrderNumber = 4, CreatedAt = new DateTime(2026, 11, 16) }, rows);
                var text = new TextRange(doc.ContentStart, doc.ContentEnd).Text;
                var uiText = string.Join(" ", doc.Blocks.OfType<BlockUIContainer>().SelectMany(b => Descendants(b.Child)).OfType<TextBlock>().Select(t => new TextRange(t.ContentStart, t.ContentEnd).Text));
                Assert.Contains("Reference Range", text);
                Assert.Contains("—", text);
                Assert.Contains("اسم المراجع", uiText);
                Assert.Contains("28 Years / Male", uiText);
                Assert.DoesNotContain("Interpretation", text + uiText);
                Assert.DoesNotContain("Lab Supervisor", text + uiText);
                Assert.DoesNotContain("Ali Hassan", text + uiText);
                Assert.Equal(FlowDirection.LeftToRight, Assert.Single(doc.Blocks.OfType<Table>()).FlowDirection);
                var path = Path.Combine(AppContext.BaseDirectory, "report-a4-design-preview.pdf");
                LabReport.SavePdf(doc, path);
                using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(path);
                Assert.Equal(1, pdf.PageCount);
                Assert.InRange(pdf.Pages[0].Width.Point, 595, 596);
                Assert.InRange(pdf.Pages[0].Height.Point, 841, 843);
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        if (failure is not null) throw failure;
    }

    internal static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach (var nested in Descendants(child)) yield return nested;
    }
}

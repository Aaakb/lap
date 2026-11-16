using MedicalLab;
namespace MedicalLab.Tests;
public class OrderTests
{
 [Fact] public void ReportDoesNotMixDifferentPatients()
 {
  Assert.Throws<ArgumentException>(()=>ReportSelection.Validate(new[] {new LabResult{PatientId=1}, new LabResult{PatientId=2}}));
 }
 [Fact] public void ReportNeedsAtLeastOneResult() => Assert.Throws<ArgumentException>(()=>ReportSelection.Validate(Array.Empty<LabResult>()));
}

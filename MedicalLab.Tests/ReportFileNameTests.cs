using MedicalLab;
namespace MedicalLab.Tests;
public class ReportFileNameTests
{
 [Fact] public void UsesPatientNameAndOrderNumber() => Assert.Equal("علي حسن - 42.pdf",ReportFileName.Create("علي حسن",42));
 [Fact] public void RemovesUnsafeCharactersAndTrailingDots() => Assert.Equal("علي حسن - 3.pdf",ReportFileName.Create(" علي:/ حسن... ",3));
 [Fact] public void FallsBackForEmptyName() => Assert.Equal("Patient - 8.pdf",ReportFileName.Create("...",8));
}

using MedicalLab;
namespace MedicalLab.Tests;
public class PatientTests
{
    [Fact] public void RejectsBlankName() => Assert.Throws<ArgumentException>(() => Patient.Create("  ", null, ""));
    [Fact] public void TrimsName() => Assert.Equal("علي", Patient.Create("  علي  ", null, "").Name);
    [Fact] public void RejectsFutureBirthDate() => Assert.Throws<ArgumentException>(() => Patient.Create("علي", DateTime.Today.AddDays(1), ""));
}

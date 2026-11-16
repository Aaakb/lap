using MedicalLab;
namespace MedicalLab.Tests;
public class ReferenceRangeTests
{
 [Fact] public void SelectsAgeAndSexSpecificRange() {
  var ranges = new[] { new LabTest { Id=1, Name="demo", MinAge=0, MaxAge=17, Sex="الكل", Low=1, High=5 }, new LabTest { Id=2, Name="demo", MinAge=18, MaxAge=99, Sex="ذكر", Low=2, High=8 } };
  Assert.Equal(2, RangeSelector.Find(ranges, new DateTime(2000, 5, 2), "ذكر", new DateTime(2026, 5, 1))!.Id);
 }
 [Fact] public void MissingBirthDateDoesNotGuessRange() => Assert.Null(RangeSelector.Find(new[] { new LabTest { MinAge=0, MaxAge=120, Sex="الكل" } }, null, "ذكر", DateTime.Today));
 [Fact] public void UnknownSexDoesNotMatchSpecificRange() => Assert.Null(RangeSelector.Find(new[] { new LabTest { MinAge=0, MaxAge=120, Sex="ذكر" } }, new DateTime(2000,1,1), "غير محدد", DateTime.Today));
}

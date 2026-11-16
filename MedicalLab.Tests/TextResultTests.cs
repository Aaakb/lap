using MedicalLab;
namespace MedicalLab.Tests;
public class TextResultTests
{
 [Fact] public void TextResultUsesConfiguredReferenceAndTrimsValue() { var r=ResultEntry.Create(new LabTest {IsText=true,ExpectedText="Negative"},"  negative  "); Assert.Equal("negative",r.TextValue); Assert.Equal("مطابق للمرجع",r.Status); Assert.Equal("Negative",r.ReferenceDisplay); }
 [Fact] public void DifferentTextIsFlagged() => Assert.Equal("مختلف عن المرجع", ResultEntry.Create(new LabTest {IsText=true,ExpectedText="Negative"},"Positive").Status);
 [Fact] public void EmptyTextIsRejected() => Assert.Throws<ArgumentException>(()=>ResultEntry.Create(new LabTest {IsText=true},"  "));
 [Theory] [InlineData("NaN")] [InlineData("Infinity")] [InlineData("abc")] public void NumericResultRejectsInvalidValue(string text) => Assert.Throws<ArgumentException>(()=>ResultEntry.Create(new LabTest(),text));
 [Fact] public void NumericResultStillFlagsHigh() => Assert.Equal("مرتفع",ResultEntry.Create(new LabTest {Low=10,High=20},"21").Status);
}

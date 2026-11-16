using MedicalLab;
namespace MedicalLab.Tests;
public class RequiredPatientDetailsTests
{
 [Fact] public void MissingBirthDateIsRejected() => Assert.Throws<ArgumentException>(()=>Patient.ValidateRequired("Ali",null,"ذكر"));
 [Fact] public void MissingSexIsRejected() => Assert.Throws<ArgumentException>(()=>Patient.ValidateRequired("Ali",new DateTime(2000,1,1),"غير محدد"));
 [Theory] [InlineData("ذكر","Male")] [InlineData("أنثى","Female")] [InlineData("Male","Male")] [InlineData("Female","Female")] public void SexIsDisplayedInEnglish(string sex,string expected) => Assert.Equal(expected,Patient.SexInEnglish(sex));
 [Fact] public void EnglishMaleMatchesLegacyMaleRange() => Assert.NotNull(RangeSelector.Find(new[]{new LabTest{MinAge=0,MaxAge=100,Sex="ذكر"}},new DateTime(2000,1,1),"Male",new DateTime(2026,1,1)));
}

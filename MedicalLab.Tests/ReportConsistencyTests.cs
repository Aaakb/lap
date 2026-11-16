using MedicalLab;
namespace MedicalLab.Tests;
public class ReportConsistencyTests
{
 [Fact] public void RejectsDifferentPatientSnapshotsForSameOrder()
 {
  var rows=new[]{new LabResult {PatientId=1,PatientName="علي",PatientSex="ذكر",BirthDate=new DateTime(2000,1,1)},new LabResult {PatientId=1,PatientName="علي",PatientSex="أنثى",BirthDate=new DateTime(2000,1,1)}};
  Assert.Throws<ArgumentException>(()=>ReportSelection.Validate(rows));
 }
}

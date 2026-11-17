using System.ComponentModel.DataAnnotations.Schema;
namespace MedicalLab;
public class LabOrder
{
 public int Id {get;set;}
 public int PatientOrderNumber {get;set;}
 public bool Approved {get;set;}
 public int PatientId {get;set;}
 public int? PatientAgeYears {get;set;}
 public string PatientName {get;set;}="";
 public DateTime CreatedAt {get;set;}
 [NotMapped] public string Label => $"طلب {PatientOrderNumber} — {CreatedAt:yyyy-MM-dd}";
}
public class OrderResult
{
 public int Id {get;set;}
 public int LabOrderId {get;set;}
 public int LabResultId {get;set;}
}
public static class ReportSelection
{
 public static void Validate(IReadOnlyCollection<LabResult> results)
 {
  if(results.Count==0) throw new ArgumentException("لا توجد نتائج في هذا الطلب.");
  if(results.Select(r=>r.PatientId).Distinct().Count()!=1) throw new ArgumentException("لا يمكن دمج نتائج مراجعين مختلفين.");
  if(results.Select(r=>(r.PatientName,r.PatientSex,r.BirthDate,r.AgeYears)).Distinct().Count()!=1) throw new ArgumentException("بيانات المراجع مختلفة بين النتائج. أعد إدخال نتائج المسودة بعد مراجعة بيانات المراجع.");
 }
}

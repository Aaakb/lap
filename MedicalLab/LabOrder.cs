using System.ComponentModel.DataAnnotations.Schema;
namespace MedicalLab;
public class LabOrder
{
 public int Id {get;set;}
 public bool Approved {get;set;}
 public int PatientId {get;set;}
 public string PatientName {get;set;}="";
 public DateTime CreatedAt {get;set;}
 [NotMapped] public string Label => $"طلب {Id} — {PatientName} — {CreatedAt:yyyy-MM-dd HH:mm} — {(Approved?"معتمد":"مسودة")}";
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
  if(results.Select(r=>r.PatientId).Distinct().Count()!=1) throw new ArgumentException("لا يمكن دمج نتائج مرضى مختلفين.");
 }
}

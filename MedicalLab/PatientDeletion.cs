using Microsoft.EntityFrameworkCore;
namespace MedicalLab;
public static class PatientDeletion
{
 public static int Delete(LabContext db,IEnumerable<int> selected)
 {
  var ids=selected.Distinct().ToArray();if(ids.Length==0)throw new ArgumentException("اختر مراجعاً واحداً على الأقل.");
  using var transaction=db.Database.BeginTransaction();
  var patients=db.Patients.Where(p=>ids.Contains(p.Id)).ToList();if(patients.Count!=ids.Length)throw new ArgumentException("تغيرت قائمة المراجعين؛ أعد المحاولة.");
  var orderIds=db.LabOrders.Where(o=>ids.Contains(o.PatientId)).Select(o=>o.Id).ToArray();
  var resultIds=db.LabResults.Where(r=>ids.Contains(r.PatientId)).Select(r=>r.Id).ToArray();
  db.OrderResults.Where(x=>orderIds.Contains(x.LabOrderId)||resultIds.Contains(x.LabResultId)).ExecuteDelete();
  db.PlannedTests.Where(x=>orderIds.Contains(x.LabOrderId)).ExecuteDelete();
  db.AuditEntries.Where(x=>orderIds.Contains(x.OrderId)).ExecuteDelete();
  db.LabResults.Where(x=>ids.Contains(x.PatientId)).ExecuteDelete();
  db.LabOrders.Where(x=>ids.Contains(x.PatientId)).ExecuteDelete();
  db.Patients.RemoveRange(patients);db.SaveChanges();transaction.Commit();return patients.Count;
 }
}

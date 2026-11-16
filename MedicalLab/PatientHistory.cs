using Microsoft.EntityFrameworkCore;
namespace MedicalLab;
public record HistoryVisit(LabOrder Order,List<LabResult> Results,List<string> Pending);
public record PatientHistory(Patient Patient,List<HistoryVisit> Orders)
{
 public static PatientHistory Load(LabContext db,int patientId)
 {
  var patient=db.Patients.AsNoTracking().SingleOrDefault(p=>p.Id==patientId) ?? throw new ArgumentException("المراجع غير موجود.");
  var orders=db.LabOrders.AsNoTracking().Where(o=>o.PatientId==patientId).OrderByDescending(o=>o.CreatedAt).ThenByDescending(o=>o.Id).ToList();
  var orderIds=orders.Select(o=>o.Id).ToList();var links=db.OrderResults.AsNoTracking().Where(l=>orderIds.Contains(l.LabOrderId)).ToList();var ids=links.Select(l=>l.LabResultId).ToList();var results=db.LabResults.AsNoTracking().Where(r=>ids.Contains(r.Id) && r.PatientId==patientId).ToList();var planned=db.PlannedTests.AsNoTracking().Where(p=>orderIds.Contains(p.LabOrderId)).ToList();
  var visits=orders.Select(o=>{var relevant=links.Where(l=>l.LabOrderId==o.Id).Select(l=>l.LabResultId).ToHashSet();var values=results.Where(r=>relevant.Contains(r.Id)).ToList();var complete=values.Select(r=>TestIdentity.Key(r.TestKey,r.TestName)).ToHashSet();return new HistoryVisit(o,values,planned.Where(p=>p.LabOrderId==o.Id && !complete.Contains(TestIdentity.Key(p.TestKey,p.TestName))).Select(p=>p.TestName).ToList());}).ToList();
  return new PatientHistory(patient,visits);
 }
}

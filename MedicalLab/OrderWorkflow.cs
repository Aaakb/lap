using Microsoft.EntityFrameworkCore;
namespace MedicalLab;
public class PanelDefinition
{
 public int Id {get;set;}
 public string Name {get;set;}="";
 public string TestNames {get;set;}="";
 public static string[] Parse(string text) => text.Split('\n').Select(s=>s.Trim()).Where(s=>s.Length>0).Distinct().ToArray();
}
public class PlannedTest
{
 public int Id {get;set;}
 public int LabOrderId {get;set;}
 public string TestName {get;set;}="";
}
public class AuditEntry
{
 public int Id {get;set;}
 public DateTime At {get;set;}
 public string Action {get;set;}="";
 public int OrderId {get;set;}
}
public static class OrderWorkflow
{
 public static void RequireDraft(LabOrder order) { if(order.Approved) throw new InvalidOperationException("الطلب معتمد. أعد فتحه للتعديل أولاً."); }
 public static void ValidateApproval(int results,int pending) { if(results==0 || pending>0) throw new ArgumentException("لا يمكن اعتماد طلب فارغ أو فيه تحاليل لم تُدخل نتائجها."); }
 public static void Log(LabContext db,int order,string action) => db.AuditEntries.Add(new AuditEntry {At=DateTime.Now,OrderId=order,Action=action});
 public static void SaveResult(LabContext db,int orderId,string name,string input)
 {
  using var transaction=db.Database.BeginTransaction();
  var order=db.LabOrders.Find(orderId) ?? throw new ArgumentException("الطلب غير موجود."); RequireDraft(order);
  var patient=db.Patients.AsNoTracking().Single(p=>p.Id==order.PatientId);
  var ranges=db.LabTests.AsNoTracking().Where(t=>t.Name==name && t.Active).ToList();
  var range=RangeSelector.Find(ranges,patient.BirthDate,patient.Sex,order.CreatedAt.Date) ?? throw new ArgumentException("لا يوجد نطاق واحد مطابق لعمر المريض في تاريخ الطلب وجنسه.");
  var ids=db.OrderResults.Where(x=>x.LabOrderId==orderId).Select(x=>x.LabResultId);
  if(db.LabResults.Any(r=>ids.Contains(r.Id) && r.TestName==name)) throw new ArgumentException("يوجد لهذا التحليل نتيجة بالفعل. احذف النتيجة السابقة من المسودة قبل تصحيحها.");
  var result=ResultEntry.Create(range,input); result.PatientId=patient.Id; result.PatientName=patient.Name; result.PatientSex=patient.Sex; result.BirthDate=patient.BirthDate; result.CreatedAt=DateTime.Now;
  db.LabResults.Add(result); db.SaveChanges(); db.OrderResults.Add(new OrderResult {LabOrderId=orderId,LabResultId=result.Id});
  if(!db.PlannedTests.Any(t=>t.LabOrderId==orderId && t.TestName==name)) db.PlannedTests.Add(new PlannedTest {LabOrderId=orderId,TestName=name});
  Log(db,orderId,"إضافة نتيجة: "+name); db.SaveChanges(); transaction.Commit();
 }
 public static void ApplyPanel(LabContext db,int orderId,int panelId)
 {
  using var transaction=db.Database.BeginTransaction(); var order=db.LabOrders.Find(orderId) ?? throw new ArgumentException("اختر الطلب."); RequireDraft(order);
  var panel=db.Panels.Find(panelId) ?? throw new ArgumentException("اختر المجموعة.");
  var names=PanelDefinition.Parse(panel.TestNames); if(names.Length==0) throw new ArgumentException("المجموعة فارغة.");
  var available=db.LabTests.Where(t=>t.Active).Select(t=>t.Name).Distinct().ToList();
  if(names.Any(n=>!available.Contains(n))) throw new ArgumentException("تتضمن المجموعة تحليلاً محذوفاً أو معطلاً؛ عدّل المجموعة أولاً.");
  var existing=db.PlannedTests.Where(t=>t.LabOrderId==orderId).Select(t=>t.TestName).ToList();
  foreach(var name in names.Except(existing)) db.PlannedTests.Add(new PlannedTest {LabOrderId=orderId,TestName=name});
  Log(db,orderId,"إضافة المجموعة: "+panel.Name); db.SaveChanges(); transaction.Commit();
 }
 public static void SetApproval(LabContext db,int orderId,bool approved)
 {
  using var transaction=db.Database.BeginTransaction();var order=db.LabOrders.Find(orderId) ?? throw new ArgumentException("اختر الطلب.");
  if(approved) {
   var ids=db.OrderResults.Where(x=>x.LabOrderId==orderId).Select(x=>x.LabResultId); var results=db.LabResults.Where(r=>ids.Contains(r.Id)).ToList(); ReportSelection.Validate(results);
   var names=results.Select(r=>r.TestName).ToList(); var pending=db.PlannedTests.Where(t=>t.LabOrderId==orderId).Select(t=>t.TestName).ToList().Count(n=>!names.Contains(n)); ValidateApproval(results.Count,pending);
  }
  order.Approved=approved; Log(db,orderId,approved?"اعتماد الطلب":"إعادة فتح الطلب للتصحيح"); db.SaveChanges(); transaction.Commit();
 }
 public static void DeleteResult(LabContext db,int orderId,int resultId)
 {
  using var transaction=db.Database.BeginTransaction(); var order=db.LabOrders.Find(orderId) ?? throw new ArgumentException("اختر الطلب."); RequireDraft(order);
  var link=db.OrderResults.Single(x=>x.LabOrderId==orderId && x.LabResultId==resultId); var result=db.LabResults.Find(resultId)!;
  db.OrderResults.Remove(link); db.LabResults.Remove(result); Log(db,orderId,"حذف نتيجة للتصحيح: "+result.TestName); db.SaveChanges(); transaction.Commit();
 }
}

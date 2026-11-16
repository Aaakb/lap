using Microsoft.EntityFrameworkCore;

namespace MedicalLab;

public record OrderReportData(LabSettings Settings, LabOrder Order, List<LabResult> Results, List<string> Pending)
{
    public static OrderReportData Load(LabContext db, int orderId)
    {
        using var transaction = db.Database.BeginTransaction();
        var order = db.LabOrders.AsNoTracking().SingleOrDefault(o => o.Id == orderId)
            ?? throw new ArgumentException("الطلب غير موجود.");
        var ids = db.OrderResults.Where(l => l.LabOrderId == orderId).Select(l => l.LabResultId);
        var results = db.LabResults.AsNoTracking().Where(r => ids.Contains(r.Id)).OrderBy(r => r.Id).ToList();
        ReportSelection.Validate(results);
        if (results.Any(r => r.PatientId != order.PatientId))
            throw new ArgumentException("بيانات النتائج لا تخص مراجع هذا الطلب؛ تعذر إصدار التقرير.");
        var pending = OrderWorkflow.PendingTests(db, orderId, results).Select(t => t.TestName).ToList();
        var settings = db.Settings.AsNoTracking().Single();
        transaction.Commit();
        return new OrderReportData(settings, order, results, pending);
    }
}

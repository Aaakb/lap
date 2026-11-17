using Microsoft.EntityFrameworkCore;

namespace MedicalLab;

public static class PatientDirectory
{
    public static List<Patient> Load(LabContext db, DateTime day, string search)
    {
        var patients = db.Patients.AsNoTracking();
        var term = search.Trim();
        if (term.Length > 0)
        {
            patients = int.TryParse(term, out var id)
                ? patients.Where(patient => patient.Id == id || patient.Name.Contains(term))
                : patients.Where(patient => patient.Name.Contains(term));
        }
        else
        {
            var start = day.Date;
            var end = start.AddDays(1);
            var visits = db.LabOrders.Where(order => order.CreatedAt >= start && order.CreatedAt < end)
                .Select(order => order.PatientId);
            patients = patients.Where(patient =>
                (patient.RegisteredAt >= start && patient.RegisteredAt < end) || visits.Contains(patient.Id));
        }

        return patients.OrderByDescending(patient => patient.Id).ToList();
    }
}

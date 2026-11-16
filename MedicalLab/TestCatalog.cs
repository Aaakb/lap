using Microsoft.EntityFrameworkCore;

namespace MedicalLab;

public static class TestIdentity
{
    public static string Key(string key, string name) => string.IsNullOrEmpty(key) ? "name:" + name : key;
}

public static class TestCatalog
{
    public static void Save(LabContext db, int? id, LabTest input)
    {
        var name = input.Name.Trim();
        if (name.Length == 0 || input.MinAge < 0 || input.MaxAge < input.MinAge || input.MaxAge > 150)
            throw new ArgumentException("تحقق من اسم التحليل والأعمار.");
        if (!input.IsText && (string.IsNullOrWhiteSpace(input.Unit) || !double.IsFinite(input.Low) ||
            !double.IsFinite(input.High) || input.Low > input.High))
            throw new ArgumentException("تحقق من الوحدة والحدود الرقمية.");

        using var transaction = db.Database.BeginTransaction();
        try
        {
            var current = id is int value
                ? db.LabTests.Find(value) ?? throw new ArgumentException("التحليل غير موجود.")
                : new LabTest();
            var oldName = current.Name;
            var renaming = id is not null && oldName != name;
            if (renaming && db.LabTests.Any(t => t.Name == name))
                throw new ArgumentException("الاسم الجديد مستخدم لتحليل آخر؛ اختر اسماً مختلفاً.");

            var group = id is null
                ? db.LabTests.Where(t => t.Name == name).ToList()
                : db.LabTests.Where(t => t.Name == oldName).ToList();
            var key = id is not null ? TestIdentity.Key(current.TestKey, oldName)
                : group.Count > 0 ? TestIdentity.Key(group[0].TestKey, name) : Guid.NewGuid().ToString("N");
            if (current.Active && group.Any(t => t.Id != id && t.Active &&
                t.MinAge <= input.MaxAge && t.MaxAge >= input.MinAge &&
                (t.Sex == "الكل" || input.Sex == "الكل" || Patient.SexInEnglish(t.Sex) == Patient.SexInEnglish(input.Sex))))
                throw new ArgumentException("يوجد نطاق فعال متداخل لنفس التحليل والعمر والجنس؛ عدّله أولاً.");

            if (renaming)
            {
                // A test's age/sex ranges share its identity and are renamed together.
                foreach (var range in group) { range.Name = name; range.TestKey = key; }
                foreach (var planned in db.PlannedTests.Where(t => t.TestName == oldName))
                { planned.TestName = name; planned.TestKey = key; }
                foreach (var panel in db.Panels.Where(p => p.TestNames.Contains(oldName)))
                    panel.TestNames = string.Join("\n", PanelDefinition.Parse(panel.TestNames).Select(n => n == oldName ? name : n).Distinct());
                // Saved results (including their displayed names and units) remain immutable snapshots.
            }

            current.TestKey = key;
            current.Name = name;
            current.Unit = input.Unit.Trim();
            current.MinAge = input.MinAge;
            current.MaxAge = input.MaxAge;
            current.Sex = input.Sex;
            current.Low = input.Low;
            current.High = input.High;
            current.IsText = input.IsText;
            current.ExpectedText = input.IsText ? input.ExpectedText.Trim() : "";
            if (id is null) db.LabTests.Add(current);
            db.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            db.ChangeTracker.Clear();
            throw;
        }
    }
}

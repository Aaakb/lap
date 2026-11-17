using System.Globalization;
namespace MedicalLab;
public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public int? AgeYears { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public string Sex { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string SexDisplay => SexInEnglish(Sex);
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string AgeDisplay => PatientAge.At(this,DateTime.Today)?.ToString(CultureInfo.InvariantCulture) ?? "—";
    public static string SexInEnglish(string sex) => sex switch { "ذكر" or "Male" => "Male", "أنثى" or "Female" => "Female", _ => "—" };
    public static void ValidateRequired(string name,int? age,string sex)
    {
        if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("يرجى إدخال اسم المراجع.");
        if(age is null || age<0 || age>150) throw new ArgumentException("يرجى إدخال العمر رقماً صحيحاً من 0 إلى 150 سنة.");
        if(SexInEnglish(sex)=="—") throw new ArgumentException("يرجى اختيار الجنس: Male أو Female.");
    }
    public static int ParseAge(string text)
    {
        var digits=new string(text.Trim().Select(c=>char.GetUnicodeCategory(c)==UnicodeCategory.DecimalDigitNumber ? (char)('0'+(int)char.GetNumericValue(c)) : c).ToArray());
        if(!int.TryParse(digits,NumberStyles.None,CultureInfo.InvariantCulture,out var age) || age<0 || age>150)
            throw new ArgumentException("يرجى إدخال العمر رقماً صحيحاً من 0 إلى 150 سنة.");
        return age;
    }
    public static Patient CreateWithAge(string name,int? age,string sex)
    {
        ValidateRequired(name,age,sex);
        return new Patient {Name=name.Trim(),AgeYears=age,Sex=sex};
    }
    public static Patient Create(string name, DateTime? birthDate, string sex)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("يرجى إدخال اسم المراجع.");
        if (birthDate?.Date > DateTime.Today) throw new ArgumentException("تاريخ الميلاد لا يمكن أن يكون في المستقبل.");
        return new Patient { Name = name.Trim(), BirthDate = birthDate?.Date, Sex = sex };
    }
}
public static class PatientAge
{
    // BirthDate remains readable for existing databases; new entry uses only AgeYears.
    public static int? FromBirthDate(DateTime? birth,DateTime date)
    {
        if(birth is null || birth.Value.Date>date.Date) return null;
        var years=date.Year-birth.Value.Year;
        return years-(birth.Value.Date>date.Date.AddYears(-years)?1:0);
    }
    public static int? At(Patient patient,DateTime date) => patient.AgeYears ?? FromBirthDate(patient.BirthDate,date);
    public static int? ForOrder(Patient patient,LabOrder? order) => order?.PatientAgeYears ?? At(patient,order?.CreatedAt.Date ?? DateTime.Today);
}

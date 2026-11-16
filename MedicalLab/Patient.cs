namespace MedicalLab;
public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public string Sex { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string SexDisplay => SexInEnglish(Sex);
    public static string SexInEnglish(string sex) => sex switch { "ذكر" or "Male" => "Male", "أنثى" or "Female" => "Female", _ => "—" };
    public static void ValidateRequired(string name,DateTime? birthDate,string sex)
    {
        if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("يرجى إدخال اسم المراجع.");
        if(birthDate is null) throw new ArgumentException("يرجى إدخال تاريخ ميلاد المراجع لتحديد العمر.");
        if(birthDate.Value.Date>DateTime.Today) throw new ArgumentException("تاريخ الميلاد لا يمكن أن يكون في المستقبل.");
        if(SexInEnglish(sex)=="—") throw new ArgumentException("يرجى اختيار الجنس: Male أو Female.");
    }
    public static Patient Create(string name, DateTime? birthDate, string sex)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("يرجى إدخال اسم المراجع.");
        if (birthDate?.Date > DateTime.Today) throw new ArgumentException("تاريخ الميلاد لا يمكن أن يكون في المستقبل.");
        return new Patient { Name = name.Trim(), BirthDate = birthDate?.Date, Sex = sex };
    }
}

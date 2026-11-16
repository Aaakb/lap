namespace MedicalLab;
public class Patient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime? BirthDate { get; set; }
    public string Sex { get; set; } = "";
    public static Patient Create(string name, DateTime? birthDate, string sex)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("يرجى إدخال اسم المريض.");
        if (birthDate?.Date > DateTime.Today) throw new ArgumentException("تاريخ الميلاد لا يمكن أن يكون في المستقبل.");
        return new Patient { Name = name.Trim(), BirthDate = birthDate?.Date, Sex = sex };
    }
}

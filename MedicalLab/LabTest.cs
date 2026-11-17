namespace MedicalLab;
public class LabTest
{
 public int Id {get;set;}
 public string TestKey {get;set;} = "";
 public string Name {get;set;} = "";
 public string Unit {get;set;} = "";
 public int MinAge {get;set;}
 public int MaxAge {get;set;}
 public string Sex {get;set;} = "الكل";
 public double Low {get;set;}
 public double High {get;set;}
 public bool IsText {get;set;}
 public string ExpectedText {get;set;} = "";
 public bool Active {get;set;} = true;
}
public class LabResult
{
 public int Id {get;set;}
 public string TestKey {get;set;} = "";
 public int PatientId {get;set;}
 public string PatientName {get;set;} = "";
 public string PatientSex {get;set;} = "";
 public DateTime? BirthDate {get;set;}
 public int? AgeYears {get;set;}
 public DateTime CreatedAt {get;set;}
 public string TestName {get;set;} = "";
 public string Unit {get;set;} = "";
 public double Value {get;set;}
 public double Low {get;set;}
 public double High {get;set;}
 public bool IsText {get;set;}
 public string TextValue {get;set;} = "";
 public string ExpectedText {get;set;} = "";
 [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string ValueDisplay => IsText ? TextValue : Value.ToString();
 [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string ReferenceDisplay => IsText ? ExpectedText : $"{Low} - {High}";
 [System.ComponentModel.DataAnnotations.Schema.NotMapped] public string FlagDisplay => Status switch { "منخفض"=>"Low", "مرتفع"=>"High", "ضمن النطاق"=>"Within range", "مطابق للمرجع"=>"Matches reference", "مختلف عن المرجع"=>"Outside reference", "وصفي"=>"Descriptive", _=>Status };
 public string Status {get;set;} = "";
}
public static class RangeSelector
{
 public static LabTest? Find(IEnumerable<LabTest> ranges, DateTime? birth, string sex, DateTime date)
 {
  return FindForAge(ranges,PatientAge.FromBirthDate(birth,date),sex);
 }
 public static LabTest? FindForAge(IEnumerable<LabTest> ranges,int? age,string sex)
 {
  if(age is null || age<0 || age>150) return null;
  var matches = ranges.Where(r => r.Active && age >= r.MinAge && age <= r.MaxAge && (r.Sex == "الكل" || (Patient.SexInEnglish(r.Sex) != "—" && Patient.SexInEnglish(r.Sex) == Patient.SexInEnglish(sex)))).ToList();
  return matches.Count == 1 ? matches[0] : null;
 }
}

using System.Globalization;
namespace MedicalLab;
public static class ResultEntry
{
 public static LabResult Create(LabTest test,string input)
 {
  var value=input.Trim(); if(value.Length==0) throw new ArgumentException("أدخل النتيجة.");
  var r=new LabResult {TestName=test.Name,Unit=test.Unit,Low=test.Low,High=test.High,IsText=test.IsText,ExpectedText=test.ExpectedText};
  if(test.IsText) { r.TextValue=value; r.Status=string.IsNullOrWhiteSpace(test.ExpectedText)?"وصفي":string.Equals(value,test.ExpectedText.Trim(),StringComparison.OrdinalIgnoreCase)?"مطابق للمرجع":"مختلف عن المرجع"; }
  else {
   if(!(double.TryParse(value,NumberStyles.Float,CultureInfo.CurrentCulture,out var number) || double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number)) || !double.IsFinite(number)) throw new ArgumentException("أدخل نتيجة رقمية صحيحة.");
   r.Value=number; r.Status=number<test.Low?"منخفض":number>test.High?"مرتفع":"ضمن النطاق";
  }
  return r;
 }
}

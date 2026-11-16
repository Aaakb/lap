using System.IO;
using System.Text;
namespace MedicalLab;
public static class ReportFileName
{
 public static string Create(string? patientName,int orderId)
 {
  var invalid=Path.GetInvalidFileNameChars();var clean=new StringBuilder();
  foreach(var c in patientName ?? "") if(!invalid.Contains(c) && !char.IsControl(c) && c!='\u202A' && c!='\u202B' && c!='\u202D' && c!='\u202E' && c!='\u202C' && c!='\u2066' && c!='\u2067' && c!='\u2068' && c!='\u2069') clean.Append(c);
  var name=clean.ToString().Trim().TrimEnd('.',' ').Trim();if(name.Length>100)name=name[..100].TrimEnd('.',' ');if(string.IsNullOrWhiteSpace(name))name="Patient";
  return $"{name} - {orderId}.pdf";
 }
}

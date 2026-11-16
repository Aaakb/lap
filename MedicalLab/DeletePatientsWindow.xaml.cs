using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
namespace MedicalLab;
public partial class DeletePatientsWindow : Window
{
 public class Option : INotifyPropertyChanged
 {
  public int Id {get;set;} public string Name {get;set;}=""; private bool selected;
  public bool Selected {get=>selected;set{if(selected==value)return;selected=value;PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(Selected)));}}
  public event PropertyChangedEventHandler? PropertyChanged;
 }
 private readonly List<Option> options;
 public bool Deleted {get;private set;}
 public DeletePatientsWindow(IEnumerable<Patient> patients){options=patients.OrderBy(x=>x.Name).Select(x=>new Option{Id=x.Id,Name=x.Name}).ToList();foreach(var item in options)item.PropertyChanged+=(_,_)=>UpdateCount();InitializeComponent();Refresh();}
 private void SearchChanged(object sender,TextChangedEventArgs e){if(PatientList is not null && options is not null)Refresh();}
 private void Refresh(){var term=Search.Text.Trim();PatientList.ItemsSource=options.Where(x=>x.Name.Contains(term,StringComparison.OrdinalIgnoreCase)||x.Id.ToString().Contains(term)).ToList();UpdateCount();}
 private void UpdateCount(){if(CountLabel is not null)CountLabel.Text=$"المحدد: {options.Count(x=>x.Selected)} من {options.Count}";}
 private void SelectAll(object sender,RoutedEventArgs e){foreach(var item in options)item.Selected=true;}
 private void ClearAll(object sender,RoutedEventArgs e){foreach(var item in options)item.Selected=false;}
 private void Delete(object sender,RoutedEventArgs e)
 {
  var ids=options.Where(x=>x.Selected).Select(x=>x.Id).ToArray();if(ids.Length==0){MessageBox.Show("اختر مراجعاً واحداً على الأقل.");return;}
  if(MessageBox.Show($"سيتم حذف {ids.Length} من المراجعين مع جميع طلباتهم ونتائجهم وسجلات العمليات المرتبطة بهم. هل تريد المتابعة؟","تأكيد الحذف",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;
  try {using var db=new LabContext();PatientDeletion.Delete(db,ids);Deleted=true;MessageBox.Show("تم حذف المراجعين المحددين.");DialogResult=true;}
  catch(Exception ex){MessageBox.Show(ex is ArgumentException?ex.Message:"تعذر الحذف؛ لم تُحذف البيانات.");}
 }
}

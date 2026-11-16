using System.Windows;
using System.Windows.Controls;
namespace MedicalLab;
public partial class SelectPatientsWindow : Window
{
 public class Option {public int Id {get;set;} public string Name {get;set;}="";public bool Selected {get;set;}}
 private readonly List<Option> options;
 public int[] SelectedIds => options.Where(x=>x.Selected).Select(x=>x.Id).ToArray();
 public SelectPatientsWindow(IEnumerable<Patient> patients){options=patients.OrderBy(x=>x.Name).Select(x=>new Option{Id=x.Id,Name=x.Name}).ToList();InitializeComponent();Refresh();}
 private void SearchChanged(object sender,TextChangedEventArgs e){if(PatientList is not null && options is not null)Refresh();}
 private void Refresh(){var term=Search.Text.Trim();PatientList.ItemsSource=options.Where(x=>x.Name.Contains(term,StringComparison.OrdinalIgnoreCase)||x.Id.ToString().Contains(term)).ToList();}
 private void Accept(object sender,RoutedEventArgs e){if(SelectedIds.Length==0){MessageBox.Show("اختر مراجعاً واحداً على الأقل.");return;}DialogResult=true;}
}

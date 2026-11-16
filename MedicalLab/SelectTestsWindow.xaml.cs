using System.Windows;
using System.Windows.Controls;
namespace MedicalLab;
public partial class SelectTestsWindow : Window
{
 public class TestOption {public string Name {get;set;}="";public bool IsSelected {get;set;}}
 private readonly List<TestOption> options;
 public string[] SelectedTests => options.Where(t=>t.IsSelected).Select(t=>t.Name).ToArray();
 public SelectTestsWindow(IEnumerable<string> tests) {options=tests.OrderBy(x=>x).Select(x=>new TestOption{Name=x}).ToList();InitializeComponent();Refresh();}
 private void SearchChanged(object sender,TextChangedEventArgs e) {if(TestList is not null && options is not null) Refresh();}
 private void Refresh() {TestList.ItemsSource=options.Where(n=>n.Name.Contains(Search.Text,StringComparison.OrdinalIgnoreCase)).ToList();}
 private void Accept(object sender,RoutedEventArgs e) {if(SelectedTests.Length==0){MessageBox.Show("اختر تحليلاً واحداً على الأقل.");return;} DialogResult=true;}
}

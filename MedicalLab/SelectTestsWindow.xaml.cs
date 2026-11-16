using System.Windows;
using System.Windows.Controls;
namespace MedicalLab;
public partial class SelectTestsWindow : Window
{
 private readonly List<string> names;
 private readonly HashSet<string> selected=new(StringComparer.Ordinal);
 private bool updating;
 public string[] SelectedTests => selected.ToArray();
 public SelectTestsWindow(IEnumerable<string> tests) {names=tests.OrderBy(x=>x).ToList();InitializeComponent();TestList.SelectionChanged+=SelectionChanged;Refresh();}
 private void SearchChanged(object sender,TextChangedEventArgs e) {if(TestList is not null) Refresh();}
 private void Refresh() {updating=true;TestList.ItemsSource=names.Where(n=>n.Contains(Search.Text,StringComparison.OrdinalIgnoreCase)).ToList();foreach(var item in TestList.Items.Cast<string>()) if(selected.Contains(item)) TestList.SelectedItems.Add(item);updating=false;}
 private void SelectionChanged(object sender,SelectionChangedEventArgs e) {if(updating)return;foreach(string name in e.RemovedItems)selected.Remove(name);foreach(string name in e.AddedItems)selected.Add(name);}
 private void Accept(object sender,RoutedEventArgs e) {if(selected.Count==0){MessageBox.Show("اختر تحليلاً واحداً على الأقل.");return;} DialogResult=true;}
}

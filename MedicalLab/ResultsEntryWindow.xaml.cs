using System.Windows;
using System.Windows.Controls;
namespace MedicalLab;
public partial class ResultsEntryWindow : Window
{
 private readonly int orderId;
 private readonly List<PendingTestRow> rows;
 public bool Saved {get;private set;}
 public ResultsEntryWindow(LabOrder order,IEnumerable<PendingTestRow> pending) {orderId=order.Id;rows=pending.ToList();InitializeComponent();PatientTitle.Text=$"المراجع: {order.PatientName} — طلب {order.PatientOrderNumber}";Entries.ItemsSource=rows;Closing+=ConfirmDiscard;}
 private void Save(object sender,RoutedEventArgs e)
 {
  Entries.CommitEdit(DataGridEditingUnit.Cell,true);Entries.CommitEdit(DataGridEditingUnit.Row,true);
  var values=rows.Where(r=>!string.IsNullOrWhiteSpace(r.Input)).ToDictionary(r=>r.Name,r=>r.Input);
  try {using var db=new LabContext();OrderWorkflow.SaveResults(db,orderId,values);Saved=true;MessageBox.Show("تم حفظ النتائج بنجاح.");DialogResult=true;}
  catch(Exception ex) {MessageBox.Show(ex is ArgumentException or InvalidOperationException ? ex.Message : "تعذر حفظ النتائج. لم تُحفظ أي نتيجة من هذه المحاولة.","تحقق من النتائج");}
 }
 private void ConfirmDiscard(object? sender,System.ComponentModel.CancelEventArgs e)
 {
  if(!Saved && rows.Any(r=>!string.IsNullOrWhiteSpace(r.Input)) && MessageBox.Show("لديك نتائج لم تُحفظ. هل تريد إغلاق النافذة دون حفظها؟","نتائج غير محفوظة",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) e.Cancel=true;
 }}

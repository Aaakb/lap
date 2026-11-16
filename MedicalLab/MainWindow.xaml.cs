using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
namespace MedicalLab;
public partial class MainWindow : Window
{
 private int? editingId;
 private int? patientEditingId;
 private int? panelEditingId;
 private bool ready;
 private byte[]? pendingLogo;
 public MainWindow()
 {
  InitializeComponent();
  try { using var db = new LabContext(); db.Initialize(); ready=true; RefreshAll(); }
  catch (Exception ex) { MessageBox.Show("تعذر فتح قاعدة البيانات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error); Close(); }
 }
 private void RefreshAll()
 {
  var selectedPatientId=(PatientChoice.SelectedItem as Patient)?.Id;
  var selectedOrderId=(OrderChoice.SelectedItem as LabOrder)?.Id;
  var selectedTest=TestChoice.SelectedItem as string;
  PendingText.Text=""; PendingChoice.ItemsSource=null; PendingGrid.ItemsSource=null;
  using var db = new LabContext();
  var settings=db.Settings.AsNoTracking().Single(); LabNameInput.Text=settings.Name; LabAddressInput.Text=settings.Address; LabPhoneInput.Text=settings.Phone; pendingLogo=settings.Logo; LogoStatus.Text=pendingLogo is null ? "لا يوجد شعار" : "تم اختيار شعار";
  var panels=db.Panels.AsNoTracking().OrderBy(p=>p.Name).ToList(); PanelsGrid.ItemsSource=panels; PanelChoice.ItemsSource=panels; AuditGrid.ItemsSource=db.AuditEntries.AsNoTracking().OrderByDescending(a=>a.Id).Take(500).ToList();
  var patients = db.Patients.AsNoTracking().OrderByDescending(p=>p.Id).ToList();
  PatientsGrid.ItemsSource = patients; PatientChoice.ItemsSource = patients;
  PatientChoice.SelectedItem=patients.FirstOrDefault(p=>p.Id==selectedPatientId);
  var orders=db.LabOrders.AsNoTracking().OrderByDescending(o=>o.Id).ToList(); OrderChoice.ItemsSource=orders; OrderChoice.SelectedItem=orders.FirstOrDefault(o=>o.Id==selectedOrderId);
  var tests = db.LabTests.AsNoTracking().OrderBy(t=>t.Name).ThenBy(t=>t.MinAge).ToList();
  TestsGrid.ItemsSource = tests; TestChoice.ItemsSource = tests.Where(t=>t.Active).Select(t=>t.Name).Distinct().ToList(); TestChoice.SelectedItem=PendingChoice.SelectedItem as string ?? selectedTest; UpdateResultDetails(null,null);
  if(OrderChoice.SelectedItem is LabOrder activeOrder) { var resultIds=db.OrderResults.Where(x=>x.LabOrderId==activeOrder.Id).Select(x=>x.LabResultId); ResultsGrid.ItemsSource=db.LabResults.AsNoTracking().Where(r=>resultIds.Contains(r.Id)).OrderByDescending(r=>r.Id).ToList(); } else ResultsGrid.ItemsSource=db.LabResults.AsNoTracking().OrderByDescending(r=>r.Id).ToList();
 }
 private void TabChanged(object sender, SelectionChangedEventArgs e) { if (ready && e.Source == Tabs) RefreshAll(); }
 private Patient SavePatient()
 {
  var input=Patient.Create(NameInput.Text,BirthInput.SelectedDate,(SexInput.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "غير محدد");
  using var db=new LabContext();
  if(patientEditingId is int id) {var current=db.Patients.Find(id) ?? throw new ArgumentException("المريض غير موجود."); current.Name=input.Name;current.BirthDate=input.BirthDate;current.Sex=input.Sex;input=current;} else db.Patients.Add(input);
  db.SaveChanges(); return input;
 }
 private void AddPatient(object sender,RoutedEventArgs e)
 {
  try { SavePatient(); NewPatient(null,null); RefreshAll(); } catch(Exception ex) {MessageBox.Show(ex is ArgumentException?ex.Message:"تعذر حفظ المريض.","تنبيه");}
 }
 private void SavePatientAndOrder(object sender,RoutedEventArgs e)
 {
  try {var patient=SavePatient(); NewPatient(null,null); CreateOrderFor(patient);}
  catch(Exception ex) {MessageBox.Show(ex is ArgumentException?ex.Message:"تعذر إكمال العملية. تحقق من قائمة المرضى قبل إعادة المحاولة.","تنبيه");}
 }
 private void SelectedPatientOrder(object sender,RoutedEventArgs e)
 {
  if(PatientsGrid.SelectedItem is not Patient patient) {MessageBox.Show("اختر المريض من القائمة أولاً.");return;}
  try {CreateOrderFor(patient);} catch(Exception) {MessageBox.Show("تعذر إنشاء الطلب.");}
 }
 private void CreateOrderFor(Patient patient)
 {
  using var db=new LabContext();var order=new LabOrder{PatientId=patient.Id,PatientName=patient.Name,CreatedAt=DateTime.Now}; db.LabOrders.Add(order);db.SaveChanges();
  Tabs.SelectedItem=ResultsTab; RefreshAll(); OrderChoice.SelectedItem=((IEnumerable<LabOrder>)OrderChoice.ItemsSource).First(o=>o.Id==order.Id); TestChoice.Focus();
 }
 private void PlanSelectedTest(object sender,RoutedEventArgs e) => RunAction(()=> {var order=SelectedOrder();if(TestChoice.SelectedItem is not string name) throw new ArgumentException("اختر التحليل أولاً.");using var db=new LabContext();OrderWorkflow.PlanTests(db,order.Id,new[]{name});});
 private static bool Number(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
 private void ClearTest(object? sender, RoutedEventArgs? e) { editingId=null; TestsGrid.SelectedItem=null; TestName.Clear(); UnitInput.Clear(); MinAge.Clear(); MaxAge.Clear(); LowInput.Clear(); HighInput.Clear(); RangeSex.SelectedIndex=0; ResultKind.SelectedIndex=0; ExpectedTextInput.Clear(); }
 private void TestSelected(object sender, SelectionChangedEventArgs e)
 {
  if (TestsGrid.SelectedItem is not LabTest t) return;
  ResultKind.SelectedIndex=t.IsText?1:0; ExpectedTextInput.Text=t.ExpectedText; editingId=t.Id; TestName.Text=t.Name; UnitInput.Text=t.Unit; MinAge.Text=t.MinAge.ToString(); MaxAge.Text=t.MaxAge.ToString(); LowInput.Text=t.Low.ToString(); HighInput.Text=t.High.ToString(); RangeSex.SelectedIndex=t.Sex switch { "ذكر"=>1, "أنثى"=>2, _=>0 };
 }
 private void SaveTest(object sender, RoutedEventArgs e)
 {
  bool isText=ResultKind.SelectedIndex==1; double low=0,high=0;
  if(string.IsNullOrWhiteSpace(TestName.Text) || !int.TryParse(MinAge.Text,out var min) || !int.TryParse(MaxAge.Text,out var max) || min<0 || max<min || max>150) { MessageBox.Show("تحقق من الاسم والأعمار."); return; }
  if(!isText && (string.IsNullOrWhiteSpace(UnitInput.Text) || !Number(LowInput.Text,out low) || !Number(HighInput.Text,out high) || !double.IsFinite(low) || !double.IsFinite(high) || low>high)) { MessageBox.Show("تحقق من الوحدة والحدود الرقمية."); return; }
  try {
   using var db=new LabContext(); var name=TestName.Text.Trim(); var sex=(RangeSex.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "الكل";
   var overlap=db.LabTests.AsNoTracking().Where(t=>t.Id!=editingId && t.Active && t.Name==name && t.MinAge<=max && t.MaxAge>=min).ToList().Any(t=>t.Sex=="الكل" || sex=="الكل" || t.Sex==sex);
   if(overlap) { MessageBox.Show("يوجد نطاق فعال متداخل لنفس التحليل والعمر والجنس؛ عدّله أولاً."); return; }
   var test=editingId is null ? new LabTest() : db.LabTests.Find(editingId.Value)!;
   test.Name=name; test.Unit=UnitInput.Text.Trim(); test.MinAge=min; test.MaxAge=max; test.Sex=sex; test.Low=low; test.High=high; test.IsText=isText; test.ExpectedText=isText?ExpectedTextInput.Text.Trim():"";
   if(editingId is null) db.LabTests.Add(test); db.SaveChanges(); ClearTest(null,null); RefreshAll();
  } catch(Exception) { MessageBox.Show("تعذر حفظ التحليل."); }
 }
 private void ToggleTest(object sender,RoutedEventArgs e)
 {
  if(TestsGrid.SelectedItem is not LabTest selected) { MessageBox.Show("اختر نطاقاً أولاً."); return; }
  using var db=new LabContext(); var t=db.LabTests.Find(selected.Id)!;
  if(!t.Active && db.LabTests.AsNoTracking().Where(x=>x.Id!=t.Id && x.Active && x.Name==t.Name && x.MinAge<=t.MaxAge && x.MaxAge>=t.MinAge).ToList().Any(x=>x.Sex=="الكل" || t.Sex=="الكل" || x.Sex==t.Sex)) { MessageBox.Show("يوجد نطاق فعال متداخل؛ لا يمكن تفعيل هذا النطاق."); return; }
  t.Active=!t.Active; db.SaveChanges(); ClearTest(null,null); RefreshAll();
 }
 private void SaveResult(object sender,RoutedEventArgs e)
 {
  if(PatientChoice.SelectedItem is not Patient patient || TestChoice.SelectedItem is not string name || string.IsNullOrWhiteSpace(ValueInput.Text)) { MessageBox.Show("اختر المريض والتحليل وأدخل النتيجة."); return; }
  try {
   if(OrderChoice.SelectedItem is not LabOrder order || order.PatientId!=patient.Id) { MessageBox.Show("اختر طلباً لهذا المريض أو أنشئ طلباً جديداً."); return; }
   using var db=new LabContext(); OrderWorkflow.SaveResult(db,order.Id,name,ValueInput.Text); ValueInput.Clear(); RefreshAll(); if(PendingChoice.SelectedItem is string next) TestChoice.SelectedItem=next; ValueInput.Focus();
  } catch(Exception ex) { MessageBox.Show(ex is ArgumentException or InvalidOperationException ? ex.Message : "تعذر حفظ النتيجة."); }
 }
 private void PrintResult(object sender,RoutedEventArgs e)
 {
  List<LabResult> rows;
  using(var db=new LabContext()) {
   if(OrderChoice.SelectedItem is not LabOrder order) { MessageBox.Show("اختر الطلب المراد طباعته."); return; }
   var ids=db.OrderResults.Where(x=>x.LabOrderId==order.Id).Select(x=>x.LabResultId); rows=db.LabResults.AsNoTracking().Where(r=>ids.Contains(r.Id)).OrderBy(r=>r.Id).ToList();
  }
  try { ReportSelection.Validate(rows); } catch(ArgumentException ex) { MessageBox.Show(ex.Message); return; }
  var r=rows[0];
  using var orderDb=new LabContext(); var printedOrder=orderDb.LabOrders.Find(((LabOrder)OrderChoice.SelectedItem).Id)!;
  var dlg=new PrintDialog(); if(dlg.ShowDialog()!=true) return;
  using var reportDb=new LabContext(); var settings=reportDb.Settings.AsNoTracking().Single(); var doc=LabReport.Build(settings,printedOrder,rows); doc.PageWidth=dlg.PrintableAreaWidth; doc.PageHeight=dlg.PrintableAreaHeight;
  dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "تقرير مختبر تجريبي");
 }
 private void PatientSelected(object sender, SelectionChangedEventArgs e)
 {
  if(PatientsGrid.SelectedItem is not Patient p) return;
  patientEditingId=p.Id; NameInput.Text=p.Name; BirthInput.SelectedDate=p.BirthDate; SexInput.SelectedIndex=p.Sex=="ذكر"?1:p.Sex=="أنثى"?2:0;
 }
 private void NewPatient(object? sender,RoutedEventArgs? e) { patientEditingId=null; PatientsGrid.SelectedItem=null; NameInput.Clear(); BirthInput.SelectedDate=null; SexInput.SelectedIndex=0; }
 private void SearchPatients(object sender,TextChangedEventArgs e)
 {
  if(!ready) return;
  using var db=new LabContext(); var term=PatientSearch.Text.Trim(); PatientsGrid.ItemsSource=db.Patients.AsNoTracking().Where(p=>p.Name.Contains(term) || p.Id.ToString()==term).OrderByDescending(p=>p.Id).ToList();
 }
 private void NewOrder(object sender,RoutedEventArgs e)
 {
  if(PatientChoice.SelectedItem is not Patient patient) { MessageBox.Show("اختر المريض أولاً."); return; }
  try { using var db=new LabContext(); var order=new LabOrder { PatientId=patient.Id,PatientName=patient.Name,CreatedAt=DateTime.Now }; db.LabOrders.Add(order); db.SaveChanges(); RefreshAll(); OrderChoice.SelectedItem=((IEnumerable<LabOrder>)OrderChoice.ItemsSource).First(o=>o.Id==order.Id); }
  catch(Exception) { MessageBox.Show("تعذر إنشاء الطلب."); }
 }
 private void OrderChanged(object sender,SelectionChangedEventArgs e)
 {
  if(!ready || OrderChoice.SelectedItem is not LabOrder order) return;
  PatientChoice.SelectedItem=((IEnumerable<Patient>)PatientChoice.ItemsSource).FirstOrDefault(p=>p.Id==order.PatientId);
  using var db=new LabContext(); var ids=db.OrderResults.Where(x=>x.LabOrderId==order.Id).Select(x=>x.LabResultId); var rows=db.LabResults.AsNoTracking().Where(r=>ids.Contains(r.Id)).OrderByDescending(r=>r.Id).ToList(); ResultsGrid.ItemsSource=rows; var completed=rows.Select(r=>r.TestName).ToHashSet(); var pending=db.PlannedTests.Where(t=>t.LabOrderId==order.Id).Select(t=>t.TestName).ToList().Where(n=>!completed.Contains(n)); var pendingNames=pending.ToList(); var patient=db.Patients.AsNoTracking().Single(p=>p.Id==order.PatientId); var available=db.LabTests.AsNoTracking().Where(t=>t.Active).ToList(); PendingGrid.ItemsSource=pendingNames.Select(name=> { var range=RangeSelector.Find(available.Where(t=>t.Name==name),patient.BirthDate,patient.Sex,order.CreatedAt.Date); return new PendingTestRow {Name=name,Kind=range is null?"Unavailable":range.IsText?"Text":"Number",Unit=range?.Unit ?? "",Reference=range is null?"No matching range":range.IsText?range.ExpectedText:$"{range.Low} - {range.High}"}; }).ToList(); PendingChoice.ItemsSource=pendingNames; PendingText.Text="التحاليل المنتظرة: "+(pendingNames.Count==0?"لا يوجد":string.Join("، ",pendingNames)); if(pendingNames.Count>0) PendingChoice.SelectedIndex=0; UpdateResultDetails(null,null);
 } private void SaveSettings(object sender,RoutedEventArgs e)
 {
  if(string.IsNullOrWhiteSpace(LabNameInput.Text)) { MessageBox.Show("أدخل اسم المختبر."); return; }
  try { using var db=new LabContext(); var settings=db.Settings.Single(); settings.Name=LabNameInput.Text.Trim(); settings.Address=LabAddressInput.Text.Trim(); settings.Phone=LabPhoneInput.Text.Trim(); settings.Logo=pendingLogo; db.SaveChanges(); MessageBox.Show("تم حفظ الإعدادات."); } catch(Exception) { MessageBox.Show("تعذر حفظ الإعدادات."); }
 }
 private void CreateBackup(object sender,RoutedEventArgs e)
 {
  var dialog=new Microsoft.Win32.SaveFileDialog { Filter="SQLite backup|*.db", FileName=$"LabBackup-{DateTime.Now:yyyyMMdd-HHmmss}.db" };
  if(dialog.ShowDialog()!=true) return;
  try { DatabaseBackup.Create(DatabaseBackup.LivePath,dialog.FileName); MessageBox.Show("تم إنشاء النسخة الاحتياطية."); } catch(Exception ex) { MessageBox.Show("تعذر إنشاء النسخة: "+ex.Message); }
 }
 private void RestoreBackup(object sender,RoutedEventArgs e)
 {
  var dialog=new Microsoft.Win32.OpenFileDialog {Filter="SQLite backup|*.db"}; if(dialog.ShowDialog()!=true) return;
  if(MessageBox.Show("ستُستبدل البيانات الحالية ببيانات النسخة المختارة، مع حفظ نسخة من البيانات الحالية قبل الاستعادة. هل تريد المتابعة؟","استعادة البيانات",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
  try { DatabaseBackup.Restore(dialog.FileName,DatabaseBackup.LivePath); using(var db=new LabContext()) db.Initialize(); NewPatient(null,null); ClearTest(null,null); OrderChoice.SelectedItem=null; RefreshAll(); MessageBox.Show("تمت الاستعادة."); } catch(Exception ex) { MessageBox.Show("تعذرت الاستعادة: "+ex.Message); }
 } private void ResultKindChanged(object sender,SelectionChangedEventArgs e)
 {
  if(LowInput is null || HighInput is null || ExpectedTextInput is null) return;
  bool text=ResultKind.SelectedIndex==1; LowInput.IsEnabled=!text; HighInput.IsEnabled=!text; ExpectedTextInput.IsEnabled=text;
 } private void NewPanel(object? sender,RoutedEventArgs? e) { panelEditingId=null; PanelsGrid.SelectedItem=null; PanelNameInput.Clear(); PanelTestsInput.Clear(); }
 private void PanelSelected(object sender,SelectionChangedEventArgs e) { if(PanelsGrid.SelectedItem is PanelDefinition p) { panelEditingId=p.Id; PanelNameInput.Text=p.Name; PanelTestsInput.Text=p.TestNames; } }
 private void SavePanel(object sender,RoutedEventArgs e)
 {
  RunAction(()=> { using var db=new LabContext(); var name=PanelNameInput.Text.Trim(); var names=PanelDefinition.Parse(PanelTestsInput.Text); if(name.Length==0 || names.Length==0) throw new ArgumentException("أدخل الاسم وتحاليل المجموعة."); var available=db.LabTests.Where(t=>t.Active).Select(t=>t.Name).ToList(); if(names.Any(n=>!available.Contains(n))) throw new ArgumentException("يوجد اسم تحليل غير فعال أو غير مطابق للقائمة."); if(db.Panels.Any(p=>p.Id!=panelEditingId && p.Name==name)) throw new ArgumentException("اسم المجموعة موجود."); var panel=panelEditingId is int id?db.Panels.Find(id)!:new PanelDefinition(); panel.Name=name; panel.TestNames=string.Join("\n",names); if(panelEditingId is null) db.Panels.Add(panel); db.SaveChanges(); NewPanel(null,null); });
 }
 private void DeletePanel(object sender,RoutedEventArgs e)
 {
  if(PanelsGrid.SelectedItem is not PanelDefinition panel) return;
  if(MessageBox.Show("حذف تعريف المجموعة؟ تبقى التحاليل المضافة للطلبات السابقة محفوظة.","حذف المجموعة",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return;
  RunAction(()=> {using var db=new LabContext(); db.Panels.Remove(db.Panels.Find(panel.Id)!); db.SaveChanges(); NewPanel(null,null);});
 }
 private LabOrder SelectedOrder() => OrderChoice.SelectedItem as LabOrder ?? throw new ArgumentException("اختر الطلب أولاً.");
 private void ApplyPanel(object sender,RoutedEventArgs e) => RunAction(()=> {var order=SelectedOrder(); if(PanelChoice.SelectedItem is not PanelDefinition panel) throw new ArgumentException("اختر المجموعة."); using var db=new LabContext(); OrderWorkflow.ApplyPanel(db,order.Id,panel.Id);});
 private void ApproveOrder(object sender,RoutedEventArgs e) => RunAction(()=> {using var db=new LabContext(); OrderWorkflow.SetApproval(db,SelectedOrder().Id,true);});
 private void ReopenOrder(object sender,RoutedEventArgs e) { if(MessageBox.Show("إعادة فتح الطلب للتصحيح؟ ستصبح حالته مسودة ويُسجّل ذلك في سجل العمليات.","تصحيح الطلب",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return; RunAction(()=> {using var db=new LabContext(); OrderWorkflow.SetApproval(db,SelectedOrder().Id,false);}); }
 private void DeleteResult(object sender,RoutedEventArgs e)
 {
  if(ResultsGrid.SelectedItem is not LabResult result) { MessageBox.Show("اختر النتيجة المراد تصحيحها."); return; }
  if(MessageBox.Show("حذف النتيجة من المسودة لإدخالها من جديد؟", "تصحيح النتيجة",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) return;
  RunAction(()=> {using var db=new LabContext(); OrderWorkflow.DeleteResult(db,SelectedOrder().Id,result.Id);});
 }
 private void RunAction(Action action) { try {action(); RefreshAll();} catch(Exception ex) { MessageBox.Show(ex is ArgumentException or InvalidOperationException?ex.Message:"تعذر تنفيذ العملية.");} } private void ExportPdf(object sender,RoutedEventArgs e)
 {
  try {
   var order=SelectedOrder(); using var db=new LabContext(); var ids=db.OrderResults.Where(x=>x.LabOrderId==order.Id).Select(x=>x.LabResultId); var rows=db.LabResults.AsNoTracking().Where(r=>ids.Contains(r.Id)).OrderBy(r=>r.Id).ToList();
   var document=LabReport.Build(db.Settings.AsNoTracking().Single(),order,rows);
   var dialog=new Microsoft.Win32.SaveFileDialog {Filter="PDF report|*.pdf",FileName=$"LabReport-{order.Id}.pdf"}; if(dialog.ShowDialog()!=true) return;
   LabReport.SavePdf(document,dialog.FileName); MessageBox.Show("تم حفظ تقرير PDF.");
  } catch(Exception ex) { MessageBox.Show(ex is ArgumentException?ex.Message:"تعذر حفظ التقرير. تحقق من أن الملف غير مفتوح ومن صلاحيات المجلد."); }
 } private void RemovePending(object sender,RoutedEventArgs e)
 {
  RunAction(()=> { var order=SelectedOrder(); if(PendingChoice.SelectedItem is not string name) throw new ArgumentException("اختر التحليل المنتظر أولاً."); using var db=new LabContext(); OrderWorkflow.RequireDraft(db.LabOrders.Find(order.Id)!); var planned=db.PlannedTests.Single(t=>t.LabOrderId==order.Id && t.TestName==name); db.PlannedTests.Remove(planned); OrderWorkflow.Log(db,order.Id,"إزالة تحليل منتظر: "+name); db.SaveChanges(); });
 } private void ChooseLogo(object sender,RoutedEventArgs e)
 {
  var dialog=new Microsoft.Win32.OpenFileDialog {Filter="Image files|*.png;*.jpg;*.jpeg",CheckFileExists=true}; if(dialog.ShowDialog()!=true) return;
  try {
   var file=new System.IO.FileInfo(dialog.FileName); if(file.Length>2_000_000) throw new ArgumentException("حجم الصورة يجب ألا يتجاوز 2 MB.");
   var bytes=System.IO.File.ReadAllBytes(dialog.FileName); using var stream=new System.IO.MemoryStream(bytes); var decoder=System.Windows.Media.Imaging.BitmapDecoder.Create(stream,System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
   if(decoder.Frames[0].PixelWidth>4000 || decoder.Frames[0].PixelHeight>4000) throw new ArgumentException("أبعاد الصورة أكبر من المسموح (4000 × 4000).");
   pendingLogo=bytes; LogoStatus.Text="تم اختيار شعار — اضغط حفظ بيانات المختبر";
  } catch(Exception ex) {MessageBox.Show(ex is ArgumentException?ex.Message:"تعذر قراءة الصورة. اختر ملف PNG أو JPEG صالحاً.");}
 }
 private void RemoveLogo(object sender,RoutedEventArgs e) {pendingLogo=null;LogoStatus.Text="ستُزال الصورة عند حفظ بيانات المختبر";} private void PendingSelected(object sender,SelectionChangedEventArgs e)
 {
  if(!ready || PendingChoice.SelectedItem is not string name) return;
  TestChoice.SelectedItem=name;
  UpdateResultDetails(null,null);
 }
 private void UpdateResultDetails(object? sender,SelectionChangedEventArgs? e)
 {
  if(!ready || ResultDetails is null) return;
  if(OrderChoice.SelectedItem is not LabOrder order || TestChoice.SelectedItem is not string name) {ResultDetails.Text="اختر طلباً وتحليلاً لعرض المرجع.";ResultDetails.FlowDirection=FlowDirection.RightToLeft;return;}
  using var db=new LabContext(); var patient=db.Patients.AsNoTracking().SingleOrDefault(p=>p.Id==order.PatientId);
  if(patient is null) {ResultDetails.Text="بيانات المريض غير موجودة.";ResultDetails.FlowDirection=FlowDirection.RightToLeft;return;}
  var ranges=db.LabTests.AsNoTracking().Where(t=>t.Active && t.Name==name).ToList(); var range=RangeSelector.Find(ranges,patient.BirthDate,patient.Sex,order.CreatedAt.Date);
  if(range is null) {ResultDetails.Text="لا يوجد نطاق مطابق؛ تحقق من تاريخ الميلاد والجنس ونطاقات التحليل.";ResultDetails.FlowDirection=FlowDirection.RightToLeft;return;}
  ResultDetails.FlowDirection=FlowDirection.LeftToRight;
  ResultDetails.Text=range.IsText ? $"{name}  |  Text result  |  Reference: {(string.IsNullOrWhiteSpace(range.ExpectedText)?"—":range.ExpectedText)}" : $"{name}  |  Numeric result  |  Reference: {range.Low} - {range.High}  |  Unit: {range.Unit}";
 } private void ChooseTests(object sender,RoutedEventArgs e)
 {
  try {var order=SelectedOrder(); using var db=new LabContext(); OrderWorkflow.RequireDraft(db.LabOrders.Find(order.Id)!); var tests=db.LabTests.AsNoTracking().Where(t=>t.Active).Select(t=>t.Name).Distinct().ToList();var dialog=new SelectTestsWindow(tests){Owner=this};if(dialog.ShowDialog()!=true)return;OrderWorkflow.PlanTests(db,order.Id,dialog.SelectedTests);RefreshAll();}
  catch(Exception ex){MessageBox.Show(ex is ArgumentException or InvalidOperationException?ex.Message:"تعذر إضافة التحاليل.");}
 }
 private void PatientHistory(object sender,RoutedEventArgs e)
 {
  if(PatientsGrid.SelectedItem is not Patient patient){MessageBox.Show("اختر المريض أولاً.");return;}
  Tabs.SelectedItem=ResultsTab;RefreshAll();PatientChoice.SelectedItem=((IEnumerable<Patient>)PatientChoice.ItemsSource).FirstOrDefault(p=>p.Id==patient.Id);
  using var db=new LabContext();var orders=db.LabOrders.AsNoTracking().Where(o=>o.PatientId==patient.Id).OrderByDescending(o=>o.Id).ToList();OrderChoice.ItemsSource=orders;if(orders.Count>0)OrderChoice.SelectedIndex=0;else MessageBox.Show("لا توجد طلبات سابقة لهذا المريض.");
 } private void PendingRowSelected(object sender,SelectionChangedEventArgs e)
 {
  if(!ready || PendingGrid.SelectedItem is not PendingTestRow row) return;
  PendingChoice.SelectedItem=row.Name; TestChoice.SelectedItem=row.Name; UpdateResultDetails(null,null); ValueInput.Focus();
 } private void OpenResultsEntry(object sender,RoutedEventArgs e)
 {
  try {var order=SelectedOrder();if(order.Approved) throw new InvalidOperationException("الطلب معتمد؛ أعد فتحه للتصحيح أولاً.");var rows=(PendingGrid.ItemsSource as IEnumerable<PendingTestRow>)?.ToList() ?? new();if(rows.Count==0)throw new ArgumentException("اختر التحاليل المطلوبة أولاً، أو لا توجد نتائج منتظرة.");var window=new ResultsEntryWindow(order,rows){Owner=this};window.ShowDialog();if(window.Saved)RefreshAll();}
  catch(Exception ex){MessageBox.Show(ex is ArgumentException or InvalidOperationException?ex.Message:"تعذر فتح جدول النتائج.");}
 }}

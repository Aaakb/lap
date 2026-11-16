using System.Threading;
using System.Windows;
namespace MedicalLab;
public partial class App : Application
{
 private Mutex? singleInstance;
 protected override void OnStartup(StartupEventArgs e)
 {
  singleInstance=new Mutex(true,"Local\\AlAsmaiLab.SingleUser",out var first);
  if(!first) { MessageBox.Show("البرنامج مفتوح بالفعل. استخدم النافذة الحالية."); Shutdown(); return; }
  base.OnStartup(e);
 }
 protected override void OnExit(ExitEventArgs e) { singleInstance?.Dispose(); base.OnExit(e); }
}

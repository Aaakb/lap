using System.Windows;

namespace MedicalLab;

public partial class PatientVisitsWindow : Window
{
    public LabOrder? SelectedVisit => Visits.SelectedItem as LabOrder;

    public PatientVisitsWindow(Patient patient, IReadOnlyList<LabOrder> visits)
    {
        InitializeComponent();
        PatientHeading.Text = $"زيارات {patient.Name} ({visits.Count})";
        Visits.ItemsSource = visits;
        Visits.SelectedIndex = 0;
    }

    private void OpenVisit(object sender, RoutedEventArgs e)
    {
        if (SelectedVisit is not null) DialogResult = true;
    }
}

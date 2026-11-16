using MedicalLab;
namespace MedicalLab.Tests;
public class WorkflowTests
{
 [Fact] public void ApprovalRejectsEmptyAndIncompleteOrders() { Assert.Throws<ArgumentException>(()=>OrderWorkflow.ValidateApproval(0,0)); Assert.Throws<ArgumentException>(()=>OrderWorkflow.ValidateApproval(1,1)); OrderWorkflow.ValidateApproval(2,0); }
 [Fact] public void ApprovedOrderCannotBeEdited() => Assert.Throws<InvalidOperationException>(()=>OrderWorkflow.RequireDraft(new LabOrder {Approved=true}));
 [Fact] public void PanelNamesAreTrimmedAndDeduplicated() => Assert.Equal(new[]{"A","B"},PanelDefinition.Parse(" A\nB\nA\n "));
}

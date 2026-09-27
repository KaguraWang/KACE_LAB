using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ChemLab.Game {
// A keyboard Submit confirms the amount, regardless of the last clicked modal
// button. In particular it must not toggle g/mol before LateUpdate confirms it.
public sealed class DoseModalButton:Button {
    public LabUI Owner;
    public override void OnSubmit(BaseEventData eventData){
        if(!IsActive()||!IsInteractable())return;
        Owner?.ConfirmDose();eventData.Use();
    }
}
}

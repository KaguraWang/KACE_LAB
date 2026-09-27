using UnityEngine;
using UnityEngine.EventSystems;
namespace ChemLab.Game {
public sealed class ReagentDrag:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler {
    public LabUI Owner;public string SubstanceId;
    public void OnBeginDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Owner.BeginReagentDrag(SubstanceId,e.position);}
    public void OnDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Owner.MoveReagentDrag(e.position);}
    public void OnEndDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Owner.EndReagentDrag(e.position);}
    void OnDisable(){if(Owner!=null)Owner.CancelReagentDrag();}
}
}

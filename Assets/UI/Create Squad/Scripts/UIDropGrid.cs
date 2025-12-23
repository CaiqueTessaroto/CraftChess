using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDropGrid : MonoBehaviour, IDropHandler
{
    public Transform gridParent;
    private SquadManager squadManager;

    void Start()
    {

        squadManager = FindObjectOfType<SquadManager>();

    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        UIDragItem uIDragItem = dropped.GetComponent<UIDragItem>();

        MovementConfigData config = JsonUtility.FromJson<MovementConfigData>(uIDragItem.Json);

        if (dropped == null) return;

        if (gridParent.childCount >= 4)
            return;

        StartCoroutine(squadManager.LoadPiecesImage(uIDragItem.name, config.piece.Squad, gridParent, uIDragItem.RootPath));

        //dropped.GetComponent<CanvasGroup>().blocksRaycasts = true;
    }

}

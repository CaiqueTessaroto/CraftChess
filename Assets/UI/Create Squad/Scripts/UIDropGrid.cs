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

        if (uIDragItem.name == squadManager.currentPieceName)
            return;

        SquadPieceData pieceData = squadManager.squadData.Pieces.Find(p => p.NameInSquad == squadManager.currentPieceName);

        if (gameObject.name == "Promotion")
        {
            //if (pieceData.PromotionPieces.Count < 4)
            if (!pieceData.PromotionPieces.Contains(uIDragItem.name))
            {
                pieceData.PromotionPieces.Add(uIDragItem.name);
                StartCoroutine(squadManager.LoadPiecesImage(uIDragItem.name, config.piece.Squad, gridParent, uIDragItem.RootPath));
            }

        }
        else if (gameObject.name == "Casteling")
        {
            if (!pieceData.CastlingPieces.Contains(uIDragItem.name))
            {
                pieceData.CastlingPieces.Add(uIDragItem.name);
                StartCoroutine(squadManager.LoadPiecesImage(uIDragItem.name, config.piece.Squad, gridParent, uIDragItem.RootPath));
            }
        }

        //StartCoroutine(squadManager.LoadPiecesImage(uIDragItem.name, config.piece.Squad, gridParent, uIDragItem.RootPath));

        //dropped.GetComponent<CanvasGroup>().blocksRaycasts = true;
    }

}

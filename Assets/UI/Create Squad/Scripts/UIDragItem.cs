using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    //public GameObject dragPrefab;
    private SquadManager squadManager;

    private GameObject draggingObject;
    private Canvas canvas;
    public string NameInSquad;
    public string Json;
    public Sprite Sprite;
    public string RootPath;

    void Start()
    {

        squadManager = FindFirstObjectByType<SquadManager>();

        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {

        draggingObject = Instantiate(squadManager.dragPrefab, canvas.transform);

        //Image image = draggingObject.AddComponent<Image>();
        //image.sprite = Sprite;
        //image.sprite = GetComponent<Image>().sprite;

        draggingObject.GetComponent<Image>().sprite = GetComponent<Image>().sprite;//GetComponent<Image>().sprite;
        //CanvasGroup canvasGroup = draggingObject.AddComponent<CanvasGroup>();

        draggingObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
        //canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        draggingObject.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Destroy(draggingObject);
    }


    public void GetPiece(string nameInSquad, string json, Sprite sprite, string rootPath)
    {
        NameInSquad = nameInSquad;
        Json = json;
        Sprite = sprite;
        RootPath = rootPath;
        
    }
}

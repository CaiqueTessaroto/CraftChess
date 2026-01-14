using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PixelCellPainter : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    public Vector2Int position;
    private PaintingGridManager gridManager;


    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    public void Init(Vector2Int pos, PaintingGridManager manager)
    {
        position = pos;
        gridManager = manager;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if (gridManager.OnSelecting && !gridManager.isSelecting)
        {
            if (gridManager.selectionStart == null)
            {
                // Primeiro clique define início da seleção
                gridManager.selectionStart = position;
            }
            else
            {
                // Segundo clique define fim e cria seleção
                gridManager.CreateSelection(gridManager.selectionStart.Value, position);
                gridManager.selectionStart = null;
            }
        }
        else if (gridManager.isMoveSelection)
        {
            gridManager.MoveSelectionTo(position);
            gridManager.ClearSelection();
            gridManager.UpdatePreview();
        }
        else
        {

            if (!gridManager.isPainting && !gridManager.eyedropperMode)
            {
                gridManager.SaveStateForUndo();
            }


            if (eventData.button == PointerEventData.InputButton.Left)
            {
                gridManager.isPainting = true;
                Paint();
            }
        }


    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Input.GetMouseButton(0)) // Botão esquerdo segurado
        {
            if (!gridManager.isPainting && !gridManager.eyedropperMode && !gridManager.OnSelecting)
            {
                gridManager.SaveStateForUndo();
            }

            gridManager.isPainting = true;
            Paint();
        }

    }


    private void Paint()
    {

        if (gridManager.paintBucketMode || gridManager.eraseAll)
        {
            gridManager.FloodFill(position);
            //gridManager.paintBucketMode = false;
            //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (gridManager.eyedropperMode)
        {
            float alpha = 60f / 255f;
            Color color = new Color(1, 1, 1, alpha);
            if (image.color != color)
            {
                // Ativa o modo conta-gotas: pega a cor da célula e define como a cor atual
                gridManager.SetSelectedColor(image.color);
            }

            gridManager.DisableTools();
            //gridManager.eyedropperMode = false; // Desliga o modo após pegar a cor
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else if (gridManager.circleMode)
        {
            if (gridManager.circleStart == null)
            {
                // Primeiro clique: guarda a 1ª extremidade
                gridManager.circleStart = position;
                //Debug.Log("test");
            }
            else
            {
                if (gridManager.circleStart != position)
                {
                    // Segundo clique: usa como 2ª extremidade
                    Vector2Int pointA = gridManager.circleStart.Value;
                    Vector2Int pointB = position;

                    // Centro = ponto médio
                    Vector2Int center = new Vector2Int(
                        Mathf.RoundToInt((pointA.x + pointB.x) / 2f),
                        Mathf.RoundToInt((pointA.y + pointB.y) / 2f)
                    );

                    // Raio = metade da distância
                    int radius = Mathf.RoundToInt(Vector2Int.Distance(pointA, pointB) / 2f);

                    gridManager.DrawCircle(center, radius);

                    // Reseta para poder criar outro círculo
                    gridManager.circleStart = null;
                    //gridManager.circleMode = false;
                }
            }
        }
        else if (gridManager.lineMode)
        {
            if (gridManager.lineStart == null)
            {
                gridManager.lineStart = position; // primeiro clique
            }
            else
            {
                if (gridManager.lineStart != position)
                {
                    gridManager.DrawLine(gridManager.lineStart.Value, position); // segundo clique
                    gridManager.lineStart = null; // reset
                }
            }
        }
        else if (gridManager.rectMode)
        {
            if (gridManager.rectStart == null)
            {
                gridManager.rectStart = position; // Primeiro clique
            }
            else
            {
                if (gridManager.rectStart != position)
                {
                    gridManager.DrawRectangle(gridManager.rectStart.Value, position, filled: false); // ou true
                    gridManager.rectStart = null; // reset
                }
            }
        }
        else if (gridManager.shadowMode)
        {
            if (image.color != gridManager.baseGridColor)
            {
                Color c = image.color;
                c.r *= 0.7f; // 30% mais escuro no vermelho
                c.g *= 0.7f; // 30% mais escuro no verde
                c.b *= 0.7f; // 30% mais escuro no azul
                image.color = c;
            }
        }
        else
        {
            // Comum: pinta com a cor atual selecionada
            image.color = gridManager.selectedColor;
        }

        gridManager.UpdatePreview();

    }







}

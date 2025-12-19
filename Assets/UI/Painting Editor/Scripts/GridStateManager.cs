using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class GridStateManager : MonoBehaviour
{
    private List<Drawing> undoStack = new List<Drawing>();
    private int maxUndo = 20;

    public PaintingGridManager gridManager; // seu script do grid

    // 🔹 Salva o estado atual do grid para o Undo


    void Start()
    {
        if (gridManager == null)
        {
            gridManager = FindObjectOfType<PaintingGridManager>();
        }
    }
    public void SaveStateForUndo()
    {
        // Cria um snapshot do grid atual
        Drawing snapshot = new Drawing();
        snapshot.list = new List<PixelData>();

        foreach (var kvp in gridManager.gridCells)
        {
            Image img = kvp.Value.GetComponent<Image>();
            Color c = img.color;

            // Salva apenas pixels que não são transparentes
            if (c.a > 0f)
            {
                snapshot.list.Add(new PixelData
                {
                    x = kvp.Key.x,
                    y = kvp.Key.y,
                    r = c.r,
                    g = c.g,
                    b = c.b,
                    a = c.a
                });
            }
        }

        snapshot.scale = gridManager.currentScale;

        // Mantém máximo de 5 estados
        if (undoStack.Count >= maxUndo)
        {
            undoStack.RemoveAt(0);
        }

        undoStack.Add(snapshot);

        //Debug.Log("Estado salvo para Undo. Total: " + undoStack.Count);
    }

    // 🔹 Restaura o último estado salvo
    public void Undo()
    {
        if (undoStack.Count == 0)
        {
            //Debug.LogWarning("Nenhum estado para desfazer!");
            return;
        }

        Drawing lastState = undoStack[undoStack.Count - 1];
        undoStack.RemoveAt(undoStack.Count - 1);

        gridManager.UpdatePaitingGrid(lastState.list, lastState.scale);
        //Debug.Log("Undo aplicado. Restam: " + undoStack.Count);
    }
}

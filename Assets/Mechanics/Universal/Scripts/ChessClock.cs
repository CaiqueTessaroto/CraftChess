using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Responsável apenas por UI do relógio e efeito de blink de check.
/// Sem NetworkObject, sem lógica de tempo.
/// </summary>
public class ChessClock : MonoBehaviour
{
    [Header("References")]
    public PieceController pieceController;
    public ChessClockNetwork chessClockNetwork;

    [Header("UI")]
    public GameObject ChessClockPanel;
    public VerticalLayoutGroup verticalLayoutGroup;
    public TMP_Text timeWhite;
    public TMP_Text timeBlack;
    public GameObject pointWhite;
    public GameObject pointBlack;

    // ───────────────────────────────────────────────────────────────────
    void Start()
    {
        if (pieceController == null)
            pieceController = FindFirstObjectByType<PieceController>();

        if (chessClockNetwork == null)
            chessClockNetwork = FindFirstObjectByType<ChessClockNetwork>();

        chessClockNetwork.StartClock();
    }

    // ── API chamada pelo ChessClockNetwork via RPC ──────────────────────

    /// <summary>Atualiza os displays de tempo. Chamado pelo RPC de broadcast.</summary>
    public void UpdateClock(float white, float black)
    {
        if (timeWhite != null) timeWhite.text = FormatTime(white);
        if (timeBlack != null) timeBlack.text = FormatTime(black);
    }

    // ── Utilitário ──────────────────────────────────────────────────────

    private static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    // ── Check blink ─────────────────────────────────────────────────────

    private Coroutine pointBlinkRoutine;
    public void UpdateTurnIndicator(bool isWhiteTurn)
    {
        GameObject active = isWhiteTurn ? pointWhite : pointBlack;
        GameObject inactive = isWhiteTurn ? pointBlack : pointWhite;

        SpriteRenderer inactiveSr = inactive?.GetComponent<SpriteRenderer>();
        if (inactiveSr != null) inactiveSr.enabled = false;

        if (pointBlinkRoutine != null) StopCoroutine(pointBlinkRoutine);
        pointBlinkRoutine = StartCoroutine(BlinkOverlay(active));
    }

    public void StopTurnIndicator()
    {
        if (pointBlinkRoutine != null)
        {
            StopCoroutine(pointBlinkRoutine);
            pointBlinkRoutine = null;
        }

        // apaga os dois
        SpriteRenderer srW = pointWhite?.GetComponent<SpriteRenderer>();
        SpriteRenderer srB = pointBlack?.GetComponent<SpriteRenderer>();
        if (srW != null) srW.enabled = false;
        if (srB != null) srB.enabled = false;
    }

    private IEnumerator BlinkOverlay(GameObject overlay)
    {
        SpriteRenderer sr = overlay.GetComponent<SpriteRenderer>();

        while (!pieceController.endGame)
        {
            if (sr != null) sr.enabled = true;
            yield return new WaitForSeconds(0.4f);
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(0.4f);
        }

        if (sr != null) sr.enabled = false;
    }
}
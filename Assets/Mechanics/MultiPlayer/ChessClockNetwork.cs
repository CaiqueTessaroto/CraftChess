using UnityEngine;
using System.Collections;
using Unity.Netcode;

/// <summary>
/// Autoridade de rede do relógio. Roda apenas lógica de tempo e RPCs.
/// Deve estar no mesmo GameObject que tem NetworkObject.
/// </summary>
public class ChessClockNetwork : NetworkBehaviour
{
    [Header("References")]
    public ChessClock chessClock;
    public PieceController pieceController;

    [Header("Settings")]
    public float matchDurationSeconds = 600f; // 10 minutos

    // ── Estado interno (host only) ──────────────────────────────────────
    private float _timeWhite;
    private float _timeBlack;
    private bool _clockRunning;
    private bool _isWhiteTurn;

    private Coroutine _tickRoutine;

    /// <summary>Inicia o relógio. Chame no host ao começar a partida.</summary>
    public void StartClock()
    {

        if (pieceController == null)
            pieceController = FindFirstObjectByType<PieceController>();

        if (chessClock == null)
            chessClock = FindFirstObjectByType<ChessClock>();

        if (!IsHost) return;

        _timeWhite = matchDurationSeconds;
        _timeBlack = matchDurationSeconds;
        _isWhiteTurn = true;
        _clockRunning = true;

        if (_tickRoutine != null) StopCoroutine(_tickRoutine);
        _tickRoutine = StartCoroutine(TickRoutine());

        chessClock?.UpdateTurnIndicator(_isWhiteTurn);
    }

    /// <summary>Troca o turno do relógio. Chame no host após cada jogada confirmada.</summary>
    public void SwitchTurn(bool isWhiteTurn)
    {
        if (!IsHost) return;
        _isWhiteTurn = isWhiteTurn;

        chessClock?.UpdateTurnIndicator(_isWhiteTurn);
    }

    /// <summary>Para o relógio. Chame no host ao encerrar a partida.</summary>
    public void StopClock()
    {
        if (!IsHost) return;

        _clockRunning = false;
        if (_tickRoutine != null)
        {
            StopCoroutine(_tickRoutine);
            _tickRoutine = null;
        }
    }

    // ── Loop de tick (host only) ────────────────────────────────────────

    private IEnumerator TickRoutine()
    {
        while (_clockRunning)
        {
            if (pieceController.endGame)
            {
                chessClock?.UpdateTurnIndicator(_isWhiteTurn);
                _clockRunning = false;
                yield break;
            }

            yield return new WaitForSeconds(1f);

            if (!_clockRunning) yield break;

            if (_isWhiteTurn)
            {
                _timeWhite -= 1f;
                if (_timeWhite <= 0f)
                {
                    _timeWhite = 0f;
                    BroadcastClockClientRpc(_timeWhite, _timeBlack);
                    HandleTimeoutClientRpc(winnerIsWhite: false);
                    yield break;
                }
            }
            else
            {
                _timeBlack -= 1f;
                if (_timeBlack <= 0f)
                {
                    _timeBlack = 0f;
                    BroadcastClockClientRpc(_timeWhite, _timeBlack);
                    HandleTimeoutClientRpc(winnerIsWhite: true);
                    yield break;
                }
            }

            BroadcastClockClientRpc(_timeWhite, _timeBlack);
        }
    }

    // ── RPCs ────────────────────────────────────────────────────────────

    [ClientRpc]
    private void BroadcastClockClientRpc(float white, float black)
    {
        chessClock?.UpdateClock(white, black);
    }

    [ClientRpc]
    private void HandleTimeoutClientRpc(bool winnerIsWhite)
    {
        _clockRunning = false;
        chessClock?.UpdateTurnIndicator(_isWhiteTurn);
        pieceController?.SetEndGame(!winnerIsWhite, winnerIsWhite, false);

    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;

public class MatchGameOptionsSync : MonoBehaviour
{
    [Header("UI")]
    public Toggle noRulesToggle;
    public Toggle noTurnsToggle;

    private bool isHost = false;

    private void Start()
    {
        if (NetworkLobbyManager.Instance.currentLobby == null) return;

        isHost = NetworkLobbyManager.Instance.currentLobby.HostId
                 == AuthenticationService.Instance.PlayerId;

        noRulesToggle.interactable = isHost;
        noTurnsToggle.interactable = isHost;

        if (isHost)
        {
            noRulesToggle.onValueChanged.AddListener(OnNoRulesChanged);
            noTurnsToggle.onValueChanged.AddListener(OnNoTurnsChanged);
        }
    }

    private void OnEnable()  => LobbyDataSync.Instance.OnLobbyDataUpdated += ApplyLobbyDataToUI;
    private void OnDisable() => LobbyDataSync.Instance.OnLobbyDataUpdated -= ApplyLobbyDataToUI;

    // ── Listeners (host only) ────────────────────────────────────────────────

    private async void OnNoRulesChanged(bool value)
    {
        noRulesToggle.interactable = false;
        try
        {
            await UpdateLobbyData("NoRules", value.ToString());
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Falha ao atualizar NoRules: {e.Message}");
            noRulesToggle.SetIsOnWithoutNotify(!value);
        }
        finally
        {
            noRulesToggle.interactable = true;
        }
    }

    private async void OnNoTurnsChanged(bool value)
    {
        noTurnsToggle.interactable = false;
        try
        {
            await UpdateLobbyData("NoTurns", value.ToString());
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Falha ao atualizar NoTurns: {e.Message}");
            noTurnsToggle.SetIsOnWithoutNotify(!value);
        }
        finally
        {
            noTurnsToggle.interactable = true;
        }
    }

    // ── Rede ─────────────────────────────────────────────────────────────────

    private async Task UpdateLobbyData(string key, string value)
    {
        if (NetworkLobbyManager.Instance.currentLobby == null) return;

        await LobbyService.Instance.UpdateLobbyAsync(
            NetworkLobbyManager.Instance.currentLobby.Id,
            new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        key,
                        new DataObject(DataObject.VisibilityOptions.Public, value)
                    }
                }
            });
    }

    // ── UI ───────────────────────────────────────────────────────────────────

    private void ApplyLobbyDataToUI(Dictionary<string, DataObject> data)
    {
        if (data == null) return;

        if (data.TryGetValue("NoRules", out var noRules))
            noRulesToggle.SetIsOnWithoutNotify(bool.Parse(noRules.Value));

        if (data.TryGetValue("NoTurns", out var noTurns))
            noTurnsToggle.SetIsOnWithoutNotify(bool.Parse(noTurns.Value));
    }
}
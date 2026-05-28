using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MatchStartOption : MonoBehaviour
{
    [Header("UI")]
    public Toggle[] startOption_toggles;
    public Image crowPlayer1;
    public Image crowPlayer2;

    [Header("Sprites")]
    public Sprite crowWhite;
    public Sprite crowBlack;
    public Sprite crowRandon;

    private const string LOBBY_KEY_CHOICE   = "StartOptionChoice";
    private const string LOBBY_KEY_RESOLVED = "StartOptionResolved";

    private bool isHost = false;
    private string _lastStartOption = "";

    private void Start()
    {
        if (NetworkLobbyManager.Instance.currentLobby == null) return;

        isHost = NetworkLobbyManager.Instance.currentLobby.HostId
                 == AuthenticationService.Instance.PlayerId;

        foreach (Toggle toggle in startOption_toggles)
        {
            toggle.interactable = isHost;

            if (isHost)
            {
                toggle.onValueChanged.AddListener((bool isOn) =>
                {
                    if (!isOn) return;

                    string cleanName = toggle.name.Replace(" (Toggle)", "");

                    if (Enum.TryParse(cleanName, true, out StartOption option))
                    {
                        _ = UpdateLobbyData(option);
                        UpdateCrowns(option);
                    }
                });
            }
        }
    }

    private void OnEnable()
    {
        if (LobbyDataSync.Instance != null)
            LobbyDataSync.Instance.OnLobbyDataUpdated += ApplyLobbyDataToUI;
    }

    private void OnDisable()
    {
        if (LobbyDataSync.Instance != null)
            LobbyDataSync.Instance.OnLobbyDataUpdated -= ApplyLobbyDataToUI;
    }

    // ── Rede ─────────────────────────────────────────────────────────────────

    private async Task UpdateLobbyData(StartOption chosen)
    {
        if (NetworkLobbyManager.Instance.currentLobby == null) return;

        StartOption resolved = chosen == StartOption.Random
            ? (UnityEngine.Random.value > 0.5f ? StartOption.White : StartOption.Black)
            : chosen;

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(
                NetworkLobbyManager.Instance.currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { LOBBY_KEY_CHOICE,   new DataObject(DataObject.VisibilityOptions.Public, chosen.ToString()) },
                        { LOBBY_KEY_RESOLVED, new DataObject(DataObject.VisibilityOptions.Public, resolved.ToString()) }
                    }
                });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Falha ao atualizar StartOption: {e.Message}");
        }
    }

    // ── UI ───────────────────────────────────────────────────────────────────

    private void ApplyLobbyDataToUI(Dictionary<string, DataObject> data)
    {
        if (data == null) return;

        // UI: preserva o sprite Random
        if (data.TryGetValue(LOBBY_KEY_CHOICE, out var choiceData))
            if (Enum.TryParse(choiceData.Value, true, out StartOption displayOption))
            {
                if (!isHost)
                    UpdateCrowns(displayOption);

                if (choiceData.Value != _lastStartOption)
                {
                    _lastStartOption = choiceData.Value;
                    OnHostChangedOption();
                }
            }

        // Lógica: valor já resolvido (White ou Black)
        if (data.TryGetValue(LOBBY_KEY_RESOLVED, out var resolvedData))
            if (Enum.TryParse(resolvedData.Value, true, out StartOption resolvedOption))
                MultiplayerLobbyUI.Instance.startOption = resolvedOption;
    }

    private void OnHostChangedOption()
    {
        if (isHost) return;

        MultiplayerLobbyUI.Instance.UpdateReadyUI(false);
        MultiplayerLobbyState.SendReadyStateToHost(false);
    }

    private void UpdateCrowns(StartOption option)
    {
        switch (option)
        {
            case StartOption.White:
                crowPlayer1.sprite = crowWhite;
                crowPlayer2.sprite = crowBlack;
                break;

            case StartOption.Random:
                crowPlayer1.sprite = crowRandon;
                crowPlayer2.sprite = crowRandon;
                break;

            case StartOption.Black:
                crowPlayer1.sprite = crowBlack;
                crowPlayer2.sprite = crowWhite;
                break;

            default:
                crowPlayer1.sprite = crowWhite;
                crowPlayer2.sprite = crowBlack;
                break;
        }
    }
}
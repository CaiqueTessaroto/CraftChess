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

    private const string LOBBY_KEY = "StartOption";
    private bool isHost = false;

    private void Start()
    {

        if (NetworkLobbyManager.Instance.currentLobby == null) return;

        // Notifica subscribers que já estavam ativos antes do LobbyDataSync existir
        //OnLobbyDataUpdated?.Invoke(LobbyManager.Instance.currentLobby.Data);

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

    private async Task UpdateLobbyData(StartOption option)
    {
        if (NetworkLobbyManager.Instance.currentLobby == null) return;

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(
                NetworkLobbyManager.Instance.currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {
                            LOBBY_KEY,
                            new DataObject(DataObject.VisibilityOptions.Public, option.ToString())
                        }
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
        if (data == null)
            return;

        if (!data.TryGetValue(LOBBY_KEY, out var startOptionData))
            return;

        if (Enum.TryParse(startOptionData.Value, true, out StartOption option))
            if(!isHost)
                UpdateCrowns(option);
        
    }

    private void UpdateCrowns(StartOption option)
    {
        MultiplayerLobbyUI.Instance.startOption = option;
        
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
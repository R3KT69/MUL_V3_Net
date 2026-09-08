using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.Modules;
using UnityEngine;

public class PlayerIdRegistry : MonoBehaviour
{
    public static PlayerIdRegistry Instance { get; private set; }

    private readonly HashSet<PlayerID> connectedPlayers = new();
    private PlayersManager playersManager;

    public PlayerID? LocalPlayerId => playersManager?.localPlayerId;
    public IReadOnlyCollection<PlayerID> ConnectedPlayers => connectedPlayers;

    public event Action<PlayerID> PlayerJoined;
    public event Action<PlayerID> PlayerLeft;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();
        if (networkManager == null ||
            !networkManager.TryGetModule<PlayersManager>(true, out playersManager))
            return;

        foreach (PlayerID player in playersManager.players)
            connectedPlayers.Add(player);

        playersManager.onPlayerJoined += HandlePlayerJoined;
        playersManager.onPlayerLeft += HandlePlayerLeft;
    }

    void OnDestroy()
    {
        if (playersManager != null)
        {
            playersManager.onPlayerJoined -= HandlePlayerJoined;
            playersManager.onPlayerLeft -= HandlePlayerLeft;
        }

        if (Instance == this)
            Instance = null;
    }

    public bool IsConnected(PlayerID playerId)
    {
        return connectedPlayers.Contains(playerId);
    }

    private void HandlePlayerJoined(PlayerID playerId, bool isReconnect, bool asServer)
    {
        if (!asServer)
            return;

        if (connectedPlayers.Add(playerId))
            PlayerJoined?.Invoke(playerId);
    }

    private void HandlePlayerLeft(PlayerID playerId, bool asServer)
    {
        if (!asServer)
            return;

        if (connectedPlayers.Remove(playerId))
            PlayerLeft?.Invoke(playerId);
    }
}

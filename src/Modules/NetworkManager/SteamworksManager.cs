using System;
using Steamworks;

namespace NetworkManager
{
  public class SteamworksManager
  {
    private Callback<P2PSessionRequest_t> p2pSessionRequestCallback;

    public SteamworksManager()
    {
      // Initialize Steamworks
      if (!SteamAPI.Init())
      {
        throw new Exception("Failed to initialize Steamworks.");
      }

      string name = SteamFriends.GetPersonaName();
      Console.WriteLine(name);

      // Register the P2P session request callback
      p2pSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
    }
    public void Update()
    {
      // Update Steamworks
      SteamAPI.RunCallbacks();
    }

    public void Shutdown()
    {
      // Shutdown Steamworks
      SteamAPI.Shutdown();
    }

    private void OnP2PSessionRequest(P2PSessionRequest_t callback)
    {
      // Accept the P2P session request
      SteamNetworking.AcceptP2PSessionWithUser(callback.m_steamIDRemote);
    }
  }
}
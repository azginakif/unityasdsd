using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace MobilOfl.Online
{
    public class RelayNetworkBootstrap : MonoBehaviour
    {
        public const string CurrentBackendLabel = "Relay SDK yolu";
        public const string RecommendedBackendLabel = "Unity Multiplayer Services SDK";

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport unityTransport;
        [SerializeField] private NetworkCaseState networkCaseState;
        [SerializeField] private GameObject offlineScenePlayerRoot;
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private string relayConnectionType = "dtls";
        [SerializeField] private bool autoInitializeUnityServices = true;

        public event Action<string> StatusChanged;
        public event Action<string> JoinCodeChanged;

        public string CurrentJoinCode { get; private set; } = string.Empty;
        public string CurrentStatus { get; private set; } = "Offline hazir.";
        public bool IsBusy { get; private set; }
        public int ConnectedClientCount => networkManager != null ? networkManager.ConnectedClientsIds.Count : 0;
        public bool IsOnlineSessionActive => networkManager != null && networkManager.IsListening;
        public bool UsesLegacyRelayFlow => true;
        public string BackendLabel => UsesLegacyRelayFlow ? CurrentBackendLabel : RecommendedBackendLabel;
        public string BackendUpgradeHint => "Unity 6 icin sonraki dogru adim: com.unity.services.multiplayer tabanli Session/MPS akisina gecis.";
        public string CurrentMode =>
            networkManager == null ? "Offline" :
            networkManager.IsHost ? "Host" :
            networkManager.IsClient ? "Client" :
            "Offline";

        private void Awake()
        {
            EnsureReferences();
        }

        public void StartOfflineHost()
        {
            EnsureReferences();

            if (networkManager == null)
            {
                PublishStatus("NetworkManager bulunamadi.");
                return;
            }

            if (networkManager.IsListening)
            {
                PublishStatus("Zaten aktif bir oturum var.");
                return;
            }

            CurrentJoinCode = string.Empty;
            JoinCodeChanged?.Invoke(CurrentJoinCode);

            if (networkManager.StartHost())
            {
                SetOfflineScenePlayerActive(false);
                PublishStatus("Offline host oturumu basladi.");
            }
            else
            {
                SetOfflineScenePlayerActive(true);
                PublishStatus("Offline host baslatilamadi.");
            }
        }

        public async Task<bool> StartRelayHostAsync()
        {
            EnsureReferences();

            if (networkManager == null || unityTransport == null)
            {
                PublishStatus("NetworkManager veya UnityTransport eksik.");
                return false;
            }

            if (networkManager.IsListening || IsBusy)
            {
                PublishStatus("Oturum zaten aktif veya islem devam ediyor.");
                return false;
            }

            try
            {
                IsBusy = true;
                PublishStatus("Unity Services baslatiliyor...");

                if (autoInitializeUnityServices)
                {
                    await InitializeServicesAsync();
                }

                PublishStatus("Relay allocation olusturuluyor...");
                var allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxPlayers - 1));
                CurrentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                JoinCodeChanged?.Invoke(CurrentJoinCode);

                var relayServerData = new RelayServerData(allocation, relayConnectionType);
                unityTransport.SetRelayServerData(relayServerData);

                if (!networkManager.StartHost())
                {
                    SetOfflineScenePlayerActive(true);
                    PublishStatus("Relay host baslatilamadi.");
                    return false;
                }

                SetOfflineScenePlayerActive(false);

                if (networkCaseState != null)
                {
                    networkCaseState.SetRelayJoinCode(CurrentJoinCode);
                }

                PublishStatus($"Online host hazir. Join code: {CurrentJoinCode}");
                return true;
            }
            catch (RelayServiceException ex)
            {
                PublishStatus($"Relay hatasi: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                PublishStatus($"Host baslatma hatasi: {ex.Message}");
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<bool> JoinRelaySessionAsync(string joinCode)
        {
            EnsureReferences();

            if (networkManager == null || unityTransport == null)
            {
                PublishStatus("NetworkManager veya UnityTransport eksik.");
                return false;
            }

            if (networkManager.IsListening || IsBusy)
            {
                PublishStatus("Oturum zaten aktif veya islem devam ediyor.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(joinCode))
            {
                PublishStatus("Join code girilmedi.");
                return false;
            }

            try
            {
                IsBusy = true;
                PublishStatus("Unity Services baslatiliyor...");

                if (autoInitializeUnityServices)
                {
                    await InitializeServicesAsync();
                }

                PublishStatus("Relay oturumuna baglaniliyor...");
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());
                var relayServerData = new RelayServerData(joinAllocation, relayConnectionType);
                unityTransport.SetRelayServerData(relayServerData);

                CurrentJoinCode = joinCode.Trim().ToUpperInvariant();
                JoinCodeChanged?.Invoke(CurrentJoinCode);

                if (!networkManager.StartClient())
                {
                    SetOfflineScenePlayerActive(true);
                    PublishStatus("Client baglantisi baslatilamadi.");
                    return false;
                }

                SetOfflineScenePlayerActive(false);
                PublishStatus($"Online oturuma baglaniliyor: {CurrentJoinCode}");
                return true;
            }
            catch (RelayServiceException ex)
            {
                PublishStatus($"Relay join hatasi: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                PublishStatus($"Join hatasi: {ex.Message}");
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void ShutdownSession()
        {
            EnsureReferences();

            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            CurrentJoinCode = string.Empty;
            JoinCodeChanged?.Invoke(CurrentJoinCode);
            SetOfflineScenePlayerActive(true);
            PublishStatus("Oturum kapatildi.");
        }

        private async Task InitializeServicesAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        private void PublishStatus(string status)
        {
            CurrentStatus = status;
            StatusChanged?.Invoke(status);
            Debug.Log($"[RelayNetworkBootstrap] {status}");
        }

        private void EnsureReferences()
        {
            if (networkManager == null)
            {
                networkManager = GetComponent<NetworkManager>();
            }

            if (networkManager == null)
            {
                networkManager = UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
            }

            if (unityTransport == null && networkManager != null)
            {
                unityTransport = networkManager.GetComponent<UnityTransport>();
            }

            if (networkCaseState == null)
            {
                networkCaseState = UnityEngine.Object.FindFirstObjectByType<NetworkCaseState>();
            }

            if (offlineScenePlayerRoot == null)
            {
                var scenePlayer = GameObject.Find("Player");
                if (scenePlayer != null && scenePlayer.GetComponent<NetworkObject>() == null)
                {
                    offlineScenePlayerRoot = scenePlayer;
                }
            }
        }

        private void SetOfflineScenePlayerActive(bool isActive)
        {
            if (offlineScenePlayerRoot != null)
            {
                offlineScenePlayerRoot.SetActive(isActive);
            }
        }
    }
}

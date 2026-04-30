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
using MobilOfl.UI;

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
        public bool CanReconnectLastSession =>
            !IsBusy &&
            !IsOnlineSessionActive &&
            _lastSessionWasRelayClient &&
            !string.IsNullOrWhiteSpace(_lastRelayJoinCode);
        public string LastDisconnectReason { get; private set; } = string.Empty;
        public string CurrentMode =>
            networkManager == null ? "Offline" :
            networkManager.IsHost ? "Host" :
            networkManager.IsClient ? "Client" :
            "Offline";

        private NetworkManager _subscribedNetworkManager;
        private string _lastRelayJoinCode = string.Empty;
        private bool _intentionalShutdown;
        private bool _lastSessionWasRelayClient;
        private bool _unexpectedStopHandled;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();
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
            ResetUnexpectedStopGuard();
            ClearReconnectState();
            _intentionalShutdown = false;

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
                ResetUnexpectedStopGuard();
                _intentionalShutdown = false;
                _lastSessionWasRelayClient = false;
                LastDisconnectReason = string.Empty;
                PublishStatus("Unity Services baslatiliyor...");

                if (autoInitializeUnityServices)
                {
                    await InitializeServicesAsync();
                }

                PublishStatus("Relay allocation olusturuluyor...");
                var allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxPlayers - 1));
                CurrentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                _lastRelayJoinCode = CurrentJoinCode;
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
                ResetUnexpectedStopGuard();
                _intentionalShutdown = false;
                _lastSessionWasRelayClient = true;
                LastDisconnectReason = string.Empty;
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
                _lastRelayJoinCode = CurrentJoinCode;
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

        public async Task<bool> AttemptReconnectToLastSessionAsync()
        {
            if (!CanReconnectLastSession)
            {
                PublishStatus("Yeniden baglanilabilecek bir onceki oturum yok.");
                return false;
            }

            PublishStatus("Son oturuma yeniden baglanma deneniyor...");
            return await JoinRelaySessionAsync(_lastRelayJoinCode);
        }

        public void ShutdownSession()
        {
            EnsureReferences();
            _intentionalShutdown = true;
            ResetUnexpectedStopGuard();
            ClearReconnectState();

            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            CurrentJoinCode = string.Empty;
            JoinCodeChanged?.Invoke(CurrentJoinCode);
            SetOfflineScenePlayerActive(true);
            LastDisconnectReason = string.Empty;
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

            SubscribeToNetworkEvents();
        }

        private void SetOfflineScenePlayerActive(bool isActive)
        {
            if (offlineScenePlayerRoot != null)
            {
                offlineScenePlayerRoot.SetActive(isActive);
            }
        }

        private void SubscribeToNetworkEvents()
        {
            if (networkManager == null || _subscribedNetworkManager == networkManager)
            {
                return;
            }

            UnsubscribeFromNetworkEvents();
            _subscribedNetworkManager = networkManager;
            _subscribedNetworkManager.OnClientConnectedCallback += HandleClientConnected;
            _subscribedNetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            _subscribedNetworkManager.OnClientStopped += HandleClientStopped;
            _subscribedNetworkManager.OnServerStopped += HandleServerStopped;
            _subscribedNetworkManager.OnTransportFailure += HandleTransportFailure;
        }

        private void UnsubscribeFromNetworkEvents()
        {
            if (_subscribedNetworkManager == null)
            {
                return;
            }

            _subscribedNetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            _subscribedNetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            _subscribedNetworkManager.OnClientStopped -= HandleClientStopped;
            _subscribedNetworkManager.OnServerStopped -= HandleServerStopped;
            _subscribedNetworkManager.OnTransportFailure -= HandleTransportFailure;
            _subscribedNetworkManager = null;
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (networkManager == null || clientId != networkManager.LocalClientId)
            {
                return;
            }

            ResetUnexpectedStopGuard();
            _intentionalShutdown = false;
            LastDisconnectReason = string.Empty;
            SetOfflineScenePlayerActive(false);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (networkManager == null || clientId != networkManager.LocalClientId)
            {
                return;
            }

            if (_intentionalShutdown)
            {
                return;
            }

            HandleUnexpectedSessionStop(_lastSessionWasRelayClient
                ? "Baglanti koptu. Istersen son relay oturumuna yeniden baglanabilirsin."
                : "Yerel istemci oturumu beklenmedik sekilde durdu.");
        }

        private void HandleClientStopped(bool wasHost)
        {
            if (_intentionalShutdown)
            {
                return;
            }

            HandleUnexpectedSessionStop(wasHost
                ? "Host tarafindaki istemci katmani durdu."
                : (_lastSessionWasRelayClient
                    ? "Istemci oturumu durdu. Son relay oturumuna yeniden baglanabilirsin."
                    : "Istemci oturumu durdu."));
        }

        private void HandleServerStopped(bool wasClient)
        {
            if (_intentionalShutdown)
            {
                return;
            }

            HandleUnexpectedSessionStop(wasClient
                ? "Host oturumu kapandi. Takim dagildi."
                : "Sunucu oturumu beklenmedik sekilde kapandi.");
        }

        private void HandleTransportFailure()
        {
            if (string.IsNullOrWhiteSpace(LastDisconnectReason))
            {
                LastDisconnectReason = "Transport baglantisi zaman asimina ugradi veya relay ile iletisim koptu.";
            }
        }

        private void HandleUnexpectedSessionStop(string fallbackStatus)
        {
            if (_unexpectedStopHandled)
            {
                return;
            }

            _unexpectedStopHandled = true;
            IsBusy = false;
            SetOfflineScenePlayerActive(true);

            var disconnectReason = ResolveDisconnectReason();
            var hasReason = !string.IsNullOrWhiteSpace(disconnectReason);
            var finalStatus = hasReason ? $"{fallbackStatus} Neden: {disconnectReason}" : fallbackStatus;

            if (!_lastSessionWasRelayClient)
            {
                CurrentJoinCode = string.Empty;
                JoinCodeChanged?.Invoke(CurrentJoinCode);
            }
            else if (!string.IsNullOrWhiteSpace(_lastRelayJoinCode))
            {
                CurrentJoinCode = _lastRelayJoinCode;
                JoinCodeChanged?.Invoke(CurrentJoinCode);
            }

            PublishStatus(finalStatus);

            var menu = MainMenuHud.Instance;
            if (menu != null)
            {
                menu.OpenMenu(finalStatus);
            }
        }

        private string ResolveDisconnectReason()
        {
            if (!string.IsNullOrWhiteSpace(LastDisconnectReason))
            {
                return LastDisconnectReason;
            }

            if (networkManager != null && !string.IsNullOrWhiteSpace(networkManager.DisconnectReason))
            {
                LastDisconnectReason = networkManager.DisconnectReason;
                return LastDisconnectReason;
            }

            return string.Empty;
        }

        private void ResetUnexpectedStopGuard()
        {
            _unexpectedStopHandled = false;
        }

        private void ClearReconnectState()
        {
            _lastSessionWasRelayClient = false;
            _lastRelayJoinCode = string.Empty;
        }
    }
}

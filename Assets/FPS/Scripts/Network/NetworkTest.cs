using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unity.FPS.Game
{
    public class NetworkTest : MonoBehaviour
    {
        [Header("UI引用")]
        [Tooltip("连接按钮")]
        public Button ConnectButton;
        
        [Tooltip("断开按钮")]
        public Button DisconnectButton;
        
        [Tooltip("状态文本")]
        public TextMeshProUGUI StatusText;

        private void Start()
        {
            // 初始化按钮事件
            if (ConnectButton != null)
            {
                ConnectButton.onClick.AddListener(OnConnectClick);
            }

            if (DisconnectButton != null)
            {
                DisconnectButton.onClick.AddListener(OnDisconnectClick);
            }

            // 更新UI状态
            UpdateUI();
        }

        private async void OnConnectClick()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("NetworkManager未初始化");
                return;
            }

            StatusText.text = "Connecting..";
            bool connected = await NetworkManager.Instance.Connect();
            StatusText.text = connected ? "Connected" : "Connect Failed";
            UpdateUI();
        }

        private void OnDisconnectClick()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("NetworkManager未初始化");
                return;
            }

            NetworkManager.Instance.Disconnect();
            StatusText.text = "Disconnected";
            UpdateUI();
        }

        private void Update()
        {
            // 实时更新连接状态
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (StatusText != null)
            {
                if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
                {
                    StatusText.text = "Connected";
                }
                else
                {
                    StatusText.text = "Disconnected";
                }
            }

            if (ConnectButton != null)
            {
                ConnectButton.interactable = NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected;
            }

            if (DisconnectButton != null)
            {
                DisconnectButton.interactable = NetworkManager.Instance != null && NetworkManager.Instance.IsConnected;
            }
        }
    }
} 
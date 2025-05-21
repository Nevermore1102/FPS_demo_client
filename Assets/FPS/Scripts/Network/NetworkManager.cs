using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using Google.Protobuf;

namespace Unity.FPS.Game
{

    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager instance;
        public static NetworkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("NetworkManager");
                    instance = go.AddComponent<NetworkManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("网络设置")]
        [Tooltip("服务器IP地址")]
        public string ServerIP = "172.23.166.114";
        
        [Tooltip("服务器端口")]
        public int ServerPort = 8888;
        
        [Tooltip("心跳间隔(秒)")]
        public float HeartbeatInterval = 5f;

        private TcpClient client;
        private NetworkStream stream;
        private bool isConnected;
        private float lastHeartbeatTime;

        public bool IsConnected => isConnected;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (isConnected && Time.time - lastHeartbeatTime >= HeartbeatInterval)
            {
                SendHeartbeat();
                lastHeartbeatTime = Time.time;
            }
        }

        public async Task<bool> Connect()
        {
            try
            {
                if (isConnected)
                {
                    return true;
                }

                client = new TcpClient();
                await client.ConnectAsync(ServerIP, ServerPort);
                stream = client.GetStream();
                isConnected = true;
                lastHeartbeatTime = Time.time;

                // 启动接收消息的循环
                StartReceiving();

                Debug.Log("成功连接到服务器");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"连接服务器失败: {e.Message}");
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }

            if (client != null)
            {
                client.Close();
                client = null;
            }

            isConnected = false;
        }

        private async void StartReceiving()
        {
            try
            {
                while (isConnected)
                {
                    // 1. 读取4字节消息长度
                    byte[] lengthBytes = new byte[4];
                    int read = 0;
                    while (read < 4)
                    {
                        int r = await stream.ReadAsync(lengthBytes, read, 4 - read);
                        if (r == 0) { Disconnect(); return; }
                        read += r;
                    }

                    // 2. 解析消息长度（网络字节序）
                    uint messageLength = BitConverter.ToUInt32(lengthBytes, 0);
                    messageLength = (uint)System.Net.IPAddress.NetworkToHostOrder((int)messageLength);
                    // 添加长度检查，防止异常大的消息
                    if (messageLength > 1024 * 1024) // 限制最大消息大小为1MB
                    {
                        Debug.LogError($"消息长度异常: {messageLength} bytes");
                        Disconnect();
                        return;
                    }

                    // 3. 读取消息体
                    byte[] messageBytes = new byte[messageLength];
                    int bodyRead = 0;
                    while (bodyRead < messageLength)
                    {
                        int r = await stream.ReadAsync(messageBytes, bodyRead, (int)messageLength - bodyRead);
                        if (r == 0) { Disconnect(); return; }
                        bodyRead += r;
                    }

                    // 4. 反序列化消息
                    NetworkMessage message = NetworkMessage.Parser.ParseFrom(messageBytes);
                    HandleMessage(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"接收消息时发生错误: {e.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private void HandleMessage(NetworkMessage message)
        {
            switch (message.MsgId)
            {
                case MessageType.Heartbeat:
                    Debug.Log("收到心跳响应");
                    break;
                case MessageType.PlayerUpdate:
                    if (message.PlayerUpdate != null)
                    {
                        Debug.Log($"收到玩家更新: 位置({message.PlayerUpdate.PositionX}, {message.PlayerUpdate.PositionY}, {message.PlayerUpdate.PositionZ})");
                    }
                    break;
                default:
                    Debug.Log($"收到未知消息类型: {message.MsgId}");
                    break;
            }
        }

        private async void SendHeartbeat()
        {
            try
            {
                var message = new NetworkMessage
                {
                    MsgId = MessageType.Heartbeat,
                    Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Heartbeat = new HeartbeatMessage()
                };

                await SendMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送心跳失败: {e.Message}");
                Disconnect();
            }
        }

        public async Task SendMessage(NetworkMessage message)
        {
            if (!isConnected)
            {
                Debug.LogError("未连接到服务器");
                return;
            }

            try
            {
                // 1. 序列化消息
                byte[] messageBytes = message.ToByteArray();
                
                // 2. 计算消息长度（网络字节序）
                uint messageLength = (uint)messageBytes.Length;
                byte[] lengthBytes = new byte[4];
                // 手动写入网络字节序（大端序）的uint32
                lengthBytes[0] = (byte)((messageLength >> 24) & 0xFF);
                lengthBytes[1] = (byte)((messageLength >> 16) & 0xFF);
                lengthBytes[2] = (byte)((messageLength >> 8) & 0xFF);
                lengthBytes[3] = (byte)(messageLength & 0xFF);

                // 3. 发送消息长度
                await stream.WriteAsync(lengthBytes, 0, 4);

                // 4. 发送消息体
                await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送消息失败: {e.Message}");
                Disconnect();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }
    }
} 
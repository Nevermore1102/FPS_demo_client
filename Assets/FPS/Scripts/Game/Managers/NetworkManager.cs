using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;

namespace Unity.FPS.Game
{
    public enum NetMessageType : uint
    {
        HEARTBEAT = 1,
        LOGIN = 2,
        LOGOUT = 3,
        PLAYER_UPDATE = 4,
        PLAYER_SHOOT = 5,
        PLAYER_HIT = 6,
        GAME_STATE = 7
    }

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
            byte[] buffer = new byte[1024];

            try
            {
                while (isConnected)
                {
                    // 1. 先读8字节消息头
                    byte[] header = new byte[8];
                    int read = 0;
                    while (read < 8)
                    {
                        int r = await stream.ReadAsync(header, read, 8 - read);
                        if (r == 0) { Disconnect(); return; }
                        read += r;
                    }

                    // 2. 解析消息头（小端序）
                    uint msgId = BitConverter.ToUInt32(header, 0);
                    uint bodySize = BitConverter.ToUInt32(header, 4);

                    // 3. 读取消息体
                    byte[] body = new byte[bodySize];
                    int bodyRead = 0;
                    while (bodyRead < bodySize)
                    {
                        int r = await stream.ReadAsync(body, bodyRead, (int)bodySize - bodyRead);
                        if (r == 0) { Disconnect(); return; }
                        bodyRead += r;
                    }

                    // 4. 处理消息
                    HandleMessage(msgId, body);
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

        private void HandleMessage(uint msgId, byte[] body)
        {
            if (msgId == (uint)NetMessageType.HEARTBEAT)
            {
                Debug.Log("收到心跳响应");
                return;
            }

            // 如果body是字符串
            if (body.Length > 0)
            {
                string msg = Encoding.UTF8.GetString(body);
                Debug.Log($"收到消息: {msg}");
            }
            else
            {
                Debug.Log($"收到消息: type={msgId}, 空消息体");
            }
        }

        private async void SendHeartbeat()
        {
            try
            {
                await SendMessage(NetMessageType.HEARTBEAT);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送心跳失败: {e.Message}");
                Disconnect();
            }
        }

        private void WriteUInt32BigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        private void WriteUInt32LittleEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        public async Task SendMessage(NetMessageType msgType, byte[] body = null)
        {
            if (!isConnected)
            {
                Debug.LogError("未连接到服务器");
                return;
            }

            try
            {
                body = body ?? new byte[0];
                uint msgId = (uint)msgType;
                uint bodySize = (uint)body.Length;

                byte[] header = new byte[8];
                // WriteUInt32BigEndian(header, 0, msgId);
                // WriteUInt32BigEndian(header, 4, bodySize);
                WriteUInt32LittleEndian(header, 0, msgId);
                WriteUInt32LittleEndian(header, 4, bodySize);



                byte[] sendBuffer = new byte[8 + body.Length];
                Array.Copy(header, 0, sendBuffer, 0, 8);
                Array.Copy(body, 0, sendBuffer, 8, body.Length);

                await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length);
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
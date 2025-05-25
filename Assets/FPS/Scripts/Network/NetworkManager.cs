using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using Google.Protobuf;
using Unity.FPS.Gameplay;
using UnityEngine.SceneManagement;

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

        [Header("玩家信息")]
        // 添加服务器玩家状态
        private ServerPlayerState serverPlayerState;

        private PlayerCharacterController playerController;
        private Health playerHealth;
        public uint localPlayerId = 1;
        private bool isPlayerInitialized = false;

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

            // 初始化服务器玩家状态
            serverPlayerState = new ServerPlayerState();

            // 注册场景加载事件
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            // 获取本地玩家组件
            playerController = FindFirstObjectByType<PlayerCharacterController>();
            if (playerController != null)
            {
                playerHealth = playerController.GetComponent<Health>();
            }


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
                case MessageType.PlayerAttribute:
                    Debug.Log("收到玩家属性");
                    break;
                case MessageType.PlayerState:
                    Debug.Log("收到玩家状态");
                    break;
                case MessageType.PlayerJoin:
                    Debug.Log("收到玩家登录回复");
                    OnPlayerJoin(message);
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

        // 场景加载完成时的回调
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 重置玩家组件引用
            isPlayerInitialized = false;
            playerController = null;
            playerHealth = null;

            // 如果是游戏场景，尝试初始化玩家组件
            if (scene.name == "NewScene_323_test") // 替换为您的游戏场景名称
            {
                InitializePlayerComponents();

                //若已经登入，则载入数据。
                if (serverPlayerState.IsConnected)
                {
                    //载入数据
                    playerController.transform.position = serverPlayerState.Position;
                    playerController.transform.rotation = serverPlayerState.Rotation;
                    playerHealth.CurrentHealth = serverPlayerState.Health;
                }
            }
        }

        // 初始化玩家组件
        private void InitializePlayerComponents()
        {
            if (isPlayerInitialized) return;

            playerController = FindFirstObjectByType<PlayerCharacterController>();
            if (playerController != null)
            {
                playerHealth = playerController.GetComponent<Health>();
                isPlayerInitialized = true;
                Debug.Log("玩家组件初始化成功");
            }
            else
            {
                Debug.LogWarning("未找到玩家控制器组件");
            }
        }

        // 获取玩家位置信息
        public UnityEngine.Vector3 GetPlayerPosition()
        {
            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerController != null)
            {
                return playerController.transform.position;
            }
            return UnityEngine.Vector3.zero;
        }

        // 获取玩家旋转信息
        public Quaternion GetPlayerRotation()
        {
            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerController != null)
            {
                return playerController.transform.rotation;
            }
            return Quaternion.identity;
        }

        // 获取玩家速度信息
        public UnityEngine.Vector3 GetPlayerVelocity()
        {
            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerController != null)
            {
                return playerController.CharacterVelocity;
            }
            return UnityEngine.Vector3.zero;
        }

        // 获取玩家是否在地面上
        public bool IsPlayerGrounded()
        {
            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerController != null)
            {
                return playerController.IsGrounded;
            }
            return false;
        }

        // 获取玩家生命值
        public float GetPlayerHealth()
        {
            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerHealth != null)
            {
                return playerHealth.CurrentHealth;
            }
            return 0f;
        }

        // 获取玩家最大生命值
        public float GetPlayerMaxHealth()
        {
            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerHealth != null)
            {
                return playerHealth.MaxHealth;
            }
            return 0f;
        }

        // 设置本地玩家ID
        public void SetLocalPlayerId(uint id)
        {
            localPlayerId = id;
        }

        // 获取本地玩家ID
        public uint GetLocalPlayerId()
        {
            return localPlayerId;
        }

        // 发送玩家状态更新
        public async Task SendPlayerStateUpdate()
        {
            if (!isConnected)
            {
                return;
            }

            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerController == null)
            {
                return;
            }

            try
            {
                var position = GetPlayerPosition();
                var rotation = GetPlayerRotation();
                var velocity = GetPlayerVelocity();

                var message = new NetworkMessage
                {
                    MsgId = MessageType.PlayerUpdate,
                    PlayerId = localPlayerId,
                    Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    PlayerUpdate = new PlayerUpdateMessage
                    {
                        PositionX = position.x,
                        PositionY = position.y,
                        PositionZ = position.z,
                        RotationX = rotation.eulerAngles.x,
                        RotationY = rotation.eulerAngles.y,
                        RotationZ = rotation.eulerAngles.z,
                        VelocityX = velocity.x,
                        VelocityY = velocity.y,
                        VelocityZ = velocity.z,
                        IsGrounded = IsPlayerGrounded(),
                        Health = GetPlayerHealth()
                    }
                };

                await SendMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送玩家状态更新失败: {e.Message}");
            }
        }

        // 发送玩家属性更新
        public async Task SendPlayerAttributeUpdate()
        {
            if (!isConnected)
            {
                return;
            }

            if (!isPlayerInitialized)
            {
                InitializePlayerComponents();
            }

            if (playerController == null)
            {
                return;
            }

            try
            {
                var message = new NetworkMessage
                {
                    MsgId = MessageType.PlayerAttribute,
                    PlayerId = localPlayerId,
                    Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    PlayerAttribute = new PlayerAttributeMessage
                    {
                        PlayerId = localPlayerId,
                        Health = GetPlayerHealth(),
                        MaxHealth = GetPlayerMaxHealth(),
                        // 这里可以添加更多属性，比如弹药、武器等
                    }
                };

                await SendMessage(message);
            }
            catch (Exception e)
            {
                Debug.LogError($"发送玩家属性更新失败: {e.Message}");
            }
        }

        //发送玩家登录：发送一个带着playerid的登录消息
        public async Task SendPlayerLogin()
        {
            if (!isConnected)
            {
                return;
            }
            var message = new NetworkMessage
            {
                MsgId = MessageType.PlayerJoin,
                PlayerId = localPlayerId,
            };
            await SendMessage(message);
        }

        //接受玩家登录回复
        private void OnPlayerJoin(NetworkMessage message)
        {
            try
            {
                if (message == null)
                {
                    Debug.LogError("登录回复消息为空");
                    return;
                }

                if (message.PlayerId != localPlayerId)
                {
                    Debug.LogWarning($"收到其他玩家的登录回复: {message.PlayerId}");
                    return;
                }

                Debug.Log("玩家登录成功");

                // 检查消息内容
                if (message.PlayerState == null)
                {
                    Debug.LogError("登录回复中缺少PlayerState信息");
                    return;
                }

                // 更新服务器玩家状态
                serverPlayerState.UpdateFromMessage(message);
                Debug.Log($"服务器玩家状态更新完成:\n{serverPlayerState}");
            }
            catch (Exception e)
            {
                Debug.LogError($"处理玩家登录回复时发生未预期的错误: {e.Message}\n{e.StackTrace}");
            }
        }

        // 添加获取服务器玩家状态的方法
        public ServerPlayerState GetServerPlayerState()
        {
            return serverPlayerState;
        }

        public UnityEngine.Vector3 GetServerPlayerPosition()
        {
            return serverPlayerState?.Position ?? UnityEngine.Vector3.zero;
        }

        public float GetServerPlayerHealth()
        {
            return serverPlayerState?.Health ?? 0f;
        }

        public Quaternion GetServerPlayerRotation()
        {
            return serverPlayerState?.Rotation ?? Quaternion.identity;
        }

        public UnityEngine.Vector3 GetServerPlayerVelocity()
        {
            return serverPlayerState?.Velocity ?? UnityEngine.Vector3.zero;
        }

        public bool IsServerPlayerGrounded()
        {
            return serverPlayerState?.IsGrounded ?? false;
        }

        private void OnDestroy()
        {
            // 取消注册场景加载事件
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Disconnect();
        }
    }
} 
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 用于存储服务器玩家状态的类
    /// </summary>
    public class ServerPlayerState
    {
        // 基础属性
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public UnityEngine.Vector3 Position { get; set; }
        public UnityEngine.Quaternion Rotation { get; set; }
        public UnityEngine.Vector3 Velocity { get; set; }
        public bool IsGrounded { get; set; }

        // 玩家ID
        public uint PlayerId { get; set; }
        // 是否已连接
        public bool IsConnected = false;

        // 构造函数
        public ServerPlayerState()
        {
            // 初始化默认值
            Health = 100f;
            MaxHealth = 100f;
            Position = UnityEngine.Vector3.zero;
            Rotation = UnityEngine.Quaternion.identity;
            Velocity = UnityEngine.Vector3.zero;
            IsGrounded = false;
        }

        // 更新状态的方法
        public void UpdateFromMessage(NetworkMessage message)
        {
            if (message == null) return;

            // 更新玩家ID
            PlayerId = message.PlayerId;

            // 检查消息类型
            if (message.MsgId != MessageType.PlayerJoin)
            {
                Debug.LogWarning($"收到非PlayerJoin消息: {message.MsgId}");
                return;
            }

            // 获取PlayerState消息
            var playerState = message.PlayerState;
            if (playerState == null)
            {
                Debug.LogError("PlayerState消息为空");
                return;
            }

            // 更新位置
            if (playerState.Position != null)
            {
                Position = new UnityEngine.Vector3(
                    (float)playerState.Position.X,
                    (float)playerState.Position.Y,
                    (float)playerState.Position.Z
                );
            }

            // 更新旋转
            if (playerState.Rotation != null)
            {
                Rotation = UnityEngine.Quaternion.Euler(
                    playerState.Rotation.X,
                    playerState.Rotation.Y,
                    playerState.Rotation.Z
                );
            }

            // 更新属性
            if (playerState.Attributes != null)
            {
                Health = playerState.Attributes.Health;
                MaxHealth = playerState.Attributes.MaxHealth;
            }

            // 更新其他状态
            // IsGrounded = playerState.IsGrounded;
            IsConnected = playerState.IsAlive;

            Debug.Log($"更新服务器玩家状态: {this}");
        }

        // 获取状态信息的字符串表示
        public override string ToString()
        {
            return $"ServerPlayerState:\n" +
                   $"PlayerId: {PlayerId}\n" +
                   $"Health: {Health}/{MaxHealth}\n" +
                   $"Position: {Position}\n" +
                   $"Rotation: {Rotation.eulerAngles}\n" +
                   $"Velocity: {Velocity}\n" +
                   $"IsGrounded: {IsGrounded}\n" +
                   $"IsConnected: {IsConnected}";
        }
    }
} 
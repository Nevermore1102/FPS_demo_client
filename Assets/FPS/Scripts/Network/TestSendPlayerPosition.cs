using UnityEngine;
using System.Collections;

namespace Unity.FPS.Game
{
    public class TestSendPlayerPosition : MonoBehaviour
    {
        [Tooltip("发送间隔（秒）")]
        public float sendInterval = 1.0f;
        private float timer = 0f;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= sendInterval)
            {
                timer = 0f;
                if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
                {
                    Debug.Log("Test: 发送玩家位置消息");
                    // 用协程方式等待异步任务
                    StartCoroutine(SendPositionCoroutine());
                }
                else
                {
                    Debug.LogWarning("Test: NetworkManager 未连接，无法发送位置消息");
                }
            }
        }
        private IEnumerator SendPositionCoroutine()
        {
            var task = NetworkManager.Instance.SendPlayerStateUpdate();
            while (!task.IsCompleted)
            {
                yield return null;
            }
            // 可选：处理异常
            if (task.IsFaulted)
            {
                Debug.LogError("发送玩家位置消息时出错: " + task.Exception);
            }
        }

    }
} 
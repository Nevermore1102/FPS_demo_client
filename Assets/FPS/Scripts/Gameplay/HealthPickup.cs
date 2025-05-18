using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class HealthPickup : Pickup
    {
        // 表示拾取时恢复的生命值数量
        [Header("Parameters")] [Tooltip("Amount of health to heal on pickup")]
        public float HealAmount;

        // 当玩家拾取该物品时调用的方法
        protected override void OnPicked(PlayerCharacterController player)
        {
            // 获取玩家身上的 Health 组件
            Health playerHealth = player.GetComponent<Health>();
            // 如果玩家有 Health 组件并且可以拾取该物品，则恢复生命值并销毁物品
            if (playerHealth && playerHealth.CanPickup())
            {
                playerHealth.Heal(HealAmount);
                PlayPickupFeedback();
                Destroy(gameObject);
            }
        }
    }
}
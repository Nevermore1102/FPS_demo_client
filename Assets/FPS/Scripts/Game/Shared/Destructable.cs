using UnityEngine;

namespace Unity.FPS.Game
{
    // 表示可破坏的游戏对象，继承自 MonoBehaviour
    public class Destructable : MonoBehaviour
    {
        Health m_Health;

        // 初始化方法，在对象启动时调用
        void Start()
        {
            m_Health = GetComponent<Health>();
            DebugUtility.HandleErrorIfNullGetComponent<Health, Destructable>(m_Health, this, gameObject);

            // 订阅 Health 组件的 OnDie 和 OnDamaged 事件
            m_Health.OnDie += OnDie;
            m_Health.OnDamaged += OnDamaged;
        }

        // 当对象受到伤害时调用的方法
        void OnDamaged(float damage, GameObject damageSource)
        {
            // TODO: damage reaction
        }

        // 当对象的生命值归零时调用的方法
        void OnDie()
        {
            // this will call the OnDestroy function
            Destroy(gameObject);
        }
    }
}
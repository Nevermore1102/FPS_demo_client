using System.Linq;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.AI
{
    public class DetectionModule : MonoBehaviour
    {
        [Tooltip("The point representing the source of target-detection raycasts for the enemy AI")]
        public Transform DetectionSourcePoint;

        [Tooltip("The max distance at which the enemy can see targets")]
        public float DetectionRange = 20f;

        [Tooltip("The max distance at which the enemy can attack its target")]
        public float AttackRange = 10f;

        [Tooltip("Time before an enemy abandons a known target that it can't see anymore")]
        public float KnownTargetTimeout = 4f;

        [Tooltip("Optional animator for OnShoot animations")]
        public Animator Animator;

        public UnityAction onDetectedTarget;
        public UnityAction onLostTarget;

        // 已知检测到的目标游戏对象
        public GameObject KnownDetectedTarget { get; private set; }
        // 目标是否在攻击范围内
        public bool IsTargetInAttackRange { get; private set; }
        // 是否正在看到目标
        public bool IsSeeingTarget { get; private set; }
        // 是否曾经有已知目标
        public bool HadKnownTarget { get; private set; }

        // 上次看到目标的时间
        protected float TimeLastSeenTarget = Mathf.NegativeInfinity;

        // 角色管理器实例
        ActorsManager m_ActorsManager;

        // 动画攻击参数名称
        const string k_AnimAttackParameter = "Attack";
        // 动画受伤参数名称
        const string k_AnimOnDamagedParameter = "OnDamaged";

        // 初始化方法，查找角色管理器实例并处理错误
        protected virtual void Start()
        {
            m_ActorsManager = FindAnyObjectByType<ActorsManager>();
            DebugUtility.HandleErrorIfNullFindObject<ActorsManager, DetectionModule>(m_ActorsManager, this);
        }

        // 处理目标检测逻辑
        public virtual void HandleTargetDetection(Actor actor, Collider[] selfColliders)
        {
            // 处理已知目标检测超时
            if (KnownDetectedTarget && !IsSeeingTarget && (Time.time - TimeLastSeenTarget) > KnownTargetTimeout)
            {
                KnownDetectedTarget = null;
            }

            // 查找最近可见的敌对角色
            float sqrDetectionRange = DetectionRange * DetectionRange;
            IsSeeingTarget = false;
            float closestSqrDistance = Mathf.Infinity;
            foreach (Actor otherActor in m_ActorsManager.Actors)
            {
                if (otherActor.Affiliation != actor.Affiliation)
                {
                    float sqrDistance = (otherActor.transform.position - DetectionSourcePoint.position).sqrMagnitude;
                    if (sqrDistance < sqrDetectionRange && sqrDistance < closestSqrDistance)
                    {
                        // 检查是否有遮挡物
                        RaycastHit[] hits = Physics.RaycastAll(DetectionSourcePoint.position,
                            (otherActor.AimPoint.position - DetectionSourcePoint.position).normalized, DetectionRange,
                            -1, QueryTriggerInteraction.Ignore);
                        RaycastHit closestValidHit = new RaycastHit();
                        closestValidHit.distance = Mathf.Infinity;
                        bool foundValidHit = false;
                        foreach (var hit in hits)
                        {
                            if (!selfColliders.Contains(hit.collider) && hit.distance < closestValidHit.distance)
                            {
                                closestValidHit = hit;
                                foundValidHit = true;
                            }
                        }

                        if (foundValidHit)
                        {
                            Actor hitActor = closestValidHit.collider.GetComponentInParent<Actor>();
                            if (hitActor == otherActor)
                            {
                                IsSeeingTarget = true;
                                closestSqrDistance = sqrDistance;

                                TimeLastSeenTarget = Time.time;
                                KnownDetectedTarget = otherActor.AimPoint.gameObject;
                            }
                        }
                    }
                }
            }

            // 判断目标是否在攻击范围内
            IsTargetInAttackRange = KnownDetectedTarget != null &&
                                    Vector3.Distance(transform.position, KnownDetectedTarget.transform.position) <=
                                    AttackRange;

            // 检测事件处理
            if (!HadKnownTarget &&
                KnownDetectedTarget != null)
            {
                OnDetect();
            }

            if (HadKnownTarget &&
                KnownDetectedTarget == null)
            {
                OnLostTarget();
            }

            // 记忆是否曾经有已知目标（用于下一帧）
            HadKnownTarget = KnownDetectedTarget != null;
        }

        // 失去目标时的处理方法
        public virtual void OnLostTarget() => onLostTarget?.Invoke();

        // 检测到目标时的处理方法
        public virtual void OnDetect() => onDetectedTarget?.Invoke();

        // 受伤时的处理方法
        public virtual void OnDamaged(GameObject damageSource)
        {
            TimeLastSeenTarget = Time.time;
            KnownDetectedTarget = damageSource;

            if (Animator)
            {
                Animator.SetTrigger(k_AnimOnDamagedParameter);
            }
        }

        // 攻击时的处理方法
        public virtual void OnAttack()
        {
            if (Animator)
            {
                Animator.SetTrigger(k_AnimAttackParameter);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    // 管理游戏中的目标，包括注册目标和检查目标是否完成
    public class ObjectiveManager : MonoBehaviour
    {
        List<Objective> m_Objectives = new List<Objective>(); // 目标列表
        bool m_ObjectivesCompleted = false; // 标记所有目标是否已完成

        // 当脚本实例被创建时注册目标创建事件的监听器
        void Awake()
        {
            Objective.OnObjectiveCreated += RegisterObjective;
        }

        // 将新创建的目标添加到目标列表中
        void RegisterObjective(Objective objective) => m_Objectives.Add(objective);

        // 每帧更新，检查所有目标是否已完成
        void Update()
        {
            if (m_Objectives.Count == 0 || m_ObjectivesCompleted)
                return;

            for (int i = 0; i < m_Objectives.Count; i++)
            {
                // 传递每个目标以检查它们是否已完成
                if (m_Objectives[i].IsBlocking())
                {
                    // 一旦发现一个未完成的目标，就中断循环
                    return;
                }
            }

            m_ObjectivesCompleted = true; // 所有目标已完成
            EventManager.Broadcast(Events.AllObjectivesCompletedEvent); // 广播所有目标已完成事件
        }

        // 当脚本实例被销毁时移除目标创建事件的监听器
        void OnDestroy()
        {
            Objective.OnObjectiveCreated -= RegisterObjective;
        }
    }
}
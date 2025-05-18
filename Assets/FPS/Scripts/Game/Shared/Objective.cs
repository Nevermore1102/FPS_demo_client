using System;
using UnityEngine;

namespace Unity.FPS.Game
{
    public abstract class Objective : MonoBehaviour
    {
        [Tooltip("Name of the objective that will be shown on screen")]
        public string Title;

        [Tooltip("Short text explaining the objective that will be shown on screen")]
        public string Description;

        [Tooltip("Whether the objective is required to win or not")]
        public bool IsOptional;

        [Tooltip("Delay before the objective becomes visible")]
        public float DelayVisible;

        public bool IsCompleted { get; private set; }
        public bool IsBlocking() => !(IsOptional || IsCompleted);

        // 定义一个静态事件，当有新的目标被创建时触发
        public static event Action<Objective> OnObjectiveCreated;
        // 定义一个静态事件，当目标被完成时触发
        public static event Action<Objective> OnObjectiveCompleted;

        // 在游戏对象启动时调用，用于初始化目标并广播目标创建事件
        protected virtual void Start()
        {
            OnObjectiveCreated?.Invoke(this);

            DisplayMessageEvent displayMessage = Events.DisplayMessageEvent;
            displayMessage.Message = Title;
            displayMessage.DelayBeforeDisplay = 0.0f;
            EventManager.Broadcast(displayMessage);
        }

        // 更新目标的描述文本、计数器文本和通知文本，并广播目标更新事件
        public void UpdateObjective(string descriptionText, string counterText, string notificationText)
        {
            ObjectiveUpdateEvent evt = Events.ObjectiveUpdateEvent;
            evt.Objective = this;
            evt.DescriptionText = descriptionText;
            evt.CounterText = counterText;
            evt.NotificationText = notificationText;
            evt.IsComplete = IsCompleted;
            EventManager.Broadcast(evt);
        }

        // 完成目标，设置完成状态为真，更新目标信息并广播目标完成事件
        public void CompleteObjective(string descriptionText, string counterText, string notificationText)
        {
            IsCompleted = true;

            ObjectiveUpdateEvent evt = Events.ObjectiveUpdateEvent;
            evt.Objective = this;
            evt.DescriptionText = descriptionText;
            evt.CounterText = counterText;
            evt.NotificationText = notificationText;
            evt.IsComplete = IsCompleted;
            EventManager.Broadcast(evt);

            OnObjectiveCompleted?.Invoke(this);
        }
    }
}
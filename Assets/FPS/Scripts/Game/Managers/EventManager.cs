using System;
using System.Collections.Generic;

namespace Unity.FPS.Game
{
    // 游戏事件基类
    public class GameEvent
    {
    }

    // 一个简单的事件系统，可用于远程系统通信
    public static class EventManager
    {
        // 存储事件类型及其对应的处理函数集合
        static readonly Dictionary<Type, Action<GameEvent>> s_Events = new Dictionary<Type, Action<GameEvent>>();

        // 用于查找特定委托的处理函数
        static readonly Dictionary<Delegate, Action<GameEvent>> s_EventLookups =
            new Dictionary<Delegate, Action<GameEvent>>();

        // 添加监听器，将特定类型的游戏事件与处理函数关联
        public static void AddListener<T>(Action<T> evt) where T : GameEvent
        {
            if (!s_EventLookups.ContainsKey(evt))
            {
                Action<GameEvent> newAction = (e) => evt((T) e);
                s_EventLookups[evt] = newAction;

                if (s_Events.TryGetValue(typeof(T), out Action<GameEvent> internalAction))
                    s_Events[typeof(T)] = internalAction += newAction;
                else
                    s_Events[typeof(T)] = newAction;
            }
        }

        // 移除监听器，断开特定类型的游戏事件与处理函数的关联
        public static void RemoveListener<T>(Action<T> evt) where T : GameEvent
        {
            if (s_EventLookups.TryGetValue(evt, out var action))
            {
                if (s_Events.TryGetValue(typeof(T), out var tempAction))
                {
                    tempAction -= action;
                    if (tempAction == null)
                        s_Events.Remove(typeof(T));
                    else
                        s_Events[typeof(T)] = tempAction;
                }

                s_EventLookups.Remove(evt);
            }
        }

        // 广播游戏事件，调用所有注册的处理函数来响应该事件
        public static void Broadcast(GameEvent evt)
        {
            if (s_Events.TryGetValue(evt.GetType(), out var action))
                action.Invoke(evt);
        }

        // 清除所有事件和监听器，重置事件系统
        public static void Clear()
        {
            s_Events.Clear();
            s_EventLookups.Clear();
        }
    }
}
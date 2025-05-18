using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    // 管理游戏中的所有角色
    public class ActorsManager : MonoBehaviour
    {
        // 获取当前游戏中的所有角色列表
        public List<Actor> Actors { get; private set; }
        // 获取当前玩家对象
        public GameObject Player { get; private set; }

        // 设置玩家对象
        public void SetPlayer(GameObject player) => Player = player;

        // 初始化角色列表
        void Awake()
        {
            Actors = new List<Actor>();
        }
    }
}
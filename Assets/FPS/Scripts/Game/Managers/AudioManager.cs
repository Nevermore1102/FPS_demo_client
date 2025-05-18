using UnityEngine;
using UnityEngine.Audio;

namespace Unity.FPS.Game
{
    // 音频管理器类，用于处理游戏中的音频设置
    public class AudioManager : MonoBehaviour
    {
        // 公开的音频混音器数组，可以在Unity编辑器中设置
        public AudioMixer[] AudioMixers;

        // 根据子路径查找匹配的音频混音器组，返回找到的第一个匹配结果
        public AudioMixerGroup[] FindMatchingGroups(string subPath)
        {
            for (int i = 0; i < AudioMixers.Length; i++)
            {
                AudioMixerGroup[] results = AudioMixers[i].FindMatchingGroups(subPath);
                if (results != null && results.Length != 0)
                {
                    return results;
                }
            }

            return null;
        }

        // 设置所有音频混音器中指定参数的浮点值
        public void SetFloat(string name, float value)
        {
            for (int i = 0; i < AudioMixers.Length; i++)
            {
                if (AudioMixers[i] != null)
                {
                    AudioMixers[i].SetFloat(name, value);
                }
            }
        }

        // 获取第一个可用音频混音器中指定参数的浮点值
        public void GetFloat(string name, out float value)
        {
            value = 0f;
            for (int i = 0; i < AudioMixers.Length; i++)
            {
                if (AudioMixers[i] != null)
                {
                    AudioMixers[i].GetFloat(name, out value);
                    break;
                }
            }
        }
    }
}
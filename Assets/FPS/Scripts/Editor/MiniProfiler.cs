using System.Collections.Generic;
using Unity.FPS.AI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Unity.FPS.EditorExt
{
    // 小型性能分析器，继承自EditorWindow，用于在Unity编辑器中分析场景性能
    public class MiniProfiler : EditorWindow
    {
        // 存储边界和网格数量的类
        class BoundsAndCount
        {
            public Bounds Bounds;
            public int Count;
        }

        // 存储网格数据的类，包括边界、数量、比例和颜色
        class CellData
        {
            public Bounds Bounds;
            public int Count;
            public float Ratio;
            public Color Color;
        }

        Vector2 m_ScrollPos; // 滚动条位置
        bool m_MustRepaint = false; // 标记是否需要重绘
        bool m_MustLaunchHeatmapNextFrame = false; // 标记是否需要在下一帧生成热图
        bool m_HeatmapIsCalculating = false; // 标记热图是否正在计算
        float m_CellTransparency = 0.9f; // 热图单元格的透明度
        float m_CellThreshold = 0f; // 热图单元格的显示阈值
        string m_LevelAnalysisString = ""; // 关卡分析结果字符串
        List<string> m_SuggestionStrings = new List<string>(); // 性能优化建议字符串列表

        static List<CellData> s_CellDatas = new List<CellData>(); // 热图单元格数据列表

        const float k_CellSize = 10; // 热图单元格大小
        const string k_NewLine = "\n"; // 换行符
        const string k_HeaderSeparator = "=============================="; // 分隔符

        // 添加菜单项到编辑器的“Tools”菜单中
        [MenuItem("Tools/MiniProfiler")]
        public static void ShowWindow()
        {
            // 显示窗口实例，如果不存在则创建一个新的
            EditorWindow.GetWindow(typeof(MiniProfiler));
        }

        // 当窗口启用时调用此方法
        void OnEnable()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
#elif UNITY_2018_1_OR_NEWER
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
        }

        // 绘制编辑器窗口的GUI
        void OnGUI()
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, false, false);

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Performance Tips");
            DisplayTips();

            GUILayout.Space(20);
            EditorGUILayout.LabelField("Level Analysis");
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("You must exit Play mode for this feature to be available",
                    MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Analyze"))
                {
                    AnalyzeLevel();
                }

                if (m_LevelAnalysisString != null && m_LevelAnalysisString != "")
                {
                    EditorGUILayout.HelpBox(m_LevelAnalysisString, MessageType.None);
                }

                if (m_SuggestionStrings.Count > 0)
                {
                    EditorGUILayout.LabelField("Suggestions");
                    foreach (var s in m_SuggestionStrings)
                    {
                        EditorGUILayout.HelpBox(s, MessageType.Warning);
                    }
                }

                if (GUILayout.Button("Clear Analysis"))
                {
                    ClearAnalysis();
                    m_MustRepaint = true;
                }
            }


            GUILayout.Space(20);
            EditorGUILayout.LabelField("Polygon count Heatmap");
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("You must exit Play mode for this feature to be available",
                    MessageType.Warning);
            }
            else
            {
                if (m_MustLaunchHeatmapNextFrame)
                {
                    DoPolycountMap();
                    m_CellTransparency = 0.9f;
                    m_CellThreshold = 0f;
                    m_MustLaunchHeatmapNextFrame = false;
                    m_MustRepaint = true;
                }

                if (GUILayout.Button("Build Heatmap"))
                {
                    m_MustLaunchHeatmapNextFrame = true;
                    m_HeatmapIsCalculating = true;
                }

                if (s_CellDatas.Count > 0)
                {
                    float prevAlpha = m_CellTransparency;
                    m_CellTransparency = EditorGUILayout.Slider("Cell Transparency", m_CellTransparency, 0f, 1f);
                    if (m_CellTransparency != prevAlpha)
                    {
                        m_MustRepaint = true;
                    }

                    float prevTreshold = m_CellThreshold;
                    m_CellThreshold = EditorGUILayout.Slider("Cell Display Threshold", m_CellThreshold, 0f, 1f);
                    if (m_CellThreshold != prevTreshold)
                    {
                        m_MustRepaint = true;
                    }
                }

                if (GUILayout.Button("Clear Heatmap"))
                {
                    m_MustRepaint = true;
                    s_CellDatas.Clear();
                }
            }

            EditorGUILayout.EndScrollView();

            if (m_MustRepaint)
            {
                EditorWindow.GetWindow<SceneView>().Repaint();
                m_MustRepaint = false;
            }

            if (m_HeatmapIsCalculating)
                EditorUtility.DisplayProgressBar("Polygon Count Heatmap", "Calculations in progress", 0.99f);
        }

        // 在场景视图中绘制热图
        void OnSceneGUI(SceneView sceneView)
        {
            // 绘制热图单元格
            foreach (CellData c in s_CellDatas)
            {
                if (c.Ratio >= m_CellThreshold && c.Count > 0)
                {
                    Color col = c.Color;
                    col.a = 1f - m_CellTransparency;
                    Handles.color = col;
                    Handles.CubeHandleCap(0, c.Bounds.center, Quaternion.identity, c.Bounds.extents.x * 2f,
                        EventType.Repaint);
                }
            }
        }

        // 清空关卡分析结果和建议字符串
        void ClearAnalysis()
        {
            m_LevelAnalysisString = "";
            m_SuggestionStrings.Clear();
        }

        // 显示性能优化提示信息
        void DisplayTips()
        {
            EditorGUILayout.HelpBox(
                "All of your meshes that will never move (floor/wall meshes, for examples) should be placed as children of the \"Level\" GameObject in the scene. This is because the \"Mesh Combiner\" script on that object will take care of combining all meshes under it on game start, and this reduces the cost of rendering them. It is more efficient to render one big mesh than lots of small meshes, even when the number of polygons is the same.",
                MessageType.None);
            EditorGUILayout.HelpBox(
                "Every light added to the level will have a performance cost. If you do add more lights to the level, consider making them not cast any shadows to reduce the performance impact. However, be aware that in WebGL there is a limit of 4 lights to be drawn on screen at the same time",
                MessageType.None);
            EditorGUILayout.HelpBox("Transparent objects are more expensive for performance than opaque objects",
                MessageType.None);
            EditorGUILayout.HelpBox(
                "Animated 3D models (known as \"Skinned Meshes\") are more expensive for performance than regular meshes",
                MessageType.None);
            EditorGUILayout.HelpBox(
                "Having a lot of enemies in the level could impact performance, due to their AI logic",
                MessageType.None);
            EditorGUILayout.HelpBox("Adding rigidbodies (physics objects) to the level could impact performance",
                MessageType.None);
            EditorGUILayout.HelpBox(
                "Open the Profiler window from the top menu bar (Window > Analysis > Profiler) to see in-depth information about your game's performance while you are playing",
                MessageType.None);
        }

        // 分析关卡性能
        void AnalyzeLevel()
        {
            ClearAnalysis();
            EditorStyles.textArea.wordWrap = true;
            MeshCombiner mainMeshCombiner = FindAnyObjectByType<MeshCombiner>();

            // 分析网格、动画模型、多边形数量、物理对象和灯光数量
            MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            SkinnedMeshRenderer[] skinnedMeshes = FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int skinnedMeshesCount = skinnedMeshes.Length;
            int meshCount = meshFilters.Length;
            int nonCombinedMeshCount = 0;
            int polyCount = 0;

            foreach (MeshFilter mf in meshFilters)
            {
                if (!mf.sharedMesh)
                    continue;

                polyCount += mf.sharedMesh.triangles.Length / 3;

                bool willBeCombined = false;
                if (mainMeshCombiner)
                {
                    foreach (GameObject combineParent in mainMeshCombiner.CombineParents)
                    {
                        if (mf.transform.IsChildOf(combineParent.transform))
                        {
                            willBeCombined = true;
                        }
                    }
                }

                if (!willBeCombined)
                {
                    if (!(mf.GetComponentInParent<PlayerCharacterController>() ||
                          mf.GetComponentInParent<EnemyController>() ||
                          mf.GetComponentInParent<Pickup>() ||
                          mf.GetComponentInParent<Objective>()))
                    {
                        nonCombinedMeshCount++;
                    }
                }
            }

            foreach (SkinnedMeshRenderer sm in skinnedMeshes)
            {
                polyCount += sm.sharedMesh.triangles.Length / 3;
            }

            int rigidbodiesCount = 0;
            foreach (var r in FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!r.isKinematic)
                {
                    rigidbodiesCount++;
                }
            }

            int lightsCount = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            int enemyCount = FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

            // 构建关卡分析字符串
            m_LevelAnalysisString += "- Meshes count: " + meshCount;
            m_LevelAnalysisString += k_NewLine;
            m_LevelAnalysisString += "- Animated models (SkinnedMeshes) count: " + skinnedMeshesCount;
            m_LevelAnalysisString += k_NewLine;
            m_LevelAnalysisString += "- Polygon count: " + polyCount;
            m_LevelAnalysisString += k_NewLine;
            m_LevelAnalysisString += "- Physics objects (rigidbodies) count: " + rigidbodiesCount;
            m_LevelAnalysisString += k_NewLine;
            m_LevelAnalysisString += "- Lights count: " + lightsCount;
            m_LevelAnalysisString += k_NewLine;
            m_LevelAnalysisString += "- Enemy count: " + enemyCount;

            // 根据分析结果生成性能优化建议
            if (nonCombinedMeshCount > 50)
            {
                m_SuggestionStrings.Add(nonCombinedMeshCount +
                                        " meshes in the scene are not setup to be combined on game start. Make sure that all the meshes " +
                                        "that will never move, change, or be removed during play are under the \"Level\" gameObject in the scene, so they can be combined for greater performance. \n \n" +
                                        "Note that it is always normal to have a few meshes that will not be combined, such as pickups, player meshes, enemy meshes, etc....");
            }
        }

        // 生成多边形数量热图
        void DoPolycountMap()
        {
            s_CellDatas.Clear();
            List<BoundsAndCount> meshBoundsAndCount = new List<BoundsAndCount>();
            Bounds levelBounds = new Bounds();
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            // 获取关卡边界和每个渲染器的边界及多边形数量
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer r = allRenderers[i];
                if (r.gameObject.GetComponent<IgnoreHeatMap>())
                    continue;

                levelBounds.Encapsulate(r.bounds);

                MeshRenderer mr = (r as MeshRenderer);
                if (mr)
                {
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf && mf.sharedMesh != null)
                    {
                        BoundsAndCount b = new BoundsAndCount();
                        b.Bounds = r.bounds;
                        b.Count = mf.sharedMesh.triangles.Length / 3;

                        meshBoundsAndCount.Add(b);
                    }
                }
                else
                {
                    SkinnedMeshRenderer smr = (r as SkinnedMeshRenderer);
                    if (smr)
                    {
                        if (smr.sharedMesh != null)
                        {
                            BoundsAndCount b = new BoundsAndCount();
                            b.Bounds = r.bounds;
                            b.Count = smr.sharedMesh.triangles.Length / 3;

                            meshBoundsAndCount.Add(b);
                        }
                    }
                }
            }

            Vector3 boundsBottomCorner = levelBounds.center - levelBounds.extents;
            Vector3Int gridResolution = new Vector3Int(Mathf.CeilToInt((levelBounds.extents.x * 2f) / k_CellSize),
                Mathf.CeilToInt((levelBounds.extents.y * 2f) / k_CellSize),
                Mathf.CeilToInt((levelBounds.extents.z * 2f) / k_CellSize));

            int highestCount = 0;
            for (int x = 0; x < gridResolution.x; x++)
            {
                for (int y = 0; y < gridResolution.y; y++)
                {
                    for (int z = 0; z < gridResolution.z; z++)
                    {
                        CellData cellData = new CellData();

                        Vector3 cellCenter = boundsBottomCorner + (new Vector3(x, y, z) * k_CellSize) +
                                             (Vector3.one * k_CellSize * 0.5f);
                        cellData.Bounds = new Bounds(cellCenter, Vector3.one * k_CellSize);
                        for (int i = 0; i < meshBoundsAndCount.Count; i++)
                        {
                            if (cellData.Bounds.Intersects(meshBoundsAndCount[i].Bounds))
                            {
                                cellData.Count += meshBoundsAndCount[i].Count;
                            }
                        }

                        if (cellData.Count > highestCount)
                        {
                            highestCount = cellData.Count;
                        }

                        s_CellDatas.Add(cellData);
                    }
                }
            }

            // 计算每个单元格的显示比例并设置颜色
            for (int i = 0; i < s_CellDatas.Count; i++)
            {
                s_CellDatas[i].Ratio = (float) s_CellDatas[i].Count / (float) highestCount;
                Color col = Color.Lerp(Color.green, Color.red, s_CellDatas[i].Ratio);
                s_CellDatas[i].Color = col;
            }

            m_HeatmapIsCalculating = false;
            EditorUtility.ClearProgressBar();
        }
    }
}
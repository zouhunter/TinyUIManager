namespace Tiny.UI
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    #region EditorDrawer
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditorInternal;
    using System;

    [CustomEditor(typeof(TinyUIManager))]
    public class TinyUIManagerDrawer : Editor
    {
        private ReorderableList panelItemsDrawer;
        private ReorderableList ruleListDrawer;

        private List<Rule> ruleList;
        private List<PanelItem> panelItemList;
        private TinyUIManager manager;
        private bool isEditRule;
        private SerializedProperty scriptProp;

        private void OnEnable()
        {
            manager = target as TinyUIManager;
            scriptProp = serializedObject.FindProperty("m_Script");
            ruleList = new List<Rule>(GetRules());
            panelItemList = new List<PanelItem>(GetPanelItems());

            panelItemsDrawer = new ReorderableList(panelItemList, typeof(PanelItem));
            panelItemsDrawer.drawHeaderCallback = DrawPanelItemsHead;
            panelItemsDrawer.elementHeight = EditorGUIUtility.singleLineHeight + 10;
            panelItemsDrawer.drawElementCallback = DrawPanelItemDetail;

            ruleListDrawer = new ReorderableList(ruleList, typeof(Rule));
            ruleListDrawer.drawHeaderCallback = DrawRuleListHead;
            ruleListDrawer.elementHeight = EditorGUIUtility.singleLineHeight + 10;
            ruleListDrawer.drawElementCallback = DrawRuleItemDetail;
        }


        #region GUI
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(scriptProp);
            EditorGUI.EndDisabledGroup();
            serializedObject.Update();
            if (isEditRule)
            {
                ruleListDrawer.DoLayoutList();
            }
            else
            {
                panelItemsDrawer.DoLayoutList();
            }
            SaveToBehaiver();
            serializedObject.ApplyModifiedProperties();
        }
        private void DrawPanelItemsHead(Rect rect)
        {
            var btn0Rect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);
            var btn1Rect = new Rect(btn0Rect.x - 70, rect.y, 60, rect.height);

            if (GUI.Button(btn0Rect, "检测"))
            {
                CheckPrefabs();
            }

            if (GUI.Button(btn1Rect, "关联"))
            {
                isEditRule = true;
            }

            EditorGUI.LabelField(rect, "预制体列表");
        }

        private void DrawRuleListHead(Rect rect)
        {
            var btn0Rect = new Rect(rect.x + rect.width - 60, rect.y, 60, rect.height);
            if (GUI.Button(btn0Rect, "完成"))
            {
                if (CheckRules())
                {
                    isEditRule = false;
                }
            }
            EditorGUI.LabelField(rect, "面板关联列表");
        }


        private void DrawPanelItemDetail(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = panelItemList[index];
            var boxRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);
            GUI.Box(boxRect, "");

            var idRect = new Rect(rect.x - 10, rect.y + 4, 30, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(idRect, index.ToString("00"));

            var fieldWidth = (boxRect.width - 40);
            var nameRect = new Rect(boxRect.x, boxRect.y, fieldWidth * 0.4f, boxRect.height);
            item.panelName = EditorGUI.TextField(nameRect, item.panelName);
            var objectRect = new Rect(boxRect.x + nameRect.width + 5, boxRect.y, fieldWidth * 0.5f, boxRect.height);
            item.prefab = EditorGUI.ObjectField(objectRect, item.prefab, typeof(GameObject), true) as GameObject;
            var btnRect = new Rect(boxRect.x + boxRect.width - 40, boxRect.y, 40, boxRect.height);

            if (item.prefab != null)
            {
                if (GUI.Button(btnRect, "o", EditorStyles.miniButtonRight))
                {
                    var instence = PrefabUtility.InstantiatePrefab(item.prefab) as GameObject;
                    instence.transform.SetParent(manager.transform, false);
                }
            }
        }

        private void DrawRuleItemDetail(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = ruleList[index];
            var boxRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10);
            GUI.Box(boxRect, "");

            var idRect = new Rect(rect.x - 10, rect.y + 4, 30, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(idRect, index.ToString("00"));

            var fieldWidth = (boxRect.width - 200);
            var name1Rect = new Rect(boxRect.x, boxRect.y, fieldWidth * 0.4f, boxRect.height);
            item.from_panel = EditorGUI.TextField(name1Rect, item.from_panel);
            var toRect = new Rect(name1Rect.x + name1Rect.width, name1Rect.y, 20, name1Rect.height);
            EditorGUI.LabelField(toRect, "→");
            var name2Rect = new Rect(boxRect.x + name1Rect.width + 20, boxRect.y, fieldWidth * 0.4f, boxRect.height);
            item.to_panel = EditorGUI.TextField(name2Rect, item.to_panel);

            var propRectA = new Rect(boxRect.x + fieldWidth, boxRect.y, 60, boxRect.height);
            var propRectB = new Rect(propRectA.x + 65, propRectA.y, propRectA.width, propRectA.height);
            var propRectC = new Rect(propRectB.x + 65, propRectA.y, propRectA.width, propRectA.height);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(item.from_panel));
            item.auto = EditorGUI.ToggleLeft(propRectA,new GUIContent("Auto", "子面板自动打开"), item.auto);
            EditorGUI.EndDisabledGroup();

            item.hide = EditorGUI.ToggleLeft(propRectB, new GUIContent("Hide", "父级面板隐藏"), item.hide);
            item.mutix = EditorGUI.ToggleLeft(propRectC, new GUIContent("Mutix", "兄弟面板互斥"), item.mutix);
        }

        #endregion
        private void SaveToBehaiver()
        {
            manager.GetType().GetField("bridges", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                 .SetValue(manager, ruleList.ToArray());
            manager.GetType().GetField("panelItems", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                 .SetValue(manager, panelItemList.ToArray());
            EditorUtility.SetDirty(manager);
        }
        private Rule[] GetRules()
        {
            var array = manager.GetType().GetField("bridges", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                 .GetValue(manager) as Rule[];
            if (array == null) array = new Rule[0];
            return array;
        }
        private PanelItem[] GetPanelItems()
        {
            var array = manager.GetType().GetField("panelItems", System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                 .GetValue(manager) as PanelItem[];
            if (array == null) array = new PanelItem[0];
            return array;
        }
        private bool CheckRules()
        {
            var ok = true;
            var keyset = new HashSet<string>();
            for (int i = 0; i < ruleList.Count; i++)
            {
                var rule = ruleList[i];
                string key = rule.to_panel;
                if (!string.IsNullOrEmpty(rule.from_panel))
                {
                    key = rule.from_panel + "." + rule.to_panel;
                }

                if (keyset.Contains(key))
                {
                    ok = false;
                    Debug.LogErrorFormat("关键字重复：{0} from:{1},to:{2}", i, rule.from_panel, rule.to_panel);
                }

                keyset.Add(key);
            }
            return ok;
        }
        private void CheckPrefabs()
        {
            var keyset = new HashSet<string>();
            for (int i = 0; i < panelItemList.Count; i++)
            {
                var item = panelItemList[i];
                if (keyset.Contains(item.panelName))
                {
                    Debug.LogErrorFormat("名称重复:{0}.{1}", i, item.panelName);
                }
                keyset.Add(item.panelName);

                if (item.prefab == null)
                {
                    Debug.LogErrorFormat("预制体为空:{0}.{1}", i, item.panelName);
                }
            }
        }
    }
#endif
#endregion
    /// <summary>
    /// 仅保留简单的打开关闭功能
    /// </summary>
    public class TinyUIManager : MonoBehaviour
    {
        [SerializeField]
        private Rule[] bridges;
        [SerializeField]
        private PanelItem[] panelItems;

        #region 固定
        private Dictionary<string, PanelItem> itemDic;//矛盾件缓存 
        private Dictionary<string, Rule> parentRuleBridgeDic;//父子级查找
        private Dictionary<string, Rule> allBridgeDic;//通用规则缓存
        private Dictionary<string, HashSet<string>> autoOpenDic;//自动打开规则
        #endregion 固定

        #region 动态改变
        private Dictionary<int, int> parentCatchDic;//记录面板的父级(创建物体时添加)
        private Dictionary<int, GameObject> panelCatchDic;//记录面板的GameObject;(创建物体时添加)
        private Dictionary<string, int> instenceCatchDic;//实例对象缓存(创建物体时添加)
        private Dictionary<int, HashSet<int>> childCatchDic;//子面板记录(创建物体时添加)
        private Dictionary<int, HashSet<int>> hideCatchDic;//隐藏作用记录(创建物体时添加)
        private Dictionary<int, HashSet<int>> passiveHideCatchDic;//被隐藏作用记录(创建物体时添加)
        private Dictionary<string, HashSet<int>> mutixBridgeDic;//互斥规则缓存
        #endregion

        private void Awake()
        {
            InitializeHelpers();
            InitializeRuntimeDics();
        }

        /// <summary>
        /// 清空创建的预制体
        /// 释放相关字典
        /// </summary>
        public void ResetAll()
        {
            using (var enumerator = panelCatchDic.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var instence = enumerator.Current.Value;
                    if (instence)
                    {
                        Destroy(instence);
                    }
                }
            }
            instenceCatchDic.Clear();
            mutixBridgeDic.Clear();
            parentCatchDic.Clear();
            panelCatchDic.Clear();
            childCatchDic.Clear();
            hideCatchDic.Clear();
            passiveHideCatchDic.Clear();
        }

        /// <summary>
        /// 打开面板
        /// 处理父类关系
        /// 处理兄弟关系
        /// 处理子类关系
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public GameObject Open(string panel, string parent = null)
        {
            if (string.IsNullOrEmpty(panel)) return null;
            Rule rule = TryFindRule(panel, parent);

            if (rule != null)
            {
                return FindOrInstencePanel(panel, rule.from_panel, rule.hide, rule.mutix);
            }
            else
            {
                return FindOrInstencePanel(panel);
            }

        }


        ///隐藏面板
        ///试图恢复父级
        ///释放父子关系
        ///试图打开被斥的面板
        ///释放斥锁
        /// </summary>
        /// <param name="panel"></param>
        public void Hide(string panel)
        {
            var instenceID = 0;
            if (instenceCatchDic.TryGetValue(panel, out instenceID))
            {
                HideInstence(instenceID);
                ShowBeHideItem(instenceID);
            }
        }

        public GameObject FindInstence(string panel)
        {
            int instenceID = 0;
            GameObject go = null;
            if (instenceCatchDic.TryGetValue(panel, out instenceID))
            {
                if (panelCatchDic.TryGetValue(instenceID, out go))
                {
                    if (go)
                        return go;
                }
            }
            return go;
        }
        public T FindComponent<T>(string panel) where T : MonoBehaviour
        {
            var instence = FindInstence(panel);
            if (instence)
            {
                return instence.GetComponent<T>();
            }
            return null;
        }

        #region private
        private GameObject FindOrInstencePanel(string panel, string parent = null, bool hideParent = false, bool mutix = false)
        {
            var instence = FindInstence(panel);
            if (instence == null)
            {
                instence = CreateInstence(panel);
            }

            if (instence != null)
            {
                if (!string.IsNullOrEmpty(parent))
                {
                    //处理父子关系
                    var parentInstenceID = 0;
                    if (instenceCatchDic.TryGetValue(parent, out parentInstenceID))
                    {
                        GameObject parentGo;
                        if (panelCatchDic.TryGetValue(parentInstenceID, out parentGo))
                        {
                            ProcessParentChild(instence, parentGo, hideParent);
                        }
                    }

                    //处理兄弟关系
                    if (mutix)
                    {
                        ProcessMutix(instence, parent);
                    }
                }

                if (!instence.activeSelf)
                {
                    var instenceID = instence.GetInstanceID();
                    if (CanShow(instenceID))
                    {
                        instence.gameObject.SetActive(true);
                    }
                }

                //自动打开子面板
                if (instence.activeInHierarchy)
                {
                    HashSet<string> subChilds = null;
                    if (autoOpenDic.TryGetValue(panel, out subChilds))
                    {
                        using (var enumerator = subChilds.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var childName = enumerator.Current;
                                Open(childName, panel);
                            }
                        }
                    }
                }
            }

            return instence;
        }

        /// <summary>
        /// 判断是否能显示指定的物体
        /// </summary>
        /// <param name="instenceID"></param>
        /// <returns></returns>
        private bool CanShow(int instenceID)
        {
            bool canShow = true;
            HashSet<int> handles = null;//隐藏持有者
            if (passiveHideCatchDic.TryGetValue(instenceID, out handles) && handles.Count > 0)//被隐藏
            {
                using (var enumerator = handles.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        GameObject targetHandle = null;
                        if (panelCatchDic.TryGetValue(enumerator.Current, out targetHandle))
                        {
                            if (targetHandle != null && targetHandle.activeInHierarchy)
                            {
                                canShow = false;
                                //被显示的对象持有
                                break;
                            }
                        }
                    }
                }
            }
            return canShow;
        }

        /// <summary>
        /// 处理父子关系
        /// </summary>
        /// <param name="instence"></param>
        /// <param name="parentGo"></param>
        /// <param name="parentState"></param>
        private void ProcessParentChild(GameObject instence, GameObject parentGo, bool hideParent)
        {
            Debug.Log("ProcessParentChild");
            int parentInstenceID = parentGo.GetInstanceID();
            var childInstenceID = instence.GetInstanceID();
            parentCatchDic[childInstenceID] = parentInstenceID;

            if (!childCatchDic.ContainsKey(parentInstenceID))
            {
                childCatchDic[parentInstenceID] = new HashSet<int>();
            }
            childCatchDic[parentInstenceID].Add(childInstenceID);

            if (hideParent)
            {
                parentGo.SetActive(false);
                RecordHide(childInstenceID, parentInstenceID);
            }
        }

        /// <summary>
        /// 处理同级互斥
        /// </summary>
        /// <param name="instence"></param>
        /// <param name="parent"></param>
        private void ProcessMutix(GameObject instence, string parent)
        {
            var mutixKey = MutixKey(parent);
            if (!mutixBridgeDic.ContainsKey(mutixKey))
            {
                mutixBridgeDic[mutixKey] = new HashSet<int>();
            }
            var mutixItems = mutixBridgeDic[mutixKey];
            var instenceID = instence.GetInstanceID();
            mutixItems.Add(instenceID);

            using (var enumerator = mutixItems.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current != instenceID)
                    {
                        GameObject mutixTarget = null;
                        if (panelCatchDic.TryGetValue(enumerator.Current, out mutixTarget))
                        {
                            if (mutixTarget != null)
                            {
                                mutixTarget.SetActive(false);
                                RecordHide(instenceID, enumerator.Current);
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="panel"></param>
        /// <returns></returns>
        private GameObject CreateInstence(string panel)
        {
            PanelItem panelItem = null;
            if (itemDic.TryGetValue(panel, out panelItem))
            {
                if (panelItem.prefab != null)
                {
                    var instence = GameObject.Instantiate(panelItem.prefab);
                    instence.transform.SetParent(transform, false);
                    instence.transform.SetAsLastSibling();
                    instence.name = panel;
                    var instenceID = instence.GetInstanceID();
                    panelCatchDic[instenceID] = instence;
                    instenceCatchDic[panel] = instenceID;
                    return instence;
                }
            }
            return null;
        }

        private void InitializeHelpers()
        {
            allBridgeDic = new Dictionary<string, Rule>();
            parentRuleBridgeDic = new Dictionary<string, Rule>();
            autoOpenDic = new Dictionary<string, HashSet<string>>();
            for (int i = 0; i < bridges.Length; i++)
            {
                var rule = bridges[i];
                if (string.IsNullOrEmpty(rule.to_panel)) continue;

                if (!string.IsNullOrEmpty(rule.from_panel))
                {
                    parentRuleBridgeDic.Add(ParentChildKey(rule.from_panel, rule.to_panel), rule);
                }
                else
                {
                    allBridgeDic.Add(rule.to_panel, rule);
                }

                if (rule.auto && !string.IsNullOrEmpty(rule.from_panel))
                {
                    if (!autoOpenDic.ContainsKey(rule.from_panel))
                    {
                        autoOpenDic[rule.from_panel] = new HashSet<string>() { rule.to_panel };
                    }
                    else
                    {
                        autoOpenDic[rule.from_panel].Add(rule.to_panel);
                    }
                }

            }
            itemDic = new Dictionary<string, PanelItem>();
            for (int i = 0; i < panelItems.Length; i++)
            {
                var panelItem = panelItems[i];
                itemDic.Add(panelItem.panelName, panelItem);
            }
        }

        private void InitializeRuntimeDics()
        {
            instenceCatchDic = new Dictionary<string, int>();
            mutixBridgeDic = new Dictionary<string, HashSet<int>>();
            parentCatchDic = new Dictionary<int, int>();
            panelCatchDic = new Dictionary<int, GameObject>();
            childCatchDic = new Dictionary<int, HashSet<int>>();
            hideCatchDic = new Dictionary<int, HashSet<int>>();
            passiveHideCatchDic = new Dictionary<int, HashSet<int>>();
        }

        private string ParentChildKey(string parent, string child)
        {
            return parent + "." + child;
        }

        private string MutixKey(string parent)
        {
            return parent;
        }
        private Rule TryFindRule(string panel, string parent)
        {
            Rule rule = null;

            if (!string.IsNullOrEmpty(parent))
            {
                var key = ParentChildKey(parent, panel);
                if (!parentRuleBridgeDic.TryGetValue(key, out rule))//指定规则
                {
                    allBridgeDic.TryGetValue(panel, out rule);//通用规则
                }
            }
            return rule;
        }

        /// <summary>
        /// <summary>
        /// 记录隐藏索引
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="target"></param>
        private void RecordHide(int handle, int target)
        {
            if (!hideCatchDic.ContainsKey(handle))
            {
                hideCatchDic[handle] = new HashSet<int>();
            }
            hideCatchDic[handle].Add(target);

            if (!passiveHideCatchDic.ContainsKey(target))
            {
                passiveHideCatchDic[target] = new HashSet<int>();
            }
            passiveHideCatchDic[target].Add(handle);

            //移除双向隐藏
            if (hideCatchDic.ContainsKey(target) && hideCatchDic[target].Contains(handle))
            {
                hideCatchDic[target].Remove(handle);
            }
            if (passiveHideCatchDic.ContainsKey(handle) && passiveHideCatchDic[handle].Contains(target))
            {
                passiveHideCatchDic[handle].Remove(target);
            }
        }

        /// <summary>
        /// 递归关闭面板
        /// </summary>
        /// <param name="instenceID"></param>
        private void HideInstence(int instenceID)
        {
            var parentID = 0;
            if (parentCatchDic.TryGetValue(instenceID, out parentID))
            {
                childCatchDic[parentID].Remove(instenceID);
                parentCatchDic.Remove(instenceID);
            }

            //关闭所有孩子
            HashSet<int> childs = null;
            if (childCatchDic.TryGetValue(instenceID, out childs))
            {
                var childCopy = new int[childs.Count];
                childs.CopyTo(childCopy);
                for (int i = 0; i < childCopy.Length; i++)
                {
                    HideInstence(childCopy[i]);
                }
                childCatchDic[instenceID].Clear();
            }

            //关闭当前面板
            GameObject panelGo = null;
            if (panelCatchDic.TryGetValue(instenceID, out panelGo))
            {
                if (panelGo.activeSelf)
                {
                    panelGo.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 显示被隐藏在面板
        /// </summary>
        /// <param name="instenceID"></param>
        private void ShowBeHideItem(int instenceID)
        {
            ///显示被锁定隐藏的物体
            HashSet<int> hidedItems = null;
            if (hideCatchDic.TryGetValue(instenceID, out hidedItems))
            {
                using (var enumerator = hidedItems.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var hidedItem = enumerator.Current;

                        if (passiveHideCatchDic.ContainsKey(hidedItem))
                        {
                            passiveHideCatchDic[hidedItem].Remove(instenceID);
                        }

                        if (CanShow(hidedItem))
                        {
                            GameObject body = null;
                            if (panelCatchDic.TryGetValue(hidedItem, out body))
                            {
                                if (!body.activeSelf)
                                {
                                    body.SetActive(true);
                                }
                            }
                        }
                    }

                }
                hideCatchDic.Remove(instenceID);
            }
        }

        #endregion
    }

    [System.Serializable]
    public class Rule//规则
    {
        public string from_panel;
        public string to_panel;
        public bool hide;
        public bool auto;
        public bool mutix;
    }

    [System.Serializable]
    public class PanelItem
    {
        public string panelName;
        public GameObject prefab;
    }
}
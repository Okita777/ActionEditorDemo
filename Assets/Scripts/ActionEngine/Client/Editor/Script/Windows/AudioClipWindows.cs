using System;
using System.Collections.Generic;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public class AudioClipWindows : EditorWindow
    {
        private static AudioClipWindows _instance = null;

        public static AudioClipWindows Instance
        {
            get
            {
                if (_instance is null) _instance = new AudioClipWindows();
                return _instance;
            }
        }
        // audioclip
        public void Open()
        {
            var window = EditorWindow.GetWindow<AudioClipWindows>("音频编辑窗口");
            window.Show();
        }
        public int selectedTool = 0;

        private const int buttonWeight = 150;//字典按钮宽度
        private const int buttonHeight = 20;//字典按钮高度
        private const int scrollWindht = 15;//滑动条宽度

        private Vector2 scrollPosition;
        private List<string> toolBar = new List<string>();
        private ReorderableList dicButtonList;
        private GUIStyle buttonStyle_R;
        private GUIStyle buttonStyle_L;
        private GUIStyle buttonStyle_C;

        private void OnEnable()
        {
            buttonStyle_R = new GUIStyle();
            buttonStyle_R.alignment = TextAnchor.MiddleRight;
            buttonStyle_R.normal.textColor = Color.yellow;
            buttonStyle_L = new GUIStyle();
            buttonStyle_L.alignment = TextAnchor.MiddleLeft;
            buttonStyle_C = new GUIStyle();
            buttonStyle_C.alignment = TextAnchor.MiddleCenter;
            buttonStyle_C.normal.textColor = Color.white;


            ActionEngineManager_AudioClip.Instance.Init();
            AudioClipDicList _DicList = ActionEngineManager_AudioClip.Instance._audioClipDicList;

            dicButtonList = new ReorderableList(_DicList.clips, null, true, false, false, false);
            dicButtonList.elementHeight = buttonHeight;
            dicButtonList.drawElementCallback = (rect, index, active, focused) =>
            {
                Rect buttonRect = new Rect(rect);

                using (new GUIColorScope(Color.gray, index == selectedTool))
                {
                    if (GUI.Button(buttonRect, _DicList.clips[index].name))
                    {
                        selectedTool = index;
                    }
                }
                if (_DicList.clips[index].initLoad)
                {
                    Rect loadRect = new Rect(rect);
                    loadRect.width = 5;
                    EditorGUI.DrawRect(loadRect, Color.blue * 0.7f);
                }
            };
        }

        private void OnGUI()
        {
            if (ActionEngineManager_AudioClip.Instance.loaded)
            {

                AudioClipDicList _DicList = ActionEngineManager_AudioClip.Instance._audioClipDicList;

                //底部菜单绘制
                Rect _downRect = new Rect(0, position.height - 20, position.width, 20);
                if (GUI.Button(_downRect, "保存列表"))
                {
                    SaveAudioConfig();
                }
                _downRect.y -= 20;
                _downRect.width = _downRect.width * 0.5f;
                if (GUI.Button(_downRect, "添加新的字典"))
                {
                    AudioClipDic _audioClipDic = new AudioClipDic();
                    _audioClipDic.name = $"字典：{_DicList.clips.Count}";
                    _audioClipDic.initialized = true;
                    _audioClipDic.loadFilish = true;
                    _DicList.clips.Add(_audioClipDic);
                }
                _downRect.x += _downRect.width;
                if (GUI.Button(_downRect, "删除当前字典"))
                {
                    if (selectedTool < _DicList.clips.Count)
                    {
                        _DicList.clips.RemoveAt(selectedTool);
                    }
                }

                if (_DicList.clips.Count < 1) return;

                toolBar.Clear();
                foreach (AudioClipDic VARIABLE in _DicList.clips)
                {
                    toolBar.Add(VARIABLE.name);
                }

                selectedTool = Mathf.Min(selectedTool, toolBar.Count - 1);

                //身体长度
                Rect _Rect = new Rect(2f, 30f, position.width, position.height - 70);
                Rect _bodyRect = new Rect(_Rect);
                if (_DicList.clips.Count > 0)
                {
                    _bodyRect.height = Math.Max(GetDrawDicMainHeight(_DicList.clips[selectedTool]), 20 * _DicList.clips.Count + 38);
                }
                else
                {
                    _bodyRect.height = 100;
                }
                _bodyRect.width = position.width - scrollWindht;

                //绘制GString
                Rect _mainRect = new Rect(0, 5, position.width, 20);
                DrawEditorAttribute.DrawGValue(_mainRect, "字典值来源(GString)", _DicList.gString, EGValueType.GString);

                //字典内容背景绘制
                Rect _DicMainRect = new Rect(_Rect);
                _DicMainRect.width = position.width - scrollWindht - buttonWeight;
                _DicMainRect.x = buttonWeight;
                EditorGUI.DrawRect(_DicMainRect, Color.black * 0.1f);

                //字典按钮绘制
                Rect _buttonRect = new Rect(_Rect);
                _buttonRect.width = buttonWeight;
                EditorGUI.DrawRect(_buttonRect, Color.black * 0.2f);
                using (var _scorll = new GUI.ScrollViewScope(_Rect, scrollPosition, _bodyRect, false, true))
                {
                    scrollPosition = _scorll.scrollPosition;

                    //按钮绘制
                    dicButtonList.DoList(_buttonRect);

                    //字典内容绘制
                    if (_DicList.clips.Count > 0)
                    {
                        DrawDicMain(_DicMainRect, _DicList.clips[selectedTool]);
                    }
                }


            }
            else
            {
                GUILayout.Label("加载音频资源中");
                if (GUILayout.Button("刷新窗口"))
                {
                    Open();
                }
            }
        }

        private void DrawDicMain(Rect _rect, AudioClipDic _dic)
        {
            _rect.x += 5;
            _rect.width -= 10;
            //头部绘制
            Rect _headerRect = new Rect(_rect);
            _headerRect.width = 100;
            _headerRect.height = buttonHeight;
            _dic.initLoad = EditorGUI.ToggleLeft(_headerRect, new GUIContent("初始化时加载", "在游戏开始前预先加载的音频资源列表"), _dic.initLoad);
            _headerRect.x += _headerRect.width;
            _headerRect.width = _rect.width - 100;
            _dic.name = EditorGUI.TextField(_headerRect, _dic.name);

            _headerRect.height = buttonHeight;
            _headerRect.width = 60;
            _headerRect.y += _headerRect.height;
            _headerRect.x = _rect.x;

            //默认音频
            GUI.Label(_headerRect, new GUIContent("默认音频", "在字典中没找到对应音频时返回的音频"));
            _headerRect.x += _headerRect.width;
            _headerRect.width = _rect.width - _headerRect.width;
            using (var _check = new EditorGUI.ChangeCheckScope())
            {
                AudioClip _obj = (AudioClip)EditorGUI.ObjectField(_headerRect, _dic.defaultClip, typeof(AudioClip), false);
                if (_check.changed)
                {
                    _dic.SetDefaultClip(_obj);
                }
            }

            //添加字典成员
            _headerRect = new Rect(_rect);
            _headerRect.y += buttonHeight * 2;
            _headerRect.height = buttonHeight;
            using (new GUIColorScope(Color.green))
            {
                if (GUI.Button(_headerRect, "添加字典成员"))
                {
                    AudioClipGroup_Part _part = new AudioClipGroup_Part();
                    _part.ClipName = "成员 " + _dic.parts.Count;
                    _dic.parts.Add(_part);
                    //更新字典
                    _dic.UpdateAudioClipDic();
                }
            }


            //字典成员列表
            _headerRect.y += 1;
            _headerRect.height = buttonHeight - 2;
            Rect _paixu = new Rect(_headerRect);
            _paixu.width = 39;
            Rect _removeRect = new Rect(_headerRect);
            _removeRect.width = 20;
            _headerRect.x += _paixu.width;
            _headerRect.width -= _paixu.width + _removeRect.width;
            _removeRect.x = _headerRect.width + _headerRect.x;
            for (int i = 0; i < _dic.parts.Count; i++)
            {
                //主要身体
                AudioClipGroup_Part _part = _dic.parts[i];
                _headerRect.y += buttonHeight;
                _paixu.y = _headerRect.y;
                _removeRect.y = _headerRect.y;
                EditorGUI.DrawRect(_headerRect, Color.black * 0.2f);
                if (GUI.Button(_headerRect, "", EditorStyles.label))
                {
                    _part.open = !_part.open;
                }

                GUI.Label(_headerRect, _part.ClipName, buttonStyle_C);
                GUI.Label(_headerRect, _part.AudioClips.Count + "  ", buttonStyle_R);

                //排序
                if (_part.isRandom) EditorGUI.DrawRect(_paixu, Color.black * 0.5f);
                else EditorGUI.DrawRect(_paixu, Color.cyan * 0.5f);
                if (GUI.Button(_paixu, _part.isRandom ? "  随机" : "  顺序", EditorStyles.label))
                {
                    _part.isRandom = !_part.isRandom;
                }

                //删除
                EditorGUI.DrawRect(_removeRect, Color.red * 0.5f);
                if (GUI.Button(_removeRect, "  -", EditorStyles.label))
                {
                    _dic.parts.RemoveAt(i);
                    _dic.UpdateAudioClipDic();
                }

                //绘制展开状态
                DrawAudioPart(_headerRect, _part, _dic);
                _headerRect.y += GetAudioPartHeight(_part);
            }
        }

        private int GetDrawDicMainHeight(AudioClipDic _dic)
        {
            int _dicHeight = 60 + _dic.parts.Count * 20;
            foreach (AudioClipGroup_Part VARIABLE in _dic.parts)
            {
                if (VARIABLE.open)
                {
                    _dicHeight += GetAudioPartHeight(VARIABLE);
                }
            }
            return _dicHeight;
        }

        private void DrawAudioPart(Rect _rect, AudioClipGroup_Part _part, AudioClipDic _clipDic)
        {
            if (_part.open)
            {
                Rect _partRect = new Rect(_rect);
                _partRect.y += _rect.height;

                //背景绘制
                _partRect.height += _part.AudioClips.Count * 20;
                EditorGUI.DrawRect(_partRect, Color.black * 0.1f);

                //绘制添加按钮
                Rect _addRect = new Rect(_partRect);
                _addRect.x -= 20;
                _addRect.width = 20;
                EditorGUI.DrawRect(_addRect, Color.green * 0.7f);
                GUI.Label(_addRect, " + ");
                if (GUI.Button(_addRect, "", EditorStyles.label))
                {
                    _part.AddAudioClip();
                }

                //绘制名称 
                _partRect.height = 18;
                _partRect.y += 1;
                using (var _check = new EditorGUI.ChangeCheckScope())
                {
                    _part.ClipName = EditorGUI.DelayedTextField(_partRect, _part.ClipName);
                    if (_check.changed)
                    {
                        _clipDic.UpdateAudioClipDic();
                    }
                }

                //绘制成员
                Rect _removeRect = new Rect(_partRect);
                _removeRect.width = 20;
                _removeRect.x += _partRect.width - _removeRect.width;
                _partRect.width -= _removeRect.width;
                for (int i = 0; i < _part.AudioClips.Count; i++)
                {
                    _partRect.y += 20;
                    _removeRect.y = _partRect.y;
                    using (var _check = new EditorGUI.ChangeCheckScope())
                    {
                        AudioClip _clip = (AudioClip)EditorGUI.ObjectField(_partRect, _part.AudioClips[i], typeof(AudioClip), false);
                        if (_check.changed)
                        {
                            _part.SetAudioClip(i, _clip);
                            _clipDic.UpdateAudioClipDic();
                        }
                    }

                    EditorGUI.DrawRect(_removeRect, Color.red * 0.3f);
                    if (GUI.Button(_removeRect, "  -", EditorStyles.label))
                    {
                        _part.RemoveClip(i);
                        _clipDic.UpdateAudioClipDic();
                    }
                }
            }
        }

        private int GetAudioPartHeight(AudioClipGroup_Part _part)
        {
            if (_part.open)
            {
                return _part.AudioClips.Count * 20 + 18;
            }
            return 0;
        }

        public void SaveAudioConfig()
        {
            AudioClipDicList _DicList = ActionEngineManager_AudioClip.Instance._audioClipDicList;
            SaveData.SaveAssetData(_DicList, ActionEngineManager_AudioClip._assetsFolder);
            AssetDatabase.Refresh();
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    [CustomEditor(typeof(ActionEngineManager))]

    public class ActionEngineManager_Editor : UnityEditor.Editor
    {
        private const string c_DynamicAttributesExcelPath = "Assets/Editor/GameData/ActionEditor/DynamicAttributesUpdata.xlsx";
        private const int c_DataStartRow = 6;
        private const int c_DataFirstCol = 2;

        private SerializedProperty m_RoleProperty;
        private ActionEngineManager main => target as ActionEngineManager;

        private void OnEnable()
        {
            m_RoleProperty = serializedObject.FindProperty("m_Role");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (var _check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_RoleProperty, new GUIContent("端角色"));
                main.OnInitHideMouse = EditorGUILayout.Toggle("运行时隐藏鼠标", main.OnInitHideMouse);

                int _number = (main.mDynamicAttributesUpdatas is null ? 0 : main.mDynamicAttributesUpdatas.Count);
                string buttonName = $"从表格导入列表数据 [{_number}]";
                if (GUILayout.Button(buttonName))
                {
                    if (EditorUtility.DisplayDialog("警告", "确定要从表格导入吗？将覆盖当前 Inspector 中的动态属性列表数据。", "导入", "取消"))
                    {
                        ImportDynamicAttributesFromExcel(main);
                    }
                }

                if (_check.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(main);
                }
            }
        }

        private static void ImportDynamicAttributesFromExcel(ActionEngineManager manager)
        {
            string path = c_DynamicAttributesExcelPath;
            if (!File.Exists(path))
            {
                EngineDebug.LogError($"未找到表格文件：{path}");
                return;
            }

            using var package = new ExcelPackage(new FileInfo(path));
            var wsList = package.Workbook.Worksheets.ToList();
            if (wsList.Count == 0)
            {
                EngineDebug.LogError($"表格无工作表：{path}");
                return;
            }
            var worksheet = wsList[0];

            if (worksheet.Dimension == null)
            {
                EngineDebug.LogError($"表格内容为空：{path}");
                return;
            }

            int lastRow = worksheet.Dimension.End.Row;
            Undo.RecordObject(manager, "Import DynamicAttributesFromExcel");
            manager.mDynamicAttributesUpdatas.Clear();

            for (int row = c_DataStartRow; row <= lastRow; row++)
            {
                if (IsRowEmpty(worksheet, row, c_DataFirstCol, 4))
                    continue;

                int c = c_DataFirstCol;
                int[] g1 = ParseIntArray(worksheet.Cells[row, c++].Value);
                int[] g2 = ParseIntArray(worksheet.Cells[row, c++].Value);
                int skilltag = ParseInt(worksheet.Cells[row, c++].Value);
                int comp = ParseInt(worksheet.Cells[row, c].Value);

                manager.mDynamicAttributesUpdatas.Add(new ActionEngineManager.SDynamicAttributesUpdata(g1, g2, skilltag, comp));
            }

            EditorUtility.SetDirty(manager);
            EngineDebug.Log($"从表格导入动态属性列表成功：{path} 数量:[{manager.mDynamicAttributesUpdatas.Count}]");
        }

        private static bool IsRowEmpty(ExcelWorksheet worksheet, int row, int firstColumn, int columnCount)
        {
            for (int c = firstColumn; c < firstColumn + columnCount; c++)
            {
                var v = worksheet.Cells[row, c].Value;
                if (v == null)
                    continue;
                if (v is string s && string.IsNullOrWhiteSpace(s))
                    continue;
                return false;
            }
            return true;
        }

        private static int[] ParseIntArray(object value)
        {
            if (value == null)
                return Array.Empty<int>();

            if (value is double d)
                return new[] { (int)d };
            if (value is int i)
                return new[] { i };

            string s = value.ToString().Trim();
            if (string.IsNullOrEmpty(s))
                return Array.Empty<int>();

            char[] separators = { ',', ';', '|', '，', ' ' };
            var parts = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return Array.Empty<int>();

            var list = new List<int>(parts.Length);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int n))
                    list.Add(n);
            }
            return list.ToArray();
        }

        private static int ParseInt(object value)
        {
            if (value == null)
                return 0;
            if (value is double d)
                return (int)d;
            if (value is int i)
                return i;
            if (value is long l)
                return (int)l;
            if (int.TryParse(value.ToString(), out int r))
                return r;
            return 0;
        }
    }
}

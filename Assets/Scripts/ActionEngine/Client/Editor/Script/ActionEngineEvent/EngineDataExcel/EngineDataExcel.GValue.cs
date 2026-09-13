using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{

    public partial class EngineDataExcel
    {
        private static List<EditorEngineGValuePart> gvaluePart = new List<EditorEngineGValuePart>();
        private const string c_GValueExcelPath = "Assets/Editor/GameData/ActionEditor/{0}.xlsx";

        // 数据区起始行（前 5 行为固定抬头/类型声明）
        private const int c_DataStartRow = 6;
        // 列定义：固定列号集中声明，避免散落魔法数字
        private const int c_ColIndex = 1;
        private const int c_ColName = 2;
        private const int c_ColSortId = 3;
        private const int c_ColType = 4;
        private const int c_ColDetail = 5;
        private const int c_ColHide = 6;

        public static void DrawGValueTool()
        {
            if (GUILayout.Button("导出", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要导出表格吗？可能会使当前未保存的表格丢失", "导出", "取消"))
                {
                    CreateExcel();
                }
            }

            if (GUILayout.Button("导入", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要从表格导入吗？可能会使当前未保存参数丢失", "导入", "取消"))
                {
                    ReadExcel();
                }
            }
        }

        private static void CreateExcel()
        {
            EditorEngineGValue _editorEngineGValue = ResourcesWindow.Instance.GetEngineGValue();
            string path = string.Format(c_GValueExcelPath, _editorEngineGValue.mName);

            var engineGValueList = _editorEngineGValue.part.ToList();
            engineGValueList.Sort((x, y) => x.Index.CompareTo(y.Index));

            if (!File.Exists(path))
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");
                // 固定抬头 - 行，列
                worksheet.Cells[1, 1].Value = "//字段说明";
                // worksheet.Cells[1, 4].Value = "GValue类型";

                worksheet.Cells[2, 1].Value = "//ID";
                worksheet.Cells[2, 2].Value = "编辑器名字";
                worksheet.Cells[2, 3].Value = "编辑器排序";
                worksheet.Cells[2, 4].Value = "GValue类型";
                worksheet.Cells[2, 5].Value = "GValue信息";
                worksheet.Cells[2, 6].Value = "编辑器中是否隐藏";

                worksheet.Cells[3, 1].Value = "unique";

                worksheet.Cells[4, 1].Value = "int";
                // worksheet.Cells[4, 2].Value = "string";      不导表
                // worksheet.Cells[4, 3].Value = "int";         不导表
                worksheet.Cells[4, 4].Value = "string";
                worksheet.Cells[4, 5].Value = "string";
                // worksheet.Cells[4, 6].Value = "int";         不导表

                worksheet.Cells[5, 1].Value = "id";
                // worksheet.Cells[5, 2].Value = "GValueName";   不导表
                // worksheet.Cells[5, 3].Value = "EditorIndex";  不导表
                worksheet.Cells[5, 4].Value = "GValueType";
                worksheet.Cells[5, 5].Value = "Detail";
                // worksheet.Cells[5, 6].Value = "EditorHide";   不导表

                var fi = new FileInfo(path);
                package.SaveAs(fi);
            }

            var row = c_DataStartRow;

            using (var package = new ExcelPackage(new FileInfo(path)))
            {
                var worksheets = package.Workbook.Worksheets.ToList();
                if (worksheets.Count == 0)
                {
                    EngineDebug.LogError($"[EngineDataExcel] 导出失败，表格无工作表：{path}");
                    return;
                }
                var worksheet = worksheets[0];

                // 清空旧数据行（Dimension 为空说明只有空表，无需清理）
                int lastRow = worksheet.Dimension?.End.Row ?? c_DataStartRow;
                int lastCol = worksheet.Dimension?.End.Column ?? c_ColHide;
                for (var i = c_DataStartRow; i <= lastRow; i++)
                {
                    worksheet.Cells[i, c_ColIndex, i, lastCol].Clear();
                }

                foreach (var part in engineGValueList)
                {
                    worksheet.Cells[row, c_ColIndex].Value = part.Index;
                    worksheet.Cells[row, c_ColName].Value = part.Name;
                    worksheet.Cells[row, c_ColSortId].Value = part.IndexID;
                    worksheet.Cells[row, c_ColType].Value = part.ValueType;
                    worksheet.Cells[row, c_ColHide].Value = _editorEngineGValue.RemoveIDs.Contains(part.Index) ? 1 : 0;
                    worksheet.Cells[row, c_ColDetail].Value = GetDetailForExport(part);

                    row++;
                }

                // 保存并覆盖原文件
                package.Save();
            }

            AssetDatabase.Refresh();
            var excelAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            if (excelAsset)
            {
                EditorGUIUtility.PingObject(excelAsset);
            }
            EngineDebug.Log($"导出GValue表成功：{path}");
        }

        private static void ReadExcel()
        {
            string path = string.Format(c_GValueExcelPath, ResourcesWindow.Instance.GetSelectGValue.mName);

            if (!File.Exists(path))
            {
                EngineDebug.LogError($"[EngineDataExcel] 没找到规则文件：{path}");
                return;
            }

            using var package = new ExcelPackage(new FileInfo(path));
            var worksheets = package.Workbook.Worksheets.ToList();
            if (worksheets.Count == 0)
            {
                EngineDebug.LogError($"[EngineDataExcel] 表格没有任何工作表：{path}");
                return;
            }

            var worksheet = worksheets[0];
            if (worksheet.Dimension == null)
            {
                EngineDebug.LogError($"[EngineDataExcel] 表格内容为空：{path}");
                return;
            }

            EditorEngineGValue _editorEngineGValue = new EditorEngineGValue
            {
                mName = ResourcesWindow.Instance.GetSelectGValue.mName,
                mID = ResourcesWindow.Instance.GetSelectGValue.mID,
            };

            var lastRow = worksheet.Dimension.End.Row;
            gvaluePart.Clear();

            int _failCount = 0;
            for (var i = c_DataStartRow; i <= lastRow; i++)
            {
                // 单行隔离：一行脏数据不应中断整个导入，记录后继续
                try
                {
                    ReadExcelRow(worksheet, i, _editorEngineGValue);
                }
                catch (Exception _ex)
                {
                    _failCount++;
                    EngineDebug.LogError($"[EngineDataExcel] 导入第[{i}]行失败：{_ex.Message}");
                }
            }

            gvaluePart.Sort((x, y) => x.Index.CompareTo(y.Index));
            _editorEngineGValue.part = gvaluePart.ToArray();
            _editorEngineGValue.SetIndexToSameType();
            ResourcesWindow.Instance.SetGValueData(_editorEngineGValue);

            if (_failCount > 0)
                EngineDebug.LogWarning($"[EngineDataExcel] GValue表导入完成（有[{_failCount}]行失败）：{path} 成功[{gvaluePart.Count}]条");
            else
                EngineDebug.Log($"[EngineDataExcel] GValue表覆写编辑器成功：{path} 长度:[{gvaluePart.Count}]");
        }

        private static void ReadExcelRow(ExcelWorksheet _worksheet, int _row, EditorEngineGValue _editorEngineGValue)
        {
            int id = GetIntValue(_worksheet.Cells[_row, c_ColIndex].Value);

            bool hide = GetIntValue(_worksheet.Cells[_row, c_ColHide].Value) > 0;
            if (hide && !_editorEngineGValue.RemoveIDs.Contains(id))
            {
                _editorEngineGValue.RemoveIDs.Add(id);
            }

            EditorEngineGValuePart targetOne = gvaluePart.Find(one => one.Index == id);
            if (targetOne == null)
            {
                string typeString = GetStringValue(_worksheet.Cells[_row, c_ColType].Value);
                if (!Enum.TryParse(typeString, out EGValueType valueType))
                {
                    EngineDebug.LogError($"[EngineDataExcel] 非法GValue类型[{typeString}]：id-{id}（行{_row}）");
                    return;
                }

                targetOne = ResourcesWindow.Instance.CreactGValuePart(valueType);
                targetOne.Index = id;
                targetOne.GroupID = ResourcesWindow.Instance.GetSelectGValue.mID;
                gvaluePart.Add(targetOne);
            }

            targetOne.Name = GetStringValue(_worksheet.Cells[_row, c_ColName].Value);
            targetOne.IndexID = GetIntValue(_worksheet.Cells[_row, c_ColSortId].Value);
            ApplyDetailValue(targetOne, _worksheet.Cells[_row, c_ColDetail].Value);
        }

        // 把表格 Detail 单元格写回 part.DefaultValue。
        // DefaultValue 为 null 或类型不匹配时自动重建，从根上消除导入端的 NRE/InvalidCast。
        private static void ApplyDetailValue(EditorEngineGValuePart _part, object _detail)
        {
            switch (_part.ValueType)
            {
                case EGValueType.GBool:
                    EnsureDefault(_part, () => new GVS_Bool()).value = GetIntValue(_detail) > 0;
                    break;
                case EGValueType.GInt:
                    EnsureDefault(_part, () => new GVS_Int()).value = GetIntValue(_detail);
                    break;
                case EGValueType.GFloat:
                    EnsureDefault(_part, () => new GVS_Float()).value = (float)Math.Round(GetIntValue(_detail) * 0.0001f, 4);
                    break;
                case EGValueType.GString:
                    EnsureDefault(_part, () => new GVS_String()).value = GetStringValue(_detail);
                    break;
                default:
                    // GEnum / GPoint / GTransform / GUnit / GGroup* 统一以 GVS_Enum 的 byte 存储
                    EnsureDefault(_part, () => new GVS_Enum()).value = (byte)GetIntValue(_detail);
                    break;
            }
        }

        // 导出端取 Detail 值，DefaultValue 缺失/类型不符时回退默认值，避免强转 null
        private static object GetDetailForExport(EditorEngineGValuePart _part)
        {
            switch (_part.ValueType)
            {
                case EGValueType.GBool:
                    return (_part.DefaultValue as GVS_Bool)?.value == true ? 1 : 0;
                case EGValueType.GInt:
                    return (_part.DefaultValue as GVS_Int)?.value ?? 0;
                case EGValueType.GFloat:
                    return Mathf.RoundToInt(((_part.DefaultValue as GVS_Float)?.value ?? 0f) * 10000);
                case EGValueType.GString:
                    return (_part.DefaultValue as GVS_String)?.value ?? string.Empty;
                default:
                    return (_part.DefaultValue as GVS_Enum)?.value ?? 0;
            }
        }

        private static T EnsureDefault<T>(EditorEngineGValuePart _part, Func<T> _factory) where T : GVS
        {
            if (_part.DefaultValue is T _typed) return _typed;
            T _new = _factory();
            _part.DefaultValue = _new;
            return _new;
        }

        private static string GetStringValue(object value) => value?.ToString() ?? string.Empty;

        // Excel 单元格数值健壮转换：兼容 null、double、int、string、bool 等，杜绝拆箱/转换异常
        private static int GetIntValue(object value)
        {
            switch (value)
            {
                case null: return 0;
                case double d: return (int)d;
                case float f: return (int)f;
                case int i: return i;
                case long l: return (int)l;
                case decimal m: return (int)m;
                case bool b: return b ? 1 : 0;
                case string s:
                    if (int.TryParse(s, out int _iv)) return _iv;
                    if (double.TryParse(s, out double _dv)) return (int)_dv;
                    return 0;
                default:
                    try { return Convert.ToInt32(value); }
                    catch { return 0; }
            }
        }
    }
}
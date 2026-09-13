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
        private const string c_SkillTagExcelPath = "Assets/Editor/GameData/ActionEditor/ActionEditorSkillTag.xlsx";

        public static void DrawSkillTagTool(List<SEditorTagData> _tagList, EditorPropertyType _type, Action _onImported)
        {
            if (GUILayout.Button("导出到表格"))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要导出到表格吗？可能会覆盖当前表格中的数据", "导出", "取消"))
                {
                    ExportSkillTag(_tagList, _type);
                }
            }

            if (GUILayout.Button("从表格导入"))
            {
                if (EditorUtility.DisplayDialog("警告", "确定要从表格导入吗？当前编辑器中未保存的数据将会丢失", "导入", "取消"))
                {
                    ImportSkillTag(_tagList, _type);
                    _onImported?.Invoke();
                }
            }
        }

        private static void ExportSkillTag(List<SEditorTagData> _tagList, EditorPropertyType _type)
        {
            string path = c_SkillTagExcelPath;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var sorted = new List<SEditorTagData>(_tagList);
            sorted.Sort((x, y) => x.ID.CompareTo(y.ID));

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                worksheet.Cells[1, 1].Value = "//字段说明";
                worksheet.Cells[2, 1].Value = "//标签ID";
                worksheet.Cells[2, 2].Value = "标签名称";
                worksheet.Cells[3, 1].Value = "unique";
                worksheet.Cells[4, 1].Value = "int";
                worksheet.Cells[4, 2].Value = "string";
                worksheet.Cells[5, 1].Value = "id";
                worksheet.Cells[5, 2].Value = "tagname";

                for (int i = 0; i < sorted.Count; i++)
                {
                    worksheet.Cells[i + 6, 1].Value = sorted[i].ID;
                    worksheet.Cells[i + 6, 2].Value = sorted[i].Name;
                }
                package.SaveAs(new FileInfo(path));
            }

            AssetDatabase.Refresh();
            var excelAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            if (excelAsset)
                EditorGUIUtility.PingObject(excelAsset);

            string typeName = _type == EditorPropertyType.EEPT_UnitTypeTag ? "单位" : "技能";
            EngineDebug.Log($"导出{typeName}标签表成功：{path} 数量:[{sorted.Count}]");
        }

        private static void ImportSkillTag(List<SEditorTagData> _tagList, EditorPropertyType _type)
        {
            string path = c_SkillTagExcelPath;
            if (!File.Exists(path))
            {
                EngineDebug.LogError($"没找到标签表格文件：{path}");
                return;
            }

            using var package = new ExcelPackage(new FileInfo(path));
            var worksheet = package.Workbook.Worksheets.ToList()[0];
            int lastRow = worksheet.Dimension.End.Row;

            _tagList.Clear();
            for (int i = 6; i <= lastRow; i++)
            {
                var idVal = worksheet.Cells[i, 1].Value;
                var nameVal = worksheet.Cells[i, 2].Value;
                if (idVal == null || nameVal == null) continue;

                int id;
                if (idVal is double d)
                    id = (int)d;
                else if (!int.TryParse(idVal.ToString(), out id))
                    continue;

                _tagList.Add(new SEditorTagData(nameVal.ToString(), id));
            }

            if (_type == EditorPropertyType.EEPT_UnitTypeTag)
                ResourcesWindow.Instance.ActionEngineEditorData.UpdateDic_Unit();
            else
                ResourcesWindow.Instance.ActionEngineEditorData.UpdateDic_Skill();

            string saveName = ResourcesWindow.EditorDatasName;
            if (!ActionWindowMain.ActionEditorFuntion.SaveAssetData(ResourcesWindow.Instance.ActionEngineEditorData, saveName))
            {
                EngineDebug.Log($"保存编辑器配置失败: {saveName}");
            }
            else
            {
                EngineDebug.LogWarning("已成功从表格导入并储存标签数据");
                AssetDatabase.Refresh();
            }

            string typeName = _type == EditorPropertyType.EEPT_UnitTypeTag ? "单位" : "技能";
            EngineDebug.Log($"从表格导入{typeName}标签成功：{path} 数量:[{_tagList.Count}]");
        }
    }
}

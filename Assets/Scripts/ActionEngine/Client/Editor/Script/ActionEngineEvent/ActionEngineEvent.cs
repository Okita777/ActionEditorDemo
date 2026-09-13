using System;
using System.Collections.Generic;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AsiTimeLine.Editor
{
    public partial class ActionEngineEvent : AsiActionEditorFuntion
    {
        public override void TimeLineInit() => EditorEventUpdate.OnInit();
        public override ActionEngineSetting GetDefaultSetting() => SaveData.GetDefaultSetting();
        //public override ActionEngineSetting getskill() => SaveData.GetDefaultSetting();
        public override ActionEngineSetting GetSkillSetting() => SaveData.GetSkillSetting();

        public override string DataName_Cam() => ActionEngineRuntimePath.DataName_Cam;
        public override string DataName_Item() => ActionEngineRuntimePath.DataName_Item;
        public override string DataName_Unit() => ActionEngineRuntimePath.DataName_Unit;
        public override string DataName_Skill() => ActionEngineRuntimePath.DataName_Skill;
        public override string DataName_Action() => ActionEngineRuntimePath.DataName_Action;
        public override string DataName_Module() => ActionEngineRuntimePath.DataName_Module;
        public override string DataName_Gvalue() => ActionEngineRuntimePath.DataName_Gvalue;
        public override string DataName_Equation() => ActionEngineRuntimePath.DataName_Equation;
        public override string DataName_Asset() => ActionEngineRuntimePath.DataName_Asset;
        public override string DataName_Input() => ActionEngineRuntimePath.DataName_Input;

        public override void ChangeAction() => EditorEventUpdate.OnChangeAction();

        public override bool TimeLineUpdate(int _time, EditorActionEvent _actionEvent)
            => EditorEventUpdate.OnUpdate(_time, _actionEvent);

        public override bool DrawEventDate(EditorActionEvent _actionEvent, bool _isInit)
            => DrawInspector.DrawEvent(_actionEvent, _isInit);

        public override bool DrawEventHelpWindows(EditorActionEvent _actionEvent, out Action _drawCallBack)
            => DrawInspectorHelpWindow.DrawEventHelpWindows(_actionEvent, out _drawCallBack);

        public override void DrawCondition(IInterruptCondition _actionCondition, bool _isInit)
            => DrawInspector.DrawCondition(_actionCondition, _isInit);

        public override string DrawEventTitle(EditorActionEvent _actionEvent, bool _isInit)
            => DrawInspector.GetTitle(_actionEvent, _isInit);

        public override string DrawEventDetailed(EditorActionEvent _actionEvent, bool _isInit)
            => DrawInspector.GetDetailed(_actionEvent, _isInit);

        public override EditorActionEvent CreactActionEvent(int _id)
            => Editor.CreactActionEvent.Events(_id);

        public override IInterruptCondition CreactCondition(int _id)
            => Editor.CreactActionEvent.Condition(_id);

        public override string BluePrint_Name(string name) => DrawBluePrintName.GetName(name);
        public override IProperty BluePrint_Create(string name) => CreactBluePrint.CreateBluePrint(name);

        public override bool LoadActionData(out EditorActionStateInfo _editorActionState, string _actionName)
            => Editor.SaveData.LoadActionData(out _editorActionState, _actionName);

        public override bool LoadUnitData(out EditorUnitWarp _saveData, string _unitName)
            => Editor.SaveData.LoadUnitData(out _saveData, _unitName);

        public override bool LoadSkillData(out EditorSkillWarp _saveData, string _name)
            => Editor.SaveData.LoadSkillData(out _saveData, _name);

        public override bool LoadCameraData(out EditorCameraWarp _saveData, string _name)
            => Editor.SaveData.LoadCameraWarp(out _saveData, _name);

        public override bool LoadPropData(out EditorPropWarp _saveData, string _name)
            => Editor.SaveData.LoadPropWarp(out _saveData, _name);

        public override bool LoadGValueData(out EditorEngineGValue _editorActionState, string _gvalueName)
            => Editor.SaveData.LoadGValueState(out _editorActionState, _gvalueName);

        public override bool LoadEquationData(out EditorGValueEquation _editorGValueEquation, string _gvalueName)
            => SaveData.LoadEquation(out _editorGValueEquation, _gvalueName);

        public override bool SaveEquationData(EditorGValueEquation _equation, string _name)
            => Editor.SaveData.SaveEquationData(_equation, _name);

        public override bool SaveActionData(EditorActionStateInfo _editorActionState, string _actionName)
            => Editor.SaveData.SaveActionData(_editorActionState, _actionName);

        public override bool SaveEditorActionData(EditorActionStateInfo _editorActionState, string _actionName)
            => Editor.SaveData.SaveEditorActionData(_editorActionState, _actionName);

        public override bool SaveGValueData(EditorEngineGValue _editorActionState, string _gvalueName)
            => Editor.SaveData.SaveGValueData(_editorActionState, _gvalueName);

        public override bool SaveInputModuleData(InputModuleInfo _cameraWarp, string _name)
            => Editor.SaveData.SaveInputModuleData(_cameraWarp, _name);

        public override bool SaveAssetData(object _asset, string _name) => Editor.SaveData.SaveAssetData(_asset, _name);

        public override bool SaveUnitData(EditorUnitWarp _saveData, string _unitName)
            => Editor.SaveData.SaveUnitData(_saveData, _unitName);

        public override bool SaveSkillData(EditorSkillWarp _saveData, string _name)
            => Editor.SaveData.SaveSkillData(_saveData, _name);

        public override bool SavePropData(EditorPropWarp _saveData, string _unitName)
            => Editor.SaveData.SavePropWarp(_saveData, _unitName);

        public override bool SaveCameraData(EditorCameraWarp _cameraWarp, string _name)
            => Editor.SaveData.SaveCameraWarp(_cameraWarp, _name);

        public override bool LoadAssetData<T>(string _name, out T _saveData) =>
            Editor.SaveData.LoadAssetData(out _saveData, _name);

        public override string GetEditorDataPath() => ActionEngineConst.EditorDataPartPath;
        public override string GetEditorPath() => ActionEngineConst.EditorDataPath;


        public override string GetRunTimeDataPath() => ActionEngineConst.RunTimeSavePath;

        public override string GetInputModuleEditor() => ActionEngineConst.EditorInputModuleSavePath();
        public override string GetInputActionEditor() => ActionEngineConst.EditorInputActionSavePath();

        public override bool ReLoadAcrionList(string _GroupName) => EditorEventUpdate.ReLoadActionData(_GroupName);

        public override void SetEngineUpdateEnble(bool _enble) => ActionEngineManager.Instance.OnUpdateEnble = _enble;

        public override ActionEngine_Unit GetPlayer() => ActionEngineManager_Input.Instance.Player;
        public override List<ActionEngine_Unit> GetUnits() => ActionEngineManager_Unit.Instance.Units;
        public override void ChangePlayer(ActionEngine_Unit _player) => ActionEngineManager_Input.Instance.ChangePlayer(_player);

        //战斗数据交互
        public override IAttackInfo AttackInfo() => new AttackInfo();

        //单位预览
        public override void ActionUnitPreview(ActionPreviewMark _previewMark, GameObject _target)
        {
            var _unitPreview = _target.AddComponent<UnitEditorPreview>();

            _unitPreview.IsPlayer = _previewMark.IsPlayer;
            _unitPreview.ActionName = _previewMark.ActionName;
            _unitPreview.DefaltCamera = _previewMark.DefaltCamera;
            _unitPreview.DefaltWeapon = _previewMark.DefaltWeapon;

            _unitPreview.CamOffsetPos = _previewMark.CamOffsetPos;
            _unitPreview.CamRotSpeed = _previewMark.CamRotSpeed;

            Object.DestroyImmediate(_previewMark);
        }

        public override void DrawGValueTool() => EngineDataExcel.DrawGValueTool();

        public override void DrawSkillTagTool(List<SEditorTagData> _tagList, EditorPropertyType _type, Action _onImported)
            => EngineDataExcel.DrawSkillTagTool(_tagList, _type, _onImported);

    }
}

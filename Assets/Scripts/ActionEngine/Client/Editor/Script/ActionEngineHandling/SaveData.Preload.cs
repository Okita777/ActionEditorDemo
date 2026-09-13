using System.Collections.Generic;
using AsiActionEngine.Editor;
using AsiActionEngine.RunTime;
using AsiTimeLine.RunTime;

namespace AsiTimeLine.Editor
{
    // 保存动作数据时，提前扫描所有事件，把需要预热的资源 path 收集到 ActionStateInfo.mCollectWarmUp，
    // 让运行时装载完成后能直接按列表预热对象池，省去运行时遍历事件的开销。
    // 当前收集 Event_PlayParticle.PartoclePath，并识别 Event_CreateSkill 进入其 SkillWarp.ActionStateInfo 递归收集。
    // 如需扩展 Light/SkillLight/AfterImageEntity/WeaponTrail 等，在 CollectFromEvent 中追加 is 分支即可。
    public partial class SaveData
    {
        public struct WarmUpCollectResult
        {
            public List<string> Paths;
            public int SkillPathCount;
        }

        public static List<string> CollectWarmUpPaths(ActionStateInfo _info)
        {
            return CollectWarmUpResult(_info).Paths;
        }

        public static WarmUpCollectResult CollectWarmUpResult(ActionStateInfo _info)
        {
            List<string> _result = new List<string>();
            if (_info?.mActionState == null)
            {
                return new WarmUpCollectResult
                {
                    Paths = _result,
                    SkillPathCount = 0
                };
            }

            HashSet<string> _seen = new HashSet<string>();
            HashSet<int> _visitedSkillIds = new HashSet<int>();
            int _skillPathCount = CollectFromActionStateInfo(_info, _seen, _visitedSkillIds, _result, false);
            return new WarmUpCollectResult
            {
                Paths = _result,
                SkillPathCount = _skillPathCount
            };
        }

        private static int CollectFromActionStateInfo(ActionStateInfo _info, HashSet<string> _seen, HashSet<int> _visitedSkillIds, List<string> _result, bool _isFromSkill)
        {
            if (_info?.mActionState == null) return 0;
            int _skillPathCount = 0;
            foreach (ActionState _state in _info.mActionState)
            {
                _skillPathCount += CollectEvents(_state.EventList, _seen, _visitedSkillIds, _result, _isFromSkill);
                _skillPathCount += CollectEvents(_state.EventList_Anim, _seen, _visitedSkillIds, _result, _isFromSkill);
                _skillPathCount += CollectFromEvent(_state.AnimEvent, _seen, _visitedSkillIds, _result, _isFromSkill);
            }
            return _skillPathCount;
        }

        private static int CollectEvents(List<ActionEvent> _list, HashSet<string> _seen, HashSet<int> _visitedSkillIds, List<string> _result, bool _isFromSkill)
        {
            if (_list == null) return 0;
            int _skillPathCount = 0;
            foreach (ActionEvent _ev in _list)
                _skillPathCount += CollectFromEvent(_ev, _seen, _visitedSkillIds, _result, _isFromSkill);
            return _skillPathCount;
        }

        private static int CollectFromEvent(ActionEvent _ev, HashSet<string> _seen, HashSet<int> _visitedSkillIds, List<string> _result, bool _isFromSkill)
        {
            if (_ev?.EventData is Event_PlayParticle _particle)
            {
                string _path = _particle.PartoclePath;
                if (string.IsNullOrEmpty(_path)) return 0;
                if (!_seen.Add(_path)) return 0;
                _result.Add(_path);
                return _isFromSkill ? 1 : 0;
            }

            if (_ev?.EventData is not Event_CreateSkill _createSkill) return 0;
            if (_createSkill.IsUsGValue) return 0;

            int _skillId = _createSkill.SkillID;
            if (!_visitedSkillIds.Add(_skillId)) return 0;

            if (!ResourcesWindow.Instance.GetSkillToID(_skillId, out EditorSkillWarp _skillWarp)) return 0;

            SkillWarp _runtimeSkill = _skillWarp.GetSkillWarp();
            return CollectFromActionStateInfo(_runtimeSkill.ActionStateInfo, _seen, _visitedSkillIds, _result, true);
        }
    }
}

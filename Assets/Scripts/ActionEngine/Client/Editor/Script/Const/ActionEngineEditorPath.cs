using UnityEditor;

namespace AsiTimeLine.Editor
{
    public class ActionEngineEditorPath
    {
        private static ActionEngineEditorPath mInstance;
        public static ActionEngineEditorPath Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new ActionEngineEditorPath();
                return mInstance;
            }
        }

        private string mEditorLocalPath = null;
        public string EditorLocalPath
        {
            get
            {
                if (string.IsNullOrEmpty(mEditorLocalPath))
                {
                    string[] _guid = AssetDatabase.FindAssets(GetType().Name);
                    string[] _nowPath = AssetDatabase.GUIDToAssetPath(_guid[0]).Split('/');
                    for (int i = 0; i < _nowPath.Length - 3; i++)
                        mEditorLocalPath += (_nowPath[i] + "/");
                }
                return mEditorLocalPath;
            }
        }
    }
}
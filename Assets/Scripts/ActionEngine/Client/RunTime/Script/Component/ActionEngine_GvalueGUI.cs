using AsiActionEngine.RunTime;
using UnityEngine;
using UnityEngine.UI;

namespace AsiTimeLine.RunTime
{
    public class ActionEngine_GvalueGUI : MonoBehaviour
    {
        [SerializeField] protected GFloat mGFloat = new GFloat(0, true);
        [SerializeField] protected GInt mGInt = new GInt(0, true);

        [EditorProperty("GFloat", EditorPropertyType.EEPT_GFloat)]
        public GFloat GFloat
        {
            get { return mGFloat; }
            set { mGFloat = value; }
        }
        [EditorProperty("GInt", EditorPropertyType.EEPT_GInt)]
        public GInt GInt
        {
            get { return mGInt; }
            set { mGInt = value; }
        }

        public bool mDrawToPlayer = true;
        public bool mIsDisFloat = true;
        public bool mBoundRange = true;
        public Text mText = null;
        public RectTransform mTrans;
        private Image mImage;
        public float mFillAmountTime = 0.3f;
        public float mFillAmountSpeed = 4f;
        //public ACImage
        public Vector2 mTransRange = new Vector2(0, 1);
        public Vector2 mValueRange = new Vector2(0, 1);

        private ActionEngine_Unit mTargetUnit;
        private bool mIsInit = false;
        private bool mDrawToText = false;
        private bool mDrawToTrans = false;
        private float mProportion = 1;
        private float mAxisP = 0;
        void Start()
        {
            StartCoroutine(InitDelayed());
        }

        /// <summary> 延迟一帧初始化：mTrans 可能引用 AC 程序集中的 ACImage，该程序集晚于 ActionEngine 加载，需等待引用解析 </summary>
        private System.Collections.IEnumerator InitDelayed()
        {
            yield return null;  // 等待一帧，确保跨程序集引用（如 mTrans→ACImage）已解析
            mIsInit = false;
            mDrawToText = mText != null;
            mDrawToTrans = mTrans != null;
            if (mDrawToTrans)
            {
                mImage = mTrans.gameObject.GetComponent<Image>();  // ACImage 继承 Image，可正确获取
                if (mImage == null) mDrawToTrans = false;
            }
            float _f1 = mTransRange.y - mTransRange.x;
            float _f2 = mValueRange.y - mValueRange.x;
            if (_f1 <= 0) yield break;
            mProportion = _f1 / _f2;
            ActionEngineManager_Input.Instance.WaitPlayerLoad(_unit =>
            {
                mIsInit = true;
                mTargetUnit = _unit;
            });
        }
        public void Update()
        {
            if (mIsInit)
            {
                if (mIsDisFloat)
                    mAxisP = mGFloat.GetValue(mTargetUnit.ActionStateMachine.FirstStatePart);
                else
                    mAxisP = mGInt.GetValue(mTargetUnit.ActionStateMachine.FirstStatePart);

                if (mDrawToTrans)
                {
                    float _width = mAxisP * mProportion;
                    if (mBoundRange) _width = Mathf.Clamp(_width, mTransRange.x, mTransRange.y);
                    float _fillAmount = _width / mTransRange.y;
                    if (_fillAmount > mImage.fillAmount)
                    {
                        mImage.fillAmount = _fillAmount;
                    }
                    else
                    {
                        float _delta = mImage.fillAmount - _fillAmount;
                        float _speed = mFillAmountSpeed * Time.deltaTime / mFillAmountTime;
                        mImage.fillAmount = Mathf.MoveTowards(mImage.fillAmount, _fillAmount, _delta * _speed);
                    }
                }
                if (mDrawToText)
                    mText.text = mAxisP.ToString();
            }
        }
    }
}
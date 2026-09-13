using System.Collections.Generic;
using AsiActionEngine.RunTime.DrawData;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace AsiActionEngine.RunTime
{
    public class EngineScenceDraw
    {
        public static void Box(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;
            Draw_BoxData _box = new Draw_BoxData(position, rotation, scale, color);
            EngineDrawListData.Instance.Draw_Box_Data.Add(_box);
        }

        public static void Sphere(Vector3 position, Quaternion rotation, float radius, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;

            Draw_SphereData _sphere = new Draw_SphereData(position, rotation, radius, color, _life);
            EngineDrawListData.Instance.Draw_Sphere_Data.Add(_sphere);
        }

        public static void Line(Vector3 start, Vector3 end, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;

            Draw_LineData _line = new Draw_LineData(start, end, color, _life);
            EngineDrawListData.Instance.Draw_Line_Data.Add(_line);
        }

        public static void Capsule(Vector3 startPos, Vector3 endPos, float radius, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;

            Draw_CapsuleData _capsule = new Draw_CapsuleData(startPos, endPos, radius, color, _life);
            EngineDrawListData.Instance.Draw_Capsule_Data.Add(_capsule);
        }

        public static void WireArc(Vector3 position, Vector3 Axis_Y, Vector3 Axis_Z, float angle, float radius, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;

            Draw_WireArcData _wireArc = new Draw_WireArcData(position, Axis_Y, Axis_Z, angle, radius, color, _life);
            EngineDrawListData.Instance.Draw_WireArc_Data.Add(_wireArc);
        }
        public static void SolidArc(Vector3 position, Vector3 Axis_Y, Vector3 Axis_Z, float angle, float radius, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;

            Draw_SolidArcData _wireArc = new Draw_SolidArcData(position, Axis_Y, Axis_Z, angle, radius, color, _life);
            EngineDrawListData.Instance.Draw_SolidArc_Data.Add(_wireArc);
        }
        public static void WireDisc(Vector3 position, Vector3 Axis_Y, float radius, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;

            Draw_WireDiscData _wireDisc = new Draw_WireDiscData(position, Axis_Y, radius, color, _life);
            EngineDrawListData.Instance.Draw_WireDisc_Data.Add(_wireDisc);
        }

        public static void Text(Vector3 position, string mainText, int size, Color color, float _life = -1)
        {
            if (!EngineDrawListData.Instance.DrawState) return;

            Draw_Text _sphere = new Draw_Text(position, mainText, size, color, _life);
            EngineDrawListData.Instance.Draw_Text_Data.Add(_sphere);
        }
    }

    #region 绘制数据的结构体
    public struct Draw_BoxData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color color;
        public float life;

        public Draw_BoxData(Vector3 position, Quaternion rotation, Vector3 scale, Color color, float life = -1)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.color = color;
            this.life = life;
        }

        public Draw_BoxData SubLife(float _life)
        {
            return new Draw_BoxData(position, rotation, scale, color, life - _life);
        }
    }

    public struct Draw_SphereData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float radius;
        public Color color;
        public float life;


        public Draw_SphereData(Vector3 position, Quaternion rotation, float radius, Color color, float life = -1)
        {
            this.position = position;
            this.rotation = rotation;
            this.radius = radius;
            this.color = color;
            this.life = life;
        }

        public Draw_SphereData SubLife(float _life)
        {
            return new Draw_SphereData(position, rotation, radius, color, life - _life);
        }
    }

    public struct Draw_Text
    {
        public Vector3 position;
        public string mainText;
        public int radius;
        public Color color;
        public float life;

        public Draw_Text(Vector3 position, string rotation, int radius, Color color, float life = -1)
        {
            this.position = position;
            this.mainText = rotation;
            this.radius = radius;
            this.color = color;
            this.life = life;
        }

        public Draw_Text SubLife(float _life)
        {
            return new Draw_Text(position, mainText, radius, color, life - _life);
        }
    }

    public struct Draw_LineData
    {
        public Vector3 start;
        public Vector3 end;
        public Color color;
        public float life;

        public Draw_LineData(Vector3 start, Vector3 end, Color color, float life = -1)
        {
            this.start = start;
            this.end = end;
            this.color = color;
            this.life = life;

        }
        public Draw_LineData SubLife(float _life)
        {
            return new Draw_LineData(start, end, color, life - _life);
        }
    }

    public struct Draw_CapsuleData
    {
        public Vector3 startPos;
        public Vector3 endPos;
        public float radius;
        public Color color;
        public float life;

        public Draw_CapsuleData(Vector3 startPos, Vector3 endPos, float radius, Color color, float life = -1)
        {
            this.startPos = startPos;
            this.endPos = endPos;
            this.radius = radius;
            this.color = color;
            this.life = life;
        }

        public Draw_CapsuleData SubLife(float _life)
        {
            return new Draw_CapsuleData(startPos, endPos, radius, color, life - _life);
        }
    }

    public struct Draw_WireArcData
    {
        public Vector3 position;
        public Vector3 Axis_Y;
        public Vector3 Axis_Z;
        public float radius;
        public float angle;
        public Color color;
        public float life;

        public Draw_WireArcData(Vector3 position, Vector3 Axis_Y, Vector3 Axis_Z, float angle, float radius, Color color, float life = -1)
        {
            this.position = position;
            this.Axis_Y = Axis_Y;
            this.Axis_Z = Axis_Z;
            this.angle = angle;
            this.radius = radius;
            this.color = color;
            this.life = life;

        }

        public Draw_WireArcData SubLife(float _life)
        {
            return new Draw_WireArcData(position, Axis_Y, Axis_Z, angle, radius, color, life - _life);
        }
    }
    public struct Draw_SolidArcData
    {
        public Vector3 position;
        public Vector3 Axis_Y;
        public Vector3 Axis_Z;
        public float radius;
        public float angle;
        public Color color;
        public float life;

        public Draw_SolidArcData(Vector3 position, Vector3 Axis_Y, Vector3 Axis_Z, float angle, float radius, Color color, float life = -1)
        {
            this.position = position;
            this.Axis_Y = Axis_Y;
            this.Axis_Z = Axis_Z;
            this.angle = angle;
            this.radius = radius;
            this.color = color;
            this.life = life;
        }

        public Draw_SolidArcData SubLife(float _life)
        {
            return new Draw_SolidArcData(position, Axis_Y, Axis_Z, angle, radius, color, life - _life);
        }

    }
    public struct Draw_WireDiscData
    {
        public Vector3 position;
        public Vector3 Axis_Y;
        public float radius;
        public Color color;
        public float life;

        public Draw_WireDiscData(Vector3 position, Vector3 Axis_Y, float radius, Color color, float life = -1)
        {
            this.position = position;
            this.Axis_Y = Axis_Y;
            this.radius = radius;
            this.color = color;
            this.life = life;

        }

        public Draw_WireDiscData SubLife(float _life)
        {
            return new Draw_WireDiscData(position, Axis_Y, radius, color, life - _life);
        }
    }

    public class Draw_PointTransData
    {
        public Vector3 pos;
        public Quaternion rot;
        public Color color;
        public string name;
        public float value;
        public float life;

        public Draw_PointTransData(Vector3 pos, Quaternion rot, string name, Color color, float life = -1)
        {
            this.pos = pos;
            this.rot = rot;
            this.color = color;
            this.name = name;
            this.life = life;
        }
    }
    #endregion
}

namespace AsiActionEngine.RunTime.DrawData
{
    // [InitializeOnLoad]
    public class EngineDrawListData
    {
        private int maxDrawConst = 500;
        private bool mDrawState = true;
        private static EngineDrawListData _instance;
        private GUIStyle _fontStyle = new GUIStyle();
        public static EngineDrawListData Instance
        {
            get
            {
                if (_instance is null)
                {
                    _instance = new EngineDrawListData();
                }
                return _instance;
            }
        }
        public bool DrawState
        {
            get
            {
                if (!Application.isPlaying) return true;//编辑模式下必定绘制
                return mDrawState;
            }
            set { mDrawState = value; }
        }
        public EngineDrawListData()
        {
            Init();
        }

        private bool isPlaying = false;
        public List<Draw_BoxData> Draw_Box_Data = new List<Draw_BoxData>();
        public List<Draw_CapsuleData> Draw_Capsule_Data = new List<Draw_CapsuleData>();
        public List<Draw_LineData> Draw_Line_Data = new List<Draw_LineData>();
        public List<Draw_SphereData> Draw_Sphere_Data = new List<Draw_SphereData>();
        public List<Draw_Text> Draw_Text_Data = new List<Draw_Text>();
        public List<Draw_WireArcData> Draw_WireArc_Data = new List<Draw_WireArcData>();
        public List<Draw_SolidArcData> Draw_SolidArc_Data = new List<Draw_SolidArcData>();
        public List<Draw_WireDiscData> Draw_WireDisc_Data = new List<Draw_WireDiscData>();
        public List<Draw_PointTransData> Draw_Point_Group = new List<Draw_PointTransData>();
        public List<Draw_PointTransData> Draw_Point_Group_Sort = new List<Draw_PointTransData>();

        public void Init()
        {
            Draw_Box_Data.Clear();
            Draw_Capsule_Data.Clear();
            Draw_Line_Data.Clear();
            Draw_Sphere_Data.Clear();
            Draw_WireArc_Data.Clear();
            Draw_SolidArc_Data.Clear();
            Draw_WireDisc_Data.Clear();
            Draw_Text_Data.Clear();
            //EngineDebug.LogWarning("初始化？？？ : ");

            // Draw_Point_Group.Clear();
        }

        public void Draw(float deltaTime = 1.0f)
        {
            _fontStyle.fontStyle = FontStyle.Bold;
            _fontStyle.alignment = TextAnchor.MiddleCenter;

            //EngineDebug.LogWarning("胶囊绘制数量: " + Draw_Capsule_Data.Count);
            //EngineDebug.LogWarning("Box绘制数量: " + Draw_Box_Data.Count);
            //EngineDebug.LogWarning("球绘制数量: " + Draw_Sphere_Data.Count);

            int _drawConst = Draw_Box_Data.Count + Draw_Capsule_Data.Count + Draw_Line_Data.Count +
                Draw_Sphere_Data.Count + Draw_Text_Data.Count + Draw_WireArc_Data.Count + Draw_SolidArc_Data.Count +
                Draw_WireDisc_Data.Count + Draw_Point_Group.Count + Draw_Point_Group_Sort.Count;
            if (_drawConst > maxDrawConst)
            {
                EngineDebug.LogWarning($"编辑器的图形绘制数量超过上限 [<color=#ffcc00>{maxDrawConst}</color>], 清空所有绘制图形");
                Init();
            }

            bool _isPlaying = Application.isPlaying;
            if (_isPlaying != isPlaying)
            {
                Init();
                isPlaying = _isPlaying;
            }

#if UNITY_EDITOR

            for (int i = 0; i < Draw_Box_Data.Count; i++)
            {
                Draw_Box_Data[i] = Draw_Box_Data[i].SubLife(deltaTime);
                Draw_BoxData VARIABLE = Draw_Box_Data[i];

                Handles.color = VARIABLE.color;

                Vector3 vector3 = VARIABLE.scale * 0.5f;
                //vector3 =  * vector3;
                Vector3[] vector3Array = new Vector3[10]
                {
                    VARIABLE.position + VARIABLE.rotation * new Vector3(-vector3.x, -vector3.y, -vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(-vector3.x, vector3.y, -vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(vector3.x, vector3.y, -vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(vector3.x, -vector3.y, -vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(-vector3.x, -vector3.y, -vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(-vector3.x, -vector3.y, vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(-vector3.x, vector3.y, vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(vector3.x, vector3.y, vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(vector3.x, -vector3.y, vector3.z),
                    VARIABLE.position + VARIABLE.rotation * new Vector3(-vector3.x, -vector3.y, vector3.z)
                };
                Handles.DrawPolyLine(vector3Array);
                Handles.DrawLine(vector3Array[1], vector3Array[6]);
                Handles.DrawLine(vector3Array[2], vector3Array[7]);
                Handles.DrawLine(vector3Array[3], vector3Array[8]);

                if (VARIABLE.life < 0)
                {
                    Draw_Box_Data.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Draw_Sphere_Data.Count; i++)
            {
                Draw_Sphere_Data[i] = Draw_Sphere_Data[i].SubLife(deltaTime);
                Draw_SphereData VARIABLE = Draw_Sphere_Data[i];

                Handles.color = VARIABLE.color;
                Handles.DrawWireDisc(VARIABLE.position, VARIABLE.rotation * Vector3.forward, VARIABLE.radius);
                Handles.DrawWireDisc(VARIABLE.position, VARIABLE.rotation * Vector3.right, VARIABLE.radius);
                Handles.DrawWireDisc(VARIABLE.position, VARIABLE.rotation * Vector3.up, VARIABLE.radius);

                if (VARIABLE.life < 0)
                {
                    Draw_Sphere_Data.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Draw_Line_Data.Count; i++)
            {
                Draw_Line_Data[i] = Draw_Line_Data[i].SubLife(deltaTime);
                Draw_LineData VARIABLE = Draw_Line_Data[i];

                Handles.color = VARIABLE.color;
                Handles.DrawLine(VARIABLE.start, VARIABLE.end);

                if (VARIABLE.life < 0)
                {
                    Draw_Line_Data.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Draw_Capsule_Data.Count; i++)
            {
                Draw_Capsule_Data[i] = Draw_Capsule_Data[i].SubLife(deltaTime);
                Draw_CapsuleData VARIABLE = Draw_Capsule_Data[i];

                Handles.color = VARIABLE.color;
                Vector3 startPos = VARIABLE.startPos;
                Vector3 endPos = VARIABLE.endPos;
                float radius = VARIABLE.radius;

                if (endPos == startPos)
                {
                    Quaternion _rot = Quaternion.Euler(0, 0, 0);
                    Handles.DrawWireDisc(startPos, _rot * Vector3.up, radius);
                    Handles.DrawWireDisc(startPos, _rot * Vector3.forward, radius);
                    Handles.DrawWireDisc(startPos, _rot * Vector3.right, radius);
                }//目标和起点重叠时
                else
                {
                    Quaternion _rot = Quaternion.LookRotation(endPos - startPos);//, AxisY
                    Handles.DrawWireDisc(startPos, _rot * Vector3.forward, radius);
                    Handles.DrawWireDisc(endPos, _rot * Vector3.forward, radius);

                    Vector3 _up = _rot * Vector3.up * radius;
                    Vector3 _dwon = _rot * Vector3.down * radius;
                    Vector3 _left = _rot * Vector3.left * radius;
                    Vector3 _right = _rot * Vector3.right * radius;
                    Handles.DrawLine(startPos + _up, endPos + _up);
                    Handles.DrawLine(startPos + _dwon, endPos + _dwon);
                    Handles.DrawLine(startPos + _left, endPos + _left);
                    Handles.DrawLine(startPos + _right, endPos + _right);

                    Handles.DrawWireArc(startPos, _rot * Vector3.right, _rot * Vector3.down, 180, radius);
                    Handles.DrawWireArc(startPos, _rot * Vector3.down, _rot * Vector3.left, 180, radius);
                    Handles.DrawWireArc(endPos, _rot * Vector3.right, _rot * Vector3.down, -180, radius);
                    Handles.DrawWireArc(endPos, _rot * Vector3.down, _rot * Vector3.left, -180, radius);
                }//绘制胶囊

                if (VARIABLE.life < 0)
                {
                    Draw_Capsule_Data.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Draw_WireArc_Data.Count; i++)
            {
                Draw_WireArc_Data[i] = Draw_WireArc_Data[i].SubLife(deltaTime);
                Draw_WireArcData VARIABLE = Draw_WireArc_Data[i];

                Handles.color = VARIABLE.color;
                Handles.DrawWireArc(VARIABLE.position, VARIABLE.Axis_Y, VARIABLE.Axis_Z, VARIABLE.angle,
                    VARIABLE.radius);
                if (VARIABLE.life < 0)
                {
                    Draw_WireArc_Data.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Draw_SolidArc_Data.Count; i++)
            {
                Draw_SolidArc_Data[i] = Draw_SolidArc_Data[i].SubLife(deltaTime);
                Draw_SolidArcData VARIABLE = Draw_SolidArc_Data[i];

                Handles.color = VARIABLE.color;
                Handles.DrawSolidArc(VARIABLE.position, VARIABLE.Axis_Y, VARIABLE.Axis_Z, VARIABLE.angle,
                    VARIABLE.radius);
                if (VARIABLE.life < 0)
                {
                    Draw_SolidArc_Data.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Draw_WireDisc_Data.Count; i++)
            {
                Draw_WireDisc_Data[i] = Draw_WireDisc_Data[i].SubLife(deltaTime);
                Draw_WireDiscData VARIABLE = Draw_WireDisc_Data[i];

                Handles.color = VARIABLE.color;
                Handles.DrawWireDisc(VARIABLE.position, VARIABLE.Axis_Y, VARIABLE.radius);
                if (VARIABLE.life < 0)
                {
                    Draw_WireDisc_Data.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < Draw_Point_Group.Count; i++)
            {
                Draw_PointTransData draw_Point = Draw_Point_Group[i];
                Handles.color = draw_Point.color;
                if (!string.IsNullOrEmpty(draw_Point.name))
                    Handles.Label(draw_Point.pos, new GUIContent(draw_Point.name));
                Vector3 startPos = draw_Point.pos;
                draw_Point.pos = Handles.PositionHandle(draw_Point.pos, draw_Point.rot);
            }

            for (int i = 0; i < Draw_Text_Data.Count; i++)
            {
                Draw_Text draw_Point = Draw_Text_Data[i];

                if (!string.IsNullOrEmpty(draw_Point.mainText))
                {
                    _fontStyle.normal.textColor = draw_Point.color;
                    _fontStyle.fontSize = draw_Point.radius;
                    Handles.Label(draw_Point.position, new GUIContent(draw_Point.mainText), _fontStyle);
                }

                Draw_Text_Data[i] = Draw_Text_Data[i].SubLife(deltaTime);
                if (draw_Point.life < 0)
                {
                    Draw_Text_Data.RemoveAt(i);
                    i--;
                }
            }
#endif
        }
    }
}
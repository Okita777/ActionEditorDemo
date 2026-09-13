using UnityEditor;
using UnityEngine;

namespace AsiTimeLine.Editor
{
    public static class HelpWindowDrawUtility
    {
        private static float m_AnimTime;
        private static double m_LastEditorTime;

        public static float AnimTime => m_AnimTime;

        public static void UpdateAnimTime()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (m_LastEditorTime == 0) m_LastEditorTime = currentTime;
            float delta = (float)(currentTime - m_LastEditorTime);
            m_LastEditorTime = currentTime;
            m_AnimTime += delta;
            if (m_AnimTime > 100f) m_AnimTime -= 100f;
        }

        public static void DrawTitle(string iconName, string displayName, string className, Color accentColor)
        {
            Rect headerRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.15f));

            GUILayout.Space(8);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(12);
                GUIContent icon = EditorGUIUtility.IconContent(iconName);
                GUILayout.Label(icon, GUILayout.Width(36), GUILayout.Height(36));
                GUILayout.Space(6);

                GUILayout.Label(displayName, new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = accentColor }
                }, GUILayout.Height(36));

                GUILayout.FlexibleSpace();

                GUILayout.Label(className, new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 10,
                    normal = { textColor = new Color(0.45f, 0.45f, 0.45f) }
                }, GUILayout.Height(36));
                GUILayout.Space(12);
            }

            float pulse = 0.3f + Mathf.PingPong(m_AnimTime * 0.6f, 0.7f);
            Rect barRect = GUILayoutUtility.GetRect(0, 3, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, new Color(accentColor.r, accentColor.g, accentColor.b, pulse));

            GUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        public static bool DrawSectionHeader(string title, bool foldState, Color accentColor)
        {
            GUILayout.Space(4);
            Rect headerRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(headerRect, new Color(accentColor.r * 0.25f, accentColor.g * 0.25f, accentColor.b * 0.25f, 0.6f));
            EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, 4, headerRect.height), accentColor);

            string arrow = foldState ? "▼" : "►";
            if (GUILayout.Button(arrow + title, new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(8, 8, 6, 6),
                normal = { textColor = accentColor },
                hover = { textColor = Color.white }
            }))
            {
                foldState = !foldState;
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);

            return foldState;
        }

        public static void DrawOverview(ref bool foldState, string content)
        {
            GUILayout.Space(4);
            foldState = DrawSectionHeader("  概  述", foldState, new Color(0.2f, 0.5f, 0.8f));
            if (!foldState) return;

            EditorGUILayout.HelpBox(content, MessageType.Info);
            GUILayout.Space(6);
        }

        public static void DrawPropertyItem(string title, string description, string tip, string currentValue, Color valueColor)
        {
            EditorGUILayout.BeginVertical("box");

            GUILayout.Label(title, new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                richText = true,
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
            });

            GUILayout.Label(description, new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 11,
                richText = true,
                padding = new RectOffset(8, 8, 2, 2),
                normal = { textColor = new Color(0.78f, 0.78f, 0.78f) }
            });

            if (!string.IsNullOrEmpty(tip))
            {
                GUILayout.Space(2);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(8);
                    GUIContent tipIcon = EditorGUIUtility.IconContent("console.infoicon.sml");
                    GUILayout.Label(tipIcon, GUILayout.Width(16), GUILayout.Height(16));
                    GUILayout.Label(tip, new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                    {
                        fontSize = 10,
                        richText = true,
                        normal = { textColor = new Color(0.5f, 0.75f, 0.5f) }
                    });
                }
            }

            GUILayout.Space(3);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                Rect statusRect = GUILayoutUtility.GetRect(8, 8, GUILayout.Width(8));
                statusRect.y += 3;
                EditorGUI.DrawRect(statusRect, valueColor);
                GUILayout.Space(4);
                GUILayout.Label(currentValue, new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 10,
                    normal = { textColor = valueColor }
                });
            }
            GUILayout.Space(4);

            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }

        public static void DrawFlowPhase(string phaseName, Color color, string[] steps)
        {
            Rect phaseRect = EditorGUILayout.BeginVertical();

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                using (new GUILayout.VerticalScope("box"))
                {
                    GUILayout.Label(phaseName, new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        normal = { textColor = color }
                    });

                    foreach (string step in steps)
                    {
                        GUILayout.Label(step, new GUIStyle(EditorStyles.label)
                        {
                            fontSize = 11,
                            richText = true,
                            padding = new RectOffset(8, 4, 1, 1),
                            normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
                        });
                    }

                    GUILayout.Space(2);
                }
            }

            float sideBarHeight = phaseRect.height > 0 ? phaseRect.height : 60;
            EditorGUI.DrawRect(new Rect(phaseRect.x + 4, phaseRect.y, 3, sideBarHeight), color);

            EditorGUILayout.EndVertical();
        }

        public static void DrawFlowArrow()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                float arrowAlpha = 0.4f + Mathf.PingPong(m_AnimTime * 1.2f, 0.6f);
                GUILayout.Label("▼", new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.5f, 0.8f, 1f, arrowAlpha) }
                });
                GUILayout.FlexibleSpace();
            }
        }

        public static void DrawUseCaseItem(string iconName, string title, string description)
        {
            using (new GUILayout.HorizontalScope("box"))
            {
                GUILayout.Space(4);
                GUIContent icon = EditorGUIUtility.IconContent(iconName);
                if (icon != null && icon.image != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24));
                }
                GUILayout.Space(4);

                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label(title, new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        normal = { textColor = new Color(0.9f, 0.75f, 1f) }
                    });
                    GUILayout.Label(description, new GUIStyle(EditorStyles.wordWrappedLabel)
                    {
                        fontSize = 11,
                        richText = true,
                        padding = new RectOffset(0, 4, 0, 2),
                        normal = { textColor = new Color(0.72f, 0.72f, 0.72f) }
                    });
                }
            }
            GUILayout.Space(1);
        }

        public static void DrawNoteItem(MessageType type, string message)
        {
            EditorGUILayout.HelpBox(message, type);
            GUILayout.Space(1);
        }
    }
}

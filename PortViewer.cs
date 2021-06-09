using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public partial class PortViewer : EditorWindow
{
    internal static class ContentProperties
    {
        public readonly static GUIContent Title = new GUIContent("Sensor Manager", "Sensor Manager window.");
    }

#if UNITY_EDITOR_OSX
    const string MAC_PORT_PREFIX = "/dev/tty.";
#endif

    private readonly static Vector2 MinWindowSize = new Vector2(300, 250);

    private List<Sensor> sensors = new List<Sensor>();
    private List<bool> foldouts = new List<bool>();
    private string[] ports;
    private Vector3 scrollPos;

    /// <summary>
    /// Open Sensor Manager window.
    /// </summary>
    [MenuItem("DT/Tools/Sensor Manager", false, 0)]
    public static void Open()
    {
        PortViewer portViewerWindow = GetWindow<PortViewer>();
        portViewerWindow.titleContent = ContentProperties.Title;
        portViewerWindow.minSize = MinWindowSize;
        portViewerWindow.ShowUtility();
    }

    /// <summary>
    /// This function is called when the object becomes enabled and active.
    /// </summary>
    protected virtual void OnEnable()
    {
        EditorApplication.update += Update;
        FillFoldoutList();
    }

    /// <summary>
    /// This function is called when the behaviour becomes disabled or inactive.
    /// </summary>
    protected virtual void OnDisable()
    {
        EditorApplication.update -= Update;
        CloseAllLocalSensors();
    }

    /// <summary>
    /// Update is called every frame, if the Sensor Manager is enabled.
    /// </summary>
    protected virtual void Update()
    {
        for (int i = 0, length = sensors.Count; i < length; i++)
        {
            Sensor sensor = sensors[i];
            if (!sensor.serialPort.IsOpen ||
                sensor.transform == null)
            {
                continue;
            }

            string data = sensor.serialPort.ReadLine();
            string[] values = data.Split(',');
            if (values != null && values.Length == 4)
            {
                float x = float.Parse(values[0]);
                float y = float.Parse(values[1]);
                float z = float.Parse(values[2]);
                float w = float.Parse(values[3]);
                sensor.transform.rotation = Quaternion.Lerp(sensor.transform.rotation, new Quaternion(x, y, z, w), Time.deltaTime * sensor.sensitivity);
            }
            // else if (values != null && values.Length == 3)
            // {
            //     float x = float.Parse(values[0]);
            //     float y = float.Parse(values[1]);
            //     float z = float.Parse(values[2]);
            //     sensor.transform.localRotation = Quaternion.Slerp(sensor.transform.localRotation, Quaternion.Euler(-z, x, -y), Time.deltaTime * sensor.time);
            // }
            else
            {
                sensor.serialPort.WriteLine("");
            }
        }
    }

    /// <summary>
    /// OnGUI is called for rendering and handling GUI events.
    /// This function can be called multiple times per frame (one call per event).
    /// </summary>
    protected virtual void OnGUI()
    {
        DrawHeaderGUI();
        DrawSensorEditorGUI();
        DrawButtons();
    }

    protected virtual void DrawHeaderGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label(ContentProperties.Title, HeaderLabel());
        GUILayout.Space(10);
        HorizontalLine();
    }

    protected virtual void DrawSensorEditorGUI()
    {
        if (sensors.Count == 0)
        {
            EditorGUILayout.HelpBox("Add new sensor...", MessageType.Info);
            return;
        }

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        for (int i = 0, length = sensors.Count; i < length; i++)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(7);
            Rect animationRemoveButtonRect = GUILayoutUtility.GetRect(0, 0);
            animationRemoveButtonRect.x = animationRemoveButtonRect.width - 15;
            animationRemoveButtonRect.y += 1;
            animationRemoveButtonRect.width = 16.5f;
            animationRemoveButtonRect.height = 16.5f;
            if (GUI.Button(animationRemoveButtonRect, GUIContent.none, GUI.skin.GetStyle("OL Minus")))
            {
                sensors.RemoveAt(i);
                foldouts.RemoveAt(i);
                break;
            }
            bool foldout = foldouts[i];
            foldout = EditorGUILayout.Foldout(foldout, string.Format("Sensor {0}", i + 1), true);
            if (foldout)
            {
                EditorGUI.indentLevel++;
                Sensor sensor = sensors[i];
                EditorGUILayout.Popup("Type", 0, new string[2] { "Local", "Wireless" });
                GUILayout.BeginHorizontal();
                GUILayout.Space(16);
                GUILayout.Label("Local Port", GUILayout.Width(79));
                GUILayout.Space(EditorGUIUtility.fieldWidth);
                string actualPortName = sensor.serialPort.PortName;
#if UNITY_EDITOR_OSX
                actualPortName = actualPortName.Replace(MAC_PORT_PREFIX, "");
#endif
                if (GUILayout.Button(actualPortName, EditorStyles.miniButton))
                {
                    GenericMenu genericMenu = new GenericMenu();
                    string[] ports = SerialPort.GetPortNames();
                    for (int j = 0; j < ports.Length; j++)
                    {
                        string port = ports[j];

#if UNITY_EDITOR_OSX
                        if (!port.Contains(MAC_PORT_PREFIX))
                        {
                            continue;
                        }
#endif

#if UNITY_EDITOR_WIN
                        genericMenu.AddItem(new GUIContent(port), false, () => { sensor.serialPort.PortName = port; });
#elif UNITY_EDITOR_OSX
                        genericMenu.AddItem(new GUIContent(port.Replace(MAC_PORT_PREFIX, "")), false, () => { sensor.serialPort.PortName = port; });
#endif

                    }
                    genericMenu.ShowAsContext();
                }
                GUILayout.EndHorizontal();

                sensor.transform = (Transform)EditorGUILayout.ObjectField("Transform", sensor.transform, typeof(Transform), true);
                sensor.sensitivity = EditorGUILayout.Slider("Sensitivity", sensor.sensitivity, 0, 20);

                GUILayout.Space(3);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(!sensor.serialPort.IsOpen ? "Start" : "Stop", GUILayout.Width(70)))
                {
                    if (!sensor.serialPort.IsOpen)
                    {
                        sensor.serialPort.Open();
                    }
                    else
                    {
                        sensor.serialPort.Close();
                    }
                }
                GUILayout.EndHorizontal();

                sensors[i] = sensor;
                EditorGUI.indentLevel--;
            }
            foldouts[i] = foldout;
            GUILayout.Space(7);
            GUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
    }

    protected virtual void DrawButtons()
    {
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add Sensor", "ButtonLeft", GUILayout.Width(120)))
        {
            sensors.Add(new Sensor(new SerialPort(), null, 5));
            foldouts.Add(false);
        }
        if (GUILayout.Button("Remove All Sensors", "ButtonRight", GUILayout.Width(120)))
        {
            sensors.Clear();
            foldouts.Clear();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.EndVertical();
    }

    /// <summary>
    /// Create new reorderable list for manage sensors.
    /// </summary>
    protected virtual ReorderableList CreateSensorList()
    {
        return new ReorderableList(sensors, typeof(Sensor), true, true, true, true)
        {
            drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, new GUIContent("Sensors"));
                },

                drawElementCallback = (rect, index, isFocused, isActive) =>
                {
                    Sensor sensor = sensors[index];
                    string actualPortName = sensor.serialPort.PortName;

#if UNITY_EDITOR_OSX
                    actualPortName = actualPortName.Replace(MAC_PORT_PREFIX, "");
#endif
                    EditorGUI.LabelField(new Rect(rect.x, rect.y + 1.5f, 60, EditorGUIUtility.singleLineHeight), "Sensor: " + (index + 1));
                    if (GUI.Button(new Rect(rect.x + 70, rect.y + 1.5f, 150, EditorGUIUtility.singleLineHeight), actualPortName, EditorStyles.miniButton))
                    {
                        GenericMenu genericMenu = new GenericMenu();
                        string[] ports = SerialPort.GetPortNames();
                        for (int i = 0; i < ports.Length; i++)
                        {
                            string port = ports[i];

#if UNITY_EDITOR_OSX
                            if (!port.Contains(MAC_PORT_PREFIX))
                            {
                                continue;
                            }
#endif

#if UNITY_EDITOR_WIN
                            genericMenu.AddItem(new GUIContent(port), false, () => { sensor.serialPort.PortName = port; });
#elif UNITY_EDITOR_OSX
                            genericMenu.AddItem(new GUIContent(port.Replace(MAC_PORT_PREFIX, "")), false, () => { sensor.serialPort.PortName = port; });
#endif

                        }
                        genericMenu.ShowAsContext();
                    }

                    sensor.transform = (Transform)EditorGUI.ObjectField(new Rect(rect.x + 235, rect.y + 1.5f, 150, EditorGUIUtility.singleLineHeight), GUIContent.none, sensor.transform, typeof(Transform), true);
                    if (GUI.Button(new Rect(rect.width - 30, rect.y + 1.5f, 50, EditorGUIUtility.singleLineHeight), !sensor.serialPort.IsOpen ? "Start" : "Stop"))
                    {
                        if (!sensor.serialPort.IsOpen)
                        {
                            sensor.serialPort.Open();
                        }
                        else
                        {
                            sensor.serialPort.Close();
                        }
                    }
                    sensors[index] = sensor;
                },

                onAddCallback = (list) =>
                {
                    sensors.Add(new Sensor(new SerialPort(), null, 5));
                }
        };
    }

    protected virtual string[] GetAvailablePorts()
    {

        string[] ports = SerialPort.GetPortNames();
        List<string> availablePort = new List<string>();
        for (int i = 0; i < ports.Length; i++)
        {
            string port = ports[i];

#if UNITY_EDITOR_WIN
            availablePort.Add(port);
#elif UNITY_EDITOR_OSX
            if (port.Contains(MAC_PORT_PREFIX))
            {
                port = port.Replace(MAC_PORT_PREFIX, "");
                availablePort.Add(port);
            }
#endif

        }
        return availablePort.ToArray();
    }

    protected virtual void CloseAllLocalSensors()
    {
        for (int i = 0, length = sensors.Count; i < length; i++)
        {
            Sensor sensor = sensors[i];
            sensor.serialPort.Close();
        }
    }

    protected void FillFoldoutList()
    {
        for (int i = 0, length = sensors.Count; i < length; i++)
        {
            foldouts.Add(false);
        }
    }

    /// <summary>
    /// Header label GUIStyle.
    /// </summary>
    public static GUIStyle HeaderLabel()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color32(50, 50, 50, 255);
        return style;
    }

    /// <summary>
    /// Draw dividing line.
    /// </summary>
    public static void HorizontalLine(float separate = 1.0f)
    {
        Rect rect = GUILayoutUtility.GetRect(0, separate);
        rect.x -= 3;
        rect.width += 7;
        GUI.Label(rect, GUIContent.none, GUI.skin.GetStyle("IN Title"));
    }
}
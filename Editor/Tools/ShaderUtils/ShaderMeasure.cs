using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using UnityEditor.Rendering;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Object = System.Object;
using Debug = UnityEngine.Debug;

namespace AssetBundleBrowser.Utils
{
    public sealed class ShaderMeasure : EditorWindow
    {
        #region [Fields]
        private Shader _shader_src;

        private Vector2 _ScrollPos;
        private string _CLIVersion = string.Empty;
        private Dictionary<string, KeyValuePair<string, string>> _MeasureInfo;
        #endregion

        #region [Menu]
        [MenuItem("Window/AssetBundle Browser/ShaderMeasure", priority = 3051)]
        public static void GUIDHelperWindow()
        {
            EditorWindow.GetWindow<ShaderMeasure>();
        }
        #endregion

        #region [GUI]
        private void OnEnable()
        {
            _CLIVersion = CLIVersion();
            _ScrollPos = Vector2.zero;
        }
        private void OnGUI()
        {
            EditorGUILayout.Space();

            var tempSupport = !string.IsNullOrEmpty(_CLIVersion);
            EditorGUILayout.LabelField("Compiler Version", _CLIVersion);
            using (new EditorGUI.DisabledScope(!tempSupport))
            {
                var tempShader = EditorGUILayout.ObjectField("Shader", _shader_src, typeof(Shader), true) as Shader;
                if (_shader_src != tempShader)
                {
                    _MeasureInfo.Clear();
                    _shader_src = tempShader;
                }
                using (new EditorGUI.DisabledScope(!_shader_src))
                {
                    if (GUILayout.Button("Measure"))
                    {
                        var tempPlatMask = 1 << (int)ShaderCompilerPlatform.GLES3x;
                        var tempESCodePath = OpenCompiledShader(_shader_src, tempPlatMask, true);
                        ScanerFields(tempESCodePath, out _MeasureInfo);
                    }
                }
            }
            if (!tempSupport)
            {
                EditorGUILayout.HelpBox("Malioc env not support!", MessageType.Error);
            }

            if (_MeasureInfo != null)
            {
                using (var tempSV = new EditorGUILayout.ScrollViewScope(_ScrollPos))
                {
                    _ScrollPos = tempSV.scrollPosition;

                    foreach (var tempKvp in _MeasureInfo)
                    {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            EditorGUILayout.LabelField("Global Keywords", tempKvp.Key);
                            EditorGUILayout.LabelField("Vertex");
                            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                            {
                                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextArea(tempKvp.Value.Key);
                            }
                            EditorGUILayout.LabelField("Fragment");
                            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                            {
                                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.TextArea(tempKvp.Value.Value);
                            }
                        }
                        EditorGUILayout.Space();
                    }
                }

            }

            MiscUtils.GUISignWithTimestamp(position, "2020/10/19");
        }
        #endregion

        #region [API]

        #region [Fields]
        private static Type _ShaderUtilType;
        private static Type ShaderUtilType
        {
            get
            {
                if (null == _ShaderUtilType)
                {
                    _ShaderUtilType = Assembly.Load("UnityEditor").GetType("UnityEditor.ShaderUtil");
                }
                return _ShaderUtilType;
            }
        }

        private static Object _ShaderUtilInstance;
        private static Object ShaderUtil
        {
            get
            {
                if (null == _ShaderUtilInstance)
                {
                    _ShaderUtilInstance = ShaderUtilType.GetConstructor(Type.EmptyTypes).Invoke(new Object[] { });
                }
                return _ShaderUtilInstance;
            }
        }

        #endregion

        public static string OpenCompiledShader(Shader varShader, int varExternPlatformsMask, bool varIncludeAllVariants)
        {
            var OpenShaderCombinations = ShaderUtilType.GetMethod("OpenCompiledShader", BindingFlags.NonPublic | BindingFlags.Static);
            OpenShaderCombinations.Invoke(ShaderUtil, new Object[] { varShader, 3, varExternPlatformsMask, varIncludeAllVariants });
            return Path.Combine(Application.dataPath, "../Temp", $"Compiled-{varShader.name.Replace("/", "-")}.shader");
        }
        #endregion

        #region [Business]
        private string CLIVersion()
        {
            string tempOutput = string.Empty, tempError = string.Empty;

            if (ToolCLICMD(out tempOutput, out tempError, "--version") == 0)
            {
                return Regex.Match(tempOutput, @"v\d+\.\d+\.\d+\s\(Build\s\d+\)").Value;
            }
            return string.Empty;
        }
        private string CLIMeasure(string varFilePath)
        {
            string tempOutput = string.Empty, tempError = string.Empty;

            if (ToolCLICMD(out tempOutput, out tempError, varFilePath) == 0)
            {
                return tempOutput;
            }
            return string.Empty;
        }

        private int ToolCLICMD(out string varOutput, out string varError, params string[] varParams)
        {
            using (var tempProcess = new Process())
            {
                tempProcess.StartInfo.FileName = "malioc";
                tempProcess.StartInfo.Arguments = string.Join(" ", varParams);

                tempProcess.StartInfo.UseShellExecute = false;
                tempProcess.StartInfo.CreateNoWindow = true;
                tempProcess.StartInfo.RedirectStandardOutput = true;
                tempProcess.StartInfo.RedirectStandardError = true;

                tempProcess.Start();

                varOutput = tempProcess.StandardOutput.ReadToEnd();
                varError = tempProcess.StandardError.ReadToEnd();
                tempProcess.WaitForExit();

                return tempProcess.ExitCode;
            }
        }

        private bool ScanerFields(string varFilePath, out Dictionary<string, KeyValuePair<string, string>> varRets)
        {
            varRets = new Dictionary<string, KeyValuePair<string, string>>();

            var tempCompilerCode = File.ReadAllText(varFilePath);
            var tempMatchRet = Regex.Matches(tempCompilerCode, @"Global Keywords:\s?(.+)\sLocal[\s\S]+?#ifdef VERTEX\s([\s\S]+?)#endif\s+#ifdef\s+FRAGMENT\s([\s\S]+?)#endif\s+--\sHardware");

            if (tempMatchRet.Count == 0) return false;

            for (int iM = 0; iM < tempMatchRet.Count; ++iM)
            {
                var tempMatch = tempMatchRet[iM];
                var tempKeyWords = tempMatch.Groups[1].Value;
                var tempVertCode = tempMatch.Groups[2].Value;
                var tempFragCode = tempMatch.Groups[3].Value;

                var tempFileName = Path.GetFileNameWithoutExtension(varFilePath) + "-" + string.Join("-", tempKeyWords.Trim().Split(' '));
                ValidFileName(ref tempFileName);
                tempFileName = Path.Combine(Path.GetDirectoryName(varFilePath), tempFileName);

                File.WriteAllText(tempFileName + ".vert", tempVertCode);
                File.WriteAllText(tempFileName + ".frag", tempFragCode);
                varRets.Add(tempKeyWords, new KeyValuePair<string, string>(CLIMeasure(tempFileName + ".vert"), CLIMeasure(tempFileName + ".frag")));
            }

            return true;
        }

        private void ValidFileName(ref string varRawName)
        {
            string tempPatten = ":\\/*?\"<>|";
            var scope = new StringBuilder();
            for (int iT = 0; iT < varRawName.Length; ++iT)
            {
                var tempChar = varRawName[iT];
                bool tempIsillegal = false;
                for (int iP = 0; iP < tempPatten.Length; ++iP)
                {
                    if (tempChar == tempPatten[iP])
                    {
                        tempIsillegal = true;
                        break;
                    }
                }

                if (tempIsillegal) continue;
                scope.Append(tempChar);
            }
            varRawName = scope.ToString();
        }
        #endregion
    }
}
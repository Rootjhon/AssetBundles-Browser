using System;
using System.IO;
using UnityEngine;
using System.Collections;
using UnityEditor.Rendering;
using System.Collections.Generic;
using System.Globalization;

using UDebug = UnityEngine.Debug;

namespace AssetBundleBrowser.Utils
{
    public class VertexProgram
    {
        #region [delegate]
        private delegate int VertexProgramDisFun(string[] varContent, int varIndex, out ShaderCompilerPlatform varCompiledPlatform, out HashSet<string> varVertxtChannel);
        #endregion

        #region [Fields]
        private static Dictionary<string, VertexProgramDisFun> _DisCompiledFun = new Dictionary<string, VertexProgramDisFun>() 
        {
            { "d3d11", DisShdaerVertexChannel_D3D },
            { "gles", DisShdaerVertexChannel_GLES },
            { "gles3", DisShdaerVertexChannel_GLES3 },
            { "metal", DisShdaerVertexChannel_Metal },
            { "glcore", DisShdaerVertexChannel_GLCore },
            { "vulkan", DisShdaerVertexChannel_Vulkan },
        };
        private const string VertexTagStr = "-- Vertex shader for ";

        private Dictionary<ShaderCompilerPlatform, HashSet<string>> _DisAssembly = new Dictionary<ShaderCompilerPlatform, HashSet<string>>();
        #endregion

        #region [Construct]
        public VertexProgram(string varCompiledShaderPath)
        {
            UDebug.Assert(File.Exists(varCompiledShaderPath));

            var tempContent = File.ReadAllLines(varCompiledShaderPath);
            for (int i = 0; i < tempContent.Length; ++i)
            {
                var tempInfo = tempContent[i];
                if (!tempInfo.StartsWith(VertexTagStr, false, CultureInfo.InvariantCulture)) continue;

                var tempPlatform = tempInfo.Substring(VertexTagStr.Length, tempInfo.Length - VertexTagStr.Length - 1).Replace("\"", string.Empty);
                if (!_DisCompiledFun.TryGetValue(tempPlatform, out var tempDisFun))
                {
                    UDebug.LogWarningFormat("[VertexProgram] not support :[{0}]", tempPlatform);
                }

                i = tempDisFun(tempContent, i, out var tempCompiledPlatform, out var tempChannel);
                //_DisAssembly.Add();
            }
        }
        #endregion

        #region [API]
        [UnityEditor.MenuItem("AAAAAA/SSSSS")]
        public static void TestFun()
        {

        }

        #endregion

        #region [Business]
        private static int DisShdaerVertexChannel_D3D(string[] varContent, int varIndex, out ShaderCompilerPlatform varCompiledPlatform, out HashSet<string> varVertxtChannel)
        {
            varCompiledPlatform = ShaderCompilerPlatform.D3D;
            varVertxtChannel = new HashSet<string>();
            return varIndex;
        }
        private static int DisShdaerVertexChannel_GLES(string[] varContent, int varIndex, out ShaderCompilerPlatform varCompiledPlatform, out HashSet<string> varVertxtChannel)
        {
            varCompiledPlatform = ShaderCompilerPlatform.GLES20;
            varVertxtChannel = new HashSet<string>();
            return varIndex;
        }
        private static int DisShdaerVertexChannel_GLES3(string[] varContent, int varIndex, out ShaderCompilerPlatform varCompiledPlatform, out HashSet<string> varVertxtChannel)
        {
            varCompiledPlatform = ShaderCompilerPlatform.GLES3x;
            varVertxtChannel = new HashSet<string>();
            return varIndex;
        }
        private static int DisShdaerVertexChannel_GLCore(string[] varContent, int varIndex, out ShaderCompilerPlatform varCompiledPlatform, out HashSet<string> varVertxtChannel)
        {
            varCompiledPlatform = ShaderCompilerPlatform.OpenGLCore;
            varVertxtChannel = new HashSet<string>();
            return varIndex;
        }
        private static int DisShdaerVertexChannel_Vulkan(string[] varContent, int varIndex, out ShaderCompilerPlatform varCompiledPlatform, out HashSet<string> varVertxtChannel)
        {
            varCompiledPlatform = ShaderCompilerPlatform.Vulkan;
            varVertxtChannel = new HashSet<string>();
            return varIndex;
        }
        private static int DisShdaerVertexChannel_Metal(string[] varContent, int varIndex, out ShaderCompilerPlatform varCompiledPlatform, out HashSet<string> varVertxtChannel)
        {
            varCompiledPlatform = ShaderCompilerPlatform.Metal;
            varVertxtChannel = new HashSet<string>();
            return varIndex;
        }
        #endregion
    }
}
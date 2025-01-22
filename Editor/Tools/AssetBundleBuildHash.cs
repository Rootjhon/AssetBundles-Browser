using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngineInternal;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

using UObject = UnityEngine.Object;
using UScene = UnityEngine.SceneManagement.Scene;

namespace AssetBundleBrowser.Utils
{
    public class AssetBundleBuildHash
    {
        #region [Struct]
        public struct BuildUsageTagGlobal
        {
            public int m_LightmapModesUsed; //bitmask of LightmapsMode
            public int m_LegacyLightmapModesUsed; // bitmask of LightmapsModeLegacy
            public int m_DynamicLightmapsUsed;
            public int m_FogModesUsed; // bitmask of FogMode
            public bool m_ForceInstancingStrip;
            public bool m_ForceInstancingKeep;
            public bool m_ShadowMasksUsed;
            public bool m_SubtractiveUsed;
        }
        public struct BuildHash
        {
            public BuildTarget BuildPlatform;
            public int BuildSubPlatform;
            public GraphicsDeviceType[] GraphicsAPIs;
            public int[] ShaderCompilerPlatforms;
            public bool stripUnusedMeshComponents;
            public BuildAssetBundleOptions AssetBundleBuildOptions;
            public BuildUsageTagGlobal[] BuildUsageTagGlobals;
        }
        #endregion

        #region [Enum]
        private enum ShaderStrippingMode
        {
            kShaderStrippingAutomatic = 0,
            kShaderStrippingCustom,
        }
        private enum InstancingStrippingMode
        {
            kInstancingStrippingStripUnused = 0,
            kInstancingStrippingStripAll,
            kInstancingStrippingKeepAll,
        }
        private enum LightmapsModeLegacy
        {
            kSingleLightmapsMode = 0,
            kDualLightmapsMode = 1,
            kDirectionalLightmapsMode = 2,
            kRNMMode = 3,
            kLightmapsModeLegacyCount // keep this last
        }
        private enum LightmapsMode
        {
            kInvalidLightmapsMode = -1,
            kNonDirectionalLightmapsMode = 0,
            kCombinedDirectionalLightmapsMode = 1,
            kLightmapsModeCount // keep this last
        }
        private enum FogMode
        {
            kFogUnknown = -1,
            kFogDisabled = 0,
            kFogLinear,
            kFogExp,
            kFogExp2,
            kFogModeCount // keep this last!
        }
        #endregion

        #region [Business]
        private static int GetActiveSubtargetFor(BuildTarget varTarget)
        {
            if (varTarget == BuildTarget.Android)
            {
                var tex = (int)EditorUserBuildSettings.androidBuildSubtarget;
                var fallback = (int)EditorUserBuildSettings.androidETC2Fallback;
                var graphicsAPIMask = GetGraphicsAPIsMaskForPlatform(BuildTarget.Android);
                return (tex & 0x3F) | ((fallback & 0x3) << 6) | ((graphicsAPIMask & 0xFFFFFF) << 8);
            }
            if (varTarget == BuildTarget.iOS || varTarget == BuildTarget.tvOS)
            {
                var tex = (int)MobileTextureSubtarget.PVRTC;
                var graphicsAPIMask = GetGraphicsAPIsMaskForPlatform(varTarget);
                return (tex & 0x3F) | ((graphicsAPIMask & 0xFFFFFF) << 8);
            }
            if (varTarget == BuildTarget.WebGL)
            {
                var tex = (int)MobileTextureSubtarget.DXT;
                var graphicsAPIMask = GetGraphicsAPIsMaskForPlatform(BuildTarget.WebGL);
                return (tex & 0x3F) | ((graphicsAPIMask & 0xFFFFFF) << 8);
            }
            return 0;
        }
        private static int GetGraphicsAPIsMaskForPlatform(BuildTarget varBuildTarget)
        {
            var tempMask = 0;
            var tempAPIs = PlayerSettings.GetGraphicsAPIs(varBuildTarget);
            for (int i = 0; i < tempAPIs.Length; ++i)
            {
                tempMask |= (1 << (int)tempAPIs[i]);
            }
            return tempMask;
        }
        private static int[] ShaderCompilerPlatformFromGfxDeviceRenderer(GraphicsDeviceType[] varGraphicsAPIs)
        {
            var kShaderCompPlatformCount = 0;
            var tempPlatArray = Enum.GetValues(typeof(ShaderCompilerPlatform));
            foreach (var item in tempPlatArray)
            {
                var tempVal = (int)item;
                if (tempVal > kShaderCompPlatformCount)
                {
                    kShaderCompPlatformCount = tempVal;
                }
            }
            var kRendererToCompilerPlatform = new int[]
            {
            kShaderCompPlatformCount, // removed: GL2
            kShaderCompPlatformCount, // removed: D3D9
            (int)ShaderCompilerPlatform.D3D, // kGfxRendererD3D11
            kShaderCompPlatformCount, // removed: PS3
            kShaderCompPlatformCount, // kGfxRendererNull, no shaders
            kShaderCompPlatformCount, // removed: Wii
            kShaderCompPlatformCount, // removed: Xbox 360
            kShaderCompPlatformCount, // removed: GLES1.1
            (int)ShaderCompilerPlatform.GLES20,// kGfxRendererGLES20
            kShaderCompPlatformCount, // removed: Flash
            kShaderCompPlatformCount, // removed: NaCl ("desktop GLES2")
            (int)ShaderCompilerPlatform.GLES3x,   // kGfxRendererOpenGLES3x
            kShaderCompPlatformCount,       // removed: PSP2
            (int)ShaderCompilerPlatform.PS4,         // kGfxRendererPS4
            (int)ShaderCompilerPlatform.XboxOneD3D11,     // kGfxRendererXboxOne
            kShaderCompPlatformCount,       // removed: PlayStation Mobile
            (int)ShaderCompilerPlatform.Metal,       // kGfxRendererMetal
            (int)ShaderCompilerPlatform.OpenGLCore,  // kGfxRendererOpenGLCore
            (int)ShaderCompilerPlatform.D3D,       // kGfxRendererD3D12, uses same shaders as DX11
            kShaderCompPlatformCount,       // removed: 3DS
            kShaderCompPlatformCount,       // removed: Wii U
            (int)ShaderCompilerPlatform.Vulkan,      // kGfxRendererVulkan
            (int)ShaderCompilerPlatform.Vulkan,      // kGfxRendererSwitch
            (int)ShaderCompilerPlatform.XboxOneD3D12 // kGfxRendererXboxOneD3D12
            };

            var temprCompilerPlatform = new List<int>();
            for (int iG = 0; iG < varGraphicsAPIs.Length; ++iG)
            {
                temprCompilerPlatform.Add(kRendererToCompilerPlatform[(int)varGraphicsAPIs[iG]]);
            }

            return temprCompilerPlatform.ToArray();
        }
        private static int GetShaderCompilerBackendVersion(ShaderCompilerPlatform varPlatform)
        {
            var kShaderCompilerHLSLccVersion = 201904020;
            var kShaderCompilerBackendVersion = new int[]
            {
            0, // OpenGL
            0, // removed platform D3D9
            0, // removed platform Xbox 360
            0, // removed platform PS3
            201707270, // D3D11
            kShaderCompilerHLSLccVersion, // OpenGL ES 2 mobile
            0, // removed platform OpenGL ES 2 desktop
            0, // removed platform Flash
            201707270, // D3D11 9.x level
            kShaderCompilerHLSLccVersion, // OpenGL ES 3
            0, // removed platform PSP2
            201511260, // PS4
            201912109, // XB1
            0, // removed platform PSM
            kShaderCompilerHLSLccVersion, // Metal
            kShaderCompilerHLSLccVersion, // GL 4.x
            0, // removed platform N3DS
            0, // removed platform Wii U
            kShaderCompilerHLSLccVersion, // Vulkan
            201708011, // Switch
            201912109  // XB1 D3D12
            };

            var shaderCompilerBackendVersion = kShaderCompilerBackendVersion[(int)varPlatform];
            var compilerVersion = 0;//GetShaderCompilerVersion((int)varPlatform);   // version of the platforms shader compiler from UnityShaderCompiler.exe (may be from platforms shader compiler plugin)

            return shaderCompilerBackendVersion ^ compilerVersion;    // by xoring the values we cause the hash the change when the platforms shader compiler version changes, also works nicely with the shader cache
        }

        private static UObject GetGraphicsSettings()
            => (UObject)typeof(GraphicsSettings).GetMethod("GetGraphicsSettings", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        private static Array GetRealtimeGITextures()
            => (Array)typeof(EditorUtility).Assembly.GetType("UnityEditor.LightmapVisualizationUtility").GetMethod("GetRealtimeGITextures",
                BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { GITextureType.Irradiance });
        private static BuildUsageTagGlobal GetSceneBuildUsageTag(UScene varScene)
        {
            var tempSetting = new SerializedObject(GetGraphicsSettings());
            var m_LightmapStripping = tempSetting.FindProperty("m_LightmapStripping").intValue;
            var m_LightmapKeepPlain = tempSetting.FindProperty("m_LightmapKeepPlain").boolValue;
            var m_LightmapKeepDirCombined = tempSetting.FindProperty("m_LightmapKeepDirCombined").boolValue;
            var m_LightmapKeepDynamicPlain = tempSetting.FindProperty("m_LightmapKeepDynamicPlain").boolValue;
            var m_LightmapKeepDynamicDirCombined = tempSetting.FindProperty("m_LightmapKeepDynamicDirCombined").boolValue;
            var m_LightmapKeepShadowMask = tempSetting.FindProperty("m_LightmapKeepShadowMask").boolValue;
            var m_LightmapKeepSubtractive = tempSetting.FindProperty("m_LightmapKeepSubtractive").boolValue;
            var m_FogStripping = tempSetting.FindProperty("m_FogStripping").intValue;
            var m_FogKeepLinear = tempSetting.FindProperty("m_FogKeepLinear").boolValue;
            var m_FogKeepExp = tempSetting.FindProperty("m_FogKeepExp").boolValue;
            var m_FogKeepExp2 = tempSetting.FindProperty("m_FogKeepExp2").boolValue;
            var m_InstancingStripping = tempSetting.FindProperty("m_InstancingStripping").intValue;

            var tempTag = new BuildUsageTagGlobal();

            if (m_LightmapStripping == (int)ShaderStrippingMode.kShaderStrippingAutomatic)
            {
                AddLightmapModeUsageFromCurrentSceneToBuildUsage(ref tempTag);
            }
            else
            {
                if (m_LightmapKeepPlain)
                    tempTag.m_LegacyLightmapModesUsed |= (1 << (int)LightmapsModeLegacy.kSingleLightmapsMode);
                if (m_LightmapKeepDirCombined)
                    tempTag.m_LegacyLightmapModesUsed |= (1 << (int)LightmapsModeLegacy.kDirectionalLightmapsMode);

                if (m_LightmapKeepPlain)
                    tempTag.m_LightmapModesUsed |= (1 << (int)LightmapsMode.kNonDirectionalLightmapsMode);
                if (m_LightmapKeepDirCombined)
                    tempTag.m_LightmapModesUsed |= (1 << (int)LightmapsMode.kCombinedDirectionalLightmapsMode);

                if (m_LightmapKeepDynamicPlain)
                    tempTag.m_DynamicLightmapsUsed |= (1 << (int)LightmapsMode.kNonDirectionalLightmapsMode);
                if (m_LightmapKeepDynamicDirCombined)
                    tempTag.m_DynamicLightmapsUsed |= (1 << (int)LightmapsMode.kCombinedDirectionalLightmapsMode);

                if (m_LightmapKeepShadowMask)
                    tempTag.m_ShadowMasksUsed = true;
                if (m_LightmapKeepSubtractive)
                    tempTag.m_SubtractiveUsed = true;
            }

            if (m_FogStripping == (int)ShaderStrippingMode.kShaderStrippingAutomatic)
            {
                GetFogModeUsageFromCurrentScene(ref tempTag);
            }
            else
            {
                if (m_FogKeepLinear)
                    tempTag.m_FogModesUsed |= (1 << (int)FogMode.kFogLinear);
                if (m_FogKeepExp)
                    tempTag.m_FogModesUsed |= (1 << (int)FogMode.kFogExp);
                if (m_FogKeepExp2)
                    tempTag.m_FogModesUsed |= (1 << (int)FogMode.kFogExp2);
            }

            tempTag.m_ForceInstancingStrip = (m_InstancingStripping == (int)InstancingStrippingMode.kInstancingStrippingStripAll);
            tempTag.m_ForceInstancingKeep = (m_InstancingStripping == (int)InstancingStrippingMode.kInstancingStrippingKeepAll);

            return tempTag;
        }
        private static void AddLightmapModeUsageFromCurrentSceneToBuildUsage(ref BuildUsageTagGlobal inoutUsage)
        {
            var HasLightmapTextures = LightmapSettings.lightmaps.Length > 0;
            if (HasLightmapTextures)
            {
                if (Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.Legacy)
                {
                    inoutUsage.m_LightmapModesUsed |= (1 << (int)LightmapSettings.lightmapsMode);

                    var HasShadowMaskTextures = false;
                    var tempLightMaps = LightmapSettings.lightmaps;
                    var tempCount = tempLightMaps.Length;
                    for (int iL = 0; iL < tempCount; iL++)
                    {
                        var tempData = tempLightMaps[iL];
                        if (tempData.shadowMask != null)
                        {
                            HasShadowMaskTextures = true;
                            break;
                        }
                    }

                    if (HasShadowMaskTextures)
                    {
                        inoutUsage.m_ShadowMasksUsed |= true;
                    }
                    else
                    {
                        inoutUsage.m_SubtractiveUsed |= LightmapEditorSettings.mixedBakeMode == MixedLightingMode.Subtractive;
                    }
                }
                else
                {
                    inoutUsage.m_LegacyLightmapModesUsed |= (1 << (int)LightmapsModeLegacy.kSingleLightmapsMode);
                }
            }

            var DynamicGILightmapTextures = GetRealtimeGITextures();
            var HasDynamicGILightmapTextures = DynamicGILightmapTextures != null && DynamicGILightmapTextures.Length != 0;
            if (HasDynamicGILightmapTextures)
            {
                inoutUsage.m_DynamicLightmapsUsed |= (1 << (int)LightmapSettings.lightmapsMode);
            }

            var tempTargetPlatform = EditorUserBuildSettings.activeBuildTarget;
            var tempAPIs = PlayerSettings.GetGraphicsAPIs(tempTargetPlatform);

            foreach (var renderer in tempAPIs)
            {
                if ((renderer == GraphicsDeviceType.OpenGLES2) || (renderer == GraphicsDeviceType.Direct3D11 && tempTargetPlatform == BuildTarget.WSAPlayer))
                {
                    if (HasLightmapTextures)
                    {
                        inoutUsage.m_LightmapModesUsed |= (1 << (int)LightmapsMode.kNonDirectionalLightmapsMode);
                        inoutUsage.m_LegacyLightmapModesUsed |= (1 << (int)LightmapsModeLegacy.kSingleLightmapsMode);
                    }

                    if (HasDynamicGILightmapTextures)
                    {
                        inoutUsage.m_DynamicLightmapsUsed |= (1 << (int)LightmapsMode.kNonDirectionalLightmapsMode);
                    }
                }
            }
        }
        private static void GetFogModeUsageFromCurrentScene(ref BuildUsageTagGlobal inoutUsage)
        {
            if (RenderSettings.fog)
            {
                inoutUsage.m_FogModesUsed |= (1 << (int)RenderSettings.fogMode);
            }
        }
        #endregion

        #region [API]
        public static BuildHash BuildHashInfo(BuildAssetBundleOptions varBuildOption)
        {
            var tempHash = new BuildHash();
            tempHash.BuildPlatform = EditorUserBuildSettings.activeBuildTarget;
            tempHash.BuildSubPlatform = GetActiveSubtargetFor(tempHash.BuildPlatform);
            tempHash.GraphicsAPIs = PlayerSettings.GetGraphicsAPIs(tempHash.BuildPlatform);

            //shaderPlatforms;
            tempHash.ShaderCompilerPlatforms = ShaderCompilerPlatformFromGfxDeviceRenderer(tempHash.GraphicsAPIs);
            //TODO - GetShaderCompilerBackendVersion();

            tempHash.stripUnusedMeshComponents = PlayerSettings.stripUnusedMeshComponents;
            tempHash.AssetBundleBuildOptions = varBuildOption
                & ~BuildAssetBundleOptions.ForceRebuildAssetBundle & ~BuildAssetBundleOptions.IgnoreTypeTreeChanges
                & ~BuildAssetBundleOptions.AppendHashToAssetBundleName & ~BuildAssetBundleOptions.DryRunBuild;

            var tempSceneGUIDs = AssetDatabase.FindAssets("t:Scene");
            if (tempSceneGUIDs.Length != 0)
            {
                var tempBuildTags = new List<BuildUsageTagGlobal>(tempSceneGUIDs.Length);
                foreach (var tempGUID in tempSceneGUIDs)
                {
                    var tempPath = AssetDatabase.GUIDToAssetPath(tempGUID);
                    var tempUScene = EditorSceneManager.OpenScene(tempPath, OpenSceneMode.Single);
                    tempBuildTags.Add(GetSceneBuildUsageTag(tempUScene));
                }
                tempHash.BuildUsageTagGlobals = tempBuildTags.ToArray();
            }

            return tempHash;
        }
        public static string BuildHashInfoToJson(BuildAssetBundleOptions varBuildOption)
        {
            var tempInfo = BuildHashInfo(varBuildOption);
            return JsonFx.Json.JsonWriter.Serialize(tempInfo);
        }
        #endregion
    }
}
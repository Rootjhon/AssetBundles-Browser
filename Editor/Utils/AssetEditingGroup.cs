using System;
using UnityEditor;

namespace AssetBundleBrowser
{
    public sealed class AssetEditingGroup : IDisposable
    {
        #region [Fields]
        private const string Key_Refresh = "kAutoRefresh";
        private bool _ProjectRefreshState = false;
        #endregion

        #region [Construct]
        public AssetEditingGroup()
        {
            _ProjectRefreshState = EditorPrefs.GetBool(Key_Refresh);
            EditorPrefs.SetBool("kAutoRefresh", false);
            AssetDatabase.StartAssetEditing();
        }
        #endregion

        #region [IDisposable]
        public void Dispose()
        {
            AssetDatabase.StopAssetEditing();
            EditorPrefs.SetBool("kAutoRefresh", _ProjectRefreshState);
        }
        #endregion
    }
}
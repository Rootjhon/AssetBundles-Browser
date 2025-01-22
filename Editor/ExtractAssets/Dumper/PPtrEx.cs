using System.Text.RegularExpressions;

namespace AssetBundleBrowser.ExtractAssets
{
    public static class PPtr
    {
        #region [API]
        public static bool IsPPtr(string varPPtrFormat, out string varTemplateCls)
        {
            varTemplateCls = string.Empty;

            var tempMatch = Regex.Match(varPPtrFormat, "^PPtr<(\\w+)>$");
            if (tempMatch.Success)
            {
                varTemplateCls = tempMatch.Groups[1].Value;
            }

            return tempMatch.Success;
        }
        #endregion
    }
}
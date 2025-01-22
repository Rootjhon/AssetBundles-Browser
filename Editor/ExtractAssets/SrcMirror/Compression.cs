using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBrowser.ExtractAssets
{
    public class Compression
    {
        public enum CompressionType : int
        {
            kCompressionNone = 0,
            kCompressionLzma,
            kCompressionLz4,
            kCompressionLz4HC,
            kCompressionLzham,

            kCompressionCount
        }

        public enum CompressionLevel : int
        {
            kCompressionLevelNone = 0,
            kCompressionLevelFastest,
            kCompressionLevelFast,
            kCompressionLevelNormal,
            kCompressionLevelHigh,
            kCompressionLevelMaximum,

            kCompressionLevelCount
        }
    }
}
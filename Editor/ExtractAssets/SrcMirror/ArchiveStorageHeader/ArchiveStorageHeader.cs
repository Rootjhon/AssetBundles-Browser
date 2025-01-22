using System;
using System.IO;
using UnityEngine;
using SevenZip;
using System.Collections.Generic;
using System.Linq;

namespace AssetBundleBrowser.ExtractAssets
{
    /// <summary>
    /// Header definitions of the ArchiveStorage
    /// </summary>
    public partial class ArchiveStorageHeader
    {
        #region [Enum]
        public enum ArchiveFlags : int
        {
            kArchiveCompressionTypeMask = (1 << 6) - 1,
            /// <summary>
            /// Block is streamed.
            /// </summary>
            kArchiveBlocksAndDirectoryInfoCombined = 1 << 6,
            kArchiveBlocksInfoAtTheEnd = 1 << 7,
            kArchiveOldWebPluginCompatibility = 1 << 8,
        }
        #endregion

        #region [Fields]
        public Header HeaderInfo;
        /// <summary>
        /// Information about compressed data blocks
        /// </summary>
        public List<StorageBlock> BlocksInfo;
        /// <summary>
        /// Files mapping information
        /// </summary>
        public List<Node> DirectoryInfo;

        #endregion

        #region [Construct]
        public ArchiveStorageHeader(EndianBinaryReader varStream)
        {
            HeaderInfo = Header.Parse(varStream);
            ReadBlocksInfoAndDirectory(varStream);
            var tempUnCompressSize = BlocksInfo.Sum(bi => bi.uncompressedSize);
            //TODO - 大于2G的情况;
            using (var tempBlockStream = new MemoryStream((int)tempUnCompressSize))
            {
                ReadBlocks(varStream, tempBlockStream);
                ReadFiles(tempBlockStream);
            }
        }
        #endregion

        #region [API]

        #endregion

        #region [Business]

        private long ReadBlocksInfoAndDirectory(EndianBinaryReader varReader)
        {
            byte[] blocksInfoBytes;
            if (HeaderInfo.version >= 7)
            {
                varReader.AlignStream(16);
            }
            if ((HeaderInfo.flags & (int)ArchiveFlags.kArchiveBlocksInfoAtTheEnd) != 0)
            {
                var tempPosition = varReader.Position;
                {
                    varReader.Position = varReader.BaseStream.Length - HeaderInfo.compressedBlocksInfoSize;
                    blocksInfoBytes = varReader.ReadBytes((int)HeaderInfo.compressedBlocksInfoSize);
                }
                varReader.Position = tempPosition;
            }
            else //0x40 kArchiveBlocksAndDirectoryInfoCombined
            {
                blocksInfoBytes = varReader.ReadBytes((int)HeaderInfo.compressedBlocksInfoSize);
            }
            var blocksInfoCompressedStream = new MemoryStream(blocksInfoBytes);
            MemoryStream blocksInfoUncompresseddStream;
            switch (HeaderInfo.GetBlocksInfoCompressionType())
            {
                default: //None
                    {
                        blocksInfoUncompresseddStream = blocksInfoCompressedStream;
                        break;
                    }
                case Compression.CompressionType.kCompressionLzma:
                    {
                        blocksInfoUncompresseddStream = new MemoryStream((int)(HeaderInfo.uncompressedBlocksInfoSize));
                        SevenZipHelper.StreamDecompress(blocksInfoCompressedStream, blocksInfoUncompresseddStream, HeaderInfo.compressedBlocksInfoSize, HeaderInfo.uncompressedBlocksInfoSize);
                        blocksInfoUncompresseddStream.Position = 0;
                        blocksInfoCompressedStream.Close();
                        break;
                    }
                case Compression.CompressionType.kCompressionLz4:
                case Compression.CompressionType.kCompressionLz4HC:
                    {
                        var uncompressedBytes = new byte[HeaderInfo.uncompressedBlocksInfoSize];
                        using (var decoder = new Lz4DecoderStream(blocksInfoCompressedStream))
                        {
                            decoder.Read(uncompressedBytes, 0, uncompressedBytes.Length);
                        }
                        blocksInfoUncompresseddStream = new MemoryStream(uncompressedBytes);
                        break;
                    }
            }
            using (var blocksInfoReader = new EndianBinaryReader(blocksInfoUncompresseddStream))
            {
                var uncompressedDataHash = blocksInfoReader.ReadBytes(16);
                var blocksInfoCount = blocksInfoReader.ReadInt32();
                BlocksInfo = new List<StorageBlock>(blocksInfoCount);
                for (int i = 0; i < blocksInfoCount; i++)
                {
                    var tempBlock = new StorageBlock().Parse(blocksInfoReader);
                    BlocksInfo.Add(tempBlock);
                }

                var nodesCount = blocksInfoReader.ReadInt32();
                DirectoryInfo = new List<Node>(nodesCount);
                for (int i = 0; i < nodesCount; i++)
                {
                    var tempDirInfo = new Node().Parse(blocksInfoReader);
                    DirectoryInfo.Add(tempDirInfo);
                }
            }

            return varReader.Position;
        }
        private void ReadBlocks(EndianBinaryReader varReader, Stream varBlocksStream)
        {
            foreach (var blockInfo in BlocksInfo)
            {
                switch (blockInfo.GetCompressionType())
                {
                    default: //None
                        {
                            varReader.BaseStream.CopyTo(varBlocksStream, blockInfo.compressedSize);
                            break;
                        }
                    case Compression.CompressionType.kCompressionLzma: //LZMA
                        {
                            SevenZipHelper.StreamDecompress(varReader.BaseStream, varBlocksStream, blockInfo.compressedSize, blockInfo.uncompressedSize);
                            break;
                        }
                    case Compression.CompressionType.kCompressionLz4: //LZ4
                    case Compression.CompressionType.kCompressionLz4HC: //LZ4HC
                        {
                            var compressedStream = new MemoryStream(varReader.ReadBytes((int)blockInfo.compressedSize));
                            using (var lz4Stream = new Lz4DecoderStream(compressedStream))
                            {
                                lz4Stream.CopyTo(varBlocksStream, blockInfo.uncompressedSize);
                            }
                            break;
                        }
                }
            }
            varBlocksStream.Position = 0;
        }
        public void ReadFiles(Stream blocksStream)
        {
            for (int i = 0; i < DirectoryInfo.Count; ++i)
            {
                var node = DirectoryInfo[i];
                node.Context = new MemoryStream((int)node.size);
                blocksStream.Position = node.offset;
                blocksStream.CopyTo(node.Context, node.size);
                node.Context.Position = 0;
            }
        }
        #endregion
    }
}

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

using URandom = UnityEngine.Random;

namespace AssetBundleBrowser.AssetBundleModel
{
    internal sealed class AssetTreeItem : TreeViewItem
    {
        private AssetInfo m_asset;
        internal AssetInfo asset
        {
            get { return m_asset; }
        }
        internal AssetTreeItem() : base(-1, -1) { }
        internal AssetTreeItem(AssetInfo a) : base(a != null ? a.fullAssetName.GetHashCode() : URandom.Range(int.MinValue, int.MaxValue), 0, a != null ? a.displayName : "failed")
        {
            m_asset = a;
            if (a != null)
                icon = AssetDatabase.GetCachedIcon(a.fullAssetName) as Texture2D;
        }

        private Color m_color = new Color(0, 0, 0, 0);
        internal Color itemColor
        {
            get
            {
                if (m_color.a == 0.0f && m_asset != null)
                {
                    m_color = m_asset.GetColor();
                }
                return m_color;
            }
            set { m_color = value; }
        }
        internal Texture2D MessageIcon()
        {
            return MessageSystem.GetIcon(HighestMessageLevel());
        }
        internal MessageType HighestMessageLevel()
        {
            return m_asset != null ?
                m_asset.HighestMessageLevel() : MessageType.Error;
        }

        internal bool ContainsChild(AssetInfo asset)
        {
            bool contains = false;
            if (children == null)
                return contains;

            if (asset == null)
                return false;
            foreach (var child in children)
            {
                var c = child as AssetTreeItem;
                if (c != null && c.asset != null && c.asset.fullAssetName == asset.fullAssetName)
                {
                    contains = true;
                    break;
                }
            }

            return contains;
        }


    }

    internal class AssetInfo
    {
        public const string AutoEmptyTag = "auto";

        internal bool isScene { get; set; }
        internal bool isFolder { get; set; }

        private long _fileSize = -1;
        internal long fileSize
        {
            get
            {
                if (_fileSize == -1)
                {
                    //TODO - maybe there's a way to ask the AssetDatabase for this size info.
                    var fileInfo = new FileInfo(m_AssetName);
                    _fileSize = fileInfo.Exists ? fileInfo.Length : 0;
                }
                return _fileSize;
            }
        }

        private long _UfileSize = 0;
        internal long UfileSize
        {
            get
            {
                if (fileSize == 0) return 0;
                if (_UfileSize != 0) return _UfileSize;
                if (assetType == typeof(Texture2D))
                {
                    if (assetType == typeof(Texture2D))
                    {
                        var tempObj = AssetDatabase.LoadAssetAtPath<Texture2D>(m_AssetName);
                        _UfileSize = tempObj.GetRawTextureData().LongLength;
                        Resources.UnloadAsset(tempObj);
                    }
                    else
                    {
                        _UfileSize = 0;
                    }
                }
                return _UfileSize;
            }
        }

        private Type _assetType;
        internal Type assetType
        {
            get
            {
                if (_assetType == null)
                {
                    _assetType = AssetDatabase.GetMainAssetTypeAtPath(m_AssetName);
                }
                return _assetType;
            }
        }

        private HashSet<string> m_Parents;
        private string m_AssetName;
        private string m_DisplayName;
        private string m_BundleName;
        private MessageSystem.MessageState m_AssetMessages = new MessageSystem.MessageState();

        internal AssetInfo(string inName, string bundleName = "")
        {
            fullAssetName = inName;
            m_BundleName = bundleName;
            m_Parents = new HashSet<string>();
            isScene = false;
            isFolder = false;
        }

        internal string fullAssetName
        {
            get { return m_AssetName; }
            set
            {
                m_AssetName = value;
                m_DisplayName = Path.GetFileNameWithoutExtension(m_AssetName);
            }
        }
        internal string displayName
        {
            get { return m_DisplayName; }
        }
        internal string bundleName
        { get { return string.IsNullOrEmpty(m_BundleName) ? AutoEmptyTag : m_BundleName; } }

        internal Color GetColor()
        {
            if (string.IsNullOrEmpty(m_BundleName))
                return Model.k_LightGrey;
            else
                return Color.white;
        }

        internal bool IsMessageSet(MessageSystem.MessageFlag flag)
        {
            return m_AssetMessages.IsSet(flag);
        }
        internal void SetMessageFlag(MessageSystem.MessageFlag flag, bool on)
        {
            m_AssetMessages.SetFlag(flag, on);
        }
        internal MessageType HighestMessageLevel()
        {
            return m_AssetMessages.HighestMessageLevel();
        }
        internal IEnumerable<MessageSystem.Message> GetMessages()
        {
            var messages = new List<MessageSystem.Message>();
            if (IsMessageSet(MessageSystem.MessageFlag.SceneBundleConflict))
            {
                var message = displayName + "\n";
                if (isScene)
                    message += "Is a scene that is in a bundle with non-scene assets. Scene bundles must have only one or more scene assets.";
                else
                    message += "Is included in a bundle with a scene. Scene bundles must have only one or more scene assets.";
                messages.Add(new MessageSystem.Message(message, MessageType.Error));
            }
            if (IsMessageSet(MessageSystem.MessageFlag.DependencySceneConflict))
            {
                var message = displayName + "\n";
                message += MessageSystem.GetMessage(MessageSystem.MessageFlag.DependencySceneConflict).message;
                messages.Add(new MessageSystem.Message(message, MessageType.Error));
            }
            if (IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
            {
                var bundleNames = Model.CheckDependencyTracker(this);
                string message = string.Format("{0}\nIs auto-included in multiple bundles: - [{1}]\n", displayName, bundleNames.Count());
                message += string.Join(", ", bundleNames);
                messages.Add(new MessageSystem.Message(message, MessageType.Warning));
            }

            if (string.IsNullOrEmpty(m_BundleName) && m_Parents.Count > 0)
            {
                //TODO - refine the parent list to only include those in the current asset list
                var message = displayName + "\n" + "Is auto included in bundle(s) due to parent(s): \n";
                message += string.Join(", ", m_Parents);
                messages.Add(new MessageSystem.Message(message, MessageType.Info));
            }

            if (m_dependencies != null && m_dependencies.Count > 0)
            {
                var message = string.Empty;
                var sortedDependencies = m_dependencies.OrderBy(d => d.bundleName);
                foreach (var dependent in sortedDependencies)
                {
                    if (dependent.bundleName != bundleName)
                    {
                        message += dependent.bundleName + " : " + dependent.displayName + "\n";
                    }
                }
                if (string.IsNullOrEmpty(message) == false)
                {
                    message = message.Insert(0, displayName + "\n" + "Is dependent on other bundle's asset(s) or auto included asset(s): \n");
                    message = message.Substring(0, message.Length - 1);//remove trailing line break.
                    messages.Add(new MessageSystem.Message(message, MessageType.Info));
                }
            }

            messages.Add(new MessageSystem.Message(displayName + "\n" + "Path: " + fullAssetName, MessageType.Info));

            return messages;
        }
        internal void AddParent(string name)
        {
            m_Parents.Add(name);
        }
        internal void RemoveParent(string name)
        {
            m_Parents.Remove(name);
        }

        internal string GetSizeString()
        {
            if (fileSize == 0)
                return "--";
            return EditorUtility.FormatBytes(fileSize);
        }
        internal string GetUSizeString()
        {
            if (UfileSize == 0 || UfileSize == fileSize)
                return "--";
            return EditorUtility.FormatBytes(UfileSize);
        }

        List<AssetInfo> m_dependencies = null;
        internal List<AssetInfo> GetDependencies()
        {
            if (m_dependencies != null) return m_dependencies;

            m_dependencies = new List<AssetInfo>();
            //TODO - not sure this refreshes enough. need to build tests around that.
            if (AssetDatabase.IsValidFolder(m_AssetName))
            {
                //if we have a folder, its dependencies were already pulled in through alternate means.  no need to GatherFoldersAndFiles
                //GatherFoldersAndFiles();
            }
            else
            {
                if (!MiscUtils.IsAtomAsset(m_AssetName))
                {
                    //TODO - 是否需要递归？从简单测试的情况来看是不需要的;
                    //var tempDeps = AssetDatabase.GetDependencies(m_AssetName, true);
                    var tempDeps = AssetDatabase.GetDependencies(m_AssetName, false);
                    m_dependencies.Capacity = tempDeps.Length > 1 ? tempDeps.Length - 1 : m_dependencies.Capacity;
                    foreach (var dep in tempDeps)
                    {
                        if (dep == m_AssetName) continue;

                        var asset = Model.CreateAsset(dep, this);
                        if (asset != null)
                            m_dependencies.Add(asset);
                    }
                }
            }

            return m_dependencies;
        }

    }

}

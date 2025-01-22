using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
//using System;

using UObject = UnityEngine.Object;


namespace AssetBundleBrowser
{
    internal class AssetListTree : TreeView
    {
        List<AssetBundleModel.BundleInfo> m_SourceBundles = new List<AssetBundleModel.BundleInfo>();
        AssetBundleManageTab m_Controller;
        List<UObject> m_EmptyObjectList = new List<UObject>();

        internal static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }
        private delegate void CreateColumn(ref List<MultiColumnHeaderState.Column> varSource, GUIContent varGUIC, float varMinWidth,
            float varWidth, float varMaxWidth, TextAlignment varheaderTextAlignment, bool varCanSort, bool varAutoResize);
        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            var tempColumn = new List<MultiColumnHeaderState.Column>();
            CreateColumn tempCreateColumn = delegate (ref List<MultiColumnHeaderState.Column> varSource, GUIContent varGUIC, float varMinWidth,
               float varWidth, float varMaxWidth, TextAlignment varheaderTextAlignment, bool varCanSort, bool varAutoResize)
            {
                var tempItem = new MultiColumnHeaderState.Column();
                tempItem.headerContent = varGUIC;
                tempItem.minWidth = varMinWidth;
                tempItem.width = varWidth;
                tempItem.maxWidth = varMaxWidth;
                tempItem.headerTextAlignment = varheaderTextAlignment;
                tempItem.canSort = varCanSort;
                tempItem.autoResize = varAutoResize;
                varSource.Add(tempItem);
            };

            tempCreateColumn(ref tempColumn, new GUIContent("Asset", "Short name of asset. For full name select asset and see message below"),
                50, 150, 300, TextAlignment.Left, true, true);

            tempCreateColumn(ref tempColumn, new GUIContent("Bundle", "Bundle name. 'auto' means asset was pulled in due to dependency"),
                50, 100, 300, TextAlignment.Left, true, true);

            tempCreateColumn(ref tempColumn, new GUIContent("AssetType", "Type of Assets"),
                50, 100, 300, TextAlignment.Left, true, true);

            tempCreateColumn(ref tempColumn, new GUIContent("Size", "Size on disk"),
                30, 75, 100, TextAlignment.Left, true, true);

            tempCreateColumn(ref tempColumn, new GUIContent("USize", "Size on Unity Compress"),
                30, 75, 100, TextAlignment.Left, true, true);

            tempCreateColumn(ref tempColumn, new GUIContent("!", "Errors, Warnings, or Info"),
                16, 16, 16, TextAlignment.Left, true, false);

            return tempColumn.ToArray();
        }
        enum MyColumns
        {
            Asset,
            Bundle,
            AssetType,
            Size,
            USize,
            Message,
        }
        internal enum SortOption
        {
            Asset,
            Bundle,
            AssetType,
            Size,
            USize,
            Message,
        }
        SortOption[] m_SortOptions =
        {
            SortOption.Asset,
            SortOption.Bundle,
            SortOption.AssetType,
            SortOption.Size,
            SortOption.USize,
            SortOption.Message,
        };

        internal AssetListTree(TreeViewState state, MultiColumnHeaderState mchs, AssetBundleManageTab ctrl) : base(state, new MultiColumnHeader(mchs))
        {
            m_Controller = ctrl;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }


        internal void Update()
        {
            bool dirty = false;
            foreach (var bundle in m_SourceBundles)
            {
                dirty |= bundle.dirty;
            }
            if (dirty)
                Reload();
        }
        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }


        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(root, rows);
            return rows;
        }

        internal void SetSelectedBundles(IEnumerable<AssetBundleModel.BundleInfo> bundles)
        {
            m_Controller.SetSelectedItems(null);
            m_SourceBundles = bundles.ToList();
            SetSelection(new List<int>());
            Reload();
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = AssetBundleModel.Model.CreateAssetListTreeView(m_SourceBundles);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item as AssetBundleModel.AssetTreeItem, args.GetColumn(i), ref args);
        }

        private void CellGUI(Rect cellRect, AssetBundleModel.AssetTreeItem item, int column, ref RowGUIArgs args)
        {
            Color oldColor = GUI.color;
            CenterRectUsingSingleLineHeight(ref cellRect);
            if (column != 3)
                GUI.color = item.itemColor;

            switch (column)
            {
                case (int)SortOption.Asset:
                    {
                        var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                        if (item.icon != null)
                            GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                        DefaultGUI.Label(
                            new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width, cellRect.height),
                            item.displayName,
                            args.selected,
                            args.focused);
                    }
                    break;
                case (int)SortOption.Bundle:
                    DefaultGUI.Label(cellRect, item.asset.bundleName, args.selected, args.focused);
                    break;
                case (int)SortOption.AssetType:
                    {
                        string tempType = item.asset.assetType.ToString();
                        tempType = tempType.Substring(tempType.LastIndexOf('.') + 1);
                        DefaultGUI.Label(cellRect, tempType, args.selected, args.focused);
                    }
                    break;
                case (int)SortOption.Size:
                    DefaultGUI.Label(cellRect, item.asset.GetSizeString(), args.selected, args.focused);
                    break;
                case (int)SortOption.Message:
                    var icon = item.MessageIcon();
                    if (icon != null)
                    {
                        var iconRect = new Rect(cellRect.x, cellRect.y, cellRect.height, cellRect.height);
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    }
                    break;
                case (int)SortOption.USize:
                    DefaultGUI.Label(cellRect, item.asset.GetUSizeString(), args.selected, args.focused);
                    break;
            }
            GUI.color = oldColor;
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetItem = FindItem(id, rootItem) as AssetBundleModel.AssetTreeItem;
            if (assetItem != null)
            {
                Object o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.asset.fullAssetName);
                EditorGUIUtility.PingObject(o);
                Selection.activeObject = o;
            }
        }

        public void SetSelection(List<string> paths)
        {
            List<int> selected = new List<int>(paths.Count);
            AddIfInPaths(paths, selected, rootItem);
            SetSelection(selected);
        }

        void AddIfInPaths(List<string> paths, List<int> selected, TreeViewItem me)
        {
            var assetItem = me as AssetBundleModel.AssetTreeItem;
            if (assetItem != null && assetItem.asset != null)
            {
                if (paths.Contains(assetItem.asset.fullAssetName))
                {
                    if (selected.Contains(me.id) == false)
                        selected.Add(me.id);
                }
            }

            if (me.hasChildren)
            {
                foreach (TreeViewItem item in me.children)
                {
                    AddIfInPaths(paths, selected, item);
                }
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null)
                return;

            var selectedObjects = new List<Object>();
            var selectedAssets = new List<AssetBundleModel.AssetInfo>();
            foreach (var id in selectedIds)
            {
                var assetItem = FindItem(id, rootItem) as AssetBundleModel.AssetTreeItem;
                if (assetItem != null)
                {
                    Object o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.asset.fullAssetName);
                    selectedObjects.Add(o);
                    Selection.activeObject = o;
                    selectedAssets.Add(assetItem.asset);
                }
            }
            m_Controller.SetSelectedItems(selectedAssets);
            Selection.objects = selectedObjects.ToArray();
        }
        protected override bool CanBeParent(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = m_EmptyObjectList.ToArray();
            List<AssetBundleModel.AssetTreeItem> items =
                new List<AssetBundleModel.AssetTreeItem>(args.draggedItemIDs.Select(id => FindItem(id, rootItem) as AssetBundleModel.AssetTreeItem));
            DragAndDrop.paths = items.Select(a => a.asset.fullAssetName).ToArray();
            DragAndDrop.SetGenericData("AssetListTreeSource", this);
            DragAndDrop.StartDrag("AssetListTree");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (IsValidDragDrop())
            {
                if (args.performDrop)
                {
                    AssetBundleModel.Model.MoveAssetToBundle(DragAndDrop.paths, m_SourceBundles[0].m_Name.bundleName, m_SourceBundles[0].m_Name.variant);
                    AssetBundleModel.Model.ExecuteAssetMove();
                    foreach (var bundle in m_SourceBundles)
                    {
                        bundle.RefreshAssetList();
                    }
                    m_Controller.UpdateSelectedBundles(m_SourceBundles);
                }
                return DragAndDropVisualMode.Copy;//Move;
            }

            return DragAndDropVisualMode.Rejected;
        }
        protected bool IsValidDragDrop()
        {
            //can't do drag & drop if data source is read only
            if (AssetBundleModel.Model.DataSource.IsReadOnly())
                return false;

            //can't drag onto none or >1 bundles
            if (m_SourceBundles.Count == 0 || m_SourceBundles.Count > 1)
                return false;

            //can't drag nothing
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0)
                return false;

            //can't drag into a folder
            var folder = m_SourceBundles[0] as AssetBundleModel.BundleFolderInfo;
            if (folder != null)
                return false;

            var data = m_SourceBundles[0] as AssetBundleModel.BundleDataInfo;
            if (data == null)
                return false; // this should never happen.

            var thing = DragAndDrop.GetGenericData("AssetListTreeSource") as AssetListTree;
            if (thing != null)
                return false;

            if (data.IsEmpty())
                return true;


            if (data.isSceneBundle)
            {
                var tempDragPaths = DragAndDrop.paths;
                foreach (var assetPath in tempDragPaths)
                {
                    if ((AssetDatabase.GetMainAssetTypeAtPath(assetPath) != typeof(SceneAsset)) &&
                        (!AssetDatabase.IsValidFolder(assetPath)))
                        return false;
                }
            }
            else
            {
                var tempDragPaths = DragAndDrop.paths;
                foreach (var assetPath in tempDragPaths)
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset))
                        return false;
                }
            }

            return true;

        }

        protected override void ContextClickedItem(int id)
        {
            if (AssetBundleModel.Model.DataSource.IsReadOnly())
            {
                return;
            }

            var tempSelects = GetSelection();
            var selectedNodes = new List<AssetBundleModel.AssetTreeItem>(tempSelects.Count);
            foreach (var nodeID in tempSelects)
            {
                selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.AssetTreeItem);
            }

            if (selectedNodes.Count > 0)
            {
                var menu = new GenericMenu();

                menu.AddItem(new GUIContent("Remove asset(s) from bundle."), false, RemoveAssets, selectedNodes);

                var tempChild = rootItem.children;
                var tempDic = new Dictionary<string, int>(tempChild.Count);
                foreach (var item in tempChild)
                {
                    var tempATI = item as AssetBundleModel.AssetTreeItem;
                    if (!string.IsNullOrEmpty(tempATI.asset.bundleName) && tempATI.asset.bundleName != AssetBundleModel.AssetInfo.AutoEmptyTag)
                    {
                        if (!tempDic.ContainsKey(tempATI.asset.bundleName))
                            tempDic.Add(tempATI.asset.bundleName, 1);
                    }
                }

                if (tempDic.Count == 1)
                {
                    menu.AddItem(new GUIContent("Add asset(s) to bundle."), false, AddAssets, new object[] { selectedNodes, tempDic.Keys.First() });
                    menu.AddItem(new GUIContent("Find References In Scene"), false, FindReferencesInScene);
                }
                else if (tempDic.Count > 1)
                {
                    var tempDicKeys = tempDic.Keys;
                    foreach (var item in tempDicKeys)
                    {
                        menu.AddItem(new GUIContent("Add asset(s) to bundle./" + item), false, AddAssets, new object[] { selectedNodes, item });
                        menu.AddItem(new GUIContent("Find References In Scene/" + item), false, FindReferencesInScene);
                    }
                }

                menu.ShowAsContext();
            }

        }
        void RemoveAssets(object obj)
        {
            var selectedNodes = obj as List<AssetBundleModel.AssetTreeItem>;
            var assets = new List<AssetBundleModel.AssetInfo>(selectedNodes.Count);
            //var bundles = new List<AssetBundleModel.BundleInfo>();
            foreach (var node in selectedNodes)
            {
                if (!string.IsNullOrEmpty(node.asset.bundleName))
                    assets.Add(node.asset);
            }
            AssetBundleModel.Model.MoveAssetToBundle(assets, string.Empty, string.Empty);
            AssetBundleModel.Model.ExecuteAssetMove();
            foreach (var bundle in m_SourceBundles)
            {
                bundle.RefreshAssetList();
            }
            m_Controller.UpdateSelectedBundles(m_SourceBundles);
            //ReloadAndSelect(new List<int>());
        }

        void AddAssets(object varObj)
        {
            var tempResult = varObj as object[];
            var selectedNodes = tempResult[0] as List<AssetBundleModel.AssetTreeItem>;
            var tempBundlename = tempResult[1] as string;

            var assets = new List<AssetBundleModel.AssetInfo>();
            foreach (var node in selectedNodes)
            {
                if (!string.IsNullOrEmpty(node.asset.bundleName))
                    assets.Add(node.asset);
            }
            AssetBundleModel.Model.MoveAssetToBundle(assets, tempBundlename, string.Empty);
            AssetBundleModel.Model.ExecuteAssetMove();
            foreach (var bundle in m_SourceBundles)
            {
                bundle.RefreshAssetList();
            }
            m_Controller.UpdateSelectedBundles(m_SourceBundles);
        }

        void FindReferencesInScene()
        {
            EditorApplication.ExecuteMenuItem("Assets/Find References In Scene");
        }

        protected override void KeyEvent()
        {
            if (m_SourceBundles.Count > 0 && Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                List<AssetBundleModel.AssetTreeItem> selectedNodes = new List<AssetBundleModel.AssetTreeItem>();
                foreach (var nodeID in GetSelection())
                {
                    selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.AssetTreeItem);
                }

                RemoveAssets(selectedNodes);
            }
        }
        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }
        void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
                return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return;

            SortByColumn();

            rows.Clear();
            var tempChild = root.children;
            for (int i = 0; i < tempChild.Count; i++)
                rows.Add(tempChild[i]);

            Repaint();
        }
        void SortByColumn()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
                return;

            var tempChilds = rootItem.children;
            var assetList = new List<AssetBundleModel.AssetTreeItem>(tempChilds.Count);
            foreach (var item in tempChilds)
            {
                assetList.Add(item as AssetBundleModel.AssetTreeItem);
            }
            var orderedItems = InitialOrder(assetList, sortedColumns);

            rootItem.children = orderedItems.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<AssetBundleModel.AssetTreeItem> InitialOrder(IEnumerable<AssetBundleModel.AssetTreeItem> myTypes, int[] columnList)
        {
            SortOption sortOption = m_SortOptions[columnList[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
                case SortOption.Asset:
                    return myTypes.Order(l => l.displayName, ascending);
                case SortOption.AssetType:
                    return myTypes.Order(l =>
                    {
                        var tempAstTypeStr = l.asset.assetType.ToString();
                        return tempAstTypeStr.Substring(tempAstTypeStr.LastIndexOf('.') + 1);
                    }, ascending);
                case SortOption.Size:
                    return myTypes.Order(l => l.asset.fileSize, ascending);
                case SortOption.USize:
                    return myTypes.Order(l => l.asset.UfileSize, ascending);
                case SortOption.Message:
                    return myTypes.Order(l => l.HighestMessageLevel(), ascending);
                case SortOption.Bundle:
                default:
                    return myTypes.Order(l => l.asset.bundleName, ascending);
            }

        }

        private void ReloadAndSelect(IList<int> hashCodes)
        {
            Reload();
            SetSelection(hashCodes);
            SelectionChanged(hashCodes);
        }
    }
    static class MyExtensionMethods
    {
        internal static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, System.Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        internal static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, System.Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}

using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System;

using UnityObject = UnityEngine.Object;
namespace Battlehub.RTSL.Interface
{
    [ProtoContract]
    public class AssetBundleItemInfo
    {
        [ProtoMember(1)]
        public string Path;

        [ProtoMember(2)]
        public int Id;

        [ProtoMember(3)]
        public int ParentId;
    }

    [ProtoContract]
    public class AssetBundleInfo
    {
        [ProtoMember(1)]
        public string UniqueName;

        [ProtoMember(2)]
        public int Ordinal;

        [ProtoMember(3)]
        public AssetBundleItemInfo[] AssetBundleItems;

        [ProtoMember(4)]
        public int Identifier = 4;
    }

    [ProtoContract]
    public class ProjectInfo
    {
        //[ProtoMember(1)]
        //public int FolderIdentifier = 1;

        [ProtoMember(2)]
        public int AssetIdentifier = 1;

        [ProtoMember(3)]
        public int BundleIdentifier = 0;

        [ProtoMember(4)]
        public string Version = RTSLVersion.Version.ToString();

        public string Name;
        public DateTime LastWriteTime;
    }

    [ProtoContract]
    [ProtoInclude(1, typeof(AssetItem))]
    public class ProjectItem
    {
        [ProtoMember(2)]
        public long ItemID;

        [ProtoMember(3)]
        public Guid ItemGUID;

        public string Name;
        public string Ext;

        public ProjectItem Parent;
        public List<ProjectItem> Children;

        public string NameExt
        {
            get { return Name + Ext; }
        }

        public virtual bool IsFolder
        {
            get { return true; }
        }

        public static bool IsValidName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }
            return Path.GetInvalidFileNameChars().All(c => !name.Contains(c));
        }

        public void AddChild(ProjectItem item)
        {
            if (Children == null)
            {
                Children = new List<ProjectItem>();
            }

            if (item.Parent != null)
            {
                item.Parent.RemoveChild(item);
            }
            Children.Add(item);
            item.Parent = this;
        }

        public void RemoveChild(ProjectItem item)
        {
            if (Children == null)
            {
                return;
            }
            Children.Remove(item);
            item.Parent = null;
        }

        public int GetSiblingIndex()
        {
            return Parent.Children.IndexOf(this);
        }

        public void SetSiblingIndex(int index)
        {
            Parent.Children.Remove(this);
            Parent.Children.Insert(index, this);
        }

        public ProjectItem[] Flatten(bool excludeFolders, bool excludeAssets = false)
        {
            List<ProjectItem> items = new List<ProjectItem>();
            Foreach(this, projectItem =>
            {
                if (excludeFolders && projectItem.IsFolder)
                {
                    return;
                }

                if(excludeAssets && !projectItem.IsFolder)
                {
                    return;
                }

                items.Add(projectItem);
            });
            return items.ToArray();
        }

        public void Foreach(ProjectItem item, Action<ProjectItem> callback)
        {
            if(item == null)
            {
                return;
            }

            callback(item);

            if(item.Children != null)
            {
                for(int i = 0; i < item.Children.Count; ++i)
                {
                    ProjectItem child = item.Children[i];
                    Foreach(child, callback);
                }
            }
        }

        public ProjectItem Get(string path, bool forceCreate = false)
        {
            path = path.Trim('/');
            string[] pathParts = path.Split('/');

            ProjectItem item = this;
            for (int i = 1; i < pathParts.Length; ++i)
            {
                string pathPart = pathParts[i];
                if (item.Children == null)
                {
                    if(forceCreate)
                    {
                        item.Children = new List<ProjectItem>();
                    }
                    else
                    {
                        return item;
                    }
                }

                ProjectItem nextItem = item.Children.Where(child => child.NameExt == pathPart).FirstOrDefault();
                if (nextItem == null)
                {
                    if (forceCreate)
                    {
                        if (string.IsNullOrEmpty(Path.GetExtension(pathPart)))
                        {
                            nextItem = new ProjectItem
                            {
                                Name = pathPart
                            };
                            item.AddChild(nextItem);
                        }
                        else
                        {
                            nextItem = new AssetItem
                            {
                                Name = Path.GetFileNameWithoutExtension(pathPart),
                                Ext = Path.GetExtension(pathPart),
                            };
                            item.AddChild(nextItem);
                        }
                    }
                    else
                    {
                        item = nextItem;
                        break;
                    }
                }
                item = nextItem;
            }
            return item;
        }

        public bool IsDescendantOf(ProjectItem ancestor)
        {
            ProjectItem projectItem = this;
            while(projectItem != null)
            {
                if(projectItem == ancestor)
                {
                    return true;
                }

                projectItem = projectItem.Parent;
            }
            return false;
        }

        public string RelativePath(bool includeExt)
        {
            StringBuilder sb = new StringBuilder();
            ProjectItem parent = this;
            while (parent.Parent != null)
            {
                sb.Insert(0, parent.Name);
                sb.Insert(0, "/");
                parent = parent.Parent;
            }
            string ext = null;
            if (includeExt)
            {
                ext = Ext;
            }
            if (string.IsNullOrEmpty(ext))
            {
                return sb.ToString().TrimStart('/'); 
            }
            return string.Format("{0}{1}", sb.ToString(), Ext).TrimStart('/');
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            ProjectItem parent = this;
            while (parent != null)
            {
                sb.Insert(0, parent.Name);
                sb.Insert(0, "/");
                parent = parent.Parent;
            }

            string ext = Ext;
            if (string.IsNullOrEmpty(ext))
            {
                return sb.ToString();
            }
            return string.Format("{0}{1}", sb.ToString(), Ext);
        }
    }

    [ProtoContract]
    public class PrefabPart
    {
        [ProtoMember(1)]
        public long PartID;

        [ProtoMember(2)]
        public long ParentID;

        [ProtoMember(3)]
        public string Name;

        [ProtoMember(4)]
        public Guid TypeGuid;

        [ProtoMember(5)]
        public Guid PartGUID;

        [ProtoMember(6)]
        public Guid ParentGUID;
    }

    [ProtoContract]
    public class Preview
    {
        [ProtoMember(1)]
        public long ItemID;

        [ProtoMember(2)]
        public byte[] PreviewData;

        [ProtoMember(3)]
        public Guid ItemGUID;
    }

    public enum ImportStatus
    {
        None,
        New,
        Conflict,
        Overwrite
    }

    public class ImportItem : AssetItem
    {
        public UnityObject Object;

        public ImportStatus Status;
    }

    [ProtoContract]
    public class AssetItem : ProjectItem
    {
        public event EventHandler PreviewDataChanged;

        [ProtoMember(1)]
        public Guid TypeGuid;

        [ProtoMember(2)]
        public PrefabPart[] Parts;

        [ProtoMember(3)]
        public long[] Dependencies;

        [ProtoMember(4, IsRequired = true)]
        public long CustomDataOffset = -1;

        [ProtoMember(5)]
        public Guid[] DependenciesGuids;

        private Preview m_preview;
        public Preview Preview
        {
            get { return m_preview; }
            set
            {
                if (m_preview != value)
                {
                    m_preview = value;
                    if (PreviewDataChanged != null)
                    {
                        PreviewDataChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public override bool IsFolder
        {
            get { return false; }
        }
    }
}


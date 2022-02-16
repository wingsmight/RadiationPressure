using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using Battlehub.UIControls.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public interface IFileImporter
    {
        string FileExt
        {
            get;
        }

        string IconPath
        {
            get;
        }

        int Priority
        {
            get;
        }

        IEnumerator Import(string filePath, string targetPath);
    }

    public abstract class FileImporter : IFileImporter
    {
        public abstract string FileExt { get; }

        public abstract string IconPath { get; }

        public virtual int Priority
        {
            get { return 0; }
        }

        public abstract IEnumerator Import(string filePath, string targetPath);
    }

    public class ImportFileDialog : RuntimeWindow
    {
        private Dialog m_parentDialog;
        private FileBrowser m_fileBrowser;

        private Dictionary<string, IFileImporter> m_extToFileImporter = new Dictionary<string, IFileImporter>();

        private ILocalization m_localization;

        protected override void AwakeOverride()
        {
            WindowType = RuntimeWindowType.ImportFile;
            base.AwakeOverride();

            m_localization = IOC.Resolve<ILocalization>();
        }

        private void Start()
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach (string assemblyName in BHRoot.Assemblies)
            {
                var asName = new AssemblyName();
                asName.Name = assemblyName;

                try
                {
                    Assembly asm = Assembly.Load(asName);
                    assemblies.Add(asm);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e.ToString());
                }
            }

            m_parentDialog = GetComponentInParent<Dialog>();
            m_parentDialog.Ok += OnOk;
            m_parentDialog.OkText = m_localization.GetString("ID_RTEditor_ImportFileDialog_Btn_Open", "Open");
            m_parentDialog.IsOkVisible = true;
            m_parentDialog.CancelText = m_localization.GetString("ID_RTEditor_ImportFileDialog_Btn_Cancel", "Cancel");
            m_parentDialog.IsCancelVisible = true;

            m_fileBrowser = GetComponent<FileBrowser>();
            m_fileBrowser.DoubleClick += OnFileBrowserDoubleClick;
            m_fileBrowser.SelectionChanged += OnFileBrowserSelectionChanged;

            List<string> allowedExts = new List<string>();
            List<FileIcon> icons = new List<FileIcon>();

            Type[] importerTypes = assemblies.SelectMany(asm => asm.GetTypes().Where(t => t != null && t.IsClass && typeof(IFileImporter).IsAssignableFrom(t))).ToArray();
            foreach (Type importerType in importerTypes)
            {
                if(importerType.IsAbstract)
                {
                    continue;
                }

                try
                {
                    IFileImporter fileImporter = (IFileImporter)Activator.CreateInstance(importerType);

                    string ext = fileImporter.FileExt;
                    ext = ext.ToLower();

                    if(!ext.StartsWith("."))
                    {
                        ext = "." + ext;
                    }

                    if (m_extToFileImporter.ContainsKey(ext))
                    {
                        if(m_extToFileImporter[ext].Priority > fileImporter.Priority)
                        {
                            continue;
                        }
                    }
                    m_extToFileImporter[ext] = fileImporter;

                    allowedExts.Add(ext);
                    icons.Add(new FileIcon { Ext = ext, Icon = Resources.Load<Sprite>(fileImporter.IconPath) });
                }
                catch (Exception e)
                {
                    Debug.LogError("Unable to instantiate File Importer " + e.ToString());
                }
            }

            m_fileBrowser.AllowedExt = allowedExts;
            m_fileBrowser.Icons = icons;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();
            if (m_parentDialog != null)
            {
                m_parentDialog.Ok -= OnOk;
            }

            if(m_fileBrowser != null)
            {
                m_fileBrowser.DoubleClick -= OnFileBrowserDoubleClick;
                m_fileBrowser.SelectionChanged -= OnFileBrowserSelectionChanged;
            }
        }

        private void OnOk(Dialog sender, DialogCancelArgs args)
        {
            args.Cancel = true;

            string path = m_fileBrowser.Open();
            if(string.IsNullOrEmpty(path))
            {    
                return;
            }

            if (!File.Exists(path))
            {
                return;
            }

            TryImport(path);
        }

        private void OnFileBrowserSelectionChanged(string path)
        {
            if (File.Exists(path))
            {
                m_parentDialog.OkText = m_localization.GetString("ID_RTEditor_ImportFileDialog_Btn_Import", "Import");
            }
            else
            {
                m_parentDialog.OkText = m_localization.GetString("ID_RTEditor_ImportFileDialog_Btn_Open", "Open");
            }
        }

        private void OnFileBrowserDoubleClick(string path)
        {
            if(File.Exists(path))
            {
                TryImport(path);
            }
        }
        
        private void TryImport(string path)
        {
            string ext = Path.GetExtension(path);
            ext = ext.ToLower();

            IFileImporter importer;
            if(!m_extToFileImporter.TryGetValue(ext, out importer))
            {
                Debug.LogWarning("Importer for " + ext + " does not exists");
                return;
            }

           Editor.StartCoroutine(CoImport(importer, path));
        }

        private IEnumerator CoImport(IFileImporter importer, string path)
        {
            IRTE rte = IOC.Resolve<IRTE>();
            rte.IsBusy = true;

            IProjectTree projectTree = IOC.Resolve<IProjectTree>();
            string targetPath = Path.GetFileNameWithoutExtension(path);
            if (projectTree != null && projectTree.SelectedItem != null)
            {
                ProjectItem folder = projectTree.SelectedItem;

                targetPath = folder.RelativePath(false) + "/" + targetPath;
                targetPath = targetPath.TrimStart('/');

                IProject project = IOC.Resolve<IProject>();
                targetPath = project.GetUniquePath(targetPath, typeof(Texture2D), folder);    
            }

            yield return Editor.StartCoroutine(importer.Import(path, targetPath));
            rte.IsBusy = false;
            m_parentDialog.Close();
        }

        
    }
}

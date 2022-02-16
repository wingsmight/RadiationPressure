using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Battlehub.UIControls.MenuControl
{
    public class MenuDefinitionAttribute : Attribute
    {
        public int Order
        {
            get;
            private set;
        }

        public MenuDefinitionAttribute(int order = 0)
        {
            Order = order;
        }
    }


    public class MenuCommandAttribute : Attribute
    {
        public string Path
        {
            get;
            private set;
        }

        public string IconPath
        {
            get;
            private set;
        }

        public bool Validate
        {
            get;
            private set;
        }

        public bool Hide
        {
            get;
            private set;
        }

        public int Priority
        {
            get;
            private set;
        }

        public bool RequiresInstance
        {
            get;
            private set;
        }

        public MenuCommandAttribute(string path, bool validate = false, bool hide = false, int priority = int.MaxValue, bool requiresInstance = false)
        {
            Path = path;
            Validate = validate;
            Hide = hide;
            Priority = priority;
            RequiresInstance = requiresInstance;
        }

        public MenuCommandAttribute(string path, string iconPath, bool requiresInstance = false)
        {
            Path = path;
            Validate = false;
            IconPath = iconPath;
            Priority = int.MaxValue;
            RequiresInstance = requiresInstance;
        }

	public MenuCommandAttribute(string path, string iconPath, int priority, bool requiresInstance = false)
        {
            Path = path;
            Validate = false;
            IconPath = iconPath;
            Priority = priority;
            RequiresInstance = requiresInstance;
        }
    }

    [DefaultExecutionOrder(-25)]
    public class MenuCreator : MonoBehaviour
    {
   
        [SerializeField]
        private GameObject m_topMenu = null;

        [SerializeField]
        private GameObject m_menuPanel = null;

        [SerializeField]
        private MainMenuButton m_menuButtonPrefab = null;

        [SerializeField]
        private Menu m_menuPrefab = null;

        private class MenuItemWithPriority
        {
            public MenuItemInfo Info;
            public int Priority;

            public MenuItemWithPriority(MenuItemInfo menuItemInfo, int priority)
            {
                Info = menuItemInfo;
                Priority = priority;
            }

            public MenuItemWithPriority()
            {
                Info = new MenuItemInfo();
                Priority = int.MaxValue;
            }
        }

        private void Awake()
        {
            List<Assembly> assemblies = new List<Assembly>();
            foreach(string assemblyName in BHRoot.Assemblies)
            {
                var asName = new AssemblyName();
                asName.Name = assemblyName;
                try
                {
                    Assembly asm = Assembly.Load(asName);
                    assemblies.Add(asm);
                }
                catch(Exception e)
                {
                    Debug.LogWarning(e.ToString());
                }
            }

            if (m_menuPanel == null)
            {
                m_menuPanel = gameObject;
            }

            if(m_topMenu == null)
            {
                m_topMenu = gameObject;
            }

            if(m_menuButtonPrefab == null)
            {
                Debug.LogError("Set Menu Button Prefab");
                return;
            }

            if(m_menuPrefab == null)
            {
                Debug.LogError("Set Menu Prefab");
                return;
            }

            bool wasButtonPrefabActive = m_menuButtonPrefab.gameObject.activeSelf;
            bool wasMenuPrefabActive = m_menuPrefab.gameObject.activeSelf;

            Dictionary<string, Menu> menuDictionary = new Dictionary<string, Menu>();
            Dictionary<string, List<MenuItemWithPriority>> menuItemsDictionary = new Dictionary<string, List<MenuItemWithPriority>>();
            Menu[] menus = m_menuPanel.GetComponentsInChildren<Menu>(true);
            for(int i = 0; i < menus.Length; ++i)
            {
                if(!menuDictionary.ContainsKey(menus[i].name))
                {
                    menuDictionary.Add(menus[i].name, menus[i]);

                    if(menus[i].Items != null)
                    {
                        List<MenuItemWithPriority> menuItemsWithPriority = new List<MenuItemWithPriority>();
                        for(int priority = 0; priority < menus[i].Items.Length; ++priority)
                        {
                            MenuItemInfo menuItemInfo = menus[i].Items[priority];
                            menuItemsWithPriority.Add(new MenuItemWithPriority(menuItemInfo, priority));
                        }
                        menuItemsDictionary.Add(menus[i].name, menuItemsWithPriority);
                    }
                    else
                    {
                        menuItemsDictionary.Add(menus[i].name, new List<MenuItemWithPriority>());
                    }
                }
            }

            Type[] menuDefinitions = assemblies.SelectMany(asm => asm.GetTypesWithAttribute(typeof(MenuDefinitionAttribute))).OrderBy(kvp => ((MenuDefinitionAttribute)kvp.Value).Order).Select(kvp => kvp.Key).ToArray();
            foreach (Type menuDef in menuDefinitions)
            {
                MethodInfo[] methods = menuDef.GetMethods(BindingFlags.Static | BindingFlags.Public);
                for (int i = 0; i < methods.Length; ++i)
                {
                    MethodInfo mi = methods[i];
                    MenuCommandAttribute cmd = (MenuCommandAttribute)mi.GetCustomAttributes(typeof(MenuCommandAttribute), true).FirstOrDefault();
                    if (cmd == null || string.IsNullOrEmpty(cmd.Path))
                    {
                        continue;
                    }

                    if(cmd.RequiresInstance)
                    {
                        if(FindObjectOfType(menuDef) == null)
                        {
                            continue;
                        }
                    }

                    string[] pathParts = cmd.Path.Split('/');
                    if (pathParts.Length < 1)
                    {
                        continue;
                    }

                    string menuName = pathParts[0];

                    if (!menuDictionary.ContainsKey(menuName))
                    {
                        MainMenuButton btn = _CreateMenu(menuName);

                        menuDictionary.Add(menuName, btn.Menu);
                        menuItemsDictionary.Add(menuName, new List<MenuItemWithPriority>());
                    }

                    if (pathParts.Length == 1)
                    {
                        if (cmd.Hide)
                        {
                            menuItemsDictionary[menuName].Clear();
                        }
                    }
                    
                    if (pathParts.Length < 2)
                    {
                        continue;
                    }

                    string path = string.Join("/", pathParts.Skip(1));
                    List<MenuItemWithPriority> menuItems = menuItemsDictionary[menuName];
                    MenuItemWithPriority menuItem = menuItems.Where(item => item.Info.Path == path).FirstOrDefault();
                    if (menuItem == null)
                    {
                        menuItem = new MenuItemWithPriority();
                        menuItems.Add(menuItem);
                    }

                    menuItem.Info.Path = string.Join("/", pathParts.Skip(1));
                    menuItem.Info.Icon = !string.IsNullOrEmpty(cmd.IconPath) ? Resources.Load<Sprite>(cmd.IconPath) : null;
                    menuItem.Info.Text = pathParts.Last();

                    if(cmd.Validate)
                    {
                        Func<bool> validate = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), mi, false);
                        if(validate == null)
                        {
                            Debug.LogWarning("method signature is invalid. bool Func() is expected. " + string.Join("/", pathParts));
                        }
                        else
                        {
                            menuItem.Info.Validate = new MenuItemValidationEvent();
                            menuItem.Info.Validate.AddListener(new UnityAction<MenuItemValidationArgs>(args => args.IsValid = validate()));
                        }
                    }
                    else
                    {
                        Action action = (Action)Delegate.CreateDelegate(typeof(Action), mi, false);
                        if (action == null)
                        {
                            Debug.LogWarning("method signature is invalid. void Action() is expected. " + string.Join("/", pathParts));
                        }
                        else
                        {
                            menuItem.Info.Action = new MenuItemEvent();
                            menuItem.Info.Action.AddListener(new UnityAction<string>(args => action()));
                        }
                    }

                    if(cmd.Hide)
                    {
                        menuItems.Remove(menuItem);
                    }

                    menuItem.Priority = cmd.Priority;
                }

                m_menuPrefab.gameObject.SetActive(wasMenuPrefabActive);
                m_menuButtonPrefab.gameObject.SetActive(wasButtonPrefabActive);
            }

            foreach(KeyValuePair<string, List<MenuItemWithPriority>> kvp in menuItemsDictionary)
            {
                menuDictionary[kvp.Key].SetMenuItems(kvp.Value.OrderBy(m => m.Priority).Select(m => m.Info).ToArray(), false);
            }
        }

        public MainMenuButton CreateMenu(string menuName)
        {
            bool wasButtonPrefabActive = m_menuButtonPrefab.gameObject.activeSelf;
            bool wasMenuPrefabActive = m_menuPrefab.gameObject.activeSelf;

            m_menuButtonPrefab.gameObject.SetActive(false);
            m_menuPrefab.gameObject.SetActive(false);
            MainMenuButton result = _CreateMenu(menuName);

            m_menuPrefab.gameObject.SetActive(wasMenuPrefabActive);
            m_menuButtonPrefab.gameObject.SetActive(wasButtonPrefabActive);
            return result;
        }

        private MainMenuButton _CreateMenu(string menuName)
        {
            Menu menu = Instantiate(m_menuPrefab, m_menuPanel.transform, false);
            menu.Items = null;

            m_menuButtonPrefab.gameObject.SetActive(false);

            MainMenuButton btn = Instantiate(m_menuButtonPrefab, m_topMenu.transform, false);
            btn.name = menuName;
            btn.Text = menuName;
            btn.Menu = menu;

            Text txt = btn.GetComponentInChildren<Text>(true);
            if (txt != null)
            {
                txt.text = menuName;
            }

            btn.gameObject.SetActive(true);
            return btn;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bee.Core;
using Bee.Util;
using System.Reflection;
using System.Web;
using System.Collections.Specialized;
using Bee.Logging;

namespace Bee.Web
{
    public class ControllerInfo
    {
        public readonly string Name;
        public readonly string LowerName;
        public readonly Type Type;
        public readonly string DefaultAction;
        public readonly bool DefaultFlag;

        public ControllerInfo(Type type)
        {
            try
            {
                this.Type = type;
                this.Name = GetControllerName();

                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(type);

                BeeControllerAttribute beeControllerAttribute = entityProxy.GetCustomerAttribute<BeeControllerAttribute>();
                if (beeControllerAttribute != null)
                {
                    DefaultFlag = beeControllerAttribute.DefaultFlag;

                    if (!string.IsNullOrEmpty(beeControllerAttribute.ControllerName))
                    {
                        if (!string.IsNullOrEmpty(beeControllerAttribute.AreaName))
                        {
                            this.Name = "{0}|{1}".FormatWith(beeControllerAttribute.AreaName, beeControllerAttribute.ControllerName);
                        }
                        else
                        {
                            this.Name = beeControllerAttribute.ControllerName;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(beeControllerAttribute.AreaName))
                        {
                            this.Name = "{0}|{1}".FormatWith(beeControllerAttribute.AreaName, this.Name);
                        }
                    }
                }
                
                this.LowerName = Name.ToLower();

                DefaultAction = "Index";
                foreach (MethodSchema item in entityProxy.GetMethodList())
                {
                    ActionAttribute actionAttribute = item.GetCustomerAttribute<ActionAttribute>();
                    if (actionAttribute != null && actionAttribute.DefaultFlag)
                    {
                        DefaultAction = item.Name;
                        break;
                    }
                }

                // 构造一个Controller实例， 以初始化类静态构造函数。
                //ReflectionUtil.CreateInstance(type);
                entityProxy.CreateInstance();
            }
            catch (Exception e)
            {
                throw new CoreException("type:{0}".FormatWith(type), e);
            }

        }

        internal object CreateInstance()
        {
            IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(Type);
            return entityProxy.CreateInstance();
        }

        internal object Invoke(BeeControllerBase instance, string methodName, BeeDataAdapter dataAdapter)
        {
            IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(Type);

            return entityProxy.Invoke(instance, methodName, dataAdapter);
        }

        private string GetControllerName()
        {
            var name = Type.Name;
            if (name.EndsWith("Controller"))
            {
                return name.Substring(0, name.Length - 10);
            }
            return name;
        }
    }


    internal sealed class ControllerManager
    {
        private static Dictionary<string, ControllerInfo> ControllerDict;
        private static readonly Type CbType = typeof(BeeControllerBase);

        private static ControllerManager instance = new ControllerManager();

        private ControllerManager()
        {
            Init();
        }

        public static ControllerManager Instance
        {
            get
            {
                return instance;
            }
        }

        public static string DefaultControllerName
        {
            get;
            set;
        }

        private void Init()
        {
            lock (CbType)
            {
                ControllerDict =
                    new Dictionary<string, ControllerInfo>(StringComparer.InvariantCultureIgnoreCase);

                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        string s = a.FullName.Split(',')[0];
                        if (!s.StartsWith("System.") && CouldBeControllerAssemebly(s))
                        {
                            SearchControllers(a);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("assembly loaded fail.{0}".FormatWith(a.FullName), e);
                    }
                }
            }
        }

        public ControllerInfo GetControllerInfo(string controllerName)
        {
            ControllerInfo result = null;
            if (ControllerDict.ContainsKey(controllerName))
            {
                result = ControllerDict[controllerName];
            }

            return result;
        }

        private static void SearchControllers(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsInterface && !type.IsAbstract && type.IsSubclassOf(CbType)
                    && !type.IsGenericType
                    && type.Name.EndsWith("Controller"))
                {
                    var ci = new ControllerInfo(type);

                    if (ci.DefaultFlag)
                    {
                        ControllerManager.DefaultControllerName = ci.LowerName;
                    }

                    ThrowExceptionUtil.ArgumentConditionTrue(!ControllerDict.ContainsKey(ci.LowerName),
                        string.Empty, "ControllerName: {0} has existed".FormatWith(ci.LowerName));

                    ControllerDict[ci.LowerName] = ci;
                }
            }
        }

        private static bool CouldBeControllerAssemebly(string s)
        {
            switch (s)
            {
                case "Bee.Core":
                case "mscorlib":
                case "System":
                    return false;
                default:
                    return true;
            }
        }
    }
}

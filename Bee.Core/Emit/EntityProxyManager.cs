using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.ComponentModel;
using Bee.Util;
using Bee.Data;
using Bee.Logging;
using System.Threading;

namespace Bee.Core
{
    internal class MethodParameterComparer : IComparer<MethodSchema>
    {
        public int Compare(MethodSchema x, MethodSchema y)
        {
            int result = 1;

            if (y == null)
            {
                result = -1;
            }
            else 
            {
                if (x == null)
                {
                    result = 1;
                }
                else
                {
                    result = y.ParameterInfos.Length - x.ParameterInfos.Length;
                }

            }

            if (result == 0 && x != y)
            {
                if (x != null && y != null && x.ParameterInfos.Length == 1)
                {
                    if (y.ParameterInfos[0].ParameterType == typeof(BeeDataAdapter))
                    {
                        result = 1;
                    }

                    if (x.ParameterInfos[0].ParameterType == typeof(BeeDataAdapter))
                    {
                        result = -1;
                    }
                }
            }

            return result;
        }
    }

    public abstract class MemberSchema
    {
        protected List<Attribute> list = null;
        protected MemberInfo memberInfo = null;

        public MemberSchema(MemberInfo memberInfo)
        {
            this.memberInfo = memberInfo;
        }

        public string Name { get { return memberInfo.Name; } }

        public MemberInfo MemberInfo { get { return memberInfo; } }

        /// <summary>
        /// 读取AT实例的特性
        /// </summary>
        /// <typeparam name="AT">特性类型</typeparam>
        /// <returns>特性实例</returns>
        public virtual AT GetCustomerAttribute<AT>()
            where AT : Attribute
        {
            AT result = null;
            if (list == null)
            {
                list = new List<Attribute>();
                object[] attributeArray = memberInfo.GetCustomAttributes(false);
                if (attributeArray.Length != 0)
                {
                    foreach (object item in attributeArray)
                    {
                        Attribute attribute = item as Attribute;
                        if (attribute != null)
                        {
                            list.Add(attribute);
                        }
                    }
                }
            }

            Type resultType = typeof(AT);
            foreach (Attribute item in list)
            {
                if (resultType == item.GetType())
                {
                    result = item as AT;
                }
            }

            return result;
        }

        public override string ToString()
        {
            return memberInfo.ToString();
        }
    }

    public class PropertySchema : MemberSchema
    {
        public Type PropertyType { get; set; }

        public PropertySchema(PropertyInfo propertyInfo)
            : base(propertyInfo)
        {
            this.PropertyType = propertyInfo.PropertyType;
        }
    }

    public class MethodSchema : MemberSchema
    {
        public ParameterInfo[] ParameterInfos { get; set; }
        public Type ReturnType { get; set; }
        public MethodSchema(MethodInfo methodInfo)
            : base(methodInfo)
        {
            ParameterInfos = methodInfo.GetParameters();
            ReturnType = methodInfo.ReturnType;
        }
    }

    public interface IEntityProxy
    {
        object CreateInstance();

        /// <summary>
        /// 获取实例对象的属性值
        /// </summary>
        /// <param name="entity">对象实例</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性值</returns>
        object GetPropertyValue(object entity, string propertyName);

        /// <summary>
        /// 设置实例对象的属性值
        /// </summary>
        /// <param name="entity">对象实例</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="propertyValue">属性值</param>
        void SetPropertyValue(object entity, string propertyName, object propertyValue);

        /// <summary>
        /// 动态调用方法。
        /// </summary>
        /// <param name="entity">对象实例</param>
        /// <param name="methodName">方法名</param>
        /// <param name="dataAdapter">参数名，参数值数据集</param>
        /// <returns>返回值</returns>
        object Invoke(object entity, string methodName, BeeDataAdapter dataAdapter);

        string ToXml(object entity);
        List<PropertySchema> GetPropertyList();

        PropertySchema GetProperty(string propertyName);

        //CheckMethodResult GetMethod(string methodName, BeeDataAdapter data);

        List<MethodSchema> GetMethodList();
        PropertySchema this[string propertyName]{get;}

        AT GetCustomerAttribute<AT>() where AT : Attribute;
    }

    public class EntityProxy<T> : IEntityProxy
    {
        private List<Attribute> list = null;


        public virtual object CreateInstance()
        {
            return null;
        }

        /// <summary>
        /// 获取实例对象的属性值
        /// </summary>
        /// <param name="entity">对象实例</param>
        /// <param name="propertyName">属性名</param>
        /// <returns>属性值</returns>
        public virtual object GetPropertyValue(object entity, string propertyName)
        {
            return null;
        }

        /// <summary>
        /// 设置实例对象的属性值
        /// </summary>
        /// <param name="entity">对象实例</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="propertyValue">属性值</param>
        public virtual void SetPropertyValue(object entity, string propertyName, object propertyValue)
        {
            
        }

        /// <summary>
        /// 动态调用方法。
        /// </summary>
        /// <param name="entity">对象实例</param>
        /// <param name="methodName">方法名</param>
        /// <param name="dataAdapter">参数名，参数值数据集</param>
        /// <returns>返回值</returns>
        public virtual object Invoke(object entity, string methodName, BeeDataAdapter dataAdapter)
        {
            return null;
        }

        public string ToXml(object entity)
        {
            XmlBuilder builder = new XmlBuilder();
            builder.tag(typeof(T).Name);
            foreach (PropertySchema property in GetPropertyList())
            {
                string xmlValue = string.Empty;
                object propertyValue = GetPropertyValue(entity, property.Name);
                if (propertyValue != null)
                {
                    TypeCode typeCode = Type.GetTypeCode(property.PropertyType);
                    if (property.PropertyType.IsArray)
                    {
                        xmlValue = propertyValue.ToString();
                    }
                    else if (typeCode == TypeCode.Object && !property.PropertyType.IsValueType)
                    {
                        IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(property.PropertyType);
                        xmlValue = entityProxy.ToXml(propertyValue);
                    }
                    else
                    {
                        xmlValue = propertyValue.ToString();
                    }
                }

                builder = builder.newline.tab.tag(property.Name).text(xmlValue, true).end;
            }
            builder = builder.newline.end;

            return builder.ToString();
        }

        public PropertySchema this[string propertyName]
        {
            get
            {
                ThrowExceptionUtil.ArgumentNotNullOrEmpty(propertyName, "propertyName");
                foreach (PropertySchema item in GetPropertyList())
                {
                    if (string.Compare(item.Name, propertyName, true) == 0)
                    {
                        return item;
                    }
                }

                return null;
            }
        }

        public List<PropertySchema> GetPropertyList()
        {
            return EntityProxyManager.Instance.GetEntityProperties(typeof(T));
        }

        public PropertySchema GetProperty(string propertyName)
        {
            PropertySchema result = null;
            foreach (PropertySchema item in GetPropertyList())
            {
                if (string.Compare(item.Name, propertyName, true) == 0)
                {
                    result = item;
                    break;
                }
            }

            return result;
        }

        public List<MethodSchema> GetMethodList()
        {
            return EntityProxyManager.Instance.GetEntityMethods(typeof(T));
        }

        /// <summary>
        /// 读取AT实例的特性
        /// </summary>
        /// <typeparam name="AT">特性类型</typeparam>
        /// <returns>特性实例</returns>
        public AT GetCustomerAttribute<AT>()
            where AT : Attribute
        {
            AT result = null;
            if (list == null)
            {
                list = new List<Attribute>();
                object[] attributeArray = typeof(T).GetCustomAttributes(false);
                if (attributeArray.Length != 0)
                {
                    foreach (object item in attributeArray)
                    {
                        Attribute attribute = item as Attribute;
                        if (attribute != null)
                        {
                            list.Add(attribute);
                        }
                    }
                }
            }

            Type resultType = typeof(AT);
            foreach (Attribute item in list)
            {
                if (resultType == item.GetType())
                {
                    result = item as AT;
                }
            }

            return result;
        }
    }

    public class EntityProxyManager : IDisposable
    {
        #region Fields

        private const string ProxyNameSpace = "BeeCoreEntityProxy";

        private static EntityProxyManager instance = new EntityProxyManager();

        private Hashtable entityProxyTable = Hashtable.Synchronized(new Hashtable());
        private Hashtable entityPropertiesTable = Hashtable.Synchronized(new Hashtable());
        private Hashtable entityMethodsTable = Hashtable.Synchronized(new Hashtable());

        private ModuleBuilder moduleBuilder;

        private AssemblyBuilder assemblyBuilder;
        private bool isDisposed;

        private string cacheDirPath;
        private static readonly object lockobject = new object();

        #endregion

        #region Constructors

        private EntityProxyManager()
        {
            cacheDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "./Cache/Bee_Core_EntityProxy/");

            if (!Directory.Exists(cacheDirPath))
            {
                Directory.CreateDirectory(cacheDirPath);
            }
            AssemblyName assembly = new AssemblyName(ProxyNameSpace);
            assembly.Version = new Version(1, 0, 0, 0);
            assemblyBuilder =
                AppDomain.CurrentDomain.DefineDynamicAssembly(assembly,
                System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave, cacheDirPath);
            

            moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule", string.Format("{0}.dll", ProxyNameSpace));
            
        }

        #endregion

        #region Properties

        public static EntityProxyManager Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion

        #region Public Methods

        public EntityProxy<T> GetEntityProxy<T>()
            where T　: class
        {
            Type type = typeof(T);
            return GetEntityProxyFromType(type) as EntityProxy<T>;
        }

        public IEntityProxy GetEntityProxyFromType(Type type)
        {
            ThrowExceptionUtil.ArgumentNotNull(type, "type");

            ThrowExceptionUtil.ArgumentConditionTrue(
                !type.IsValueType && Type.GetTypeCode(type) == TypeCode.Object,
                "type", "the type should be class. not support {0}".FormatWith(type));

            IEntityProxy result = null;
            try
            {
                lock (lockobject)
                {
                    if (!entityProxyTable.ContainsKey(type))
                    {
                        PrepareForType(type);
                    }

                    //Logger.Debug(string.Format("threadid:{0}", Thread.CurrentThread.ManagedThreadId));

                    result = entityProxyTable[type] as IEntityProxy;
                }
            }
            catch (Exception e)
            {
                throw new ArgumentException("Create Proxy Error. Type:{0}".FormatWith(type), e);
            }

            return result;
        }

        public List<PropertySchema> GetEntityProperties(Type type)
        {
            List<PropertySchema> result = null;

            lock (lockobject)
            {
                if (!entityPropertiesTable.Contains(type))
                {
                    entityPropertiesTable.Add(type, GetPropertySchemas(type));
                }

                result = entityPropertiesTable[type] as List<PropertySchema>;
            }
            return result;
        }

        public List<MethodSchema> GetEntityMethods(Type type)
        {
            //Logger.Debug(string.Format("threadid:{0}", Thread.CurrentThread.ManagedThreadId));

            List<MethodSchema> result = null;
            lock (lockobject)
            {
                if (!entityMethodsTable.Contains(type))
                {
                    result = GetMethodSchemas(type);
                    // 保证参数长的先被匹配
                    result.Sort(new MethodParameterComparer());

                    entityMethodsTable.Add(type, result);
                }
            }

            result = entityMethodsTable[type] as List<MethodSchema>;
            return result;
        }

        #endregion

        #region Internal Methods

        #endregion

        #region Private Methods

        private void PrepareForType(Type type)
        {
            Type proxyType = CreateEntityProxyType(type);
            object instance = Activator.CreateInstance(proxyType);

            entityProxyTable.Add(type, instance);
        }

        private List<PropertySchema> GetPropertySchemas(Type entityType)
        {
            List<PropertySchema> result = new List<PropertySchema>();
            PropertyInfo[] properties = 
                entityType.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.GetIndexParameters().Length == 0)
                {
                    result.Add(new PropertySchema(propertyInfo));
                }
            }

            return result;
        }

        private List<MethodSchema> GetMethodSchemas(Type entityType)
        {
            List<MethodSchema> result = new List<MethodSchema>();
            MethodInfo[] methods = entityType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (MethodInfo methodInfo in methods)
            {
                string methodName = methodInfo.Name;

                if ((string.Compare(methodName, "get_Item", false) == 0 ||
                    string.Compare(methodName, "set_Item", false) == 0) && methodInfo.GetParameters().Length != 0)
                {
                    result.Add(new MethodSchema(methodInfo));
                } 

                if (methodName.IndexOf("get_", StringComparison.InvariantCultureIgnoreCase) < 0
                    && methodName.IndexOf("set_", StringComparison.InvariantCultureIgnoreCase) < 0
                    && methodName != "ToString" && methodName != "Equals"
                    && methodName != "GetHashCode" && methodName != "GetType")
                {
                    result.Add(new MethodSchema(methodInfo));
                }
            }

            return result;
        }

        private Type CreateEntityProxyType(Type entityType)
        {
            Type interfaceType
                = typeof(EntityProxy<>).MakeGenericType(new Type[] { entityType });
            TypeBuilder typeBuilder
                = this.moduleBuilder.DefineType(string.Format("{0}.{1}Proxy", ProxyNameSpace, entityType.Name), TypeAttributes.Public, interfaceType);

            EmitConstruct(typeBuilder);

            MethodInfo baseMethod = interfaceType.GetMethod("GetPropertyValue");
            EmitGetPropertyValue(typeBuilder, baseMethod, entityType);

            baseMethod = interfaceType.GetMethod("SetPropertyValue");
            EmitSetPropertyValue(typeBuilder, baseMethod, entityType);

            baseMethod = interfaceType.GetMethod("Invoke");
            EmitInvoke(typeBuilder, baseMethod, entityType);

            baseMethod = interfaceType.GetMethod("CreateInstance");
            EmitCreateInstance(typeBuilder, baseMethod, entityType);

            return typeBuilder.CreateType();
        }

        private void EmitCreateInstance(TypeBuilder typeBuilder, MethodInfo baseMethod, Type entityType)
        {
            MethodBuilder methodInfoBody = typeBuilder.DefineMethod("CreateInstance",
                 System.Reflection.MethodAttributes.Public
                    | System.Reflection.MethodAttributes.Virtual
                    | System.Reflection.MethodAttributes.HideBySig,
                baseMethod.CallingConvention, baseMethod.ReturnType, new Type[]{});
            ConstructorInfo constructorInfo = entityType.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
                null, new Type[] { }, null);
            ILGenerator ilGenerator = methodInfoBody.GetILGenerator();
            LocalBuilder local = ilGenerator.DeclareLocal(typeof(object));
            //Label label = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Nop);
            if (constructorInfo != null)
            {
                ilGenerator.Emit(OpCodes.Newobj, constructorInfo);
                ilGenerator.Emit(OpCodes.Stloc_0);
            }

            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodInfoBody, baseMethod);
        }

        private void EmitInvoke(TypeBuilder typeBuilder, MethodInfo baseMethod, Type entityType)
        {
            Type[] parameterTypes = new Type[] { typeof(object), typeof(string), typeof(BeeDataAdapter) };

            MethodBuilder methodInfoBody = typeBuilder.DefineMethod(baseMethod.Name,
                System.Reflection.MethodAttributes.Public
                | System.Reflection.MethodAttributes.Virtual
                | System.Reflection.MethodAttributes.HideBySig,
                baseMethod.CallingConvention, baseMethod.ReturnType, parameterTypes);

            ILGenerator ilGenerator = methodInfoBody.GetILGenerator();

            MethodInfo getEntityMethodName = typeof(ReflectionUtil).GetMethod(
                ReflectionUtil.GetMemberNameByAttribute(typeof(ReflectionUtil), "GetEntityMethodName"),
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, new Type[] { typeof(Type), typeof(String), typeof(BeeDataAdapter) }, null);
            MethodInfo getItemMethod = typeof(BeeDataAdapter).GetMethod("get_Item",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new Type[]{typeof(String) }, null);
            MethodInfo equalMethod =
                typeof(string).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) });
            MethodInfo getTypeFromHandleMethodInfo =
                typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
            MethodInfo convertConvertUtil =
                typeof(ConvertUtil).GetMethod("Convert",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.InvokeMethod,
                null, new Type[] { typeof(object), typeof(Type) }, null);

            FieldInfo methodNameField = typeof(CheckMethodResult).GetField(
                ReflectionUtil.GetMemberNameByAttribute(typeof(CheckMethodResult), "MethodName"),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo dataAdapterField = typeof(CheckMethodResult).GetField(
                ReflectionUtil.GetMemberNameByAttribute(typeof(CheckMethodResult), "DataAdapter"),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            LocalBuilder checkMethodResult = ilGenerator.DeclareLocal(typeof(CheckMethodResult));
            LocalBuilder matchedMethodName = ilGenerator.DeclareLocal(typeof(String));
            LocalBuilder filteredDataAdapter = ilGenerator.DeclareLocal(typeof(BeeDataAdapter));
            LocalBuilder returnObject = ilGenerator.DeclareLocal(typeof(Object));
            LocalBuilder localEntity = ilGenerator.DeclareLocal(entityType);

            List<MethodSchema> methods
                = this.GetEntityMethods(entityType);

            Label loc = ilGenerator.DefineLabel();
            Label label = ilGenerator.DefineLabel();
            Label[] labelArray = new Label[methods.Count + 1];
            for (int num = 0; num < methods.Count; num++)
            {
                labelArray[num] = ilGenerator.DefineLabel();
            }
            labelArray[methods.Count] = loc;

            ilGenerator.Emit(OpCodes.Nop);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            if (entityType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, entityType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Castclass, entityType);

            }
            ilGenerator.Emit(OpCodes.Stloc_S, 4);

            ilGenerator.Emit(OpCodes.Ldtoken, entityType);
            ilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethodInfo);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Ldarg_3);
            ilGenerator.Emit(OpCodes.Call, getEntityMethodName);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldfld, methodNameField);
            ilGenerator.Emit(OpCodes.Stloc_1);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ldfld, dataAdapterField);
            ilGenerator.Emit(OpCodes.Stloc_2);

            for(int num = 0 ; num < methods.Count; num++)
            {
                MethodSchema methodSchema = methods[num];
                MethodInfo methodInfo = methodSchema.MemberInfo as MethodInfo;
                
                // 如果有ref out 则忽略
                bool validFlag = true;
                foreach (ParameterInfo parameterInfo in methodSchema.ParameterInfos)
                {
                    if (parameterInfo.IsOut)
                    {
                        validFlag = false;
                        break;
                    }
                }
                if (!validFlag) continue;

                ilGenerator.MarkLabel(labelArray[num]);

                ilGenerator.Emit(OpCodes.Ldloc_1);
                string name = methodInfo.ToString();
                ilGenerator.Emit(OpCodes.Ldstr, name);
                ilGenerator.Emit(OpCodes.Call, equalMethod);
                ilGenerator.Emit(OpCodes.Brfalse, labelArray[num + 1]);
                ilGenerator.Emit(OpCodes.Nop);

                // Invoke
                ilGenerator.Emit(OpCodes.Ldloc_S, 4);

                foreach (ParameterInfo parameterInfo in methodSchema.ParameterInfos)
                {
                    ilGenerator.Emit(OpCodes.Ldloc_2);
                    ilGenerator.Emit(OpCodes.Ldstr, parameterInfo.Name);
                    ilGenerator.Emit(OpCodes.Call, getItemMethod);
                    ilGenerator.Emit(OpCodes.Ldtoken, parameterInfo.ParameterType);
                    ilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethodInfo);
                    ilGenerator.Emit(OpCodes.Call, convertConvertUtil);

                    if (parameterInfo.ParameterType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, parameterInfo.ParameterType);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Castclass, parameterInfo.ParameterType);
                    }
                }

                ilGenerator.Emit(OpCodes.Call, methodInfo);

                if (methodInfo.ReturnType == typeof(void))
                {
                    ilGenerator.Emit(OpCodes.Ldnull);
                    ilGenerator.Emit(OpCodes.Stloc_3);
                }
                else if (methodInfo.ReturnType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, methodInfo.ReturnType);
                    ilGenerator.Emit(OpCodes.Stloc_3);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Stloc_3);
                }


                ilGenerator.Emit(OpCodes.Br, label);
            }

            ilGenerator.MarkLabel(loc);
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Stloc_3);
            ilGenerator.Emit(OpCodes.Br, label);
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ldloc_3);
            ilGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodInfoBody, baseMethod);

        }

        private void EmitGetPropertyValue(TypeBuilder typeBuilder, MethodInfo baseMethod, Type entityType)
        {
            Type[] parameterTypes = new Type[] { typeof(object), typeof(string) };
            MethodBuilder methodInfoBody = typeBuilder.DefineMethod("GetPropertyValue", 
                 System.Reflection.MethodAttributes.Public
                    | System.Reflection.MethodAttributes.Virtual
                    | System.Reflection.MethodAttributes.HideBySig,
                baseMethod.CallingConvention, baseMethod.ReturnType, parameterTypes);
            MethodInfo method = typeof(string).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) });
            ILGenerator ilGenerator = methodInfoBody.GetILGenerator();
            ilGenerator.DeclareLocal(typeof(object));
            LocalBuilder localEntity = ilGenerator.DeclareLocal(entityType);
            ilGenerator.Emit(OpCodes.Nop);
            List<PropertySchema> properties
                = this.GetEntityProperties(entityType);
            Label loc = ilGenerator.DefineLabel();
            Label label = ilGenerator.DefineLabel();
            Label[] labelArray = new Label[properties.Count + 1];
            for (int num = 0; num < properties.Count; num++)
            {
                labelArray[num] = ilGenerator.DefineLabel();
            }
            labelArray[properties.Count] = loc;

            MethodInfo toLowerMethodInfo = typeof(string).GetMethod("ToLower", new Type[0]);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            if (entityType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, entityType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Castclass, entityType);

            }
            ilGenerator.Emit(OpCodes.Stloc_1);

            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Call, toLowerMethodInfo);
            ilGenerator.Emit(OpCodes.Starg_S, 2);

            for (int num = 0; num < properties.Count; num++)
            {
                PropertyInfo propertyInfo = properties[num].MemberInfo as PropertyInfo;
                ilGenerator.MarkLabel(labelArray[num]);

                ilGenerator.Emit(OpCodes.Ldarg_2);
                string propertyName = propertyInfo.Name;
                string name = propertyName.ToLower();
                ilGenerator.Emit(OpCodes.Ldstr, name);
                ilGenerator.Emit(OpCodes.Call, method);
                ilGenerator.Emit(OpCodes.Brfalse, labelArray[num + 1]);
                ilGenerator.Emit(OpCodes.Nop);

                ilGenerator.Emit(OpCodes.Ldloc_1);
                MethodInfo methodInfo = entityType.GetMethod("get_" + propertyName, new Type[0]);
                ilGenerator.Emit(OpCodes.Call, methodInfo);
                if (propertyInfo.PropertyType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, propertyInfo.PropertyType);
                }
                ilGenerator.Emit(OpCodes.Stloc_0);


                ilGenerator.Emit(OpCodes.Br, label);
            }
            ilGenerator.MarkLabel(loc);
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Stloc_0);
            ilGenerator.Emit(OpCodes.Br, label);
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodInfoBody, baseMethod);

        }

        private void EmitSetPropertyValue(TypeBuilder typeBuilder, MethodInfo baseMethod, Type entityType)
        {
            Type[] parameterTypes = new Type[] { typeof(object), typeof(string), typeof(object) };

            int num;
            MethodBuilder methodInfoBody = typeBuilder.DefineMethod("SetPropertyValue", 
                System.Reflection.MethodAttributes.Public
                | System.Reflection.MethodAttributes.Virtual
                | System.Reflection.MethodAttributes.HideBySig,
                baseMethod.CallingConvention, baseMethod.ReturnType, parameterTypes);
            MethodInfo method = 
                typeof(string).GetMethod("op_Equality", new Type[] { typeof(string), typeof(string) });
            MethodInfo toLowerMethodInfo = 
                typeof(string).GetMethod("ToLower", new Type[0]);
            MethodInfo getTypeFromHandleMethodInfo = 
                typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
            MethodInfo convertConvertUtil = 
                typeof(ConvertUtil).GetMethod("Convert", 
                BindingFlags.Public | BindingFlags.NonPublic|BindingFlags.Static | BindingFlags.InvokeMethod, 
                null, new Type[] { typeof(object), typeof(Type) }, null);

            ILGenerator ilGenerator = methodInfoBody.GetILGenerator();

            LocalBuilder localEntity = ilGenerator.DeclareLocal(entityType);

            Label label = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Nop);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            if (entityType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, entityType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Castclass, entityType);

            }
            ilGenerator.Emit(OpCodes.Stloc_0);

            //参数tolower
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Call, toLowerMethodInfo);
            ilGenerator.Emit(OpCodes.Starg_S, 2);

            // If the parameter value is null and equal DbNull, return.
            ilGenerator.Emit(OpCodes.Ldarg_3);
            ilGenerator.Emit(OpCodes.Brfalse, label);
            FieldInfo fieldInfo = typeof(DBNull).GetField("Value");
            ilGenerator.Emit(OpCodes.Ldarg_3);
            ilGenerator.Emit(OpCodes.Ldsfld, fieldInfo);
            ilGenerator.Emit(OpCodes.Ceq);
            ilGenerator.Emit(OpCodes.Brtrue, label);

            List<PropertySchema> properties = new List<PropertySchema>();
            foreach (PropertySchema propertySchema in GetEntityProperties(entityType))
            {
                if (((PropertyInfo)propertySchema.MemberInfo).CanWrite)
                {
                    properties.Add(propertySchema);
                }
            }

            Label[] labelArray = new Label[properties.Count + 1];
            for (num = 0; num < properties.Count; num++)
            {
                labelArray[num] = ilGenerator.DefineLabel();
            }
            labelArray[properties.Count] = label;
            for (num = 0; num < properties.Count; num++)
            {
                PropertyInfo propertyInfo = properties[num].MemberInfo as PropertyInfo;
                if (propertyInfo.CanWrite)
                {
                    ilGenerator.MarkLabel(labelArray[num]);
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    string propertyName = propertyInfo.Name;
                    string name = propertyName.ToLower();
                    ilGenerator.Emit(OpCodes.Ldstr, name);
                    ilGenerator.Emit(OpCodes.Call, method);
                    ilGenerator.Emit(OpCodes.Brfalse, labelArray[num + 1]);
                    ilGenerator.Emit(OpCodes.Nop);

                    MethodInfo setMethodInfo = entityType.GetMethod("set_" + propertyName, new Type[] { propertyInfo.PropertyType });
                    
                    //TypeCode typeCode = Type.GetTypeCode(propertyInfo.PropertyType);
                    ilGenerator.Emit(OpCodes.Ldloc_0);
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                    ilGenerator.Emit(OpCodes.Ldtoken, propertyInfo.PropertyType);
                    ilGenerator.Emit(OpCodes.Call, getTypeFromHandleMethodInfo);
                    ilGenerator.Emit(OpCodes.Call, convertConvertUtil);

                    if (propertyInfo.PropertyType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
                    }
                    
                    
                    ilGenerator.Emit(OpCodes.Call, setMethodInfo);

                    ilGenerator.Emit(OpCodes.Ret);

                }
            }
            ilGenerator.MarkLabel(label);
            ilGenerator.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(methodInfoBody, baseMethod);
        }

        private void EmitConstruct(TypeBuilder typeBuilder)
        {
            ConstructorBuilder constructorBuilder
                = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);

            constructorBuilder.GetILGenerator().Emit(OpCodes.Ret);
        }

        #endregion

        #region IDisposable 成员

        private void Dispose(bool disposing)
        {
            try
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                }
                if (assemblyBuilder != null)
                {
                    assemblyBuilder.Save(ProxyNameSpace + ".dll");
                }
                assemblyBuilder = null;
            }
            catch { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EntityProxyManager()
        {
            Dispose(false);
        }

        #endregion
    }
}

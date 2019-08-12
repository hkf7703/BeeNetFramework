using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Bee.Core;
using Bee.Data;
using Bee.Logging;
using System.IO;
using System.CodeDom.Compiler;

namespace Bee.Util
{
    internal class CheckMethodResult
    {
        [Secure("MethodName")]
        public string MethodName;
        [Secure("DataAdapter")]
        public BeeDataAdapter DataAdapter;
    }

    internal class SecureAttribute : Attribute
    {
        public string MethodName;

        public SecureAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }

    /// <summary>
    /// The Util of the reflection.
    /// </summary>
    public static class ReflectionUtil
    {
        public static readonly BindingFlags InstanceFlag;
        public static readonly object lockobject = new object();

        static ReflectionUtil()
        {
            InstanceFlag = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        }

        /// <summary>
        /// Check the type is <typeparamref name="Nullable<>"/>
        /// </summary>
        /// <param name="type">the type.</param>
        /// <returns>true, if the type is subclass of the <typeparamref name="Nullable<>"/></returns>
        public static bool IsNullableType(Type type)
        {
            ThrowExceptionUtil.ArgumentNotNull(type, "type");
            return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Nullable<>)));
        }

        /// <summary>
        /// Provided the method to copy the value from src to target via map.
        /// if the mapping is null or empty, so use the property name mapping.
        /// </summary>
        /// <param name="src">the source data.</param>
        /// <param name="target">the target data.</param>
        /// <param name="map">the property mapping.</param>
        public static void CopyProperty(object src, object target, Dictionary<string, string> mapping)
        {
            ThrowExceptionUtil.ArgumentNotNull(src, "src");
            ThrowExceptionUtil.ArgumentNotNull(target, "target");

            IEntityProxy srcProxy = EntityProxyManager.Instance.GetEntityProxyFromType(src.GetType());
            IEntityProxy targetProxy = EntityProxyManager.Instance.GetEntityProxyFromType(target.GetType());

            if (mapping != null)
            {
                foreach (string item in mapping.Keys)
                {
                    targetProxy.SetPropertyValue(target, mapping[item], srcProxy.GetPropertyValue(src, item));
                }
            }
            else
            {
                List<PropertySchema> list = srcProxy.GetPropertyList();
                foreach (PropertySchema item in list)
                {
                    targetProxy.SetPropertyValue(target, item.Name, srcProxy.GetPropertyValue(src, item.Name));
                }
            }

        }

        /// <summary>
        /// Set the property value of the instance.
        /// </summary>
        /// <typeparam name="T">the type of instance.</typeparam>
        /// <param name="src">the instance.</param>
        /// <param name="propertyName">the property name. ignore the case sensitive.</param>
        /// <param name="propertyValue">the property value.</param>
        public static void SetPropertyValue<T>(T src, string propertyName, object propertyValue)
            where T : class
        {
            EntityProxy<T> proxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            proxy.SetPropertyValue(src, propertyName, propertyValue);
        }

        /// <summary>
        /// Get the property value of the instance.
        /// </summary>
        /// <typeparam name="T">the type of instance.</typeparam>
        /// <param name="src">the instance.</param>
        /// <param name="propertyName">the property name. ignore the case sensitive.</param>
        public static object GetPropertyValue<T>(T src, string propertyName)
            where T : class
        {
            EntityProxy<T> proxy = EntityProxyManager.Instance.GetEntityProxy<T>();
            return proxy.GetPropertyValue(src, propertyName);
        }

        public static T[] GetAttributes<T>(this Type info, bool inherit) where T : Attribute
        {
            return (T[])info.GetCustomAttributes(typeof(T), inherit);
        }

        public static T[] GetAttributes<T>(this PropertyInfo info, bool inherit) where T : Attribute
        {
            return (T[])info.GetCustomAttributes(typeof(T), inherit);
        }

        public static T GetAttribute<T>(this Type info) where T : Attribute
        {
            return info.GetAttribute<T>(false);
        }

        public static T GetAttribute<T>(this Type info, bool inherit) where T : Attribute
        {
            T[] attributes = info.GetAttributes<T>(inherit);
            if ((attributes != null) && (attributes.Length > 0))
            {
                return attributes[0];
            }
            return default(T);
        }

        public static T GetAttribute<T>(this PropertyInfo info) where T : Attribute
        {
            return info.GetAttribute<T>(false);
        }

        public static T GetAttribute<T>(this PropertyInfo info, bool inherit) where T : Attribute
        {
            T[] attributes = info.GetAttributes<T>(inherit);
            if ((attributes != null) && (attributes.Length > 0))
            {
                return attributes[0];
            }
            return default(T);
        }


        #region CreateInstance

        public static T CreateInstance<T>()
        {
            return (T)CreateInstance(typeof(T));
        }

        public static object CreateInstance(string className)
        {
            return CreateInstance(Type.GetType(className, true));
        }

        public static object CreateInstance(string className, params object[] os)
        {
            return CreateInstance(Type.GetType(className, true, true), os);
        }

        public static object CreateInstance(Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }
            else
            {
                return CreateInstance(t, new object[0]);
            }
        }

        public static T CreateInstance<T>(params object[] os)
        {
            return (T)CreateInstance(typeof(T), os);
        }

        public static object CreateInstance(Type t, params object[] os)
        {
            Type[] typesByObjs = GetTypesByObjs(os);
            return t.GetConstructor(InstanceFlag, null, typesByObjs, null).Invoke(os);
        }

        #endregion


        public static Assembly CompileSource(string source, List<string> references)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Scripts\");
            IOUtil.SafeCreateDirectory(path);
            File.WriteAllText(@"{1}{0}.cs".FormatWith(DateTime.Now.ToString("yyMMddhhmmss"), path), source);

            //string oldDirectory = Directory.GetCurrentDirectory();
            //FileInfo fileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            //Directory.SetCurrentDirectory(fileInfo.Directory.FullName);
            Assembly generatedAssembly = null;
            CodeDomProvider codeProvider = new Microsoft.CSharp.CSharpCodeProvider();

            CompilerParameters compilerParameters = new CompilerParameters();

            compilerParameters.CompilerOptions = "/target:library /optimize";
            compilerParameters.GenerateExecutable = false;
            compilerParameters.GenerateInMemory = true;
            compilerParameters.ReferencedAssemblies.Add("System.Data.dll");
            compilerParameters.ReferencedAssemblies.Add("System.dll");
            compilerParameters.ReferencedAssemblies.Add("System.Xml.dll");
            compilerParameters.ReferencedAssemblies.Add("Bee.Core.dll");

            if (references != null && references.Count > 0)
            {
                compilerParameters.ReferencedAssemblies.AddRange(references.ToArray());
            }

            CompilerResults compilerResults;
            try
            {
                compilerResults =
                   codeProvider.CompileAssemblyFromSource(compilerParameters, source);
            }
            catch (NotImplementedException ex)
            {
                throw new CoreException("Occurs errors when compiling.", ex);
            }

            //Directory.SetCurrentDirectory(oldDirectory);

            if (compilerResults.Errors.Count > 0)
            {
                StringBuilder stringBuilder = new StringBuilder();

                foreach (CompilerError compilerError in compilerResults.Errors)
                {
                    stringBuilder.Append(string.Format("第{0}行 第{1}列 出错了：{2}",
                        compilerError.Line, compilerError.Column, compilerError.ErrorText));
                }

                throw new CoreException("compile-errors", new ApplicationException(stringBuilder.ToString()));
            }
            else
            {
                generatedAssembly = compilerResults.CompiledAssembly;
            }
            return generatedAssembly;
        }

        internal static string GetMemberNameByAttribute(Type type, string methodName)
        {
            string result = string.Empty;
            foreach (MemberInfo memberInfo in type.GetMembers(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
            {
                SecureAttribute[] attributes = memberInfo.GetCustomAttributes(typeof(SecureAttribute), false)
                    as SecureAttribute[];
                if (attributes != null && attributes.Length == 1)
                {
                    if (string.Compare(attributes[0].MethodName, methodName, true) == 0)
                    {

                        result = memberInfo.Name;
                        break;
                    }
                }
            }

            return result;
        }

        [SecureAttribute("GetEntityMethodName")]
        internal static CheckMethodResult GetEntityMethodName(Type type, string methodName, BeeDataAdapter dataAdapter)
        {
            ThrowExceptionUtil.ArgumentNotNull(type, "type");
            ThrowExceptionUtil.ArgumentNotNullOrEmpty(methodName, "methodName");

            if (dataAdapter == null)
            {
                dataAdapter = new BeeDataAdapter();
            }

            CheckMethodResult result = new CheckMethodResult();

            MethodSchema methodSchema = null;
            List<MethodSchema> list = null;
            //lock (lockobject)
            {
                // 保证参数长的先被匹配
                // 该方法本身就排过序了
                list = EntityProxyManager.Instance.GetEntityMethods(type);
               
                foreach (MethodSchema item in list)
                {
                   
                    // Check the name of the method.
                    if (string.Compare(item.Name, methodName, true) == 0)
                    {
                        if (CheckMethod(item, methodName, dataAdapter, out result.DataAdapter))
                        {
                            methodSchema = item;
                            break;
                        }
                    }
                }
            }

            if (methodSchema != null)
            {
                result.MethodName = methodSchema.MemberInfo.ToString();
            }
            else
            {
                CoreException exception = new CoreException("Can not match a method for {0}.{1}\r\n".FormatWith(type.Name, methodName));
                exception.ErrorCode = ErrorCode.MVCNoAction;
                throw exception;
            }
            return result;
        }

        #region Private Methods

        private static bool CheckMethod(MethodSchema methodSchema, string methodName
            , BeeDataAdapter dataAdapter, out BeeDataAdapter filteredData)
        {
            bool result = false;
            filteredData = new BeeDataAdapter();

            bool flag = true;
            List<ParameterInfo> customerTypeParaList = new List<ParameterInfo>();
            List<ParameterInfo> simpleTypeParaList = new List<ParameterInfo>();

            foreach (ParameterInfo parameterInfo in methodSchema.ParameterInfos)
            {
                if (Type.GetTypeCode(parameterInfo.ParameterType) == TypeCode.Object
                    && parameterInfo.ParameterType != typeof(Guid))
                {
                    customerTypeParaList.Add(parameterInfo);
                }
                else
                {
                    simpleTypeParaList.Add(parameterInfo);
                }
            }
            foreach (ParameterInfo parameterInfo in simpleTypeParaList)
            {
                // check the simple parameters name
                if (!dataAdapter.ContainsKey(parameterInfo.Name))
                {
                    flag = false;
                    break;
                }
            }

            if (flag)
            {
                foreach (ParameterInfo parameterInfo in simpleTypeParaList)
                {
                    filteredData.Add(parameterInfo.Name, dataAdapter[parameterInfo.Name]);
                }

                if (customerTypeParaList.Count == 0)
                {
                    result = true;
                }
                else
                {
                    bool allParameterFlag = true; 
                    foreach (ParameterInfo parameterInfo in customerTypeParaList)
                    {
                        object dataValue = dataAdapter[parameterInfo.Name];
                        if (dataValue == null || parameterInfo.ParameterType != dataValue.GetType())
                        {
                            allParameterFlag = false;
                        }
                    }

                    if (allParameterFlag)
                    {
                        result = true;
                    }
                    else if (customerTypeParaList.Count == 1)
                    {
                        // try to match if possible
                        foreach (ParameterInfo parameterInfo in customerTypeParaList)
                        {
                            if (parameterInfo.ParameterType == typeof(BeeDataAdapter))
                            {
                                //dataAdapter.RemoveEmptyOrNull();
                                filteredData.Add(parameterInfo.Name, dataAdapter);
                            }
                            else
                            {
                                filteredData.Add(parameterInfo.Name,
                                    ConvertUtil.ConvertDataToObject(parameterInfo.ParameterType, dataAdapter));
                                //dataAdapter.RemoveEmptyOrNull();
                            }
                        }

                        result = true;
                    }
                    else
                    {
                        // do nothing here.
                    }

                }
            }

            return result;
        }

        private static Type[] GetTypesByObjs(params object[] os)
        {
            Type[] typeArray = new Type[os.Length];
            for (int i = 0; i < os.Length; i++)
            {
                typeArray[i] = os[i].GetType();
            }
            return typeArray;
        }

        #endregion

    }
}

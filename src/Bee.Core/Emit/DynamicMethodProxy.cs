using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Bee.Core
{
    /// <summary>
    /// Delegate for calling static method
    /// </summary>
    /// <param name="paramObjs">The parameters passing to the invoking method.</param>
    /// <returns>The return value.</returns>
    public delegate object StaticDynamicMethodProxyDelegate(params object[] paramObjs);

    /// <summary>
    /// Delegate for calling non-static method
    /// </summary>
    /// <param name="ownerInstance">The object instance owns the invoking method.</param>
    /// <param name="paramObjs">The parameters passing to the invoking method.</param>
    /// <returns>The return value.</returns>
    public delegate object DynamicMethodProxyDelegate(object ownerInstance, params object[] paramObjs);

    public class DynamicMethodProxyFactory
    {
        #region Helper Methods

        protected static void LoadIndex(ILGenerator gen, int index)
        {
            switch (index)
            {
                case 0:
                    gen.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    gen.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    gen.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    gen.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    gen.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    gen.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    gen.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    gen.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    gen.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (index < 128)
                    {
                        gen.Emit(OpCodes.Ldc_I4_S, index);
                    }
                    else
                    {
                        gen.Emit(OpCodes.Ldc_I4, index);
                    }
                    break;
            }
        }

        protected static void StoreLocal(ILGenerator gen, int index)
        {
            switch (index)
            {
                case 0:
                    gen.Emit(OpCodes.Stloc_0);
                    break;
                case 1:
                    gen.Emit(OpCodes.Stloc_1);
                    break;
                case 2:
                    gen.Emit(OpCodes.Stloc_2);
                    break;
                case 3:
                    gen.Emit(OpCodes.Stloc_3);
                    break;
                default:
                    if (index < 128)
                    {
                        gen.Emit(OpCodes.Stloc_S, index);
                    }
                    else
                    {
                        gen.Emit(OpCodes.Stloc, index);
                    }
                    break;
            }
        }

        protected static void LoadLocal(ILGenerator gen, int index)
        {
            switch (index)
            {
                case 0:
                    gen.Emit(OpCodes.Ldloc_0);
                    break;
                case 1:
                    gen.Emit(OpCodes.Ldloc_1);
                    break;
                case 2:
                    gen.Emit(OpCodes.Ldloc_2);
                    break;
                case 3:
                    gen.Emit(OpCodes.Ldloc_3);
                    break;
                default:
                    if (index < 128)
                    {
                        gen.Emit(OpCodes.Ldloc_S, index);
                    }
                    else
                    {
                        gen.Emit(OpCodes.Ldloc, index);
                    }
                    break;
            }
        }

        protected static MethodInfo MakeMethodGeneric(MethodInfo genericMethodInfo, Type[] genericParameterTypes)
        {
            MethodInfo makeGenericMethodInfo;
            if (genericParameterTypes != null && genericParameterTypes.Length > 0)
            {
                makeGenericMethodInfo = genericMethodInfo.MakeGenericMethod(genericParameterTypes);
            }
            else
            {
                makeGenericMethodInfo = genericMethodInfo;
            }
            return makeGenericMethodInfo;
        }

        protected static void DeclareLocalVariablesForMethodParameters(ILGenerator il, ParameterInfo[] pis)
        {
            for (int i = 0; i < pis.Length; ++i)
            {
                il.DeclareLocal(pis[i].ParameterType);
            }
        }

        protected static void LoadParameterValues(ILGenerator il, ParameterInfo[] pis)
        {
            for (int i = 0; i < pis.Length; ++i)
            {
                LoadLocal(il, i);
            }
        }

        protected static void ParseValuesFromParametersToLocalVariables(ILGenerator il, ParameterInfo[] pis, bool isMethodStatic)
        {
            for (int i = 0; i < pis.Length; ++i)
            {
                if (isMethodStatic)
                {
                    il.Emit(OpCodes.Ldarg_0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                }
                LoadIndex(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                if (pis[i].ParameterType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, pis[i].ParameterType);
                }
                else if (pis[i].ParameterType != typeof(object))
                {
                    il.Emit(OpCodes.Castclass, pis[i].ParameterType);
                }
                StoreLocal(il, i);
            }
        }

        #endregion

        #region Get static method delegate

        protected StaticDynamicMethodProxyDelegate DoGetStaticMethodDelegate(
            Module targetModule,
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            #region Validate parameters

            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule could not be null!");
            }

            if (genericMethodInfo == null)
            {
                throw new ArgumentNullException("genericMethodInfo to be invoke could not be null!");
            }

            if (genericParameterTypes != null)
            {
                if (genericParameterTypes.Length != genericMethodInfo.GetGenericArguments().Length)
                {
                    throw new ArgumentException("The number of generic type parameter of genericMethodInfo and the input types must equal!");
                }
            }
            else
            {
                if (genericMethodInfo.GetGenericArguments().Length > 0)
                {
                    throw new ArgumentException("Must specify types of type parameters for genericMethodInfo!");
                }
            }

            if (!genericMethodInfo.IsStatic)
            {
                throw new ArgumentException("genericMethodInfo must be static here!");
            }

            #endregion

            // Create a dynamic method proxy delegate used to call the specified methodinfo
            DynamicMethod dm = new DynamicMethod(
                Guid.NewGuid().ToString("N"),
                typeof(object),
                new Type[] { typeof(object[]) },
                targetModule);

            ILGenerator il = dm.GetILGenerator();

            #region Create local variables for all the parameters passing to the invoking method and parse values to local variables

            MethodInfo makeGenericMethodInfo = MakeMethodGeneric(genericMethodInfo, genericParameterTypes);
            ParameterInfo[] pis = makeGenericMethodInfo.GetParameters();
            DeclareLocalVariablesForMethodParameters(il, pis);
            ParseValuesFromParametersToLocalVariables(il, pis, true);

            #endregion

            #region Execute the target method

            LoadParameterValues(il, pis);

            il.Emit(OpCodes.Call, makeGenericMethodInfo);

            if (makeGenericMethodInfo.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ldnull);
            }

            #endregion

            il.Emit(OpCodes.Ret);

            return (StaticDynamicMethodProxyDelegate)dm.CreateDelegate(typeof(StaticDynamicMethodProxyDelegate));
        }

        public virtual StaticDynamicMethodProxyDelegate GetStaticMethodDelegate(
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            return DoGetStaticMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        public virtual StaticDynamicMethodProxyDelegate GetStaticMethodDelegate(
            Module targetModule,
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            return DoGetStaticMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
        }

        #endregion

        #region Get non-static method delegate

        protected DynamicMethodProxyDelegate DoGetMethodDelegate(
            Module targetModule,
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            #region Validate parameters

            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule could not be null!");
            }

            if (genericMethodInfo == null)
            {
                throw new ArgumentNullException("genericMethodInfo to be invoke could not be null!");
            }

            if (genericParameterTypes != null)
            {
                if (genericParameterTypes.Length != genericMethodInfo.GetGenericArguments().Length)
                {
                    throw new ArgumentException("The number of generic type parameter of genericMethodInfo and the input types must equal!");
                }
            }
            else
            {
                if (genericMethodInfo.GetGenericArguments().Length > 0)
                {
                    throw new ArgumentException("Must specify types of type parameters for genericMethodInfo!");
                }
            }

            if (genericMethodInfo.IsStatic)
            {
                throw new ArgumentException("genericMethodInfo must not be static here!");
            }

            #endregion

            //Create a dynamic method proxy delegate used to call the specified methodinfo
            DynamicMethod dm = new DynamicMethod(
                Guid.NewGuid().ToString("N"),
                typeof(object),
                new Type[] { typeof(object), typeof(object[]) },
                targetModule);

            ILGenerator il = dm.GetILGenerator();

            #region Create local variables for all the parameters passing to the invoking method and parse values to local variables

            MethodInfo makeGenericMethodInfo = MakeMethodGeneric(genericMethodInfo, genericParameterTypes);
            ParameterInfo[] pis = makeGenericMethodInfo.GetParameters();
            DeclareLocalVariablesForMethodParameters(il, pis);
            ParseValuesFromParametersToLocalVariables(il, pis, false);

            #endregion

            #region Execute the target method

            il.Emit(OpCodes.Ldarg_0);

            LoadParameterValues(il, pis);

            il.Emit(OpCodes.Callvirt, makeGenericMethodInfo);

            if (makeGenericMethodInfo.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ldnull);
            }

            #endregion

            il.Emit(OpCodes.Ret);

            return (DynamicMethodProxyDelegate)dm.CreateDelegate(typeof(DynamicMethodProxyDelegate));
        }

        public virtual DynamicMethodProxyDelegate GetMethodDelegate(
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            return DoGetMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        public virtual DynamicMethodProxyDelegate GetMethodDelegate(
            Module targetModule,
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            return DoGetMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
        }

        #endregion

        #region Visit internal members

        private MethodInfo GetMethodInfoFromArrayBySignature(string signature, MethodInfo[] mis)
        {
            if (mis == null)
            {
                return null;
            }

            foreach (MethodInfo mi in mis)
            {
                if (mi.ToString() == signature)
                {
                    return mi;
                }
            }

            return null;
        }

        public MethodInfo GetMethodInfoBySignature(Type type, string signature, bool isPublic, bool isStatic)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type could not be null!");
            }

            BindingFlags flags = BindingFlags.Instance | (isPublic ? BindingFlags.Public : BindingFlags.NonPublic);
            if (isStatic)
            {
                flags = flags | BindingFlags.Static;
            }
            return GetMethodInfoFromArrayBySignature(signature, type.GetMethods(flags));
        }

        public object CreateInstance(Module targetModule, string typeFullName, bool ignoreCase, bool isPublic, Binder binder, System.Globalization.CultureInfo culture, object[] activationAttrs, params object[] paramObjs)
        {
            //get method info of Assembly.CreateInstance() method first
            MethodInfo mi = GetMethodInfoFromArrayBySignature(
                "System.Object CreateInstance(System.String, Boolean, System.Reflection.BindingFlags, System.Reflection.Binder, System.Object[], System.Globalization.CultureInfo, System.Object[])",
                typeof(Assembly).GetMethods());

            DynamicMethodProxyDelegate dmd = GetMethodDelegate(targetModule, mi);
            return dmd(targetModule.Assembly, typeFullName, ignoreCase, BindingFlags.Instance | (isPublic ? BindingFlags.Public : BindingFlags.NonPublic), binder, paramObjs, culture, activationAttrs);
        }

        public object CreateInstance(Module targetModule, string typeFullName, bool ignoreCase, bool isPublic, params object[] paramObjs)
        {
            return CreateInstance(targetModule, typeFullName, ignoreCase, isPublic, null, null, null, paramObjs);
        }

        #endregion
    }

    public class CachableDynamicMethodProxyFactory : DynamicMethodProxyFactory
    {
        private Dictionary<string, StaticDynamicMethodProxyDelegate> cache = new Dictionary<string, StaticDynamicMethodProxyDelegate>();
        private Dictionary<string, DynamicMethodProxyDelegate> cache2 = new Dictionary<string, DynamicMethodProxyDelegate>();

        #region Get static method delegate

        public override StaticDynamicMethodProxyDelegate GetStaticMethodDelegate(
            Module targetModule,
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            #region  Construct cache key

            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule could not be null!");
            }

            string key = targetModule.FullyQualifiedName + "|" + genericMethodInfo.DeclaringType.ToString() + "|" + genericMethodInfo.ToString();
            if (genericParameterTypes != null)
            {
                for (int i = 0; i < genericParameterTypes.Length; ++i)
                {
                    key += "|" + genericParameterTypes[i].ToString();
                }
            }

            #endregion

            StaticDynamicMethodProxyDelegate dmd;

            lock (cache)
            {
                if (cache.ContainsKey(key))
                {
                    dmd = cache[key];
                }
                else
                {
                    dmd = DoGetStaticMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
                    cache.Add(key, dmd);
                }
            }

            return dmd;
        }

        public override StaticDynamicMethodProxyDelegate GetStaticMethodDelegate(
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            return GetStaticMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        #endregion

        #region Get non-static method delegate

        public override DynamicMethodProxyDelegate GetMethodDelegate(
            Module targetModule,
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            #region  Construct cache key

            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule could not be null!");
            }

            string key = targetModule.FullyQualifiedName + "|" + genericMethodInfo.DeclaringType.ToString() + "|" + genericMethodInfo.ToString();
            if (genericParameterTypes != null)
            {
                for (int i = 0; i < genericParameterTypes.Length; ++i)
                {
                    key += "|" + genericParameterTypes[i].ToString();
                }
            }

            #endregion

            DynamicMethodProxyDelegate dmd;

            lock (cache2)
            {
                if (cache2.ContainsKey(key))
                {
                    dmd = cache2[key];
                }
                else
                {
                    dmd = DoGetMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
                    cache2.Add(key, dmd);
                }
            }

            return dmd;
        }

        public override DynamicMethodProxyDelegate GetMethodDelegate(
            MethodInfo genericMethodInfo,
            params Type[] genericParameterTypes)
        {
            return GetMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace Bee.Core
{
    internal static class EmitUtil
    {
        public static void LoadIndex(ILGenerator gen, int index)
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

        public static void StoreLocal(ILGenerator gen, int index)
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

        public static void LoadLocal(ILGenerator gen, int index)
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


        public static void DeclareLocalVariablesForMethodParameters(ILGenerator il, ParameterInfo[] pis)
        {
            for (int i = 0; i < pis.Length; ++i)
            {
                il.DeclareLocal(pis[i].ParameterType);
            }
        }

        public static void LoadParameterValues(ILGenerator il, ParameterInfo[] pis)
        {
            for (int i = 0; i < pis.Length; ++i)
            {
                LoadLocal(il, i);
            }
        }

        public static void ParseValuesFromParametersToLocalVariables(ILGenerator il, ParameterInfo[] pis, bool isMethodStatic)
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
    }
}

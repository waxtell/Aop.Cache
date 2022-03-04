using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aop.Cache.Extensions;

internal static class TypeExtensions
{
    public static Type GetBaseType(this Type type)
    {
        var returnType = type.GetTypeInfo();

        if (returnType.IsGenericType)
        {
            var gt = returnType.GetGenericTypeDefinition();

            if (gt == typeof(Task<>))
            {
                return 
                    returnType.GenericTypeArguments[0];
            }
        }

        return type;
    }
}


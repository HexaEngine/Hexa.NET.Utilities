﻿#if NET5_0_OR_GREATER
namespace Hexa.NET.Utilities.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    /// <summary>
    /// Provides a hacky way to access the local array of a <see cref="List{T}"/>.
    /// </summary>

    internal static class ArrayAccessor<T>
    {
        /// <summary>
        /// Gets a delegate that can be used to retrieve the local array of a <see cref="List{T}"/>.
        /// </summary>
        public static Func<List<T>, T[]> Getter;

#nullable disable

        static ArrayAccessor()
        {
            // Create a DynamicMethod to access the internal array field.
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            // suppressing warning because this code is always available in runtime
            var dm = new DynamicMethod("get", MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(T[]), [typeof(List<T>)], typeof(ArrayAccessor<T>), true);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            var il = dm.GetILGenerator();

            // Load List<T> argument onto the evaluation stack.
            il.Emit(OpCodes.Ldarg_0);

            // Load the internal array field from the List<T> instance.
            il.Emit(OpCodes.Ldfld, typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance));

            // Return the internal array field.
            il.Emit(OpCodes.Ret);

            // Create a delegate from the DynamicMethod to get the internal array.
            Getter = (Func<List<T>, T[]>)dm.CreateDelegate(typeof(Func<List<T>, T[]>));
        }

#nullable restore
    }
}
#endif
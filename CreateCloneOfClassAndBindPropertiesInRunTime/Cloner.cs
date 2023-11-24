using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace CreateCloneOfClassAndBindPropertiesInRunTime
{
    internal class Cloner<T>
    {
        public object CreateCopy<T>(T value)
        {
            return methodInfo.Value.Invoke(this, new object[] { value });
        }

        private Lazy<MethodInfo> methodInfo = new Lazy<MethodInfo>(() =>
        {
            var copyType = CreateDynamicType<T>(typeof(T).Name + "_coppy");

            MethodInfo generateMappingFunctionMethodInfo =
                typeof(Cloner<T>)
                    .GetMethod(
                        nameof(CopyObject),
                        BindingFlags.NonPublic | BindingFlags.Instance);


            return generateMappingFunctionMethodInfo.MakeGenericMethod(typeof(T), copyType);
        });

        private static Type CreateDynamicType<TBase>(string typeName)
        {
            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            // Копирование свойств из базового класса
            foreach (var property in typeof(TBase).GetProperties())
            {
                var propertyName = property.Name;
                Type propertyType = property.PropertyType;

                // Создаем поле для хранения значения свойства
                FieldBuilder fieldBuilder = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);

                // Создаем свойство с геттером и сеттером
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);

                // Генерация геттера
                MethodBuilder getMethodBuilder = typeBuilder.DefineMethod($"get_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
                ILGenerator getIL = getMethodBuilder.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);             // загрузка 'this' (экземпляра объекта)
                getIL.Emit(OpCodes.Ldfld, fieldBuilder);  // загрузка значения из поля
                getIL.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(getMethodBuilder);

                // Генерация сеттера
                MethodBuilder setMethodBuilder = typeBuilder.DefineMethod($"set_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { propertyType });
                ILGenerator setIL = setMethodBuilder.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);             // загрузка 'this' (экземпляра объекта)
                setIL.Emit(OpCodes.Ldarg_1);             // загрузка значения, которое вы хотите установить
                setIL.Emit(OpCodes.Stfld, fieldBuilder);  // установка значения в поле
                setIL.Emit(OpCodes.Ret);
                propertyBuilder.SetSetMethod(setMethodBuilder);
            }

            return typeBuilder.CreateType();
        }

        private Func<TSource, TTarget> GenerateMappingFunction<TSource, TTarget>()
        {
            ParameterExpression sourceParam = Expression.Parameter(typeof(TSource), "source");

            var bindings = typeof(TTarget)
                .GetProperties()
                .Where(targetProperty => targetProperty.CanWrite)
                .Select(targetProperty =>
                {
                    PropertyInfo sourceProperty = typeof(TSource).GetProperty(targetProperty.Name);
                    if (sourceProperty != null)
                    {
                        MemberExpression propertyExpr = Expression.Property(sourceParam, sourceProperty);
                        return Expression.Bind(targetProperty, propertyExpr);
                    }
                    return null;
                })
                .Where(binding => binding != null);

            MemberInitExpression memberInitExpr = Expression.MemberInit(Expression.New(typeof(TTarget)), bindings);
            return Expression.Lambda<Func<TSource, TTarget>>(memberInitExpr, sourceParam).Compile();
        }

        private TTarget CopyObject<TSource, TTarget>(TSource source)
            where TTarget : new()
        {
            return GenerateMappingFunction<TSource, TTarget>()(source);
        }
    }
}

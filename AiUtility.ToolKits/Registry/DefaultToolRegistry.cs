// 位於 AiUtility.ToolKits 專案
using AiUtility.ToolKits.Abstractions;
using ReflectionUtilityServices;
using System.Reflection;

namespace AiUtility.ToolKits.Registry
{
    public class DefaultToolRegistry<TMetadata, TAttribute> : ToolRegistry<TMetadata , TAttribute>
        where TMetadata : ToolMetadataBase
        where TAttribute : Attribute
    {
        public DefaultToolRegistry(
            IReflectionUtilityService reflectionService ,
            Func<MethodInfo , Func<Type , object>?,IEnumerable<Attribute> , TMetadata> concreteFactory
        ) : base((method , resolver) =>
        {
            // 1. 處理效能優化：註冊 FastDelegate
            reflectionService.AddFastDelegate(method);

            // 2. 擷取所有 Data Annotations (包含您要求的其他 Attribute)
            var allAttributes = method.GetCustomAttributes(true).Cast<Attribute>();

            // 3. 調用外部傳入的工廠來建立具體的 Metadata 實例
            return concreteFactory(
                method ,
                resolver ,
                allAttributes
            );
        })
        {
          
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.ToolKits.Abstractions
{
    /// <summary>
    /// Converter
    /// </summary>
    /// <typeparam name="TOutput"></typeparam>
    public interface IAiToolConverter<out TOutput>
    {
        /// <summary>
        /// Convert the given tool metadata
        /// (POCO, determine how to convert a method that marked with data annotation into a cached delegate)
        /// into a tool declaration of type TOutput.
        /// </summary>
        TOutput ToToolDeclaration(ToolMetadataBase metadata);
    }
}

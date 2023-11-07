using System;using System.Reflection;namespace JeffPires.VisualChatGPTStudio.Utils{
    /// <summary>
    /// A helper class for working with enums in C#.
    /// </summary>
    public static class EnumHelper    {
        /// <summary>
        /// Gets the string value associated with the specified enum item.
        /// </summary>
        /// <param name="enumItem">The enum item.</param>
        /// <returns>The string value associated with the enum item.</returns>
        public static string GetStringValue(this Enum enumItem)        {            string enumStringValue = string.Empty;            Type type = enumItem.GetType();            FieldInfo objFieldInfo = type.GetField(enumItem.ToString());            EnumStringValue[] enumStringValues = objFieldInfo.GetCustomAttributes(typeof(EnumStringValue), false) as EnumStringValue[];            if (enumStringValues.Length > 0)            {                enumStringValue = enumStringValues[0].Value;            }            return enumStringValue;        }    }

    /// <summary>
    /// Represents an attribute that can be used to associate a string value with an enum value.
    /// </summary>
    public class EnumStringValue : Attribute    {        private readonly string value;

        /// <summary>
        /// Initializes a new instance of the EnumStringValue class with the specified value.
        /// </summary>
        /// <param name="value">The value of the EnumStringValue.</param>
        public EnumStringValue(string value)        {            this.value = value;        }

        /// <summary>
        /// Gets the value of the property.
        /// </summary>
        /// <returns>The value of the property.</returns>
        public string Value        {            get            {                return value;            }        }    }}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace Editor.AssetProcessor
{
    public static class AssetProcessorTypeUtilities
    {
        #region Type Utilities
        public static bool IsInteger(this Type type)
        {
            return type.IsAssignableFrom(typeof(byte)) ||
                type.IsAssignableFrom(typeof(short)) ||
                type.IsAssignableFrom(typeof(int)) ||
                type.IsAssignableFrom(typeof(long)) ||
                type.IsAssignableFrom(typeof(sbyte)) ||
                type.IsAssignableFrom(typeof(ushort)) ||
                type.IsAssignableFrom(typeof(uint)) ||
                type.IsAssignableFrom(typeof(ulong)) ||
                type.IsAssignableFrom(typeof(BigInteger));
        }

        private static bool IsFloat(this Type type)
        {
            return type.IsAssignableFrom(typeof(float)) ||
                type.IsAssignableFrom(typeof(double)) ||
                type.IsAssignableFrom(typeof(decimal));
        }

        public static bool IsNumeric(this Type type)
        {
            return type.IsAssignableFrom(typeof(byte)) ||
                type.IsAssignableFrom(typeof(short)) ||
                type.IsAssignableFrom(typeof(int)) ||
                type.IsAssignableFrom(typeof(long)) ||
                type.IsAssignableFrom(typeof(sbyte)) ||
                type.IsAssignableFrom(typeof(ushort)) ||
                type.IsAssignableFrom(typeof(uint)) ||
                type.IsAssignableFrom(typeof(ulong)) ||
                type.IsAssignableFrom(typeof(BigInteger)) ||
                type.IsAssignableFrom(typeof(decimal)) ||
                type.IsAssignableFrom(typeof(double)) ||
                type.IsAssignableFrom(typeof(float));
        }

        private static bool IsVector(this Type type)
        {
            return type.IsAssignableFrom(typeof(UnityEngine.Vector2)) ||
                   type.IsAssignableFrom(typeof(UnityEngine.Vector3)) ||
                   type.IsAssignableFrom(typeof(UnityEngine.Vector4));
        }

        private static bool IsString(this Type type)
        {
            return type.IsAssignableFrom(typeof(string));
        }

        public static bool IsCustomArrayType(this Type type)
        {
            return type.IsVector() ||
                   type.IsAssignableFrom(typeof(Quaternion)) ||
                   type.IsAssignableFrom(typeof(Color));
        }

        public static OperatorTypes GetOperatorType(this Type type)
        {
            var result = OperatorTypes.Bool;

            if (type.IsEnum)
            {
                result = OperatorTypes.Enum;
            }
            else if (type.IsInteger())
            {
                result = OperatorTypes.Integer;
            }
            else if (type.IsFloat())
            {
                result = OperatorTypes.Float;
            }
            else if (type.IsString())
            {
                result = OperatorTypes.String;
            }

            return result;
        }
        #endregion
        
        #region Custom Filtering
        public static bool HasCustomFilterUI(this Type type)
        {
            // This will be implemented with custom filter types
            return false;
        }
        
        public static PopupField<MemberInfo> GetCustomFilterUI(this Type type)
        {
            // implement this with custom filter types
            return null;
        }
        
        public static bool PreviousFilterRequiresCustomUI(this Type type)
        {
            // this is for System and Unity types that result in unusable get methods like Vector and other arrays
            return type.IsAssignableFrom(typeof(Vector2)) ||
                   type.IsAssignableFrom(typeof(Vector3)) ||
                   type.IsAssignableFrom(typeof(Vector4)) ||
                   type.IsAssignableFrom(typeof(Quaternion)) ||
                   type.IsAssignableFrom(typeof(Color));
        }

        /// <summary>
        /// All of the System and Unity types that require custom field filter UI for the next field. Vectors, colors
        /// and other arrays are perfect candidates.
        /// </summary>
        /// <returns>The PopupField UI to display</returns>
        public static PopupField<MemberInfo> GetCustomUIForPreviousProperty(this Type type,
            IReadOnlyList<PropertyField> fields,
            int index)
        {
            var result = new PopupField<MemberInfo>();
            var currentValue = fields[index].selectedValue;

            // struct parsing (vector, color, etc.)
            if (type.IsCustomArrayType())
            {
                var propertyFields = type.GetFields()
                    .Where(IsFieldAccessibleViaMethod)
                    .Cast<MemberInfo>()
                    .ToList();

                var selectedField = propertyFields.FirstOrDefault();
                if (propertyFields.Select(field => field.Name).ToList().Contains(currentValue))
                {
                    selectedField = propertyFields.First(field => field.Name == currentValue);
                }
                
                result = new PopupField<MemberInfo>(propertyFields, selectedField,
                    info => info.Name,
                    info => info.Name)
                {
                    tooltip =  "Item"
                };

                fields[index].selectedValue = selectedField?.Name;

                result.RegisterValueChangedCallback(evt => fields[index].selectedValue = evt.newValue.Name);
            }
            else
            {
                Debug.LogErrorFormat($"This type {type.Name} does not have a custom UI for the previous property \"{fields[index - 1].selectedProperty.Name}.\" " +
                                     $"Please talk to the developers to make sure that gets implemented!");
            }

            return result;
        }

        private static bool IsFieldAccessibleViaMethod(this FieldInfo field)
        {
            // there might be a way to determine this procedurally, only investigate if boilerplate gets ridiculous
            return string.Equals(field.Name, "x", StringComparison.InvariantCultureIgnoreCase) ||
                   string.Equals(field.Name, "y", StringComparison.InvariantCultureIgnoreCase) ||
                   string.Equals(field.Name, "z", StringComparison.InvariantCultureIgnoreCase) ||
                   string.Equals(field.Name, "w", StringComparison.InvariantCultureIgnoreCase) ||
                   string.Equals(field.Name, "r", StringComparison.InvariantCultureIgnoreCase) ||
                   string.Equals(field.Name, "g", StringComparison.InvariantCultureIgnoreCase) ||
                   string.Equals(field.Name, "b", StringComparison.InvariantCultureIgnoreCase) ||
                   string.Equals(field.Name, "a", StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion
        
        #region Get Methods Custom Types
        public static MethodInfo GetCustomGetMethod(this Type type)
        {
            MethodInfo result = null;

            if (type.IsCustomArrayType())
            {
                var args = new[] { typeof(int) };
                result = type.GetMethod("get_Item", args);
            }

            return result;
        }

        public static object[] GetParametersFromType(this Type type, string selectedParameter)
        {
            object[] result = null;
            
            if (type.IsCustomArrayType())
            {
                result = GetParams(selectedParameter);
            }

            return result;
        }

        private static object[] GetParams(string value)
        {
            object[] result  = null;
            
            if (string.Equals(value, "x", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(value, "r", StringComparison.InvariantCultureIgnoreCase))
            {
                result = new[] { (object)0 };
            }
            else if (string.Equals(value, "y", StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(value, "g", StringComparison.InvariantCultureIgnoreCase))
            {
                result = new[] { (object)1 };
            }
            else if (string.Equals(value, "z", StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(value, "b", StringComparison.InvariantCultureIgnoreCase))
            {
                result = new[] { (object)2 };
            }
            else if (string.Equals(value, "w", StringComparison.InvariantCultureIgnoreCase) ||
                     string.Equals(value, "a", StringComparison.InvariantCultureIgnoreCase))
            {
                result = new[] { (object)3 };
            }

            return result;
        }
        #endregion
    }
}

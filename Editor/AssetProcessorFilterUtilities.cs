﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetProcessor_Editor
{
    public static class AssetProcessorFilterUtilities
    {
        public static bool ProcessFilters(this List<PropertiesFilter> filters, Object obj,
            List<AssetProcessorResult> results)
        {
            // create the result so we can inject the result value into it
            var assetResult = CreateResult(obj);
        
            var firstFilter = filters.First();
            var result = firstFilter.FilterObject(obj, assetResult);

            for (var i = 1; i < filters.Count; i++)
            {
                var filter = filters[i];
                var nextResult = filter.FilterObject(obj, assetResult);
            
                // compare against previous result
                if (filters[i - 1].andOrField == AndOr.And)
                {
                    result &= nextResult;
                }
                else
                {
                    result |= nextResult;
                }
            }

            // add to our results if we should
            if (result)
            {
                results.Add(assetResult);
            }
        
            return result;
        }

        private static bool FilterObject(this PropertiesFilter filter, Object obj, AssetProcessorResult assetResult)
        {
            var result = false;
            var propertyFields = filter.propertyFields;
        
            object currentResult = obj;

            if (currentResult is GameObject go)
            {
                var component = go.GetComponent(filter.filterType);
                if (component == null) return false;
                currentResult = component;
            }

            for (var i = 0; i < propertyFields.Count; i++)
            {
                var field = propertyFields[i];
            
                // Get the property value until we hit the operator type, then filter based on operator
                if (field.selectedProperty.PropertyType.IsOperatorType(propertyFields, i))
                {
                    object[] parameters = null;
                
                    // set the appropriate parameters for custom filtering
                    if (i > 0 && propertyFields[i - 1].selectedProperty.PropertyType.PreviousFilterRequiresCustomUI())
                    {
                        parameters = propertyFields[i - 1].selectedProperty.PropertyType
                            .GetParametersFromType(propertyFields[i].selectedValue);
                    }

                    result = FilterWithOperator(currentResult, field, filter, assetResult.values, parameters);
                }
                else
                {
                    currentResult = field.getValue.Invoke(currentResult, null);

                    if (currentResult == null)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private static bool FilterWithOperator(object value,
            PropertyField field,
            PropertiesFilter filter,
            ICollection<string> results,
            object[] parameters = null)
        {
            if (field.getValue == null) return false;

            var result = false;

            var selectedOperator = filter.selectedOperator;
            var operatorValue = filter.operatorValue;

            var filterValue = field.getValue.Invoke(value, parameters);
            var operatorType = field.selectedProperty.PropertyType.GetOperatorType();
        
            results.Add(filterValue.ToString());

            var parsedFilter = double.NaN;
            var parsedOperator = double.NaN;
        
            switch(selectedOperator)
            {
                case "==":
                    result = GetEquality(filterValue, operatorValue, operatorType);
                    break;
                case "!=":
                    result = !GetEquality(filterValue, operatorValue, operatorType);
                    break;
                case "contains":
                    break;
                case "not contains":
                    break;
                case ">":
                    double.TryParse(filterValue.ToString(), out parsedFilter);
                    double.TryParse(operatorValue.ToString(), out parsedOperator);
                    result = parsedFilter > parsedOperator;
                    break;
                case ">=":
                    double.TryParse(filterValue.ToString(), out parsedFilter);
                    double.TryParse(operatorValue.ToString(), out parsedOperator);
                    result = parsedFilter >= parsedOperator;
                    break;
                case "<":
                    double.TryParse(filterValue.ToString(), out parsedFilter);
                    double.TryParse(operatorValue.ToString(), out parsedOperator);
                    result = parsedFilter < parsedOperator;
                    break;
                case "<=":
                    double.TryParse(filterValue.ToString(), out parsedFilter);
                    double.TryParse(operatorValue.ToString(), out parsedOperator);
                    result = parsedFilter <= parsedOperator;
                    break;
            }

            return result;
        }
    
        private static AssetProcessorResult CreateResult(Object obj)
        {
            var result = ScriptableObject.CreateInstance<AssetProcessorResult>();
            result.displayName = obj.name;
            result.gameObject = obj;

            return result;
        }

        private static bool GetEquality(object a, object b, OperatorTypes operatorType)
        {
            var result = false;

            // the objects will already be converted to the correct parsed value
            switch (operatorType)
            {
                case OperatorTypes.Bool:
                    bool.TryParse(a.ToString(), out var aBool);
                    bool.TryParse(b.ToString(), out var bBool);
                    result = aBool == bBool;
                    break;
                case OperatorTypes.Enum:
                case OperatorTypes.String:
                    result = string.Equals(a.ToString(), b);
                    break;
                case OperatorTypes.Integer:
                    int.TryParse(a.ToString(), out var aInt);
                    int.TryParse(b.ToString(), out var bInt);
                    result = aInt == bInt;
                    break;
                case OperatorTypes.Float:
                    double.TryParse(a.ToString(), out var aDouble);
                    double.TryParse(b.ToString(), out var bDouble);
                    result = Math.Abs(aDouble - bDouble) < 0.000000000000000001;
                    break;
                case OperatorTypes.Array:
                    break;
            }

            return result;
        }
    }
}
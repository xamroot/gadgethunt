﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace gadgethunt.lib
{
    internal class SerialUtils
    {
        public static bool HasParameterlessConstructor(ConstructorInfo[] constructors)
        {
            foreach (var constructor in constructors)
            {
                if (constructor.GetParameters().Length == 0)
                    return true;
            }
            return false;
        }

        public static string BuildSerializedPayload(Type type)
        {
            string outputText = "";

            // build ctors
            // REQUIRED BY NEWTONSOFT DESERIALIZATION
            // we need a type which has a parameterless constructor
            // is there a 
            if (HasParameterlessConstructor(type.GetConstructors()))
            {
                string payloadFormat = "\"$type\":\"{0}\"{1}";
                string payloadBody = "";
                string propertyTemplate = ",\"{0}\":{1}";

                foreach (var prop in type.GetProperties())
                {
                    object value = new object();
                    bool wasValueSet = false;
                    // check that the property has a setter
                    if (prop?.GetSetMethod() != null)
                    {
                        switch (prop.PropertyType.ToString())
                        {
                            case "System.String":
                                value = "\"test string\"";
                                wasValueSet = true;
                                break;
                            case "System.Boolean":
                                value = "true";
                                wasValueSet = true;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        bool specialNonSetterBehaviorUsedFlag = false;

                        switch (prop.PropertyType.ToString())
                        {
                            case "System.Boolean":
                                payloadBody += string.Format(propertyTemplate, prop.Name, "false");
                                specialNonSetterBehaviorUsedFlag = true;
                                break;
                            default:
                                break;
                        }

                        if (specialNonSetterBehaviorUsedFlag)
                        {
                            continue;
                        }
                    }

                    if (wasValueSet)
                    {
                        //prop.SetValue(o1, value);
                        payloadBody += string.Format(propertyTemplate, prop.Name, value.ToString());
                    }
                    else
                    {
                        payloadBody += string.Format(propertyTemplate, prop.Name, "null");
                    }
                }

                string payload = "{" + string.Format(payloadFormat, type.FullName + ", " + type.Assembly.ToString().Split(",")[0], payloadBody) + "}";
                outputText += $"\nWorking on the type: {type.FullName}\n";
                object obj = null;
                try
                {
                    obj = JsonConvert.DeserializeObject(payload, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

                }
                catch (Exception e)
                {
                    return outputText + "fail!\n";
                }

                if (obj != null)
                {
                    outputText += $"deserialized object: {obj.GetType().FullName}\n";
                    outputText += $"payload used: {payload}\n";
                }
                else
                {
                    outputText += "fail";
                }
            }

            return $"{outputText}";
        }

    }
}

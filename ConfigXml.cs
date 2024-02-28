using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace RealEco.Config;

[XmlRoot("Configuration")]
public class ConfigurationXml
{
    [XmlArray("ValidPrefabTypes")]
    [XmlArrayItem(typeof(string), ElementName = "PrefabType")]
    public string[] ValidPrefabTypes;
    
    public bool IsPrefabValid(string nameToCheck)
    {
        if (ValidPrefabTypes is null || ValidPrefabTypes.Length == 0) return false;
        return Array.IndexOf(ValidPrefabTypes, nameToCheck) != -1;
    }

    [XmlElement("Prefab")]
    public List<PrefabXml> Prefabs { get; set; }

    public bool TryGetPrefab(string name, out PrefabXml prefab)
    {
        prefab = default(PrefabXml);
        foreach (var item in Prefabs)
            if (item.Name == name)
            {
                prefab = item;
                return true;
            }
        return false;
    }
}

public class PrefabXml
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("Component")]
    public List<ComponentXml> Components { get; set; }

    public override string ToString()
    {
        return $"PrefabXml: {Name}";
    }

    public void DumpToLog()
    {
        Plugin.Log(ToString());
        foreach (ComponentXml component in Components)
            component.DumpToLog();
    }

    internal bool TryGetComponent(string name, out ComponentXml component)
    {
        component = default(ComponentXml);
        foreach (var item in Components)
            if (item.Name == name)
            {
                component = item;
                return true;
            }
        return false;
    }
}

public class ComponentXml
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    // Different types of elements can be defined here using XmlElement attributes
    [XmlElement("Field")]
    public List<FieldXml> Fields { get; set; }

    public override string ToString()
    {
        return $"ComponentXml: {Name}";
    }

    public void DumpToLog()
    {
        Plugin.Log(ToString());
        foreach (FieldXml field in Fields)
            Plugin.Log(field.ToString());
    }

    internal bool TryGetField(string name, out FieldXml field)
    {
        field = default(FieldXml);
        foreach (var item in Fields)
            if (item.Name == name)
            {
                field = item;
                return true;
            }
        return false;
    }
}

public class FieldXml
{
    public override string ToString()
    {
        string res = $"{Name}=";
        if (ValueInt.HasValue)
            res += $" {ValueInt} (int)";
        if (ValueFloat.HasValue)
            res += $" {ValueFloat} (float)";
        if (!string.IsNullOrEmpty(Value))
            res += $" {Value}";
        return res;
    }

    [XmlAttribute("name")]
    public string Name { get; set; }

    // STRING is the default value
    // use string.IsNullOrEmpty() to check if specified

    [XmlAttribute(AttributeName = "value", DataType = "string")]
    public string Value { get; set; }

    // INTEGER

    [XmlIgnore]
    public bool ValueIntSpecified { get; set; } // Use a bool field to determine if the value is present

    [XmlIgnore]
    public int? ValueInt { get; set; } // Nullable in case it is not defined

    [XmlAttribute("valueInt")]
    public int XmlValueInt
    {
        get { return ValueInt.GetValueOrDefault(); }
        set { ValueInt = value; ValueIntSpecified = true; }
    }

    // FLOAT
    
    [XmlIgnore]
    public bool ValueFloatSpecified { get; set; } // Use a bool field to determine if the value is present

    [XmlIgnore]
    public float? ValueFloat { get; set; } // Nullable in case it is not defined

    [XmlAttribute(AttributeName = "valueFloat", DataType = "float")]
    public float XmlValueFloat
    {
        get { return ValueFloat.GetValueOrDefault(); }
        set { ValueFloat = value; ValueFloatSpecified = true; }
    }
}


public static class ConfigToolXml
{
    private static readonly string _configFileName = "Config.xml";
    private static readonly string _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private static readonly string _configFile = Path.Combine(_assemblyPath, _configFileName); // TODO: change to a CO framework method

    private static readonly string _dumpFileName = "Config_Dump.xml";
    private static readonly string _dumpFile = Path.Combine(_assemblyPath, _dumpFileName); // TODO: change to a CO framework method

    private static ConfigurationXml _config = null;
    public static ConfigurationXml Config { get { return _config; } }

    /// <summary>
    /// Loads prefab config data from a file in the mod directory.
    /// Settings are set to null id there is any problem during loading.
    /// </summary>
    public static void LoadConfig()
    {
        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationXml));
            using (FileStream fs = new FileStream(_configFile, FileMode.Open))
            {
                _config = (ConfigurationXml)serializer.Deserialize(fs);
            }
            // Verify and output deserialized data
            //Plugin.Log($"NULL: {Settings is null}");
            
            if (Config.ValidPrefabTypes.Length == 0)
            {
                Plugin.Log("Warning! No valid prefab types are defined.");
            }
            else
            {
                Plugin.LogIf($"VALID PREFAB TYPES {Config.ValidPrefabTypes.Length}");
                foreach (string name in Config.ValidPrefabTypes)
                    Plugin.LogIf(name);
            }
            
            if (Plugin.Logging.Value)
            {
                Plugin.Log("PREFAB CONFIG DATA");
                foreach (PrefabXml prefab in Config?.Prefabs)
                    prefab.DumpToLog();
            }
        }
        catch (Exception e)
        {
            Plugin.Log($"ERROR: Cannot load settings, exception {e.Message}");
            _config = null;
        }
    }

    public static void SaveConfig()
    {
        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationXml));
            using (FileStream fs = new FileStream(_dumpFile, FileMode.Create))
            {
                serializer.Serialize(fs, Config);
            }
            Plugin.Log($"Configuration saved to file {_dumpFile}.");
        }
        catch (Exception e)
        {
            Plugin.Log($"ERROR: Cannot save configuration, exception {e.Message}.");
        }
    }
}

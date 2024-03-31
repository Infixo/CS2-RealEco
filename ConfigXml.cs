using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

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
        //Plugin.Log($"TryGetPrefab: {name}");
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
    [XmlAttribute("type")]
    public string Type { get; set; }

    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("Component")]
    public List<ComponentXml> Components { get; set; }

    public override string ToString()
    {
        return $"PrefabXml: {Type}.{Name}";
    }

    public void DumpToLog()
    {
        Mod.log.Info(ToString());
        foreach (ComponentXml component in Components)
            component.DumpToLog();
    }

    internal bool TryGetComponent(string name, out ComponentXml component)
    {
        //Plugin.Log($"TryGetComponent: {name}");
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
        Mod.log.Info(ToString());
        foreach (FieldXml field in Fields)
            Mod.log.Info(field.ToString());
    }

    internal bool TryGetField(string name, out FieldXml field)
    {
        //Plugin.Log($"TryGetField: {name}");
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
    private static readonly string _dumpFileName = "Config_Dump.xml";

    private static ConfigurationXml _config = null;
    public static ConfigurationXml Config { get { return _config; } }

    /// <summary>
    /// Loads prefab config data from a file in the mod directory.
    /// Settings are set to null id there is any problem during loading.
    /// </summary>
    public static void LoadConfig(string assetPath)
    {
        //Mod.Log($"{assetPath}");
        //Mod.Log($"{Path.GetDirectoryName(assetPath)}");
        try
        {
            string configDir = Path.Combine(Path.GetDirectoryName(assetPath));
            if (Mod.setting.UseLocalConfig)
            {
                string appDataDir = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
                configDir = Path.Combine(appDataDir, @"LocalLow\Colossal Order\Cities Skylines II\Mods", Mod.modAsset == null ? "RealCity" : Mod.modAsset.name);
            }
            Mod.log.Info($"Using {(Mod.setting.UseLocalConfig ? "local" : "default")} {_configFileName} at {configDir}.");

            XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationXml));
            using (FileStream fs = new FileStream(Path.Combine(configDir, _configFileName), FileMode.Open))
            {
                _config = (ConfigurationXml)serializer.Deserialize(fs);
            }
            // Verify and output deserialized data
            //Plugin.Log($"NULL: {Settings is null}");
            
            if (Config.ValidPrefabTypes.Length == 0)
            {
                Mod.log.Info("Warning! No valid prefab types are defined.");
            }
            else
            {
                Mod.LogIf($"VALID PREFAB TYPES {Config.ValidPrefabTypes.Length}");
                foreach (string name in Config.ValidPrefabTypes)
                    Mod.LogIf(name);
            }
            
            if (Mod.setting.Logging)
            {
                Mod.log.Info("PREFAB CONFIG DATA");
                foreach (PrefabXml prefab in Config?.Prefabs)
                    prefab.DumpToLog();
            }
            // Save copy
            SaveConfig();
        }
        catch (Exception e)
        {
            Mod.log.Info($"ERROR: Cannot load settings, exception {e.Message}");
            _config = null;
        }
    }
    
    public static void SaveConfig()
    {
        try
        {
            string appDataDir = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            string dumpDir = Path.Combine(appDataDir, @"LocalLow\Colossal Order\Cities Skylines II\Mods", Mod.modAsset == null ? "RealCity" : Mod.modAsset.name);
            string dumpFile = Path.Combine(dumpDir, _dumpFileName);
            XmlSerializer serializer = new XmlSerializer(typeof(ConfigurationXml));
            using (FileStream fs = new FileStream(dumpFile, FileMode.Create))
            {
                serializer.Serialize(fs, Config);
            }
            Mod.log.Info($"Configuration saved to file {dumpFile}.");
        }
        catch (Exception e)
        {
            Mod.log.Info($"ERROR: Cannot save configuration, exception {e.Message}.");
        }
    }
    
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CustomActivatableEquipment {
  public class LocalSettingsHelper {
    public static string ResetSettings() {
      return Core.GlobalSettings.SerializeLocal();
    }
    public static void ReadSettings(string json) {
      try {
        Settings local = JsonConvert.DeserializeObject<Settings>(json);
        Core.Settings.ApplyLocal(local);
      } catch (Exception e) {
        Log.Debug?.TWL(0, e.ToString(), true);
      }
    }
  }
  [System.AttributeUsage(System.AttributeTargets.Property)]
  public class GameplaySafe : System.Attribute {
    public GameplaySafe() { }
  }
  public class ColorJsonAlpha {
    [JsonIgnore]
    public Color color { get; private set; } = Color.white;
    public string html {
      get {
        return "#" + ColorUtility.ToHtmlStringRGB(this.color);
      }
      set {
        Color temp;
        if (ColorUtility.TryParseHtmlString(value, out temp)) {
          this.color = new Color(temp.r,temp.g,temp.b,this.color.a);
        } else {
          Log.Debug?.TWL(0,value + " is bad color");
          color = UnityEngine.Color.magenta;
        }
      }
    }
    public float alpha {
      get {
        return this.color.a;
      }
      set {
        this.color = new Color(color.r, color.g, color.b, value);
      }
    }
    public ColorJsonAlpha() { }
    public ColorJsonAlpha(Color color) { this.color = color; }
  }
  public class Settings {
    [GameplaySafe]
    public bool debug { get; set; }
    public float AIComponentUsefullModifyer { get; set; }
    public float AIComponentExtreamlyUsefulModifyer { get; set; }
    public float AIOffenceUsefullCoeff { get; set; }
    public float AIDefenceUsefullCoeff { get; set; }
    public float AIHeatCoeffCoeff { get; set; }
    public float AIOverheatCoeffCoeff { get; set; }
    [GameplaySafe]
    public float ToolTipWarningFailChance { get; set; }
    [GameplaySafe]
    public float ToolTipAlertFailChance { get; set; }
    public float StartupMinHeatRatio { get; set; }
    public bool StartupByHeatControl { get; set; }
    public bool StoodUpPilotingRoll { get; set; }
    public float StoodUpPilotingRollCoeff { get; set; }
    public float DefaultArmsAbsenceStoodUpMod { get; set; }
    public float LegAbsenceStoodUpMod { get; set; }
    public List<string> AdditionalAssets { get; set; }
    public float AIActivatableCheating { get; set; }
    public AuraUpdateFix auraUpdateFix { get; set; }
    [GameplaySafe]
    public float auraUpdateMinTimeDelta { get; set; }
    [GameplaySafe]
    public float auraUpdateMinPosDelta { get; set; }
    public AuraDef sensorsAura { get; set; }
    public string unaffectedByHeadHitStatName { get; set; }
    [GameplaySafe]
    public float auraStartupTime { get; set; }
    [GameplaySafe]
    public float equipmentFlashFailChance { get; set; }
    [GameplaySafe]
    public float componentHeatBarSize { get; set; } = 1f;
    [GameplaySafe]
    public ColorJsonAlpha componentHeatBarBorderColor { get; set; } = new ColorJsonAlpha(new Color(0f,0f,0f,0f));
    [GameplaySafe]
    public ColorJsonAlpha componentHeatBarActivateColor { get; set; } = new ColorJsonAlpha(new Color(1f, 0f, 0f, 1f));
    [GameplaySafe]
    public ColorJsonAlpha componentHeatBarDeactivateColor { get; set; } = new ColorJsonAlpha(new Color(0f, 0f, 1f, 1f));
    public Settings() {
      debug = true;
      AdditionalAssets = new List<string>();
      AIComponentUsefullModifyer = 0.4f;
      AIComponentExtreamlyUsefulModifyer = 0.6f;
      AIOffenceUsefullCoeff = 0.2f;
      AIDefenceUsefullCoeff = 0.2f;
      AIHeatCoeffCoeff = 0.9f;
      AIOverheatCoeffCoeff = 0.8f;
      ToolTipWarningFailChance = 0.2f;
      ToolTipAlertFailChance = 0.4f;
      StartupByHeatControl = false;
      StartupMinHeatRatio = 0.4f;
      StoodUpPilotingRoll = false;
      StoodUpPilotingRollCoeff = 0.1f;
      DefaultArmsAbsenceStoodUpMod = -0.1f;
      LegAbsenceStoodUpMod = -0.1f;
      AIActivatableCheating = 0.8f;
      auraUpdateFix = AuraUpdateFix.None;
      auraUpdateMinTimeDelta = 1f;
      auraUpdateMinPosDelta = 20f;
      auraStartupTime = 10f;
      sensorsAura = new AuraDef();
      unaffectedByHeadHitStatName = "unaffectedByHeadHit";
      equipmentFlashFailChance = 0.1f;
    }
    public void ApplyLocal(Settings local) {
      PropertyInfo[] props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
      Log.Debug?.TWL(0, "Settings.ApplyLocal");
      foreach (PropertyInfo prop in props) {
        bool skip = true;
        object[] attrs = prop.GetCustomAttributes(true);
        foreach (object attr in attrs) { if ((attr as GameplaySafe) != null) { skip = false; break; } };
        if (skip) { continue; }
        Log.Debug?.WL(1, "updating:" + prop.Name);
        prop.SetValue(Core.Settings, prop.GetValue(local));
      }
    }
    public string SerializeLocal() {
      Log.Debug?.TWL(0, "Settings.SerializeLocal");
      JObject json = JObject.FromObject(this);
      PropertyInfo[] props = this.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
      foreach (PropertyInfo prop in props) {
        bool skip = true;
        object[] attrs = prop.GetCustomAttributes(true);
        foreach (object attr in attrs) { if ((attr as GameplaySafe) != null) { skip = false; break; } };
        if (skip) {
          if (json[prop.Name] != null) {
            json.Remove(prop.Name);
          }
        }
      }
      return json.ToString(Formatting.Indented);
    }
  }
}
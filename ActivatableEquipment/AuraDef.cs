using BattleTech;
using CustomActivatableEquipment;
using HarmonyLib;
using HBS.Collections;
using HBS.Util;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomActivatableEquipment {
  public enum AuraState {
    Online,
    Offline,
    Persistent
  }
  public enum AuraLineType { Dashes, Dots }
  [MessagePackObject]
  public class AuraBubbleVFXDef {
    [Key(0)]
    public string VFXname { get; set; } = string.Empty;
    [Key(1)]
    public bool scale { get; set; } = true;
    [Key(2)]
    public float scaleToRangeFactor { get; set; } = 1f;
  }
  [MessagePackObject]
  public class AuraDef {
    private static Dictionary<string, AuraDef> dataManager = new Dictionary<string, AuraDef>();
    private static Dictionary<string, VersionManifestEntry> manifest = new Dictionary<string, VersionManifestEntry>();
    public static void Register(string id, VersionManifestEntry entry) { manifest[id] = entry; }
    [Key(0)]
    public string Id { get; set; } = string.Empty;
    [Key(1)]
    public string Name { get; set; } = string.Empty;
    [Key(2)]
    public AuraLineType LineType { get; set; } = AuraLineType.Dashes;
    [Key(3)]
    public bool isSpining { get; set; } = false;
    [JsonIgnore, IgnoreMember]
    public Color AuraColor { get; private set; } = Color.magenta;
    [JsonIgnore, Key(4)]
    public string _ReticleColor { get; set; } = string.Empty;
    [IgnoreMember]
    public string ReticleColor {
      get {
        return "#" + ColorUtility.ToHtmlStringRGB(this.AuraColor);
      }
      set {
        Color temp;
        if (ColorUtility.TryParseHtmlString(value, out temp)) {
          AuraColor = temp;
        } else {
          Log.Error?.WL(0, $"Bad color:{value}",true);
          AuraColor = UnityEngine.Color.magenta;
        }
        _ReticleColor = ReticleColor;
      }
    }
    [Key(5)]
    public float Range { get; set; } = 0f;
    [Key(6)]
    public string RangeStatistic { get; set; } = string.Empty;
    [Key(7)]
    public bool RemoveOnSensorLock { get; set; } = false;
    [Key(8)]
    public bool NotApplyMoving { get; set; } = false;
    [Key(9)]
    public bool ApplyOnlyMoving { get; set; } = false;
    [Key(10)]
    public bool HideOnNotSelected { get; set; } = true;
    [Key(11)]
    public bool NotShowOnSelected { get; set; } = false;
    [Key(12)]
    public bool FloatieAtEndOfMove { get; set; } = true;
    [Key(13)]
    public bool ApplySelf { get; set; } = false;
    [Key(14)]
    public bool TrackAcivation { get; set; } = false;
    [Key(15)]
    public AuraState State { get; set; } = AuraState.Persistent;
    [Key(16)]
    public StealthAffection AllyStealthAffection { get; set; } = StealthAffection.None;
    [Key(17)]
    public StealthAffection EnemyStealthAffection { get; set; } = StealthAffection.None;
    [Key(18)]
    public bool IsPositiveToAlly { get; set; } = false;
    [Key(19)]
    public bool IsNegativeToAlly { get; set; } = false;
    [Key(20)]
    public bool IsNegativeToEnemy { get; set; } = false;
    [Key(21)]
    public bool IsPositiveToEnemy { get; set; } = false;
    [Key(22)]
    public bool MinefieldDetector { get; set; } = false;
    [Key(23)]
    public List<EffectData> OnFireEffects { get; set; } = new List<EffectData>();
    [Key(24)]
    public List<AuraBubbleVFXDef> onlineVFX { get; set; } = new List<AuraBubbleVFXDef>();
    [Key(25)]
    public List<string> targetVFX { get; set; } = new List<string>();
    [Key(26)]
    public List<string> removeOwnerVFX { get; set; } = new List<string>();
    [Key(27)]
    public List<string> removeTargetVFX { get; set; } = new List<string>();
    [JsonIgnore, IgnoreMember]
    public TagSet _neededTags { get; set; } = new TagSet();
    [Key(28)]
    private List<string> f_neededTags { get; set; } = new List<string>();
    [IgnoreMember]
    public List<string> neededTags {
      set {
        _neededTags.Clear();
        f_neededTags = new List<string>(value);
        foreach (string tag in value) { _neededTags.Add(tag); }
      }
    }
    [JsonIgnore, IgnoreMember]
    public TagSet _neededOwnerTags { get; set; } = new TagSet();
    [Key(29)]
    private List<string> f_neededOwnerTags { get; set; } = new List<string>();
    [IgnoreMember]
    public List<string> neededOwnerTags {
      set {
        _neededOwnerTags.Clear();
        f_neededOwnerTags = new List<string>(value);
        foreach (string tag in value) { _neededOwnerTags.Add(tag); }
      }
    }
    [Key(30)]
    public List<string> ownerSFX { get; set; } = new List<string>();
    [Key(31)]
    public List<string> targetSFX { get; set; } = new List<string>();
    [Key(32)]
    public List<string> removeOwnerSFX { get; set; } = new List<string>();
    [Key(33)]
    public List<string> removeTargetSFX { get; set; } = new List<string>();
    [Key(34)]
    public List<EffectData> statusEffects { get; set; } = new List<EffectData>();
    public bool checkTarget(AbstractActor unit) {
      if (_neededTags.Count == 0) { return true; }
      return unit.EncounterTags.ContainsAll(_neededTags);
    }
    public bool checkOwner(AbstractActor unit) {
      if (_neededOwnerTags.Count == 0) { return true; }
      return unit.EncounterTags.ContainsAll(_neededOwnerTags);
    }
    public static AuraDef FromJSON(JObject json, bool load_external = true) {
      AuraDef aura = json.ToObject<AuraDef>();
      if (load_external) {
        if (string.IsNullOrEmpty(aura.Name)) {
          if (AuraDef.dataManager.TryGetValue(aura.Id, out var result)) { return result; }
          if(AuraDef.manifest.TryGetValue(aura.Id, out var entry)) {
            try {
              JObject ext_json = JObject.Parse(File.ReadAllText(entry.FilePath));
              AuraDef ext_aura = AuraDef.FromJSON(ext_json, false);
              AuraDef.dataManager.Add(ext_aura.Id, ext_aura);
              Log.Debug?.WL(0,$"Load aura {aura.Id} from {entry.FilePath}");
              return ext_aura;
            } catch(Exception e) {
              Log.Error?.TWL(0,entry.FilePath);
              Log.Error?.WL(0, e.ToString(), true);
            }
          }
        }
      }
      if (json["statusEffects"] != null) {
        if (json["statusEffects"].Type == JTokenType.Array) {
          JToken statusEffects = json["statusEffects"];
          aura.statusEffects = new List<EffectData>();
          foreach (JObject statusEffect in statusEffects) {
            BattleTech.EffectData effect = new BattleTech.EffectData();
            JSONSerializationUtility.FromJSON<BattleTech.EffectData>(effect, statusEffect.ToString());
            aura.statusEffects.Add(effect);
          }
        }
      }
      //Log.Debug?.TWL(0,$"Aura.FromJSON {aura.Id} statusEffects:{aura.statusEffects.Count}");
      //foreach(var effect in aura.statusEffects) {
        //Log.Debug?.WL(1, $"Id:{effect.Description.Id} Name:'{effect.Description.Name}'");
      //}
      return aura;
    }
    public static void ExtractAuras(JObject content, List<AuraDef> auras) {
      if (content["Auras"] != null) {
        if (content["Auras"].Type == JTokenType.Array) {
          JToken jauras = content["Auras"];
          foreach (JObject jaura in jauras) { AuraDef aura = AuraDef.FromJSON(jaura); auras.Add(aura); }
        }
        content.Remove("Auras");
      }
    }
    public static void fromPrewarm(List<AuraDef> auras) {
      foreach(var aura in auras) {
        aura.neededTags = aura.f_neededTags;
        aura.neededOwnerTags = aura.f_neededOwnerTags;
        aura.ReticleColor = aura._ReticleColor;
      }
    }
  }
}

namespace CustomActivatableEquipment {
  public class AuraDefsParseState {
    public string exception { get; set; } = string.Empty;
    public List<AuraDef> payload { get; set; } = new List<AuraDef>();
  }
  [HarmonyPatch(typeof(UpgradeDef))]
  [HarmonyPatch("FromJSON")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class UpgradeDef_FromJSONAuras {
    public static void Prefix(MechComponentDef __instance, ref string json, ref AuraDefsParseState __state) {
      __state = new AuraDefsParseState();
      try {
        if (__instance.Description != null) {
          __state.payload = CustomPrewarm.Core.getDeserializedObject(BattleTechResourceType.UpgradeDef, __instance.Description.Id, "CustomActivatableEquipment") as List<AuraDef>;
          if (__state.payload != null) { AuraDef.fromPrewarm(__state.payload); return; }
          __state.payload = new List<AuraDef>();
          Log.Debug?.TWL(0, "UpgradeDef.FromJSON Description:" + __instance.Description.Id + " is not null, but auras list not found");
        }
        JObject content = JObject.Parse(json);
        AuraDef.ExtractAuras(content, __state.payload);
        json = content.ToString();
      } catch (Exception e) {
        __state.exception = e.ToString();
      }
    }
    public static void Postfix(MechComponentDef __instance, ref AuraDefsParseState __state) {
      try {
        if (__state == null) { return; }
        if (__instance == null) { return; }
        if (string.IsNullOrEmpty(__state.exception) == false) {
          Log.Error?.TWL(0, __instance.Description?.Id);
          Log.Error?.WL(0, __state.exception, true);
        }
        __instance.AddAuras(__state.payload);
      } catch(Exception e) {
        Log.Error?.TWL(0, __instance.Description?.Id);
        Log.Error?.WL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(WeaponDef))]
  [HarmonyPatch("FromJSON")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class WeaponDef_FromJSONAuras {
    public static void Prefix(MechComponentDef __instance, ref string json, ref AuraDefsParseState __state) {
      __state = new AuraDefsParseState();
      try {
        if (__instance.Description != null) {
          __state.payload = CustomPrewarm.Core.getDeserializedObject(BattleTechResourceType.WeaponDef, __instance.Description.Id, "CustomActivatableEquipment") as List<AuraDef>;
          if (__state.payload != null) { AuraDef.fromPrewarm(__state.payload); return; }
          __state.payload = new List<AuraDef>();
          Log.Debug?.TWL(0, "WeaponDef.FromJSON Description:" + __instance.Description.Id + " is not null, but auras list not found");
        }
        JObject content = JObject.Parse(json);
        AuraDef.ExtractAuras(content, __state.payload);
        json = content.ToString();
      } catch (Exception e) {
        __state.exception = e.ToString();
      }
    }
    public static void Postfix(MechComponentDef __instance, ref AuraDefsParseState __state) {
      try {
        if (__state == null) { return; }
        if (__instance == null) { return; }
        if (string.IsNullOrEmpty(__state.exception) == false) {
          Log.Error?.TWL(0, __instance.Description?.Id);
          Log.Error?.WL(0, __state.exception, true);
        }
        __instance.AddAuras(__state.payload);
      } catch (Exception e) {
        Log.Error?.TWL(0, __instance.Description?.Id);
        Log.Error?.WL(0, e.ToString(), true);
      }
    }
  }
  [HarmonyPatch(typeof(AbilityDef))]
  [HarmonyPatch("FromJSON")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class AbilityDef_FromJSONAuras {
    public static void Prefix(AbilityDef __instance, ref string json, ref AuraDefsParseState __state) {
      __state = new AuraDefsParseState();
      try {
        if (__instance.Description != null) {
          if (string.IsNullOrEmpty(__instance.Description.Id) == false) {
            __state.payload = CustomPrewarm.Core.getDeserializedObject(BattleTechResourceType.AbilityDef, __instance.Description.Id, "CustomActivatableEquipment") as List<AuraDef>;
            if (__state.payload != null) { AuraDef.fromPrewarm(__state.payload); return; }
            __state.payload = new List<AuraDef>();
            Log.Debug?.TWL(0, $"AbilityDef.FromJSON Description:'{__instance.Description.Id}' is not null, but auras list not found");
          }
        }
        JObject content = JObject.Parse(json);
        AuraDef.ExtractAuras(content, __state.payload);
        json = content.ToString();
      } catch (Exception e) {
        __state.exception = e.ToString();
      }
    }
    public static void Postfix(AbilityDef __instance, ref AuraDefsParseState __state) {
      try {
        if (__state == null) { return; }
        if (__instance == null) { return; }
        if (string.IsNullOrEmpty(__state.exception) == false) {
          Log.Error?.TWL(0, __instance.Description?.Id);
          Log.Error?.WL(0, __state.exception, true);
        }
        __instance.AddAuras(__state.payload);
      } catch (Exception e) {
        Log.Error?.TWL(0, __instance.Description?.Id);
        Log.Error?.WL(0, e.ToString(), true);
        UnityGameInstance.BattleTechGame.DataManager?.logger.LogException(e);
      }
    }
  }
}
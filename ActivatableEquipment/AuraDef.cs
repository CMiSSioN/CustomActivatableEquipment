using BattleTech;
using CustomActivatableEquipment;
using Harmony;
using HBS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatableEquipment {
  public enum AuraState {
    Online,
    Offline,
    Persistent
  }
  public enum AuraLineType { Dashes, Dots }
  public class AuraBubbleVFXDef {
    public string VFXname { get; set; }
    public bool scale { get; set; }
    public float scaleToRangeFactor { get; set; }
    public AuraBubbleVFXDef() {
      scale = false;
      scaleToRangeFactor = 1f;
    }
  }
  public class AuraDef {
    public string Id { get; set; }
    public string Name { get; set; }
    public AuraLineType LineType { get; set; }
    public bool isSpining { get; set; }
    public Color AuraColor { get; private set; }
    public string ReticleColor {
      set {
        Color temp;
        if (ColorUtility.TryParseHtmlString(value, out temp)) {
          AuraColor = temp;
        } else {
          Log.WriteCritical("Bad color:" + value + "\n");
          AuraColor = UnityEngine.Color.magenta;
        }
      }
    }
    public float Range { get; set; }
    public string RangeStatistic { get; set; }
    public bool RemoveOnSensorLock { get; set; }
    public bool HideOnNotSelected { get; set; }
    public bool NotShowOnSelected { get; set; }
    public bool FloatieAtEndOfMove { get; set; }
    public bool ApplySelf { get; set; }
    public AuraState State { get; set; }
    public StealthAffection AllyStealthAffection { get; set; }
    public StealthAffection EnemyStealthAffection { get; set; }
    public bool IsPositiveToAlly { get; set; }
    public bool IsNegativeToAlly { get; set; }
    public bool IsNegativeToEnemy { get; set; }
    public bool IsPositiveToEnemy { get; set; }
    public bool MinefieldDetector { get; set; }
    public List<EffectData> OnFireEffects { get; set; }
    public List<AuraBubbleVFXDef> onlineVFX { get; set; }
    public List<string> targetVFX { get; set; }
    public List<string> removeOwnerVFX { get; set; }
    public List<string> removeTargetVFX { get; set; }
    public List<string> ownerSFX { get; set; }
    public List<string> targetSFX { get; set; }
    public List<string> removeOwnerSFX { get; set; }
    public List<string> removeTargetSFX { get; set; }
    public List<EffectData> statusEffects { get; set; }
    public AuraDef() {
      AllyStealthAffection = StealthAffection.None;
      EnemyStealthAffection = StealthAffection.None;
      IsPositiveToAlly = false;
      IsNegativeToAlly = false;
      IsNegativeToEnemy = false;
      IsPositiveToEnemy = false;
      Range = 0f;
      State = AuraState.Persistent;
      ApplySelf = false;
      RemoveOnSensorLock = false;
      OnFireEffects = new List<EffectData>();
      onlineVFX = new List<AuraBubbleVFXDef>();
      targetVFX = new List<string>();
      removeOwnerVFX = new List<string>();
      removeTargetVFX = new List<string>();
      ownerSFX = new List<string>();
      targetSFX = new List<string>();
      removeOwnerSFX = new List<string>();
      removeTargetSFX = new List<string>();
      statusEffects = new List<EffectData>();
      HideOnNotSelected = true;
      NotShowOnSelected = false;
      FloatieAtEndOfMove = true;
      LineType = AuraLineType.Dashes;
      isSpining = false;
      MinefieldDetector = false;
    }
  }
}

namespace CustAmmoCategoriesPatches {
  [HarmonyPatch(typeof(UpgradeDef))]
  [HarmonyPatch("FromJSON")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class UpgradeDef_FromJSONAuras {
    public static void Prefix(MechComponentDef __instance, ref string json) {
      try {
        JObject content = JObject.Parse(json);
        if (content["Auras"] != null) {
          if (content["Auras"].Type == JTokenType.Array) {
            List<AuraDef> auras = new List<AuraDef>();
            JToken jauras = content["Auras"];
            foreach (JObject jaura in jauras) {
              AuraDef aura = JsonConvert.DeserializeObject<AuraDef>(jaura.ToString());
              if (jaura["statusEffects"] != null) {
                if (jaura["statusEffects"].Type == JTokenType.Array) {
                  JToken statusEffects = jaura["statusEffects"];
                  aura.statusEffects = new List<EffectData>();
                  foreach (JObject statusEffect in statusEffects) {
                    EffectData effect = new EffectData();
                    JSONSerializationUtility.FromJSON<EffectData>(effect, statusEffect.ToString());
                    aura.statusEffects.Add(effect);
                  }
                }
              }
              auras.Add(aura);
            }
            __instance.AddAuras(auras);
          }
          content.Remove("Auras");
        }
        json = content.ToString();
      } catch (Exception e) {
        Log.Debug?.Write("Error:" + e.ToString() + "\n");
        Log.Debug?.Write("IN:" + json + "\n");
      }
    }
  }
  [HarmonyPatch(typeof(WeaponDef))]
  [HarmonyPatch("FromJSON")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class WeaponDef_FromJSONAuras {
    public static void Prefix(MechComponentDef __instance, ref string json) {
      try {
        JObject content = JObject.Parse(json);
        if (content["Auras"] != null) {
          Log.Debug?.Write("Auras WeaponDef:"+ content["Description"]["Id"]+"\n");
          if (content["Auras"].Type == JTokenType.Array) {
            List<AuraDef> auras = new List<AuraDef>();
            JToken jauras = content["Auras"];
            foreach (JObject jaura in jauras) {
              AuraDef aura = JsonConvert.DeserializeObject<AuraDef>(jaura.ToString());
              if (jaura["statusEffects"] != null) {
                if (jaura["statusEffects"].Type == JTokenType.Array) {
                  JToken statusEffects = jaura["statusEffects"];
                  aura.statusEffects = new List<EffectData>();
                  foreach (JObject statusEffect in statusEffects) {
                    EffectData effect = new EffectData();
                    JSONSerializationUtility.FromJSON<EffectData>(effect, statusEffect.ToString());
                    aura.statusEffects.Add(effect);
                  }
                }
              }
              auras.Add(aura);
            }
            __instance.AddAuras(auras);
          }
          content.Remove("Auras");
        }
        json = content.ToString();
      } catch (Exception e) {
        Log.Debug?.Write("Error:" + e.ToString() + "\n");
        Log.Debug?.Write("IN:" + json + "\n");
      }
    }
  }
}
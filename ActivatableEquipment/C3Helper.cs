using BattleTech;
using BattleTech.UI;
using CustAmmoCategories;
using Harmony;
using HBS.Collections;
using Localize;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(CombatHUDWeaponSlot), "AddToolTipDetail")]
  public static class CombatHUDWeaponSlot_AddToolTipDetail {
    public static void Postfix(CombatHUDWeaponSlot __instance, string description, int modifier) {
      try {
        if (modifier != 0) { return; }
        __instance.ToolTipHoverElement.BuffStrings.Add(new Text("{0} +0", new object[1]
        {
          (object) description
        }));
      } catch (Exception ex) {
        Log.Error?.TWL(0, ex.ToString(), true);
      }
    }
  }
  public static class C3Helper {
    private static Dictionary<AbstractActor, Dictionary<ICombatant, Vector3>> C3Cache = new Dictionary<AbstractActor, Dictionary<ICombatant, Vector3>>();
    public static void Clear() {
      C3Cache.Clear();
    }
    public static Vector3 GetC3NonCachedPos(AbstractActor attacker, ICombatant target) {
      try {
        if (attacker.EncounterTags.ContainsAny(Core.Settings._C3NetworkEncounterTags) == false) {
          return Vector3.zero;
        }
        TagSet effectiveC3tags = new TagSet();
        foreach(var tag in Core.Settings._C3NetworkEncounterTags) {
          if (attacker.EncounterTags.Contains(tag)) { effectiveC3tags.Add(tag); }
        }
        List<AbstractActor> allies = attacker.Combat.GetAllAlliesOf(attacker);
        AbstractActor closest = null;
        float closest_dist = 0f;
        foreach(AbstractActor ally in allies) {
          if (ally.IsDead) { continue; }
          if (ally.IsDeployDirector()) { continue; }
          if (ally.IsShutDown) { continue; }
          if (ally.EncounterTags.ContainsAny(effectiveC3tags) == false) { continue; }
          if (closest == null) { closest = ally; closest_dist = Vector3.Distance(target.CurrentPosition, ally.CurrentPosition); continue; }
          float dist = Vector3.Distance(target.CurrentPosition, ally.CurrentPosition);
          if (dist < closest_dist) { closest_dist = dist; closest = ally; }
        }
        if(closest != null) {
          Log.Debug?.TWL(0, "found C3 spotter for "+ attacker.PilotableActorDef.ChassisID+" target: "+ target.DisplayName+" spotter:"+closest.PilotableActorDef.ChassisID+" distance: "+closest_dist);
        }
        return closest == null ? Vector3.zero: closest.CurrentPosition;
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        return Vector3.zero;
      }
    }
    public static Vector3 GetC3CachedPos(AbstractActor attacker, ICombatant target) {      
      if (C3Cache.TryGetValue(attacker, out var c3team_cache)) {
        if(c3team_cache.TryGetValue(target, out var result)) {
          return result;
        } else {
          result = GetC3NonCachedPos(attacker, target);
          c3team_cache.Add(target, result);
          return result;
        }
      } else {
        c3team_cache = new Dictionary<ICombatant, Vector3>();
        Vector3 result = GetC3NonCachedPos(attacker, target);
        c3team_cache.Add(target, result);
        C3Cache.Add(attacker, c3team_cache);
        return result;
      }      
    }
    public static void Init() {
      ToHitModifiersHelper.registerModifier2("RANGE", "RANGE", true, false, C3Helper.GetRangeModifier, C3Helper.GetRangeModifierName);
    }
    public static float GetRangeModifier(ToHit instance, AbstractActor attacker, Weapon weapon, ICombatant target, Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, MeleeAttackType meleeAttackType, bool isCalledShot) {
      if (attacker.EncounterTags.ContainsAny(Core.Settings._C3NetworkEncounterTags) == false) {
        return ToHitModifiersHelper.GetRangeModifier(instance, attacker, weapon, target, attackPosition, targetPosition, lofLevel, meleeAttackType, isCalledShot);
      }
      float realDist = Vector3.Distance(attackPosition, targetPosition);
      float distance = realDist;
      float distMod = 0f;
      float minRange = weapon.MinRange;
      float shortRange = weapon.ShortRange;
      float medRange = weapon.MediumRange;
      float longRange = weapon.LongRange;
      float maxRange = weapon.MaxRange;
      Vector3 alternateAttackPos = GetC3CachedPos(attacker, target);
      if (alternateAttackPos != Vector3.zero) {
        float alternateDist = Vector3.Distance(alternateAttackPos, targetPosition);
        if (alternateDist < realDist) {
          if (alternateDist < minRange) { distance = minRange; } else { distance = alternateDist; }
        }
      }
      if (distance < minRange) {
        distMod = weapon.parent.MinRangeAccMod();
        //Log.LogWrite(" minRange "); 
      } else
      if (distance < shortRange) {
        distMod = weapon.parent.ShortRangeAccMod();
        //Log.LogWrite(" shortRange ");
      } else
      if (distance < medRange) {
        distMod = weapon.parent.MediumRangeAccMod();
        //Log.LogWrite(" medRange ");
      } else
      if (distance < longRange) {
        distMod = weapon.parent.LongRangeRangeAccMod();
        //Log.LogWrite(" longRange ");
      } else
      if (distance < maxRange) {
        distMod = weapon.parent.ExtraLongRangeAccMod();
        //Log.LogWrite(" extraRange ");
      };
      //return distMod;
      return distMod + instance.GetRangeModifierForDist(weapon, distance);
    }
    private static string SmartRange(float min, float range, float max) {
      if (min <= 0 || range - min > max - range)
        return " (<" + (int)max + "m)"; // Show next range boundery when no lower boundary or target is closer to next than lower.
      return " (>" + (int)min + "m)";
    }
    public static string GetRangeModifierName(ToHit instance, AbstractActor attacker, Weapon w, ICombatant target, Vector3 attackPosition, Vector3 targetPosition, LineOfFireLevel lofLevel, MeleeAttackType meleeAttackType, bool isCalledShot, int modifier) {
      if (attacker.EncounterTags.ContainsAny(Core.Settings._C3NetworkEncounterTags) == false) {
        if (modifier == 0) { return string.Empty; }
        return ToHitModifiersHelper.GetRangeModifierName(instance, attacker, w, target, attackPosition, targetPosition, lofLevel, meleeAttackType, isCalledShot);
      }
      float real_range = Vector3.Distance(attackPosition, targetPosition);
      float range = real_range;
      float MinRange = w.MinRange;
      float ShortRange = w.ShortRange;
      float MediumRange = w.MediumRange;
      float LongRange = w.LongRange;
      float MaxRange = w.MaxRange;
      string c3_prefix = string.Empty;
      Vector3 alternateAttackPos = GetC3CachedPos(attacker, target);
      if (alternateAttackPos != Vector3.zero) {
        float alternateDist = Vector3.Distance(alternateAttackPos, targetPosition);
        //Log.Debug?.TWL(0, "GetRangeModifierName "+attacker.PilotableActorDef.ChassisID+" weapon:"+w.defId+" target:"+target.PilotableActorDef.ChassisID+" real_dist:"+ real_range+" alt dist:"+ alternateDist+" max range:"+MaxRange+" modifier:"+modifier);
        if ((alternateDist < real_range) && (alternateDist < MaxRange)) {
          c3_prefix = "(C3)";
          if (alternateDist < MinRange) { range = MinRange; } else { range = alternateDist; }
        }
      }
      if (string.IsNullOrEmpty(c3_prefix)) {
        if (modifier == 0) { return string.Empty; }
      }
      if (range < MinRange) return (c3_prefix+"__/AIM.MIN_RANGE/__ (<" + MinRange + "m)");
      if (range < ShortRange) return (c3_prefix + "__/AIM.SHORT_RANGE/__" + SmartRange(MinRange, range, ShortRange));
      if (range < MediumRange) return (c3_prefix + "__/AIM.MED_RANGE/__" + SmartRange(ShortRange, range, MediumRange));
      if (range < LongRange) return (c3_prefix + "__/AIM.LONG_RANGE/__" + SmartRange(MediumRange, range, LongRange));
      if (range < MaxRange) return (c3_prefix + "__/AIM.MAX_RANGE/__" + SmartRange(LongRange, range, MaxRange));
      return (c3_prefix + "__/AIM.OUT_OF_RANGE/__ (>" + MaxRange + "m)");
    }

  }
}
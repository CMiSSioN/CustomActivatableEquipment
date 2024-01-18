using BattleTech;
using BattleTech.UI;
using HarmonyLib;
using HBS.Collections;
using System;
using System.Collections.Generic;

namespace CustomActivatableEquipment {
  public interface IMechComponentDynamicDef {
    MechComponentDef original { get; set; }
    void Update(MechComponentDef orig);
    void Reset();
  }
  public class MechComponentDynamicDef: MechComponentDef, IMechComponentDynamicDef {
    public MechComponentDef original { get; set; }
    public static void Update(MechComponentDef dest, MechComponentDef source) {
      if(source is IMechComponentDynamicDef srcdyn) {
        if(dest is IMechComponentDynamicDef dyn) { dyn.original = srcdyn.original; }
      } else {
        if(dest is IMechComponentDynamicDef dyn) { dyn.original = source; }
      }
      dest.AbilityDefs = source.AbilityDefs == null ? null : new List<AbilityDef>(source.AbilityDefs);
    }
    public void Update(MechComponentDef orig) { MechComponentDynamicDef.Update(this, orig); }
    public void Reset() { ComponentDefHelper.CopyFromTo_MechComponentDef(this.original, this); }
  }
  public class AmmunitionBoxDynamicDef: AmmunitionBoxDef, IMechComponentDynamicDef {
    public MechComponentDef original { get; set; }
    public AmmunitionBoxDef originalDef { get { return this.original as AmmunitionBoxDef; } }
    public void Update(MechComponentDef orig) {
      MechComponentDynamicDef.Update(this, orig);
      if(orig is AmmunitionBoxDef box) { this.Ammo = box.Ammo; }
    }
    public void Reset() { ComponentDefHelper.CopyFromTo_AmmunitionBoxDef(this.originalDef, this); }
  }
  public class HeatSinkDynamicDef :HeatSinkDef, IMechComponentDynamicDef {
    public MechComponentDef original { get; set; }
    public HeatSinkDef originalDef { get { return this.original as HeatSinkDef; } }
    public void Update(MechComponentDef orig) { MechComponentDynamicDef.Update(this, orig); }
    public void Reset() { ComponentDefHelper.CopyFromTo_HeatSinkDef(this.originalDef, this); }
  }
  public class JumpJetDynamicDef :JumpJetDef, IMechComponentDynamicDef {
    public MechComponentDef original { get; set; }
    public JumpJetDef originalDef { get { return this.original as JumpJetDef; } }
    public void Update(MechComponentDef orig) { MechComponentDynamicDef.Update(this, orig); }
    public void Reset() { ComponentDefHelper.CopyFromTo_JumpJetDef(this.originalDef, this); }
  }
  public class StatUpgradeDynamicDef :StatUpgradeDef, IMechComponentDynamicDef {
    public MechComponentDef original { get; set; }
    public StatUpgradeDef originalDef { get { return this.original as StatUpgradeDef; } }
    public void Update(MechComponentDef orig) { MechComponentDynamicDef.Update(this, orig); }
    public void Reset() { ComponentDefHelper.CopyFromTo_StatUpgradeDef(this.originalDef, this); }
  }
  public class UpgradeDynamicDef :UpgradeDef, IMechComponentDynamicDef {
    public MechComponentDef original { get; set; }
    public UpgradeDef originalDef { get { return this.original as UpgradeDef; } }
    public void Update(MechComponentDef orig) { MechComponentDynamicDef.Update(this, orig); }
    public void Reset() { ComponentDefHelper.CopyFromTo_UpgradeDef(this.originalDef, this); }
  }
  public class WeaponDynamicDef :WeaponDef, IMechComponentDynamicDef {
    public MechComponentDef original { get; set; }
    public WeaponDef originalDef { get { return this.original as WeaponDef; } }
    public void Update(MechComponentDef orig) { MechComponentDynamicDef.Update(this, orig); }
    public void Reset() { ComponentDefHelper.CopyFromTo_WeaponDef(this.originalDef, this); }
  }
  public static class ComponentDefHelper {
    public static void CopyFromTo_BaseDescriptionDef(BaseDescriptionDef src, BaseDescriptionDef dest) {
      dest.detailsParsed = src.detailsParsed;
      dest.localizedDetails = src.localizedDetails;
      dest.Id = src.Id;
      dest.Name = src.Name;
      dest.Details = src.Details;
      dest.Icon = src.Icon;
    }
    public static void CopyFromTo_DescriptionDef(DescriptionDef src, DescriptionDef dest) {
      CopyFromTo_BaseDescriptionDef(src, dest);
      dest.Cost = src.Cost;
      dest.Rarity = src.Rarity;
      dest.Purchasable = src.Purchasable;
      dest.Manufacturer = src.Manufacturer;
      dest.Model = src.Model;
      dest.UIName = src.UIName;
    }
    public static void CopyFromTo_MechComponentDef(MechComponentDef src, MechComponentDef dest) {
      dest.dataManager = src.dataManager;
      if(dest.Description == null) { dest.Description = new DescriptionDef(); }
      CopyFromTo_DescriptionDef(src.Description, dest.Description);
      dest.BonusValueA = src.BonusValueA;
      dest.BonusValueB = src.BonusValueB;
      dest.ComponentType = src.ComponentType;
      dest.ComponentSubType = src.ComponentSubType;
      dest.PrefabIdentifier = src.PrefabIdentifier;
      dest.BattleValue = src.BattleValue;
      dest.InventorySize = src.InventorySize;
      dest.Tonnage = src.Tonnage;
      dest.AllowedLocations = src.AllowedLocations;
      dest.DisallowedLocations = src.DisallowedLocations;
      dest.CriticalComponent = src.CriticalComponent;
      dest.CanExplode = src.CanExplode;
      dest.statusEffects = src.statusEffects;
      dest.AbilityDefs = src.AbilityDefs == null ? null : new List<AbilityDef>(src.AbilityDefs);
      dest.additionalData = src.additionalData;
      dest.ComponentTags = dest.ComponentTags == null? null : new TagSet(src.ComponentTags);      
    }
    public static void CopyFromTo_AmmunitionBoxDef(AmmunitionBoxDef src, AmmunitionBoxDef dest) {
      CopyFromTo_MechComponentDef(src, dest);
      dest.ammoID = src.ammoID;
      dest.Ammo = src.Ammo;
      dest.Capacity = src.Capacity;
    }
    public static void CopyFromTo_HeatSinkDef(HeatSinkDef src, HeatSinkDef dest) {
      CopyFromTo_MechComponentDef(src, dest);
      dest.DissipationCapacity = src.DissipationCapacity;
    }
    public static void CopyFromTo_JumpJetDef(JumpJetDef src, JumpJetDef dest) {
      CopyFromTo_MechComponentDef(src, dest);
      dest.JumpCapacity = src.JumpCapacity;
      dest.MinTonnage = src.MinTonnage;
      dest.MaxTonnage = src.MaxTonnage;
    }
    public static void CopyFromTo_StatUpgradeDef(StatUpgradeDef src, StatUpgradeDef dest) {
      CopyFromTo_MechComponentDef(src, dest);
      dest.StatName = src.StatName;
      dest.RelativeModifier = src.RelativeModifier;
      dest.AbsoluteModifier = src.AbsoluteModifier;
    }
    public static void CopyFromTo_UpgradeDef(UpgradeDef src, UpgradeDef dest) {
      CopyFromTo_MechComponentDef(src, dest);
      dest.StatName = src.StatName;
      dest.RelativeModifier = src.RelativeModifier;
      dest.AbsoluteModifier = src.AbsoluteModifier;
    }
    public static void CopyFromTo_WeaponDef(WeaponDef src, WeaponDef dest) {
      CopyFromTo_MechComponentDef(src, dest);
      dest.weaponCategoryID = src.weaponCategoryID;
      dest.weaponCategoryValue = src.weaponCategoryValue;
      dest.ammoCategoryID = src.ammoCategoryID;
      dest.ammoCategoryValue = src.ammoCategoryValue;
      dest.Category = src.Category;
      dest.Type = src.Type;
      dest.WeaponSubType = src.WeaponSubType;
      dest.MinRange = src.MinRange;
      dest.MaxRange = src.MaxRange;
      dest.RangeSplit = src.RangeSplit;
      dest.StartingAmmoCapacity = src.StartingAmmoCapacity;
      dest.HeatGenerated = src.HeatGenerated;
      dest.Damage = src.Damage;
      dest.OverheatedDamageMultiplier = src.OverheatedDamageMultiplier;
      dest.EvasiveDamageMultiplier = src.EvasiveDamageMultiplier;
      dest.EvasivePipsIgnored = src.EvasivePipsIgnored;
      dest.DamageVariance = src.DamageVariance;
      dest.HeatDamage = src.HeatDamage;
      dest.StructureDamage = src.StructureDamage;
      dest.AccuracyModifier = src.AccuracyModifier;
      dest.CriticalChanceMultiplier = src.CriticalChanceMultiplier;
      dest.AOECapable = src.AOECapable;
      dest.IndirectFireCapable = src.IndirectFireCapable;
      dest.RefireModifier = src.RefireModifier;
      dest.ShotsWhenFired = src.ShotsWhenFired;
      dest.ProjectilesPerShot = src.ProjectilesPerShot;
      dest.VolleyDivisor = src.VolleyDivisor;
      dest.AttackRecoil = src.AttackRecoil;
      dest.Instability = src.Instability;
      dest.ClusteringModifier = src.ClusteringModifier;
      dest.WeaponEffectID = src.WeaponEffectID;
    }
    public static object CreateInstance(object source) {
      object result = null;
      if(source is WeaponDef) { result = new WeaponDynamicDef(); }else
      if(source is UpgradeDef) { result = new UpgradeDynamicDef(); } else
      if(source is StatUpgradeDef) { result = new StatUpgradeDynamicDef(); } else
      if(source is JumpJetDef) { result = new JumpJetDynamicDef(); } else
      if(source is HeatSinkDef) { result = new HeatSinkDynamicDef(); } else
      if(source is AmmunitionBoxDef) { result = new AmmunitionBoxDynamicDef(); } else
      if(source is MechComponentDef) { result = new MechComponentDynamicDef(); } else {
        result = Activator.CreateInstance(source.GetType());
      }
      if(result is IMechComponentDynamicDef dynDef) { dynDef.original = source as MechComponentDef; }
      return result;
    }
    public static object DeepCopy(object source) {
      try {
        if(source == null) { return null; }
        object result = CreateInstance(source);
        if((result is WeaponDef dest_WeaponDef) && (source is WeaponDef src_WeaponDef)) { CopyFromTo_WeaponDef(src_WeaponDef, dest_WeaponDef); } else
        if((result is UpgradeDef dest_UpgradeDef) && (source is UpgradeDef src_UpgradeDef)) { CopyFromTo_UpgradeDef(src_UpgradeDef, dest_UpgradeDef); } else
        if((result is StatUpgradeDef dest_StatUpgradeDef) && (source is StatUpgradeDef src_StatUpgradeDef)) { CopyFromTo_StatUpgradeDef(src_StatUpgradeDef, dest_StatUpgradeDef); } else
        if((result is JumpJetDef dest_JumpJetDef) && (source is JumpJetDef src_JumpJetDef)) { CopyFromTo_JumpJetDef(src_JumpJetDef, dest_JumpJetDef); } else
        if((result is HeatSinkDef dest_HeatSinkDef) && (source is HeatSinkDef src_HeatSinkDef)) { CopyFromTo_HeatSinkDef(src_HeatSinkDef, dest_HeatSinkDef); } else
        if((result is AmmunitionBoxDef dest_AmmunitionBoxDef) && (source is AmmunitionBoxDef src_AmmunitionBoxDef)) { CopyFromTo_AmmunitionBoxDef(src_AmmunitionBoxDef, dest_AmmunitionBoxDef); } else
        if((result is MechComponentDef dest_MechComponentDef) && (source is MechComponentDef src_MechComponentDef)) { CopyFromTo_MechComponentDef(src_MechComponentDef, dest_MechComponentDef); } else {
          result = source;
        }
        return result;
      } catch(Exception e) {
        if(source is MechComponentDef src) {
          Log.Error?.TWL(0, $"id:{(src.Description == null?"null":src.Description.Id)}");
        }        
        Log.Error?.WL(0,e.ToString());
        UnityGameInstance.logger.LogException(e);
        return source;
      }
    }
  }
  [HarmonyPatch(typeof(BaseComponentRef))]
  [HarmonyPatch(MethodType.Constructor)]
  [HarmonyPatch(new Type[] { typeof(BaseComponentRef), typeof(string) })]
  public static class BaseComponentRef_Constructor {
    public static void Postfix(BaseComponentRef __instance, BaseComponentRef other, string simGameUID) {
      try {
        if(other.Def is IMechComponentDynamicDef) {
          __instance.Def = other.Def;
        }
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UnityGameInstance.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(BaseComponentRef))]
  [HarmonyPatch("RefreshComponentDef")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class RefreshComponentDef_RefreshComponentDef {
    public static void Prefix(BaseComponentRef __instance, ref IMechComponentDynamicDef __state) {
      try {
        //Log.Debug?.TWL(0, $"BaseComponentRef.RefreshComponentDef prefix {__instance.componentDefID}:{__instance.GetHashCode()}:{(__instance.Def == null?"null":__instance.Def.GetType().ToString())}");
        __state = __instance.Def as IMechComponentDynamicDef;
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UnityGameInstance.logger.LogException(e);
      }
    }
    public static void Postfix(BaseComponentRef __instance, ref IMechComponentDynamicDef __state) {
      try {
        if(__state != null) {
          __state.Update(__instance.Def);
          __instance.Def = __state as MechComponentDef;
        }
        //Log.Debug?.TWL(0, $"BaseComponentRef.RefreshComponentDef postfix {__instance.componentDefID}:{__instance.GetHashCode()}:{(__instance.Def == null ? "null" : __instance.Def.GetType().ToString())}");
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UnityGameInstance.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(MechLabLocationWidget))]
  [HarmonyPatch("OnRemoveItem")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(IMechLabDraggableItem), typeof(bool) })]
  public static class MechLabPanel_OnRemoveItem {
    public static void Prefix(ref bool __runOriginal, MechLabLocationWidget __instance, IMechLabDraggableItem item, bool validate) {
      try {
        if(__runOriginal == false) { return; }
        if(item is MechLabItemSlotElement mechComponent) {
          Log.Debug?.TWL(0, $"MechLabLocationWidget.OnRemoveItem prefix {mechComponent.ComponentRef.ComponentDefID}");
          if(mechComponent.ComponentRef.IsFixed) { return; }
          if(mechComponent.ComponentRef.Def is IMechComponentDynamicDef component) { mechComponent.ComponentRef.Def = component.original; }
        }
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UnityGameInstance.logger.LogException(e);
      }
    }
  }
  [HarmonyPatch(typeof(MechLabPanel))]
  [HarmonyPatch("CreateMechDef")]
  [HarmonyPatch(MethodType.Normal)]
  public static class MechLabPanel_CreateMechDef {
    public static void Postfix(MechLabPanel __instance, ref MechDef __result) {
      try {
        Log.Debug?.TWL(0, "MechLabPanel.CreateMechDef");
        foreach(var component in __result.inventory) {
          Log.Debug?.WL(1, $"{component.ComponentDefID}:{component.Def.GetType()}:{component.GetHashCode()}:{component.Def.Tonnage}");
        }
      } catch(Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
        UnityGameInstance.logger.LogException(e);
      }
    }
  }
}
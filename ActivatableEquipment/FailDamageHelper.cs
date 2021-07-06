using BattleTech;
using CustAmmoCategories;
using HBS.Collections;
using Localize;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomActivatableEquipment {
  public static class FailCritDamageHelper {
    public static ChassisLocations FakeVehicleLocation(this VehicleChassisLocations loc) {
      switch (loc) {
        case VehicleChassisLocations.Front: return ChassisLocations.LeftArm;
        case VehicleChassisLocations.Rear: return ChassisLocations.RightArm;
        case VehicleChassisLocations.Turret: return ChassisLocations.Head;
        case VehicleChassisLocations.Left: return ChassisLocations.LeftLeg;
        case VehicleChassisLocations.Right: return ChassisLocations.RightLeg;
        default: return ChassisLocations.None;
      }
    }
    public static void StructureDamage(this AbstractActor target, List<int> locations, float amount, bool critComponents, HashSet<string> excludeTags, HashSet<string> onlyTags) {
      WeaponHitInfo fakeHit = new WeaponHitInfo(-1, -1, -1, -1, target.GUID, target.GUID, 1, null, null, null, null, null, null, null, null, null, null, null);
      fakeHit.toHitRolls = new float[1] { 1.0f };
      fakeHit.locationRolls = new float[1] { 1.0f };
      fakeHit.dodgeRolls = new float[1] { 0.0f };
      fakeHit.dodgeSuccesses = new bool[1] { false };
      fakeHit.hitLocations = new int[1] { 0 };
      fakeHit.hitVariance = new int[1] { 0 };
      fakeHit.hitQualities = new AttackImpactQuality[1] { AttackImpactQuality.Solid };
      fakeHit.attackDirections = new AttackDirection[1] { AttackDirection.FromArtillery };
      fakeHit.hitPositions = new Vector3[1] { target.CurrentPosition };
      fakeHit.secondaryTargetIds = new string[1] { null };
      fakeHit.secondaryHitLocations = new int[1] { 0 };
      Log.Debug?.TWL(0, "StructureDamage:"+target.DisplayName+" amount:"+amount+ " critComponents:"+critComponents);
      foreach (int location in locations) {
        Log.Debug?.WL(1,"location:"+location);
        fakeHit.hitLocations[0] = location;
        fakeHit.hitPositions[0] = target.GameRep.GetHitPosition(location);
        if(amount >= 1f) target.TakeWeaponDamage(fakeHit, location, target.ImaginaryLaserWeapon, 0, amount, 0, DamageType.ComponentExplosion);
        if (critComponents) { target.CritComponentsInLocation(location, ref fakeHit, excludeTags, onlyTags); }
      }
    }
    public static List<MechComponent> GetComponentsInLocation(this AbstractActor target, int location, HashSet<string> excludeTags, HashSet<string> onlyTags) {
      List<MechComponent> result = new List<MechComponent>();
      TagSet oTags = new TagSet(onlyTags);
      TagSet eTags = new TagSet(excludeTags);
      Log.Debug?.TWL(0, "GetComponentsInLocation " + target.DisplayName);
      for (int t = 0; t < target.allComponents.Count; ++t) {
        MechComponent component = target.allComponents[t];
        if (component.IsFunctional == false) { continue; }
        if (component.Location != location) { continue; }
        Log.Debug?.W(1, "component:"+ component.defId);
        if (onlyTags.Count > 0) {
          if (component.componentDef.ComponentTags.ContainsAny(oTags) == false) {
            Log.Debug?.WL(1, "onlyTags check fail");
            continue; }
        }
        if(excludeTags.Count > 0) {
          if (component.componentDef.ComponentTags.ContainsAny(eTags)) {
            Log.Debug?.WL(1, "excludeTags check fail");
            continue; }
        }
        Log.Debug?.WL(1, "suitable");
        for (int i = 0; i < component.inventorySize; ++i) { result.Add(component); }
      }
      return result;
    }
    public static void CritComponent(this MechComponent component, ref WeaponHitInfo hitInfo) {
      Weapon weapon1 = component as Weapon;
      AmmunitionBox ammoBox = component as AmmunitionBox;
      Jumpjet jumpjet = component as Jumpjet;
      HeatSinkDef componentDef = component.componentDef as HeatSinkDef;
      bool flag = weapon1 != null;
      if (component.parent != null) {
        if (component.parent.GameRep != null) {
          WwiseManager.SetSwitch<AudioSwitch_weapon_type>(AudioSwitch_weapon_type.laser_medium, component.parent.GameRep.audioObject);
          WwiseManager.SetSwitch<AudioSwitch_surface_type>(AudioSwitch_surface_type.mech_critical_hit, component.parent.GameRep.audioObject);
          int num1 = (int)WwiseManager.PostEvent<AudioEventList_impact>(AudioEventList_impact.impact_weapon, component.parent.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
          int num2 = (int)WwiseManager.PostEvent<AudioEventList_explosion>(AudioEventList_explosion.explosion_small, component.parent.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
          if (component.parent.team.LocalPlayerControlsTeam)
            AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "critical_hit_friendly ", (AkGameObj)null, (AkCallbackManager.EventCallback)null);
          else if (!component.parent.team.IsFriendly(component.parent.Combat.LocalPlayerTeam))
            AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "critical_hit_enemy", (AkGameObj)null, (AkCallbackManager.EventCallback)null);
          if (jumpjet == null && componentDef == null && (ammoBox == null && component.DamageLevel > ComponentDamageLevel.Functional)) {
            if (component.parent is Mech mech) {
              mech.GameRep.PlayComponentCritVFX(component.Location);
            }
          }
          if (ammoBox != null && component.DamageLevel > ComponentDamageLevel.Functional)
            component.parent.GameRep.PlayVFX(component.Location, (string)component.parent.Combat.Constants.VFXNames.componentDestruction_AmmoExplosion, true, Vector3.zero, true, -1f);
        }
      }
      ComponentDamageLevel damageLevel = component.DamageLevel;
      switch (damageLevel) {
        case ComponentDamageLevel.Functional:
        if (flag) {
          damageLevel = ComponentDamageLevel.Penalized;
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)component.parent, new Text("{0} CRIT", new object[1]
          {
                    (object) component.UIName
          }), FloatieMessage.MessageNature.CriticalHit, true)));
          goto case ComponentDamageLevel.Destroyed;
        } else {
          damageLevel = ComponentDamageLevel.Destroyed;
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)component.parent, new Text("{0} DESTROYED", new object[1]
          {
                    (object) component.UIName
          }), FloatieMessage.MessageNature.ComponentDestroyed, true)));
          goto case ComponentDamageLevel.Destroyed;
        }
        case ComponentDamageLevel.Destroyed:
        component.DamageComponent(hitInfo, damageLevel, true);
        break;
        default:
        damageLevel = ComponentDamageLevel.Destroyed;
        component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)component.parent, new Text("{0} DESTROYED", new object[1]
        {
                  (object) component.UIName
        }), FloatieMessage.MessageNature.ComponentDestroyed, true)));
        goto case ComponentDamageLevel.Destroyed;
      }
    }
    public static void CritComponentsInLocation(this AbstractActor target, int location, ref WeaponHitInfo hitInfo, HashSet<string> excludeTags, HashSet<string> onlyTags) {
      List<MechComponent> components = target.GetComponentsInLocation(location, excludeTags, onlyTags);
      Log.Debug?.TWL(0, "CritComponentsInLocation:" + target.DisplayName + " location:" + location);
      for (int t = 0; t < components.Count; ++t) {
        Log.Debug?.WL(1,"["+t+"]"+components[t].defId);
      }
      if (components.Count == 0) { return; }
      int slotRoll = Random.Range(0, components.Count);
      MechComponent component = components[slotRoll];
      component.CritComponent(ref hitInfo);
    }
    public static bool FakeVehicle(this ICombatant combatant) {
      return false;
    }
    public static void CritComponentInLocations(this AbstractActor target, ActivatableComponent activatable, MechComponent component) {
      List<MechComponent> components = new List<MechComponent>();
      Mech mech = target as Mech;
      Vehicle vehicle = target as Vehicle;
      Turret turret = target as Turret;
      HashSet<int> affectedLocation = new HashSet<int>();
      if (mech != null) {
        if (mech.FakeVehicle() == false) {
          foreach (ChassisLocations loc in activatable.FailCritLocations) {
            LocationDef locDef = mech.MechDef.Chassis.GetLocationDef(loc);
            if ((locDef.InternalStructure <= 1.0f) && (locDef.MaxArmor == 0f)) { continue; }
            affectedLocation.Add((int)loc);
          }
        } else {
          foreach (VehicleChassisLocations vloc in activatable.FailCritVehicleLocations) {
            ChassisLocations loc = vloc.FakeVehicleLocation();
            LocationDef locDef = mech.MechDef.Chassis.GetLocationDef(loc);
            if ((locDef.InternalStructure <= 1.0f) && (locDef.MaxArmor <= 0f)) { continue; }
            affectedLocation.Add((int)loc);
          }
        }
      } else if (vehicle != null) {
        foreach (VehicleChassisLocations vloc in activatable.FailCritVehicleLocations) {
          VehicleLocationDef locDef = vehicle.VehicleDef.Chassis.GetLocationDef(vloc);
          if ((locDef.InternalStructure <= 1.0f) && (locDef.MaxArmor <= 0f)) { continue; }
          affectedLocation.Add((int)vloc);
        }
      } else if (turret != null) {
        affectedLocation.Add((int)BuildingLocation.Structure);
      }
      if (activatable.FailDamageToInstalledLocation) { affectedLocation.Add(component.Location); }
      HashSet<string> excludeTags = new HashSet<string>();
      HashSet<string> onlyTags = new HashSet<string>();
      foreach (string tag in activatable.FailCritExcludeComponentsTags) { excludeTags.Add(tag); }
      foreach (string tag in activatable.FailCritOnlyComponentsTags) { onlyTags.Add(tag); }
      Log.Debug?.TWL(0, "CritComponentsInLocations:" + target.DisplayName);
      foreach (int loc in affectedLocation) {
        List<MechComponent> locComps = target.GetComponentsInLocation(loc, excludeTags, onlyTags);
        components.AddRange(locComps);
      }
      for (int t = 0; t < components.Count; ++t) {
        Log.Debug?.WL(1, "[" + t + "]" + components[t].defId+" location:"+ components[t].Location);
      }
      int slotRoll = Random.Range(0, components.Count);
      MechComponent critComp = components[slotRoll];
      WeaponHitInfo fakeHit = new WeaponHitInfo(-1, -1, -1, -1, target.GUID, target.GUID, 1, null, null, null, null, null, null, null, null, null, null, null);
      fakeHit.toHitRolls = new float[1] { 1.0f };
      fakeHit.locationRolls = new float[1] { 1.0f };
      fakeHit.dodgeRolls = new float[1] { 0.0f };
      fakeHit.dodgeSuccesses = new bool[1] { false };
      fakeHit.hitLocations = new int[1] { critComp.Location };
      fakeHit.hitVariance = new int[1] { 0 };
      fakeHit.hitQualities = new AttackImpactQuality[1] { AttackImpactQuality.Solid };
      fakeHit.attackDirections = new AttackDirection[1] { AttackDirection.FromArtillery };
      fakeHit.hitPositions = new Vector3[1] { target.GameRep.GetHitPosition(critComp.Location) };
      fakeHit.secondaryTargetIds = new string[1] { null };
      fakeHit.secondaryHitLocations = new int[1] { 0 };
      critComp.CritComponent(ref fakeHit);
    }
    public static void CritComponent(this MechComponent component) {
      WeaponHitInfo fakeHit = new WeaponHitInfo(-1, -1, -1, -1, component.parent.GUID, component.parent.GUID, 1, null, null, null, null, null, null, null, null, null, null, null);
      fakeHit.toHitRolls = new float[1] { 1.0f };
      fakeHit.locationRolls = new float[1] { 1.0f };
      fakeHit.dodgeRolls = new float[1] { 0.0f };
      fakeHit.dodgeSuccesses = new bool[1] { false };
      fakeHit.hitLocations = new int[1] { component.Location };
      fakeHit.hitVariance = new int[1] { 0 };
      fakeHit.hitQualities = new AttackImpactQuality[1] { AttackImpactQuality.Solid };
      fakeHit.attackDirections = new AttackDirection[1] { AttackDirection.FromArtillery };
      fakeHit.hitPositions = new Vector3[1] { component.parent.GameRep.GetHitPosition(component.Location) };
      fakeHit.secondaryTargetIds = new string[1] { null };
      fakeHit.secondaryHitLocations = new int[1] { 0 };
      component.CritComponent(ref fakeHit);
    }
    public static void StructureDamage(this AbstractActor target, ActivatableComponent activatable, MechComponent component) {
      Mech mech = target as Mech;
      Vehicle vehicle = target as Vehicle;
      Turret turret = target as Turret;
      HashSet<int> affectedLocation = new HashSet<int>();
      if (mech != null) {
        if (mech.FakeVehicle() == false) {
          foreach (ChassisLocations loc in activatable.FailCritLocations) {
            LocationDef locDef = mech.MechDef.Chassis.GetLocationDef(loc);
            if ((locDef.InternalStructure <= 1.0f) && (locDef.MaxArmor == 0f)) { continue; }
            affectedLocation.Add((int)loc);
          }
        } else {
          foreach (VehicleChassisLocations vloc in activatable.FailCritVehicleLocations) {
            ChassisLocations loc = vloc.FakeVehicleLocation();
            LocationDef locDef = mech.MechDef.Chassis.GetLocationDef(loc);
            if ((locDef.InternalStructure <= 1.0f) && (locDef.MaxArmor <= 0f)) { continue; }
            affectedLocation.Add((int)loc);
          }
        }
      } else if(vehicle != null) {
        foreach (VehicleChassisLocations vloc in activatable.FailCritVehicleLocations) {
          VehicleLocationDef locDef = vehicle.VehicleDef.Chassis.GetLocationDef(vloc);
          if ((locDef.InternalStructure <= 1.0f) && (locDef.MaxArmor <= 0f)) { continue; }
          affectedLocation.Add((int)vloc);
        }
      } else if(turret != null) {
        affectedLocation.Add((int)BuildingLocation.Structure);
      }
      if (activatable.FailDamageToInstalledLocation) { affectedLocation.Add(component.Location); }
      HashSet<string> excludeTags = new HashSet<string>();
      HashSet<string> onlyTags = new HashSet<string>();
      foreach (string tag in activatable.FailCritExcludeComponentsTags) { excludeTags.Add(tag); }
      foreach (string tag in activatable.FailCritOnlyComponentsTags) { onlyTags.Add(tag); }
      target.StructureDamage(affectedLocation.ToList(), activatable.FailISDamage, activatable.FailCrit, excludeTags, onlyTags);
    }
  }
}
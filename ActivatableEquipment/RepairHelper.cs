using BattleTech;
using BattleTech.UI;
using CustomComponents;
using Harmony;
using Localize;
using System;
using System.Collections.Generic;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(CombatHUD))]
  [HarmonyPatch("Init")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(CombatGameState) })]
  public static class CombatHUD_InitReapair {
    public static void Postfix(CombatHUD __instance, CombatGameState Combat) {
      try {
        foreach (AbstractActor unit in Combat.AllActors) {

        }
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("TakeWeaponDamage")]
  [HarmonyPatch(MethodType.Normal)]
#if BT1_8
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(DamageType) })]
#else
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(int), typeof(DamageType) })]
#endif
  public static class Mech_TakeWeaponDamage {
#if BT1_8
    public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType) {
#else
    public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, int hitIndex, DamageType damageType) {
#endif
      Log.LogWrite("Mech.TakeWeaponDamage " + __instance.DisplayName + ":" + __instance.GUID + "\n");
      __instance.AddDamagedLocation(hitLocation);
    }
  }
  [HarmonyPatch(typeof(Vehicle))]
  [HarmonyPatch("TakeWeaponDamage")]
  [HarmonyPatch(MethodType.Normal)]
#if BT1_8
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(DamageType) })]
#else
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(int), typeof(DamageType) })]
#endif
  public static class Vehicle_TakeWeaponDamage {
#if BT1_8
    public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType) {
#else
    public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, int hitIndex, DamageType damageType) {
#endif
      Log.LogWrite("Vehicle.TakeWeaponDamage " + __instance.DisplayName + ":" + __instance.GUID + "\n");
      __instance.AddDamagedLocation(hitLocation);
    }
  }
  [HarmonyPatch(typeof(Turret))]
  [HarmonyPatch("TakeWeaponDamage")]
  [HarmonyPatch(MethodType.Normal)]
#if BT1_8
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(DamageType) })]
#else
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(int), typeof(DamageType) })]
#endif
  public static class Turret_TakeWeaponDamage {
#if BT1_8
    public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType) {
#else
    public static void Postfix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, int hitIndex, DamageType damageType) {
#endif
      Log.LogWrite("Turret.TakeWeaponDamage " + __instance.DisplayName + ":" + __instance.GUID + "\n");
      __instance.AddDamagedLocation(hitLocation);
    }
  }
  public static class RepairHelper {
    public static Dictionary<ICombatant, HashSet<int>> DamagedStructureLocationsThisTurn = new Dictionary<ICombatant, HashSet<int>>();

    public static void AddDamagedLocation(this Mech unit,int Location) {
      if (DamagedStructureLocationsThisTurn.ContainsKey(unit) == false) {
        DamagedStructureLocationsThisTurn.Add(unit, new HashSet<int>());
      }
      DamagedStructureLocationsThisTurn[unit].Add((int)MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)Location));
      unit.StatCollection.SetOrCreateStatisic(unit.GetArmorStringForLocation(Location) + "_damageRound", unit.Combat.TurnDirector.CurrentRound);
    }
    public static void AddDamagedLocation(this AbstractActor unit, int Location) {
      if (DamagedStructureLocationsThisTurn.ContainsKey(unit) == false) {
        DamagedStructureLocationsThisTurn.Add(unit, new HashSet<int>());
      }
      DamagedStructureLocationsThisTurn[unit].Add(Location);
      unit.StatCollection.SetOrCreateStatisic(unit.GetArmorStringForLocation(Location) + "_damageRound", unit.Combat.TurnDirector.CurrentRound);
    }
    public static int turnsSinceLocationDamage(this AbstractActor unit, int Location) {
      Statistic stat = unit.StatCollection.GetStatistic(unit.GetArmorStringForLocation(Location) + "_damageRound");
      if (stat == null) { return 65536; };
      return unit.Combat.TurnDirector.CurrentRound - stat.Value<int>();
    }
    public static Statistic GetArmorStatisticForLocation(this AbstractActor unit, int Location) {
      Mech mech = unit as Mech;
      Vehicle vehicle = unit as Vehicle;
      Turret turret = unit as Turret;
      if(mech != null) {
        return unit.StatCollection.GetStatistic(mech.GetStringForArmorLocation((ArmorLocation)Location));
      }
      if (vehicle != null) {
        return unit.StatCollection.GetStatistic(vehicle.GetStringForArmorLocation((VehicleChassisLocations)Location));
      }
      if (turret != null) {
        return unit.StatCollection.GetStatistic(turret.GetStringForArmorLocation((BuildingLocation)Location));
      }
      return null;
    }
    public static Statistic GetStructureStatisticForLocation(this AbstractActor unit, int Location) {
      Mech mech = unit as Mech;
      Vehicle vehicle = unit as Vehicle;
      Turret turret = unit as Turret;
      if (mech != null) {
        return unit.StatCollection.GetStatistic(mech.GetStringForStructureLocation(MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)Location)));
      }
      if (vehicle != null) {
        return unit.StatCollection.GetStatistic(vehicle.GetStringForStructureLocation((VehicleChassisLocations)Location));
      }
      if (turret != null) {
        return unit.StatCollection.GetStatistic(turret.GetStringForStructureLocation((BuildingLocation)Location));
      }
      return null;
    }
    public static string GetArmorStringForLocation(this AbstractActor unit, int Location) {
      Mech mech = unit as Mech;
      Vehicle vehicle = unit as Vehicle;
      Turret turret = unit as Turret;
      if (mech != null) {
        return mech.GetStringForArmorLocation((ArmorLocation)Location);
      }
      if (vehicle != null) {
        return vehicle.GetStringForArmorLocation((VehicleChassisLocations)Location);
      }
      if (turret != null) {
        return turret.GetStringForArmorLocation((BuildingLocation)Location);
      }
      return string.Empty;
    }
    public static void CommitCAEDamageData(this AbstractActor unit) {
      Log.LogWrite("CommitDamageData "+unit.DisplayName+":"+unit.GUID+"\n");
      if (DamagedStructureLocationsThisTurn.ContainsKey(unit) == false) {
        DamagedStructureLocationsThisTurn.Add(unit, new HashSet<int>());
      }
      HashSet<int> dmsLocations = DamagedStructureLocationsThisTurn[unit];
      Log.LogWrite(" DamagedLocations:");
      foreach (int Location in dmsLocations) { Log.LogWrite(" "+Location); };
      Log.LogWrite("\n");
      foreach (MechComponent component in unit.allComponents) {
        if (component.IsFunctional == false) { continue; }
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { continue; };
        Log.LogWrite("  "+component.defId+":"+ activatable.Repair.repairTrigger + ":"+ component.Location + "\n");
        if (activatable.Repair.repairTrigger.AtEndOfTurn == true) {
          if(activatable.Repair.repairTrigger.OnDamage == RepairDamageTrigger.None) {
            activatable.Repair.Repair(component,true);
          } else
          if (activatable.Repair.repairTrigger.OnDamage == RepairDamageTrigger.AllUnit) {
            if (dmsLocations.Count > 0) {
              activatable.Repair.Repair(component,true);
            }
          }else
          if (activatable.Repair.repairTrigger.OnDamage == RepairDamageTrigger.InstalledLocation) {
            if (dmsLocations.Contains(component.Location)) {
              activatable.Repair.Repair(component,true);
            }
          }
        }
      }
      unit.ClearDamageData();
    }
    public static void ClearDamageData(this ICombatant unit) {
      if (DamagedStructureLocationsThisTurn.ContainsKey(unit) == false) {
        DamagedStructureLocationsThisTurn.Add(unit, new HashSet<int>());
      } else {
        DamagedStructureLocationsThisTurn[unit].Clear();
      }
    }
    public static void Clear() {
      DamagedStructureLocationsThisTurn.Clear();
    }
  };
  public enum RepairDamageTrigger {
    None,
    AllUnit,
    InstalledLocation
  }
  public class RepairTrigger {
    public bool OnActivation { get; set; }
    public RepairDamageTrigger OnDamage { get; set; }
    public bool AtEndOfTurn { get; set; }
    public RepairTrigger() {
      OnActivation = false;
      OnDamage = RepairDamageTrigger.None;
      AtEndOfTurn = false;
    }
  }
  public class RepairRecord {
    public float InnerStructure { get; set; }
    public float Armor { get; set; }
    public int Components { get; set; }
    public ChassisLocations[] MechStructureLocations { get; set; }
    public ArmorLocation[] MechArmorLocations { get; set; }
    public VehicleChassisLocations[] VehicleLocations { get; set; }
    public BuildingLocation[] BuildingLocations { get; set; }
    public bool AffectInstalledLocation { get; set; }
    public RepairTrigger repairTrigger { get; set; }
    public int TurnsSinceDamage { get; set; }
    public RepairRecord() {
      InnerStructure = 0f;
      Armor = 0f;
      Components = 0;
      TurnsSinceDamage = 1;
      MechStructureLocations = new ChassisLocations[0];
      MechArmorLocations = new ArmorLocation[0];
      VehicleLocations = new VehicleChassisLocations[0];
      AffectInstalledLocation = false;
      repairTrigger = new RepairTrigger();
    }
    public void Repair(MechComponent component,bool isFloatieMessage = false) {
      Mech mech = component.parent as Mech;
      Vehicle vehicle = component.parent as Vehicle;
      Turret turret = component.parent as Turret;
      Log.LogWrite("Repair:" + component.parent.DisplayName + ":" + component.parent.GUID + ":" + component.defId + " mech:" + (mech != null ? "true" : "false") + " vehicle:" + (vehicle != null ? "true" : "false") + " turret:" + (turret != null ? "true" : "false") + "\n");
      HashSet<int> affectedISLocations = new HashSet<int>();
      HashSet<int> affectedArmorLocations = new HashSet<int>();
      bool armorReparied = false;
      bool structureReparied = false;
      if (this.AffectInstalledLocation) {
        affectedISLocations.Add(component.Location);
        if (mech != null) {
          ChassisLocations componentLocation = (ChassisLocations)component.Location;
          switch (componentLocation) {
            case ChassisLocations.CenterTorso:
              affectedArmorLocations.Add((int)ArmorLocation.CenterTorso);
              affectedArmorLocations.Add((int)ArmorLocation.CenterTorsoRear);
              break;
            case ChassisLocations.RightTorso:
              affectedArmorLocations.Add((int)ArmorLocation.RightTorso);
              affectedArmorLocations.Add((int)ArmorLocation.RightTorsoRear);
              break;
            case ChassisLocations.LeftTorso:
              affectedArmorLocations.Add((int)ArmorLocation.LeftTorso);
              affectedArmorLocations.Add((int)ArmorLocation.LeftTorsoRear);
              break;
            default:
              affectedArmorLocations.Add((int)componentLocation);
              break;
          }
        } else {
          affectedArmorLocations.Add(component.Location);
        }
      };
      int stackItemUID = component.parent.Combat.StackManager.NextStackUID;
      if (mech != null) {
        for (int t = 0; t < this.MechStructureLocations.Length; ++t) {
          affectedISLocations.Add((int)this.MechStructureLocations[t]);
        }
        for (int t = 0; t < this.MechArmorLocations.Length; ++t) {
          affectedArmorLocations.Add((int)this.MechArmorLocations[t]);
        }
      } else
      if (vehicle != null) {
        for (int t = 0; t < this.VehicleLocations.Length; ++t) {
          affectedISLocations.Add((int)this.VehicleLocations[t]);
          affectedArmorLocations.Add((int)this.VehicleLocations[t]);
        }
      } else {
        for (int t = 0; t < this.BuildingLocations.Length; ++t) {
          affectedISLocations.Add((int)this.BuildingLocations[t]);
          affectedArmorLocations.Add((int)this.BuildingLocations[t]);
        }
      }
      Log.LogWrite(" Affected armor locations:");
      foreach (var loc in affectedArmorLocations) { Log.LogWrite(" " + loc); }
      Log.LogWrite(" \n");
      if (this.Armor > Core.Epsilon) {
        foreach (int Location in affectedArmorLocations) {
          Statistic stat = component.parent.GetArmorStatisticForLocation(Location);
          if(stat == null) {
            Log.TWL(0, "Can't get armor stat " + new Text(component.parent.DisplayName).ToString() + " location:" +Location, true);
            continue;
          }
          Log.TWL(0, "turnsSinceLocationDamage:"+ component.parent.turnsSinceLocationDamage(Location)+" component:"+TurnsSinceDamage);
          if (TurnsSinceDamage >= 0) {
            if (component.parent.turnsSinceLocationDamage(Location) > TurnsSinceDamage) {
              Log.WL(1, "damage too long ago. No reparing performed.");
              continue;
            }
          }
          float maxArmor = stat.DefaultValue<float>();
          float currentArmor = stat.Value<float>();
          int StructureLocation = Location;
          if (mech != null) {
            StructureLocation = (int)MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)Location);
          }
          currentArmor += this.Armor;
          if (currentArmor > maxArmor) { currentArmor = maxArmor; };
          float delta = currentArmor - component.parent.ArmorForLocation(Location);
          Log.WL(2, "location:"+Location+" maxArmor:"+ maxArmor + " currentArmor:"+ currentArmor+"("+delta+")");
          if (delta > Core.Epsilon) {
            if (mech != null) {
              Log.LogWrite("  mech stat armor location:" + mech.GetStringForArmorLocation((ArmorLocation)Location) + "\n");
              LocationDamageLevel locationDamageLevel = mech.GetLocationDamageLevel((ChassisLocations)StructureLocation);
              if (locationDamageLevel >= LocationDamageLevel.Destroyed) {
                Log.LogWrite(" can't repair destroyed location\n");
                continue;
              }
              armorReparied = true;
              component.parent.StatCollection.ModifyStat<float>(component.parent.GUID, stackItemUID, mech.GetStringForArmorLocation((ArmorLocation)Location), StatCollection.StatOperation.Float_Add, delta, -1, true);
            } else
            if (vehicle != null) {
              Log.LogWrite("  vehicle stat armor location:" + vehicle.GetStringForArmorLocation((VehicleChassisLocations)Location) + "\n");
              LocationDamageLevel locationDamageLevel = vehicle.GetLocationDamageLevel((VehicleChassisLocations)StructureLocation);
              if (locationDamageLevel >= LocationDamageLevel.Destroyed) {
                Log.LogWrite(" can't repair destroyed location\n");
                continue;
              }
              armorReparied = true;
              component.parent.StatCollection.ModifyStat<float>(component.parent.GUID, stackItemUID, vehicle.GetStringForArmorLocation((VehicleChassisLocations)Location), StatCollection.StatOperation.Float_Add, delta, -1, true);
            } else
            if (turret != null) {
              Log.LogWrite("  turret stat armor location:" + turret.GetStringForArmorLocation((BuildingLocation)Location) + "\n");
              LocationDamageLevel locationDamageLevel = turret.GetLocationDamageLevel((BuildingLocation)StructureLocation);
              if (locationDamageLevel >= LocationDamageLevel.Destroyed) {
                Log.LogWrite(" can't repair destroyed location\n");
                armorReparied = true;
                continue;
              }
              component.parent.StatCollection.ModifyStat<float>(component.parent.GUID, stackItemUID, turret.GetStringForArmorLocation((BuildingLocation)Location), StatCollection.StatOperation.Float_Add, delta, -1, true);
            } else {
              Log.LogWrite("  combatant has no armor\n");
            }
          }
        }
      }
      Log.LogWrite(" Affected inner structure locations:");
      foreach (var loc in affectedISLocations) { Log.LogWrite(" " + loc); }
      Log.LogWrite(" \n");
      if (this.InnerStructure > Core.Epsilon) {
        foreach (int Location in affectedISLocations) {
          float maxStructure = component.parent.MaxStructureForLocation(Location);
          float currentStructure = component.parent.StructureForLocation(Location);
          if (currentStructure < Core.Epsilon) {
            Log.LogWrite(" can't repair locations with zero structure\n");
            continue;
          }
          currentStructure += this.InnerStructure;
          if (currentStructure > maxStructure) { currentStructure = maxStructure; };
          float delta = currentStructure - component.parent.StructureForLocation(Location);
          Log.LogWrite(" inner structure repair amount:" + delta + "\n");
          if (delta > Core.Epsilon) {
            if (mech != null) {
              Log.LogWrite("  mech stat structure location:" + mech.GetStringForStructureLocation((ChassisLocations)Location) + "\n");
              LocationDamageLevel locationDamageLevel = mech.GetLocationDamageLevel((ChassisLocations)Location);
              if (locationDamageLevel >= LocationDamageLevel.Destroyed) {
                Log.LogWrite(" can't repair destroyed location\n");
                continue;
              }
              structureReparied = true;
              component.parent.StatCollection.ModifyStat<float>(component.parent.GUID, stackItemUID, mech.GetStringForStructureLocation((ChassisLocations)Location), StatCollection.StatOperation.Float_Add, delta, -1, true);
            } else
            if (vehicle != null) {
              Log.LogWrite("  vehicle stat structure location:" + vehicle.GetStringForStructureLocation((VehicleChassisLocations)Location) + "\n");
              LocationDamageLevel locationDamageLevel = vehicle.GetLocationDamageLevel((VehicleChassisLocations)Location);
              if (locationDamageLevel >= LocationDamageLevel.Destroyed) {
                Log.LogWrite(" can't repair destroyed location\n");
                continue;
              }
              component.parent.StatCollection.ModifyStat<float>(component.parent.GUID, stackItemUID, vehicle.GetStringForStructureLocation((VehicleChassisLocations)Location), StatCollection.StatOperation.Float_Add, delta, -1, true);
            } else
            if (turret != null) {
              Log.LogWrite("  turret stat structure location:" + turret.GetStringForArmorLocation((BuildingLocation)Location) + "\n");
              LocationDamageLevel locationDamageLevel = turret.GetLocationDamageLevel((BuildingLocation)Location);
              if (locationDamageLevel == LocationDamageLevel.Destroyed) {
                Log.LogWrite(" can't repair destroyed location\n");
                continue;
              }
              structureReparied = true;
              component.parent.StatCollection.ModifyStat<float>(component.parent.GUID, stackItemUID, turret.GetStringForStructureLocation((BuildingLocation)Location), StatCollection.StatOperation.Float_Add, delta, -1, true);
            } else {
              Log.LogWrite("  other structure location:" + "Structure" + "\n");
              structureReparied = true;
              component.parent.StatCollection.ModifyStat<float>(component.parent.GUID, stackItemUID, "Structure", StatCollection.StatOperation.Float_Add, delta, -1, true);
            }
          }
        }
      }
      if((armorReparied || structureReparied)&&(isFloatieMessage)) {
        string text = component.Description.UIName + " __/CAE.REPAIRED/__" + (armorReparied ? " __/CAE.REPAIRARMOR/__" : " ") + (structureReparied ? " __/CAE.REPAIRSTRUCTURE/__" : "");
        component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID,text, FloatieMessage.MessageNature.Buff));
      }
    }
  }
}
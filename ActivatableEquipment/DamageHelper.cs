using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using HBS;
using CustomComponents;
using CustomAmmoCategoriesPatches;
using Newtonsoft.Json;

namespace CustomActivatableEquipment.DamageHelpers {
  public class IncomingDamageRecord {
    [JsonIgnore]
    public AbstractActor owner { get; set; }
    public Dictionary<int, float> armorLocationMax { get; set; }
    public Dictionary<int, float> structureLocaltionMax { get; set; }
    public Dictionary<int, float> armorDamgeLocationThreshold { get; set; }
    public Dictionary<int, float> armorDamageLocationHighest { get; set; }
    public Dictionary<int, float> structureDamgeLocationThreshold { get; set; }
    public Dictionary<int, float> structureDamageLocationHighest { get; set; }
    public float thresholdHeat { get; set; }
    public float highestHeat { get; set; }
    public float thresholdStability { get; set; }
    public float highestStability { get; set; }
    public IncomingDamageRecord(AbstractActor owner) {
      this.owner = owner;
      armorDamgeLocationThreshold = new Dictionary<int, float>();
      armorDamageLocationHighest = new Dictionary<int, float>();
      structureDamgeLocationThreshold = new Dictionary<int, float>();
      structureDamageLocationHighest = new Dictionary<int, float>();
      armorLocationMax = new Dictionary<int, float>();
      structureLocaltionMax = new Dictionary<int, float>();
      HashSet<int> allArmorLocations = new HashSet<int>();
      Mech mech = owner as Mech;
      Vehicle vehicle = owner as Vehicle;
      Turret turret = owner as Turret;
      if(mech != null) {
        foreach (ArmorLocation location in Enum.GetValues(typeof(ArmorLocation))) {
          if ((location == ArmorLocation.None) || (location == ArmorLocation.Invalid)) { continue; }
          allArmorLocations.Add((int)location);
        }
      }else if (vehicle != null) {
        allArmorLocations.Add((int)VehicleChassisLocations.Turret);
        allArmorLocations.Add((int)VehicleChassisLocations.Front);
        allArmorLocations.Add((int)VehicleChassisLocations.Rear);
        allArmorLocations.Add((int)VehicleChassisLocations.Left);
        allArmorLocations.Add((int)VehicleChassisLocations.Right);
      } else {
        allArmorLocations.Add((int)BuildingLocation.Structure);
      }
      foreach (int location in allArmorLocations) {
        Statistic armor = owner.GetArmorStatisticForLocation(location);
        if (armor != null) { armorLocationMax.Add(location, armor.DefaultValue<float>()); }
        Statistic structure = owner.GetStructureStatisticForLocation(location);
        if (structure != null) { structureLocaltionMax.Add(location, structure.DefaultValue<float>()); }
      }
    }
    public void Damage(int location, float damage, float apDamage) {
      float curArmor = owner.ArmorForLocation(location);
      float curStructure = owner.StructureForLocation(location);
      if (damage > curArmor) { apDamage += (damage - curArmor); }
      if (apDamage > curStructure) { apDamage = curStructure; };
      if (armorDamgeLocationThreshold.ContainsKey(location) == false) { armorDamgeLocationThreshold.Add(location, damage); } else { armorDamgeLocationThreshold[location] += damage; }
      if (armorDamageLocationHighest.ContainsKey(location) == false) { armorDamageLocationHighest.Add(location, damage); } else { armorDamageLocationHighest[location] = armorDamageLocationHighest[location] < damage ? damage : armorDamageLocationHighest[location]; };
      if (structureDamgeLocationThreshold.ContainsKey(location) == false) { structureDamgeLocationThreshold.Add(location, apDamage); } else { structureDamgeLocationThreshold[location] += apDamage; }
      if (structureDamageLocationHighest.ContainsKey(location) == false) { structureDamageLocationHighest.Add(location, apDamage); } else { structureDamageLocationHighest[location] = structureDamageLocationHighest[location] < apDamage ? apDamage : structureDamageLocationHighest[location]; };
    }
    public void Heat(int heat) {
      thresholdHeat += heat;
      if (highestHeat > heat) { highestHeat = heat; }
    }
    public void Stability(float stability) {
      thresholdStability += stability;
      if (highestStability > stability) { highestStability = stability; }
    }
    public void Clear() {
      thresholdHeat = 0f;
      highestHeat = 0f;
      thresholdStability = 0f;
      highestStability = 0f;
      armorDamgeLocationThreshold.Clear();
      armorDamageLocationHighest.Clear();
      structureDamgeLocationThreshold.Clear();
      structureDamageLocationHighest.Clear();
    }
    public HashSet<int> getSensibleLocations(MechComponent component) {
      HashSet<int> result = new HashSet<int>();
      ActivatableComponent tactivatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (tactivatable == null) { return result; }
      if (component.parent.GUID != this.owner.GUID) { return result; }
      Mech mech = owner as Mech;
      Vehicle vehicle = owner as Vehicle;
      if (tactivatable.ActivateOnDamageToInstalledLocation) {
        result.Add(component.Location);
        if (mech != null) {
          switch (component.mechComponentRef.MountedLocation) {
            case ChassisLocations.CenterTorso:  result.Add((int)ArmorLocation.CenterTorsoRear); break;
            case ChassisLocations.LeftTorso: result.Add((int)ArmorLocation.LeftTorsoRear); break;
            case ChassisLocations.RightTorso: result.Add((int)ArmorLocation.RightTorsoRear); break;
          }
        }
      }
      if(mech != null) {
        foreach(ArmorLocation location in tactivatable.ActivateOnDamageToMechLocations) {
          result.Add((int)location);
        }
      }else if(vehicle != null) {
        foreach (VehicleChassisLocations location in tactivatable.ActivateOnDamageToVehicleLocations) {
          result.Add((int)location);
        }
      } else {
        result.Add((int)BuildingLocation.Structure);
      }
      return result;
    }
    public void commitDamage() {
      if (owner.IsDead || owner.IsFlaggedForDeath || owner.IsShutDown) { Clear(); return; }
      if ((thresholdHeat < Core.Epsilon) && (thresholdStability < Core.Epsilon)
        && (armorDamgeLocationThreshold.Count == 0) && (structureDamgeLocationThreshold.Count == 0)) { Clear(); return; };
      Mech mech = owner as Mech;
      Vehicle vehicle = owner as Vehicle;
      foreach (MechComponent component in owner.allComponents) {
        if (component.IsFunctional == false) { continue; }
        if (owner.StructureForLocation(component.Location) < Core.Epsilon) { continue; }
        ActivatableComponent tactivatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (tactivatable == null) { continue; }
        if (ActivatableComponent.canBeDamageActivated(component) == false) { continue; };
        if (ActivatableComponent.isOutOfCharges(component)) { continue; }
        if (component.isActive()) { continue; }
        bool activate = false;
        if(tactivatable.AutoActivateOnIncomingHeat > Core.Epsilon) {
          switch (tactivatable.incomingHeatActivationType) {
            case DamageActivationType.Threshhold: activate = tactivatable.AutoActivateOnIncomingHeat < thresholdHeat; break;
            case DamageActivationType.Single: activate = tactivatable.AutoActivateOnIncomingHeat < highestHeat; break;
            case DamageActivationType.Level: activate = tactivatable.AutoActivateOnIncomingHeat < (mech != null?thresholdHeat / mech.MaxHeat:0f); break;
          }
        }
        if (activate) { ActivatableComponent.activateComponent(component,true,false); continue; }
        HashSet<int> sensibleLocations = getSensibleLocations(component);
        foreach(int location in sensibleLocations) {
          float maxArmor = armorLocationMax.ContainsKey(location)?armorLocationMax[location]:1f;
          float maxStruct = structureLocaltionMax.ContainsKey(location) ? structureLocaltionMax[location] : 1f;
          float thresholdArmor = structureDamgeLocationThreshold.ContainsKey(location) ? structureDamgeLocationThreshold[location] : 0f;
          float thresholdStruct = structureDamgeLocationThreshold.ContainsKey(location) ? structureDamgeLocationThreshold[location] : 0f;
          float singleArmor = armorDamageLocationHighest.ContainsKey(location) ? armorDamageLocationHighest[location] : 0f;
          float singleStruct = structureDamageLocationHighest.ContainsKey(location) ? structureDamageLocationHighest[location] : 0f;
          if(tactivatable.AutoActivateOnArmorDamage > Core.Epsilon) {
            switch (tactivatable.damageActivationType) {
              case DamageActivationType.Threshhold: activate = tactivatable.AutoActivateOnArmorDamage < thresholdArmor; break;
              case DamageActivationType.Single: activate = tactivatable.AutoActivateOnArmorDamage < singleArmor; break;
              case DamageActivationType.Level: activate = tactivatable.AutoActivateOnArmorDamage < thresholdArmor / maxArmor; break;
            }
            if (activate) { break; }
          }
          if (tactivatable.AutoActivateOnStructureDamage > Core.Epsilon) {
            switch (tactivatable.damageActivationType) {
              case DamageActivationType.Threshhold: activate = tactivatable.AutoActivateOnStructureDamage < thresholdStruct; break;
              case DamageActivationType.Single: activate = tactivatable.AutoActivateOnStructureDamage < singleStruct; break;
              case DamageActivationType.Level: activate = tactivatable.AutoActivateOnStructureDamage < thresholdStruct / maxStruct; break;
            }
            if (activate) { break; }
          }
        }
        if (activate) { ActivatableComponent.activateComponent(component, true, false); continue; }
      }
      Clear();
    }
  };
  [HarmonyPatch(typeof(AbstractActor), "OnActivationBegin")]
  public static class AbstractActor_OnActivationBegin_Patch {
    public static void Prefix(AbstractActor __instance) {
      DamageHelper.newTurnFor(__instance);
    }
  }
  [HarmonyPatch(typeof(AttackDirector))]
  [HarmonyPatch("OnAttackComplete")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.Last)]
  [HarmonyPatch(new Type[] { typeof(MessageCenterMessage) })]
  public static class AttackDirector_OnAttackCompleteTA {
    public static void Postfix(AttackDirector __instance, MessageCenterMessage message, ref AbstractActor __state) {
      __instance.Combat.commitDamage();
    }
  }
  [HarmonyPatch(typeof(AbstractActor), "OnActivationEnd")]
  public static class AbstractActor_OnActivationEnd_Patch {
    public static void Prefix(AbstractActor __instance) {
      DamageHelper.completedTurnFor(__instance);
    }
  }

  [HarmonyPatch(typeof(Mech), "AddExternalHeat")]
  public static class Mech_AddExternalHeat_Patch {

    private static void Postfix(Mech __instance, string reason, int amt) {
      if (__instance == null) {
        Log.LogWrite("No mech\n");
        return;
      }
      Log.LogWrite($"{new string('═', 46)}\n");
      Log.LogWrite($"{__instance.DisplayName} :{__instance.GUID } took {amt} Heat Damage from {reason ?? "null"}\n");
      //DamageHelper.BatchHeatDamage(__instance, amt);
      if (__instance.isHasHeat()) { __instance.IncomingDamage().Heat(amt); }
    }
  }
  [HarmonyPatch(typeof(Mech), "AddAbsoluteInstability")]
  public static class Mech_AddAbsoluteInstability_Patch {

    private static void Postfix(Mech __instance, float amt, StabilityChangeSource source, string sourceGuid) {
      if (__instance == null) {
        Log.LogWrite("No mech\n");
        return;
      }
      Log.LogWrite($"{new string('═', 46)}\n");
      Log.LogWrite($"{__instance.DisplayName} :{__instance.GUID } took {amt} Stability Damage from {source}\n");
      //DamageHelper.BatchHeatDamage(__instance, amt);
      if (__instance.isHasStability()) { __instance.IncomingDamage().Stability(amt); }
    }
  }

  [HarmonyPatch(typeof(GameInstance), "LaunchContract", typeof(Contract), typeof(string))]
  public static class GameInstance_LaunchContract_Patch {
    // reset on new contracts
    private static void Postfix() {
      DamageHelper.Reset();
    }
  }

  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("TakeWeaponDamage")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(DamageType) })]
  public static class Mech_TakeWeaponDamage {
    public static void Prefix(Mech __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType) {
      try {
        if (__instance == null) {
          Log.LogWrite("No mech\n");
          return;
        }
        Log.LogWrite($"{new string('═', 46)}\n");
        string wname = (weapon != null) ? (weapon.Name ?? "null") : "null";
        Log.LogWrite($"{__instance.DisplayName} :{__instance.GUID } took Damage from {wname} - {damageType.ToString()}\n");
        __instance.IncomingDamage().Damage(hitLocation, damageAmount, directStructureDamage);
      } catch (Exception e) {
        Log.TWL(0,e.ToString(),true);
      }
      //DamageHelper.BatchDamage(__instance, damageAmount, directStructureDamage);
    }
  }

  [HarmonyPatch(typeof(Vehicle))]
  [HarmonyPatch("TakeWeaponDamage")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(DamageType) })]
  public static class Vehicle_TakeWeaponDamage {
    public static void Prefix(Vehicle __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType) {
      try {
        if (__instance == null) {
          Log.LogWrite("No mech\n");
          return;
        }
        Log.LogWrite($"{new string('═', 46)}\n");
        string wname = (weapon != null) ? (weapon.Name ?? "null") : "null";
        Log.LogWrite($"{__instance.DisplayName} :{__instance.GUID } took Damage from {wname} - {damageType.ToString()}\n");
        __instance.IncomingDamage().Damage(hitLocation, damageAmount, directStructureDamage);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }

  [HarmonyPatch(typeof(Turret))]
  [HarmonyPatch("TakeWeaponDamage")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo), typeof(int), typeof(Weapon), typeof(float), typeof(float), typeof(int), typeof(DamageType) })]
  public static class Turret_TakeWeaponDamage {
    public static void Prefix(Turret __instance, WeaponHitInfo hitInfo, int hitLocation, Weapon weapon, float damageAmount, float directStructureDamage, int hitIndex, DamageType damageType) {
      try {
        if (__instance == null) {
          Log.LogWrite("No mech\n");
          return;
        }
        Log.LogWrite($"{new string('═', 46)}\n");
        string wname = (weapon != null) ? (weapon.Name ?? "null") : "null";
        Log.LogWrite($"{__instance.DisplayName} :{__instance.GUID } took Damage from {wname} - {damageType.ToString()}\n");
        __instance.IncomingDamage().Damage(hitLocation, damageAmount, directStructureDamage);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
  }
  public static class DamageHelper {

    private static Dictionary<string, int> turnExternalHeatAccumulator = new Dictionary<string, int>();
    private static List<AbstractActor> activationVictims = new List<AbstractActor>();
    private static AbstractActor attacker = null;
    private static Dictionary<AbstractActor, IncomingDamageRecord> incomingDamage = new Dictionary<AbstractActor, IncomingDamageRecord>();
    public static void Clear() {
      incomingDamage.Clear();
    }
    public static void commitDamage(this CombatGameState combat) {
      foreach(AbstractActor actor in combat.AllActors) {
        actor.IncomingDamage().commitDamage();
      }
    }
    public static IncomingDamageRecord IncomingDamage(this AbstractActor actor) {
      if(incomingDamage.TryGetValue(actor, out var dmgRec)) {
        return dmgRec;
      }
      dmgRec = new IncomingDamageRecord(actor);
      incomingDamage.Add(actor, dmgRec);
      return dmgRec;
    }
    internal static void Reset() {
      Log.LogWrite($"DamageHelper Reset - new Mission\n");
      turnExternalHeatAccumulator = new Dictionary<string, int>();
      attacker = null;
      activationVictims = new List<AbstractActor>();
    }
    internal static void newTurnFor(AbstractActor actor) {
      Log.LogWrite($"new Turn Activation for {actor.Nickname} - {actor.DisplayName} - {actor.GUID}\n");
      attacker = actor;
      if (actor is Mech) {
        turnExternalHeatAccumulator[actor.GUID] = 0;//external heat 0 start of activation
      }
    }
    internal static void completedTurnFor(AbstractActor instance) {
      if (attacker != null) {
        Log.LogWrite($"completed Turn Activation for {instance.Nickname} - {instance.DisplayName} - {instance.GUID}\n");
        foreach (AbstractActor actor in activationVictims) {
          Log.LogWrite($"{instance.DisplayName}|{instance.Nickname}|{instance.GUID} damaged during turn.\n");
          ActivateComponentsBasedOnHeatDamage(actor as Mech, turnExternalHeatAccumulator[actor.GUID]);
          ActivateComponentsBasedOnDamage(actor as Mech);
        }
        activationVictims.Clear();
      }
      attacker = null;
    }


    internal static float MaxArmorForLocation(Mech mech, int Location) {
      if (mech != null) {
        Statistic stat = mech.StatCollection.GetStatistic(mech.GetStringForArmorLocation((ArmorLocation)Location));
        if (stat == null) {
          Log.LogWrite($"Can't get armor stat  { mech.DisplayName } location:{ Location.ToString()}\n");
          return 0;
        }
        //Log.LogWrite($"armor stat  { mech.DisplayName } location:{ Location.ToString()} :{stat.DefaultValue<float>()}");
        return stat.DefaultValue<float>();
      }
      Log.LogWrite($"Mech null\n");
      return 0;
    }
    internal static float MaxStructureForLocation(Mech mech, int Location) {
      if (mech != null) {
        Statistic stat = mech.StatCollection.GetStatistic(mech.GetStringForStructureLocation((ChassisLocations)Location));
        if (stat == null) {
          Log.LogWrite($"Can't get structure stat  { mech.DisplayName } location:{ Location.ToString()}\n");
          return 0;
        }
        //Log.LogWrite($"structure stat  { mech.DisplayName } location:{ Location.ToString()}:{stat.DefaultValue<float>()}");
        return stat.DefaultValue<float>();
      }
      Log.LogWrite($"Mech null\n");
      return 0;
    }

    public static void ActivateComponentsBasedOnHeatDamage(Mech defender, int heatDamage) {
      if (heatDamage <= 0) {
        Log.LogWrite("No heat damage\n");
        return;
      }

      if (defender == null) {
        Log.LogWrite("No mech\n");
        return;
      }

      if (defender.IsDead || defender.IsFlaggedForDeath || defender.IsShutDown) {
        Log.LogWrite($"{defender.DisplayName} dead or shutdown.\n");//<check> do we need to handle incoming damage when shutdown on startup?
        return;
      }

      foreach (MechComponent component in defender.allComponents) {
        ActivatableComponent tactivatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (tactivatable == null) { continue; }
        if (ActivatableComponent.canBeDamageActivated(component) == false) { continue; };
        if (defender.IsLocationDestroyed((ChassisLocations)component.Location)) {
          Log.LogWrite($"Ignored {component.Name} installed in destroyed {((ChassisLocations)component.Location).ToString()}\n");
          continue;
        };
        if (defender is Mech mech) {
          Log.LogWrite($"Damage >>> D: {0:F3} DS: {0:F3} H: {heatDamage}\n");
        } else {
          Log.LogWrite($"Not a mech, somethings broken\n");
        }

        if (defender.isHasHeat()) {//if not battle armor 
          ActivatableComponent.ActivateOnIncomingHeat(component, heatDamage);
        } else {
          Log.LogWrite($" { defender.DisplayName } can't have incoming heat damage\n");
        }
      }

    }

    public static void ActivateComponentsBasedOnDamage(Mech defender) {
      if (defender == null) {
        Log.LogWrite("No mech\n");
        return;
      }
      if (defender.IsDead || defender.IsFlaggedForDeath || defender.IsShutDown) {
        Log.LogWrite($"{defender.DisplayName} dead or shutdown.\n");//<check> do we need to handle incoming damage when shutdown on startup?
        return;
      }

      bool gotdamagevalues = false;

      float Head_s = 0;
      float LeftArm_s = 0;
      float LeftTorso_s = 0;
      float CenterTorso_s = 0;
      float RightTorso_s = 0;
      float RightArm_s = 0;
      float LeftLeg_s = 0;
      float RightLeg_s = 0;

      float Head_a = 0;
      float LeftArm_a = 0;
      float LeftTorso_a = 0;
      float CenterTorso_a = 0;
      float RightTorso_a = 0;
      float RightArm_a = 0;
      float LeftLeg_a = 0;
      float RightLeg_a = 0;

      foreach (MechComponent component in defender.allComponents) {
        ActivatableComponent tactivatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (tactivatable == null) { continue; }
        if (ActivatableComponent.canBeDamageActivated(component) == false) { continue; };
        if (defender.IsLocationDestroyed((ChassisLocations)component.Location)) {
          Log.LogWrite($"Ignored {component.Name} installed in destroyed {((ChassisLocations)component.Location).ToString()}\n");
          continue;
        };
        if (!gotdamagevalues) {//have atleast 1 damage activateable component get the damage values
          Mech mech = defender;

          Log.LogWrite($"{new string('-', 46)}\n");
          Log.LogWrite($"{"Location",-20} | {"Armor Damage",12} | {"Structure Damage",12}\n");
          Log.LogWrite($"{new string('-', 46)}\n");
          Head_s = MaxStructureForLocation(mech, (int)ChassisLocations.Head) - defender.HeadStructure;
          Head_a = MaxArmorForLocation(mech, (int)ChassisLocations.Head) - defender.HeadArmor;
          Log.LogWrite($"{ChassisLocations.Head.ToString(),-20} | {Head_a,12:F3} | {Head_s,12:F3}\n");
          CenterTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.CenterTorso) - defender.CenterTorsoStructure;
          CenterTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.CenterTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.CenterTorsoRear) - defender.CenterTorsoFrontArmor - defender.CenterTorsoRearArmor;
          Log.LogWrite($"{ChassisLocations.CenterTorso.ToString(),-20} |  {CenterTorso_a,12:F3} | {CenterTorso_s,12:F3}\n");
          LeftTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftTorso) - defender.LeftTorsoStructure;
          LeftTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.LeftTorsoRear) - defender.LeftTorsoFrontArmor - defender.LeftTorsoRearArmor;
          Log.LogWrite($"{ChassisLocations.LeftTorso.ToString(),-20} |  {LeftTorso_a,12:F3} | {LeftTorso_s,12:F3}\n");
          RightTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightTorso) - defender.RightTorsoStructure;
          RightTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.RightTorsoRear) - defender.RightTorsoFrontArmor - defender.RightTorsoRearArmor;
          Log.LogWrite($"{ChassisLocations.RightTorso.ToString(),-20} |  {RightTorso_a,12:F3} | {RightTorso_s,12:F3}\n");
          LeftLeg_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftLeg) - defender.LeftLegStructure;
          LeftLeg_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftLeg) - defender.LeftLegArmor;
          Log.LogWrite($"{ChassisLocations.LeftLeg.ToString(),-20} |  {LeftLeg_a,12:F3} | {LeftLeg_s,12:F3}\n");
          RightLeg_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightLeg) - defender.RightLegStructure;
          RightLeg_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightLeg) - defender.RightLegArmor;
          Log.LogWrite($"{ChassisLocations.RightLeg.ToString(),-20} |  {RightLeg_a,12:F3} | {RightLeg_s,12:F3}\n");
          LeftArm_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftArm) - defender.LeftArmStructure;
          LeftArm_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftArm) - defender.LeftArmArmor;
          Log.LogWrite($"{ChassisLocations.LeftArm.ToString(),-20} |  {LeftArm_a,12:F3} | {LeftArm_s,12:F3}\n");
          RightArm_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightArm) - defender.RightArmStructure;
          RightArm_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightArm) - defender.RightArmArmor;
          Log.LogWrite($"{ChassisLocations.RightArm.ToString(),-20} |  {RightArm_a,12:F3} | {RightArm_s,12:F3}\n");

          Log.LogWrite($"{ChassisLocations.Torso.ToString(),-20} |  {CenterTorso_a + LeftTorso_a + RightTorso_a,12:F3} | {CenterTorso_s + LeftTorso_s + RightTorso_s,12:F3}\n");
          Log.LogWrite($"{ChassisLocations.Legs.ToString(),-20} |  {LeftLeg_a + RightLeg_a,12:F3} | { LeftLeg_s + RightLeg_s,12:F3}\n");
          Log.LogWrite($"{ChassisLocations.Arms.ToString(),-20} |  {LeftArm_a + RightArm_a,12:F3} | { LeftArm_s + RightArm_s,12:F3}\n");
          Log.LogWrite($"{ChassisLocations.All.ToString(),-20} |  {CenterTorso_a + LeftTorso_a + RightTorso_a + LeftLeg_a + RightLeg_a + LeftArm_a + RightArm_a,12:F3} | {CenterTorso_s + LeftTorso_s + RightTorso_s + LeftLeg_s + RightLeg_s + LeftArm_s + RightArm_s,12:F3}\n");
          gotdamagevalues = true;
        }
        // we stop trying to activate the component if any of these return true i.e activated;
        //ignore the damage from this hit and use the current damage levels.
        //Not handling ChassisLocation MainBody as i dont know what locations it covers.
        if (
          ActivatableComponent.ActivateOnDamage(component, Head_a, Head_s, ChassisLocations.Head) ||
          ActivatableComponent.ActivateOnDamage(component, CenterTorso_a, CenterTorso_s, ChassisLocations.CenterTorso) ||
          ActivatableComponent.ActivateOnDamage(component, LeftTorso_a, LeftTorso_s, ChassisLocations.LeftTorso) ||
          ActivatableComponent.ActivateOnDamage(component, RightTorso_a, RightTorso_s, ChassisLocations.RightTorso) ||
          ActivatableComponent.ActivateOnDamage(component, LeftLeg_a, LeftLeg_s, ChassisLocations.LeftLeg) ||
          ActivatableComponent.ActivateOnDamage(component, RightLeg_a, RightLeg_s, ChassisLocations.RightLeg) ||
          ActivatableComponent.ActivateOnDamage(component, LeftArm_a, LeftArm_s, ChassisLocations.LeftArm) ||
          ActivatableComponent.ActivateOnDamage(component, RightArm_a, RightArm_s, ChassisLocations.RightArm) ||
          ActivatableComponent.ActivateOnDamage(component, CenterTorso_a + LeftTorso_a + RightTorso_a, CenterTorso_s + LeftTorso_s + RightTorso_s, ChassisLocations.Torso) ||
          ActivatableComponent.ActivateOnDamage(component, LeftLeg_a + RightLeg_a, LeftLeg_s + RightLeg_s, ChassisLocations.Legs) ||
          ActivatableComponent.ActivateOnDamage(component, LeftArm_a + RightArm_a, LeftArm_s + RightArm_s, ChassisLocations.Arms) ||
          ActivatableComponent.ActivateOnDamage(component, CenterTorso_a + LeftTorso_a + RightTorso_a + LeftLeg_a + RightLeg_a + LeftArm_a + RightArm_a, CenterTorso_s + LeftTorso_s + RightTorso_s + LeftLeg_s + RightLeg_s + LeftArm_s + RightArm_s, ChassisLocations.All)
          ) {
          continue;
        }


      }

    }

    internal static void BatchHeatDamage(Mech instance, int amt) {
      if (instance != null && amt > 0) {
        if (!turnExternalHeatAccumulator.ContainsKey(instance.GUID)) {
          turnExternalHeatAccumulator[instance.GUID] = 0;
        }
        turnExternalHeatAccumulator[instance.GUID] = turnExternalHeatAccumulator[instance.GUID] + amt;
        if (!activationVictims.Contains(instance)) {
          activationVictims.Add(instance);
          Log.LogWrite($"{instance.DisplayName}|{instance.Nickname}|{instance.GUID} added to victims [heat]\n");
        }
      }
    }

    internal static void BatchDamage(Mech instance, float damageAmount, float directStructureDamage) {
      if (damageAmount <= 0 && directStructureDamage <= 0)
        return;
      if (instance != null) {
        if (!turnExternalHeatAccumulator.ContainsKey(instance.GUID)) {
          turnExternalHeatAccumulator[instance.GUID] = 0;
        }

        if (!activationVictims.Contains(instance)) {
          activationVictims.Add(instance);
          Log.LogWrite($"{instance.DisplayName}|{instance.Nickname}|{instance.GUID} added to victims\n");
        }
      }
    }
  }
}

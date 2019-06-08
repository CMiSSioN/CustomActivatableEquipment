using CustomComponents;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using BattleTech;
using BattleTech.UI;
using UnityEngine;
using InControl;
using Random = UnityEngine.Random;
using Localize;
using CustomActivatableEquipment;

namespace CustomActivatablePatches {
  [HarmonyPatch(typeof(CombatHUDButtonBase))]
  [HarmonyPatch("OnClick")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatHUDActionButton_ExecuteClick {

    public static bool Prefix(CombatHUDActionButton __instance) {
      CustomActivatableEquipment.Log.LogWrite("CombatHUDActionButton.ExecuteClick '" + __instance.GUID + "'/'" + CombatHUD.ButtonID_Move + "' " + (__instance.GUID == CombatHUD.ButtonID_Move) + "\n");
      CombatHUD HUD = (CombatHUD)typeof(CombatHUDActionButton).GetProperty("HUD", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance, null);
      if (__instance.GUID == CombatHUD.ButtonID_Move) {
        CustomActivatableEquipment.Log.LogWrite(" button is move\n");
        bool modifyers = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        if (modifyers) {
          CustomActivatableEquipment.Log.LogWrite(" ctrl is pressed\n");
          if (HUD.SelectedActor != null) {
            CustomActivatableEquipment.Log.LogWrite(" actor is selected\n");
            if (HUD.SelectedActor is Mech) {
              CustomActivatableEquipment.Log.LogWrite(" mech is selected\n");
              CustomActivatableEquipment.Core.ShowEquipmentDlg(HUD.SelectedActor as Mech, HUD);
            }
          }
          return false;
        }
      }else
      if (__instance.GUID == CombatHUD.ButtonID_DoneWithMech) {
        CustomActivatableEquipment.Log.LogWrite(" button is brase\n");
        bool modifyers = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        if (modifyers) {
          CustomActivatableEquipment.Log.LogWrite(" ctrl is pressed\n");
          if (HUD.SelectedActor != null) {
            CustomActivatableEquipment.Log.LogWrite(" actor is selected\n");
            if (HUD.SelectedActor is Mech) {
              CustomActivatableEquipment.Log.LogWrite(" mech is selected\n");
              CustomActivatableEquipment.Core.ShowHeatDlg(HUD.SelectedActor as Mech);
            }
          }
          return false;
        }
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(ActorMovementSequence))]
  [HarmonyPatch("CompleteOrders")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class ActorMovementSequence_CompleteOrders {
    public static void Postfix(ActorMovementSequence __instance) {
      CustomActivatableEquipment.Log.LogWrite("ActorMovementSequence.ActorMovementSequence " + __instance.owningActor.GUID + ":" + __instance.owningActor.DisplayName + "\n");
      if (__instance.meleeType == MeleeAttackType.NotSet) {
        foreach (MechComponent component in __instance.owningActor.allComponents) {
          if (component.IsFunctional == false) { continue; };
          if (CustomActivatableEquipment.ActivatableComponent.isComponentActivated(component)) {
            int aRounds = CustomActivatableEquipment.ActivatableComponent.getComponentActiveRounds(component);
            CustomActivatableEquipment.Log.LogWrite("Component:" + component.defId + " is active for " + aRounds + "\n");
            if (CustomActivatableEquipment.ActivatableComponent.rollFail(component, false) == false) {
              CustomActivatableEquipment.Log.LogWrite("Component fail\n");
              CustomActivatableEquipment.ActivatableComponent.deactivateComponent(component);
            }
          }
        }
      }
    }
  }
  [HarmonyPatch(typeof(MechMeleeSequence))]
  [HarmonyPatch("CompleteOrders")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class MechMeleeSequence_CompleteOrders {

    public static void Postfix(MechMeleeSequence __instance) {
      CustomActivatableEquipment.Log.LogWrite("MechMeleeSequence.CompleteOrders " + __instance.OwningMech.GUID + ":" + __instance.OwningMech.DisplayName + "\n");
      foreach (MechComponent component in __instance.OwningMech.allComponents) {
        if (component.IsFunctional == false) { continue; };
        if (CustomActivatableEquipment.ActivatableComponent.isComponentActivated(component)) {
          int aRounds = CustomActivatableEquipment.ActivatableComponent.getComponentActiveRounds(component);
          CustomActivatableEquipment.Log.LogWrite("Component:" + component.defId + " is active for " + aRounds + "\n");
          if (CustomActivatableEquipment.ActivatableComponent.rollFail(component, false) == false) {
            CustomActivatableEquipment.Log.LogWrite("Component fail\n");
            CustomActivatableEquipment.ActivatableComponent.deactivateComponent(component);
          }
        }
      }
    }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("GetHeatSinkDissipation")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class Mech_GetHeatSinkDissipation {

    public static void Postfix(Mech __instance, ref float __result) {
      //Log.LogWrite("Mech.GetHeatSinkDissipation:" + __instance.DisplayName + ":"+__instance.GUID+"\n");
      for (int index = 0; index < __instance.miscComponents.Count; ++index) {
        MechComponent miscComponent = __instance.miscComponents[index];
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) { continue; }
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName)) {
          //Log.LogWrite("  Offline heat sink found:" + miscComponent.getCCGUID() + ". Heat dissipation was:"+__result);
          if (__result > componentDef.DissipationCapacity) { __result -= componentDef.DissipationCapacity; } else { __result = 0f; };
          //Log.LogWrite(" become:" + __result+"\n");
        };
      }
    }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("ApplyHeatSinks")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(int) })]
  public static class Mech_ApplyHeatSinks {

    public static void Postfix(Mech __instance, int stackID) {
      Log.LogWrite("Mech.ApplyHeatSinks:" + __instance.DisplayName + ":" + __instance.GUID + "\n");
      foreach (MechComponent component in __instance.allComponents) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { continue; }
        if(activatable.CanBeactivatedManualy) {continue;}
        float OverheatLevel = (float)__instance.CurrentHeat / (float)__instance.OverheatLevel;
        if (ActivatableComponent.isComponentActivated(component)) {
          Log.LogWrite(" "+component.defId+" active "+__instance.CurrentHeat+"/"+ activatable.AutoDeactivateOnHeat + "\n");
          Log.LogWrite(" " + component.defId + " active " + OverheatLevel + "/" + activatable.AutoDeactivateOverheatLevel + "\n");
          if (activatable.AutoDeactivateOverheatLevel < CustomActivatableEquipment.Core.Epsilon) {
            if (__instance.CurrentHeat < activatable.AutoDeactivateOnHeat) {
              ActivatableComponent.deactivateComponent(component);
            }
          } else {
            if (OverheatLevel < activatable.AutoDeactivateOverheatLevel) {
              ActivatableComponent.deactivateComponent(component);
            }
          }
        } else {
          Log.LogWrite(" " + component.defId + " not active " + __instance.CurrentHeat + "/" + activatable.AutoActivateOnHeat + "\n");
          if (activatable.AutoActivateOnOverheatLevel < CustomActivatableEquipment.Core.Epsilon) {
            if (__instance.CurrentHeat >= activatable.AutoActivateOnHeat) {
              ActivatableComponent.activateComponent(component,true);
            }
          } else {
            if (OverheatLevel >= activatable.AutoActivateOnOverheatLevel) {
              ActivatableComponent.activateComponent(component,true);
            }
          }
        }
      }
    }
  }
  [HarmonyPatch(typeof(TurnDirector))]
  [HarmonyPatch("EndCurrentRound")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class TurnDirector_EndCurrentRound {

    public static void Postfix(TurnDirector __instance) {
      CustomActivatableEquipment.Log.LogWrite("TurnDirector.EndCurrentRound\n");
      foreach (var mech in __instance.Combat.AllActors) {
        CustomActivatableEquipment.Log.LogWrite(" Actor:"+mech.DisplayName+":"+mech.GUID+"\n");
        foreach (MechComponent component in mech.allComponents) {
          ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
          if (activatable == null) { continue; }
          float curFailChance = ActivatableComponent.getComponentFailChance(component);
          CustomActivatableEquipment.Log.LogWrite("  " + component.defId + " activatable\n");
          if (CustomActivatableEquipment.ActivatableComponent.isComponentActivated(component)) {
            if (curFailChance < activatable.FailFlatChance) { curFailChance = activatable.FailFlatChance; };
            curFailChance += activatable.FailChancePerTurn;
            int actRounds = ActivatableComponent.getComponentActiveRounds(component);
            ++actRounds;
            ActivatableComponent.setComponentActiveRounds(component,actRounds);
            CustomActivatableEquipment.Log.LogWrite("  active for "+actRounds+"\n");
          } else {
            curFailChance -= activatable.FailChancePerTurn;
            if (curFailChance < activatable.FailFlatChance) { curFailChance = activatable.FailFlatChance; };
          }
          ActivatableComponent.setComponentFailChance(component, curFailChance);
          CustomActivatableEquipment.Log.LogWrite("  new fail chance " + curFailChance + "\n");
        }
      }
    }
  }
}

namespace CustomActivatableEquipment {
  public static class Log {
    //private static string m_assemblyFile;
    private static string m_logfile;
    private static readonly Mutex mutex = new Mutex();
    public static string BaseDirectory;
    public static void InitLog() {
      //Log.m_assemblyFile = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
      Log.m_logfile = Path.Combine(BaseDirectory, "ActivatableComponents.log");
      //Log.m_logfile = Path.Combine(Log.m_logfile, "CustomAmmoCategories.log");
      File.Delete(Log.m_logfile);
    }
    public static void LogWrite(string line, bool isCritical = false) {
      try {
        if ((Core.Settings.debug) || (isCritical)) {
          if (Log.mutex.WaitOne(1000)) {
            File.AppendAllText(Log.m_logfile, line);
            Log.mutex.ReleaseMutex();
          }
        }
      } catch (Exception) {
        //i'm sertanly don't know what to do
      }
    }
  }
}

namespace CustomActivatableEquipment {
  public class AoEExplosion {
    public float Range;
    public float Damage;
    public float Heat;
    public float Stability;
    public AoEExplosion() {
      Range = 0f;
      Damage = 0f;
      Heat = 0f;
      Stability = 0f;
    }
  }
  [CustomComponent("ActivatableComponent")]
  public partial class ActivatableComponent : SimpleCustomComponent, IPreValidateDrop {
    public static string CAEComponentActiveStatName = "CAEComnonentActive";
    public static string CAEComponentActiveRounds = "CAEComnonentActiveRounds";
    public static string CAEComponentFailChance = "CAEFailChance";
    public static string CAEComponentChargesCount = "CAEChargesCount";
    public string ButtonName { get; set; }
    public float FailFlatChance { get; set; }
    public int FailRoundsStart { get; set; }
    public float FailChancePerTurn { get; set; }
    public float FailISDamage { get; set; }
    public float FailStabDamage { get; set; }
    public int FailPilotingBase { get; set; }
    public float FailPilotingMult { get; set; }
    public bool FailCrit { get; set; }
    public bool SelfCrit { get; set; }
    public float MechTonnageWeightMult { get; set; }
    public float MechTonnageSlotsMult { get; set; }
    public float EngineTonnageWeightMult { get; set; }
    public float EngineTonnageSlotsMult { get; set; }
    public float AutoActivateOnHeat { get; set; }
    public float AutoDeactivateOnHeat { get; set; }
    public float AutoActivateOnOverheatLevel { get; set; }
    public float AutoDeactivateOverheatLevel { get; set; }
    public string ActivationMessage { get; set; }
    public string DeactivationMessage { get; set; }
    public bool ActivationIsBuff { get; set; }
    public bool NoUniqueCheck { get; set; }
    public int ChargesCount { get; set; }
    public ChassisLocations[] FailDamageLocations { get; set; }
    public EffectData[] statusEffects { get; set; }
    public VFXInfo presistantVFX { get; set; }
    public VFXInfo activateVFX { get; set; }
    public VFXInfo destroyedVFX { get; set; }
    public AoEExplosion Explosion { get; set; }
    public bool CanNotBectivatedManualy { get; set; }
    public bool ExplodeOnFail { get; set; }
    public bool AlwaysFail { get; set; }
    public bool ExplodeOnDamage { get; set; }
    public float FailChancePerActivation { get; set; }
    public ActivatableComponent() {
      ButtonName = "NotSet";
      FailFlatChance = 0f;
      FailStabDamage = 0f;
      FailChancePerTurn = 0f;
      FailISDamage = 0f;
      FailRoundsStart = 0;
      SelfCrit = false;
      MechTonnageWeightMult = 0f;
      MechTonnageSlotsMult = 0f;
      EngineTonnageWeightMult = 0f;
      EngineTonnageSlotsMult = 0f;
      FailPilotingBase = 5;
      FailPilotingMult = 0f;
      AutoActivateOnHeat = 0f;
      AutoDeactivateOnHeat = 0f;
      FailDamageLocations = new ChassisLocations[0];
      statusEffects = new EffectData[0];
      ActivationMessage = "ON";
      DeactivationMessage = "OFF";
      ActivationIsBuff = true;
      NoUniqueCheck = false;
      ChargesCount = 0;
      presistantVFX = new VFXInfo();
      activateVFX = new VFXInfo();
      destroyedVFX = new VFXInfo();
      Explosion = new AoEExplosion();
      CanNotBectivatedManualy = false;
      ExplodeOnFail = false;
      ExplodeOnDamage = false;
      AlwaysFail = true;
      FailChancePerActivation = 0f;
      Log.LogWrite("ActivatableComponent constructor\n");
    }
    
    public static bool isOutOfCharges(MechComponent component) {
      if (component == null) { return false; };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return false; }
      if (activatable.ChargesCount == 0) { return false; };
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentChargesCount) == false) { return false; }
      int charges = component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentChargesCount).Value<int>();
      if (charges > 0) { return false; };
      return true;
    }
    public static int getChargesCount(MechComponent component) {
      if (component == null) { return 0; };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0; }
      if (activatable.ChargesCount <= 0) { return 0; };
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentChargesCount) == false) { return activatable.ChargesCount; }
      int charges = component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentChargesCount).Value<int>();
      return charges;
    }
    public static void setChargesCount(MechComponent component,int charges) {
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentChargesCount) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentChargesCount, charges);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentChargesCount, charges);
      }
    }
    public bool CanBeactivatedManualy {
      get {
        //if (this.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) { return false; }
        //if (this.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) { return false; }
        return !this.CanNotBectivatedManualy;
      }
    }
    public static void critComponent(Mech mech,MechComponent componentInSlot, ChassisLocations location, ref WeaponHitInfo hitInfo) {
      Weapon weapon1 = componentInSlot as Weapon;
      AmmunitionBox ammoBox = componentInSlot as AmmunitionBox;
      Jumpjet jumpjet = componentInSlot as Jumpjet;
      HeatSinkDef componentDef = componentInSlot.componentDef as HeatSinkDef;
      bool flag = weapon1 != null;
      if ((UnityEngine.Object)mech.GameRep != (UnityEngine.Object)null) {
        WwiseManager.SetSwitch<AudioSwitch_weapon_type>(AudioSwitch_weapon_type.laser_medium, mech.GameRep.audioObject);
        WwiseManager.SetSwitch<AudioSwitch_surface_type>(AudioSwitch_surface_type.mech_critical_hit, mech.GameRep.audioObject);
        int num1 = (int)WwiseManager.PostEvent<AudioEventList_impact>(AudioEventList_impact.impact_weapon, mech.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
        int num2 = (int)WwiseManager.PostEvent<AudioEventList_explosion>(AudioEventList_explosion.explosion_small, mech.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
        if (mech.team.LocalPlayerControlsTeam)
          AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "critical_hit_friendly ", (AkGameObj)null, (AkCallbackManager.EventCallback)null);
        else if (!mech.team.IsFriendly(mech.Combat.LocalPlayerTeam))
          AudioEventManager.PlayAudioEvent("audioeventdef_musictriggers_combat", "critical_hit_enemy", (AkGameObj)null, (AkCallbackManager.EventCallback)null);
        if (jumpjet == null && componentDef == null && (ammoBox == null && componentInSlot.DamageLevel > ComponentDamageLevel.Functional))
          mech.GameRep.PlayComponentCritVFX((int)location);
        if (ammoBox != null && componentInSlot.DamageLevel > ComponentDamageLevel.Functional)
          mech.GameRep.PlayVFX((int)location, (string)mech.Combat.Constants.VFXNames.componentDestruction_AmmoExplosion, true, Vector3.zero, true, -1f);
      }
      ComponentDamageLevel damageLevel = componentInSlot.DamageLevel;
      switch (damageLevel) {
        case ComponentDamageLevel.Functional:
          if (flag) {
            damageLevel = ComponentDamageLevel.Penalized;
            mech.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)mech, new Text("{0} CRIT", new object[1]
            {
                    (object) componentInSlot.UIName
            }), FloatieMessage.MessageNature.CriticalHit, true)));
            goto case ComponentDamageLevel.Destroyed;
          } else {
            damageLevel = ComponentDamageLevel.Destroyed;
            mech.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)mech, new Text("{0} DESTROYED", new object[1]
            {
                    (object) componentInSlot.UIName
            }), FloatieMessage.MessageNature.ComponentDestroyed, true)));
            goto case ComponentDamageLevel.Destroyed;
          }
        case ComponentDamageLevel.Destroyed:
          componentInSlot.DamageComponent(hitInfo, damageLevel, true);
          break;
        default:
          damageLevel = ComponentDamageLevel.Destroyed;
          mech.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage((IStackSequence)new ShowActorInfoSequence((ICombatant)mech, new Text("{0} DESTROYED", new object[1]
          {
                  (object) componentInSlot.UIName
          }), FloatieMessage.MessageNature.ComponentDestroyed, true)));
          goto case ComponentDamageLevel.Destroyed;
      }
    }

    public static void CritLocation(Mech mech, ChassisLocations location, ref WeaponHitInfo hitInfo) {
      Log.LogWrite("CritLocation " + mech.DisplayName + ":"+location+"\n");
      int maxSlots = mech.MechDef.GetChassisLocationDef(location).InventorySlots;
      Log.LogWrite(" slots in location:"+maxSlots+"\n");
      List<int> slotsWithComponents = new List<int>();
      for(int slotIndex = 0;slotIndex < maxSlots; ++slotIndex) {
        MechComponent testInSlot = mech.GetComponentInSlot(location, slotIndex);
        if (testInSlot != null) {
          Log.LogWrite(" slots:" + slotIndex + ":"+testInSlot.defId+"\n");
          slotsWithComponents.Add(slotIndex);
        } else {
          Log.LogWrite(" slots:" + slotIndex + ":empty\n");
        }
      }
      int slotRoll = (int)(((float)slotsWithComponents.Count)*Random.Range(0f,1f));
      Log.LogWrite(" slotRoll:" + slotRoll + "\n");
      MechComponent componentInSlot = mech.GetComponentInSlot(location, slotRoll);
      if(componentInSlot != null) {
        Log.LogWrite(" critComponent:" + componentInSlot.defId + "\n");
        ActivatableComponent.critComponent(mech, componentInSlot, location, ref hitInfo);
      } else {
        Log.LogWrite(" crit to empty slot. possibly only if no components in location\n");
      }
    }

    public static bool rollFail(MechComponent component, bool isInital = false) {
      Log.LogWrite("rollFail " + component.defId + "\n");
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return false; }
      if ((ActivatableComponent.isComponentActivated(component) == false)&&(isInital == false)) {
        Log.LogWrite(" not activated\n");
        return false;
      };
      int actRounds = ActivatableComponent.getComponentActiveRounds(component);
      if ((isInital == false) && (actRounds < activatable.FailRoundsStart)) {
        Log.LogWrite(" check not needed\n");
        return true;
      }
      //if (activatable.FailDamageLocations.Length <= 0) {
        //Log.LogWrite(" no locations to hit\n");
        //return true;
      //}
      if (!(component.parent is Mech)) {
        Log.LogWrite(" owner is not mech\n");
        return true;
      }
      Mech owner = component.parent as Mech;
      float chance = ActivatableComponent.getEffectiveComponentFailChance(component);
      //if(chance < activatable.FailFlatChance) {
      //  chance = activatable.FailFlatChance;
      //  ActivatableComponent.setComponentFailChance(component, activatable.FailFlatChance);
      //}
      Log.LogWrite(" chance:"+chance+"\n");
      float roll = Random.Range(0f, 1f);
      if (activatable.AlwaysFail) {
        roll = -1f;
        Log.LogWrite(" always fail\n");
      }
      Log.LogWrite(" roll:" + roll + "\n");
      if(roll < chance) {
        /*ObjectSpawnDataSelf activateEffect = component.ActivateVFX();
        if (activateEffect != null) {
          Log.LogWrite(" "+component.defId+" activate VFX is not null\n");
          activateEffect.SpawnSelf(component.parent.Combat);
        } else {
          Log.LogWrite(" " + component.defId + " activate VFX is null\n");
        }
        component.AoEExplodeComponent();*/
        var fakeHit = new WeaponHitInfo(-1, -1, -1, -1, component.parent.GUID, component.parent.GUID, -1, null, null, null, null, null, null, null, null, null, null, null);
        if (activatable.FailISDamage >= 1f) {
          foreach(ChassisLocations location in activatable.FailDamageLocations) {
            Log.LogWrite(" apply inner structure damage:" + location + "\n");
            owner.ApplyStructureStatDamage(location,activatable.FailISDamage,fakeHit);
            if (owner.IsLocationDestroyed(location)) {
              owner.NukeStructureLocation(fakeHit, (int)location, location, Vector3.zero, DamageType.OverheatSelf);
            }
          }
          component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, "STRUCTURE DAMAGE", FloatieMessage.MessageNature.CriticalHit));
        }
        if (activatable.FailCrit) {
          foreach (ChassisLocations location in activatable.FailDamageLocations) {
            Log.LogWrite(" apply crit:" + location + "\n");
            ActivatableComponent.CritLocation(owner,location,ref fakeHit);
          }
        }
        if (activatable.SelfCrit) {
          Log.LogWrite(" apply crit to self\n");
          ActivatableComponent.critComponent(owner, component, MechStructureRules.GetChassisLocationFromArmorLocation((ArmorLocation)component.Location), ref fakeHit);
        }
        if(activatable.FailStabDamage > Core.Epsilon) {
          owner.AddAbsoluteInstability(activatable.FailStabDamage, StabilityChangeSource.Effect, owner.GUID);
        }
        Log.LogWrite(" owner status. Death:"+owner.IsFlaggedForDeath+" Knockdown:"+owner.IsFlaggedForKnockdown+"\n");
        bool needToDone = false;
        if (owner.IsFlaggedForDeath || owner.IsFlaggedForKnockdown) {
          Log.LogWrite(" need done with actor\n");
          needToDone = true;
          owner.HasFiredThisRound = true;
          owner.HasMovedThisRound = true;
        }
        owner.HandleDeath(owner.GUID);
        if (owner.IsDead == false) {
          owner.HandleKnockdown(-1, owner.GUID, Vector2.one, (SequenceFinished)null);
        }
        if (needToDone) {
          Log.LogWrite(" done with actor\n");
          owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new AddSequenceToStackMessage(owner.DoneWithActor()));
        }
        if (activatable.ExplodeOnFail) {
          component.AoEExplodeComponent();
        }
        return false;
      }
      return true;
    }
    public static bool isComponentActivated(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return false; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
        return false;
      }
      return component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentActiveStatName).Value<bool>();
    }
      public static bool isComponentActivatable(MechComponent component) {
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable == null) { return false; }
        return true;
      }
      public static int getComponentActiveRounds(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        return 0;
      }
      return component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentActiveRounds).Value<int>();
    }
    public static void setComponentActiveRounds(MechComponent component,int aRounds) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, aRounds);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActiveRounds, aRounds);
      }
    }
    public static float getComponentFailChance(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentFailChance) == false) {
        return 0;
      }
      return component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentFailChance).Value<float>();
    }
    public static float getEffectiveComponentFailChance(MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return 0; }
      float FailChance = 0f;
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentFailChance) == true) {
        FailChance = component.StatCollection.GetStatistic(ActivatableComponent.CAEComponentFailChance).Value<float>();
      }
      if (FailChance < activatable.FailFlatChance) { FailChance = activatable.FailFlatChance; };
      FailChance += (activatable.FailPilotingBase - component.parent.SkillPiloting) * activatable.FailPilotingMult;
      if (FailChance < 0f) { FailChance = 0f; };
      return FailChance;
    }
    public static void setComponentFailChance(MechComponent component, float chance) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentFailChance) == false) {
        component.StatCollection.AddStatistic<float>(ActivatableComponent.CAEComponentFailChance, chance);
      } else {
        component.StatCollection.Set<float>(ActivatableComponent.CAEComponentFailChance, chance);
      }
    }
    public static void activateComponent(MechComponent component,bool autoActivate = false) {
      Log.LogWrite("activateComponent " + component.defId + "\n");
      if (component.IsFunctional == false) {
        Log.LogWrite(" not functional\n");
        return;
      };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        Log.LogWrite(" not activatable\n");
        return;
      }
      if (ActivatableComponent.isComponentActivated(component) == true) {
        Log.LogWrite(" already activated\n");
        return;
      };
      if (ActivatableComponent.isOutOfCharges(component)) {
        Log.LogWrite(" out of charges\n");
        return;
      }
      if (autoActivate == false) {
        if (ActivatableComponent.rollFail(component, true) == false) {
          Log.LogWrite(" fail to activate\n");
          return;
        }
      } else {
        Log.LogWrite(" auto activation. no fail roll needed\n");
      }
      if (activatable.ChargesCount > 0) {
        int charges = ActivatableComponent.getChargesCount(component);
        if(charges > 0) {
          --charges;
          ActivatableComponent.setChargesCount(component,charges);
          Log.LogWrite(" remains charges:"+charges+"\n");
        } else {
          Log.LogWrite(" out of charges\n");
          return;
        }
      } else {
        if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
          component.StatCollection.AddStatistic<bool>(ActivatableComponent.CAEComponentActiveStatName, true);
        } else {
          component.StatCollection.Set<bool>(ActivatableComponent.CAEComponentActiveStatName, true);
        }
      }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      }
      if(activatable.FailChancePerActivation > Core.Epsilon) {
        float curFailChance = ActivatableComponent.getComponentFailChance(component);
        curFailChance += activatable.FailChancePerActivation;
        if (curFailChance < activatable.FailFlatChance) { curFailChance = activatable.FailFlatChance; };
        ActivatableComponent.setComponentFailChance(component,curFailChance);
      }
      if (activatable.statusEffects == null) {
        Log.LogWrite(" no activatable effects\n");
        return;
      }
      Log.LogWrite(" activatable effects count: "+ activatable.statusEffects.Length + "\n");
      Log.LogWrite(" sprint:"+component.parent.MaxSprintDistance+"\n");
      Log.LogWrite(" walk:" + component.parent.MaxWalkDistance + "\n");
      for (int index = 0; index < activatable.statusEffects.Length; ++index) {
        EffectData statusEffect = activatable.statusEffects[index];
        if (statusEffect.targetingData.effectTriggerType == EffectTriggerType.Passive) {
          string effectID = string.Format("ActivatableEffect_{0}_{1}", (object)component.parent.GUID, (object)component.uid);
          if (statusEffect.targetingData.effectTargetType == EffectTargetType.Creator) {
            typeof(MechComponent).GetMethod("ApplyPassiveEffectToTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component,new object[4] {
              (object)statusEffect,(object)component.parent,(object)((ICombatant)component.parent),(object)effectID
            });
            component.createdEffectIDs.Add(effectID);
            Log.LogWrite("Activate effect "+effectID+":"+statusEffect.Description.Id+"\n");
          }
        }
      }
      component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID,
        component.Description.UIName + " " + activatable.ActivationMessage,
        (activatable.ActivationIsBuff) ? FloatieMessage.MessageNature.Buff : FloatieMessage.MessageNature.Debuff
      ));
      component.parent.ResetPathing(false);
      ObjectSpawnDataSelf activeVFX = component.ActivateVFX();
      if (activeVFX != null) { activeVFX.SpawnSelf(component.parent.Combat); }
      Log.LogWrite(" sprint:" + component.parent.MaxSprintDistance + "\n");
      Log.LogWrite(" walk:" + component.parent.MaxWalkDistance + "\n");
    }

    public static void deactivateComponent(MechComponent component) {
      if (component.IsFunctional == false) { return; };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (ActivatableComponent.isComponentActivated(component) == false) { return; };
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
        component.StatCollection.AddStatistic<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      } else {
        component.StatCollection.Set<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      } else {
        component.StatCollection.Set<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      }
      if (activatable.statusEffects == null) {
        return;
      }
      List<string> actEffectsIDs = new List<string>();
      foreach (string effID in component.createdEffectIDs) {
        if (effID.StartsWith("ActivatableEffect_")) {
          actEffectsIDs.Add(effID);
        }
      }
      foreach(string effId in actEffectsIDs) {
        List<Effect> allEffectsWithId = component.parent.Combat.EffectManager.GetAllEffectsWithID(effId);
        for (int index2 = 0; index2 < allEffectsWithId.Count; ++index2) {
          Log.LogWrite("Removing effect " + effId + ":" + allEffectsWithId[index2].EffectData.Description.Id + "\n");
          component.parent.CancelEffect(allEffectsWithId[index2], false);
        }
        component.createdEffectIDs.Remove(effId);
      }
      component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, component.parent.GUID, 
        component.Description.UIName + " "+activatable.DeactivationMessage,
        (activatable.ActivationIsBuff)?FloatieMessage.MessageNature.Debuff: FloatieMessage.MessageNature.Buff
      ));
      ObjectSpawnDataSelf activeVFX = component.ActivateVFX();
      if (activeVFX != null) { activeVFX.CleanupSelf(); }
    }
    public static void toggleComponentActivation(MechComponent component) {
      Log.LogWrite("toggleComponentActivation "+component.defId+"\n");
      if (component.IsFunctional == false) {
        Log.LogWrite(" not functional\n");
        return;
      };
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) {
        Log.LogWrite(" not activatable\n");
        return;
      }
      component.parent.OnActivationBegin(component.parent.GUID, -1);
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveStatName) == false) {
        component.StatCollection.AddStatistic<bool>(ActivatableComponent.CAEComponentActiveStatName, false);
      }
      if (CustomActivatableEquipment.Core.checkExistance(component.StatCollection, ActivatableComponent.CAEComponentActiveRounds) == false) {
        component.StatCollection.AddStatistic<int>(ActivatableComponent.CAEComponentActiveRounds, 0);
      }
      if (ActivatableComponent.isComponentActivated(component) == false) {
        Log.LogWrite(" activating\n");
        ActivatableComponent.activateComponent(component);
      } else {
        ActivatableComponent.deactivateComponent(component);
      }
    }

    public string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location, MechLabHelper mechlab) {
      Log.LogWrite("PreValidateDrop\n");
      if (this.MechTonnageWeightMult > CustomActivatableEquipment.Core.Epsilon) {
        float self_tonnage = (float)Math.Ceiling((double)this.Def.Tonnage);
        float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.MechTonnageWeightMult));
        float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.MechTonnageWeightMult));
        Log.LogWrite(" checking on tonnage. mech : "+downLimit+" - "+upLimit+"\n");
        if ((mechlab.MechLab.activeMechDef.Chassis.Tonnage <= downLimit) || (mechlab.MechLab.activeMechDef.Chassis.Tonnage > upLimit)) {
          string result = "This component is not sutable for this chassis. Tonnage must be " + (downLimit + 1f) + "-" + upLimit;
          return result;
        }
      }
      if (this.MechTonnageSlotsMult > CustomActivatableEquipment.Core.Epsilon) {
        float self_tonnage = this.Def.InventorySize;
        float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.MechTonnageSlotsMult));
        float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.MechTonnageSlotsMult));
        Log.LogWrite(" checking on tonnage. mech : " + downLimit + " - " + upLimit + "\n");
        if ((mechlab.MechLab.activeMechDef.Chassis.Tonnage <= downLimit) || (mechlab.MechLab.activeMechDef.Chassis.Tonnage > upLimit)) {
          string result = "This component is not sutable for this chassis. Tonnage must be " + (downLimit + 1f) + "-" + upLimit;
          return result;
        }
      }
      if ((this.EngineTonnageWeightMult > CustomActivatableEquipment.Core.Epsilon)||(this.EngineTonnageSlotsMult > CustomActivatableEquipment.Core.Epsilon)) {
        Log.LogWrite(" checking on engine weight\n");
        List<MechComponentRef> components = mechlab.MechLab.activeMechInventory;
        float engineTonnage = 0f;
        foreach (var comp in components) {
          if (comp.Def.IsCategory("EnginePart")) {
            engineTonnage += comp.Def.Tonnage;
          };
        }
        if(engineTonnage < CustomActivatableEquipment.Core.Epsilon) {
          return string.Empty;
        }
        if (this.EngineTonnageWeightMult > CustomActivatableEquipment.Core.Epsilon) {
          float self_tonnage = (float)Math.Ceiling((double)this.Def.Tonnage);
          float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.EngineTonnageWeightMult));
          float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.EngineTonnageWeightMult));
          Log.LogWrite(" checking on tonnage. engine : " + downLimit + " - " + upLimit + "\n");
          if ((engineTonnage <= downLimit) || (engineTonnage > upLimit)) {
            string result = "This component is not sutable for this chassis. Engine tonnage must be " + (downLimit + 1f) + "-" + upLimit;
            return result;
          }
        }
        if (this.EngineTonnageSlotsMult > CustomActivatableEquipment.Core.Epsilon) {
          float self_tonnage = this.Def.InventorySize;
          float upLimit = (float)Math.Ceiling((double)(self_tonnage * this.EngineTonnageSlotsMult));
          float downLimit = (float)Math.Ceiling((double)((self_tonnage - 1f) * this.EngineTonnageSlotsMult));
          Log.LogWrite(" checking on tonnage. engine : " + downLimit + " - " + upLimit + "\n");
          if ((engineTonnage <= downLimit) || (engineTonnage > upLimit)) {
            string result = "This component is not sutable for this chassis. Engine tonnage must be " + (downLimit+1f) + "-" + upLimit;
            return result;
          }
        }
      }
      if (this.NoUniqueCheck == false) {
        foreach (var comp in mechlab.MechLab.activeMechInventory) {
          ActivatableComponent activatable = comp.Def.GetComponent<ActivatableComponent>();
          if (activatable != null) {
            if (activatable.ButtonName == this.ButtonName) {
              string result = "This mech already have component of the same type";
              return result;
            }
          }
        }
      }
      return string.Empty;
      //mechlab.MechLab.activeMechDef.
    }
  }
  public class Settings {
    public bool debug { get; set; }
    public float AIComponentUsefullModifyer { get; set; }
    public float AIComponentExtreamlyUsefulModifyer { get; set; }
    public float AIOffenceUsefullCoeff { get; set; }
    public float AIDefenceUsefullCoeff { get; set; }
    public float AIHeatCoeffCoeff { get; set; }
    public float AIOverheatCoeffCoeff { get; set; }
    public List<string> AdditionalAssets { get; set; }
    public Settings() {
      debug = true;
      AdditionalAssets = new List<string>();
      AIComponentUsefullModifyer = 0.4f;
      AIComponentExtreamlyUsefulModifyer = 0.6f;
      AIOffenceUsefullCoeff = 0.2f;
      AIDefenceUsefullCoeff = 0.2f;
      AIHeatCoeffCoeff = 0.9f;
      AIOverheatCoeffCoeff = 0.8f;
    }
  }
  public class ComponentToggle {
    public MechComponent component;
    public ActivatableComponent activatable;
    public ComponentToggle(MechComponent c, ActivatableComponent a) {
      this.component = c;
      this.activatable = a;
    }
    public void toggle() {
      Log.LogWrite("Toggle activatable " + component.defId + "\n");
      ActivatableComponent.toggleComponentActivation(this.component);
    }
  }

  public class HeatSinkToggle {
    public Mech mech;
    public GenericPopup popup;
    public HeatSinkToggle(Mech m) {
      this.mech = m;
    }
    public void AddHeatSink() {
      if (mech == null) {
        Log.LogWrite("WARNING! Alter mech heatsink state without mech selected", true);
        return;
      }
      for (int index = 0; index < mech.miscComponents.Count; ++index) {
        MechComponent miscComponent = mech.miscComponents[index];
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) { continue; }
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName) == false) { continue; };
        miscComponent.RemoveTag(CustomActivatableEquipment.Core.HeatSinkOfflineTagName);
        this.popup.TextContent = HeatSinkToggle.MechHeatSinksInfo(this.mech);
        break;
      }
    }
    public void RemoveHeatSink() {
      if (mech == null) {
        Log.LogWrite("WARNING! Alter mech heatsink state without mech selected", true);
        return;
      }
      Log.LogWrite("Deactivating heatsink:"+mech.DisplayName+":"+mech.GUID+"\n");
      for (int index = 0; index < mech.miscComponents.Count; ++index) {
        MechComponent miscComponent = mech.miscComponents[index];
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) {continue;}
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName)) {continue;};
        Log.LogWrite("  Active heat sinc found:" + miscComponent.getCCGUID() + "\n");
        miscComponent.AddTag(CustomActivatableEquipment.Core.HeatSinkOfflineTagName);
        Log.LogWrite("  Add tag:" + miscComponent.ComponentTags().ToString() + "\n");
        this.popup.TextContent = HeatSinkToggle.MechHeatSinksInfo(this.mech);
        break;
      }
    }
    public static string MechHeatSinksInfo(Mech mech) {
      int AvaibleHeatSinks = 0;
      int DestroyedHeatSinks = 0;
      int OfflineHeatSinks = 0;
      float OnlineHeatDissipation = 0f;
      if(mech == null) {
        Log.LogWrite("WARNING! trying to get mech heat info without mech selected",true);
        return "No mech selected";
      }
      if(mech.miscComponents == null) {
        Log.LogWrite("WARNING! trying to get mech heat info without misc components on mech", true);
        return "No heatsinks found";
      }
      for (int index = 0; index < mech.miscComponents.Count; ++index) {
        MechComponent miscComponent = mech.miscComponents[index];
        if (miscComponent == null) {
          Log.LogWrite("WARNING! empty component", true);
          continue;
        }
        if (miscComponent.componentType != ComponentType.HeatSink) { continue; };
        HeatSinkDef componentDef = miscComponent.componentDef as HeatSinkDef;
        if (miscComponent == null) {
          Log.LogWrite("WARNING! Component without def", true);
          continue;
        }
        if (componentDef.DissipationCapacity < CustomActivatableEquipment.Core.Epsilon) { continue; };
        ++AvaibleHeatSinks;
        if (miscComponent.DamageLevel > ComponentDamageLevel.NonFunctional) {
          ++DestroyedHeatSinks; continue;
        }
        if (miscComponent.ComponentTags().Contains(CustomActivatableEquipment.Core.HeatSinkOfflineTagName)) {
          ++OfflineHeatSinks;continue;
        }
        OnlineHeatDissipation += componentDef.DissipationCapacity;
      }
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("Current heat level:" + mech.CurrentHeat+"\n");
      stringBuilder.Append("Overheat heat level:" + mech.OverheatLevel + "\n");
      stringBuilder.Append("Shutdown heat level:" + mech.MaxHeat + "\n");
      stringBuilder.Append("All chassis dedicated heat sinks count:" + AvaibleHeatSinks + "\n");
      stringBuilder.Append("Destroyed dedicated heat sinks count:" + DestroyedHeatSinks + "\n");
      stringBuilder.Append("Offline dedicated heat sinks count:" + OfflineHeatSinks + "\n");
      stringBuilder.Append("Online dedicated heat sinks count:" + (AvaibleHeatSinks - DestroyedHeatSinks - OfflineHeatSinks) + "\n");
      /*stringBuilder.Append("Online dedicated heat sinks dissipation:" + (OnlineHeatDissipation) + "\n");
      int tot
      stringBuilder.Append("Temporary heat sinks dissipation:" + mech.HeatSinkCapacity + "\n");
      float num = 1f;
      if (mech.occupiedDesignMask != null && !Mathf.Approximately(mech.occupiedDesignMask.heatSinkMultiplier, 1f)) {
        num *= mech.occupiedDesignMask.heatSinkMultiplier;
        stringBuilder.Append("Mech position heat sink multiplier:" + (mech.occupiedDesignMask.heatSinkMultiplier) + "\n");
      }
      if (mech.Combat.MapMetaData.biomeDesignMask != null && !Mathf.Approximately(mech.Combat.MapMetaData.biomeDesignMask.heatSinkMultiplier, 1f)) {
        num *= mech.Combat.MapMetaData.biomeDesignMask.heatSinkMultiplier;
        stringBuilder.Append("Biome heat sink multiplier:" + (mech.occupiedDesignMask.heatSinkMultiplier) + "\n");
      }
      stringBuilder.Append("Global combat heat sink multiplyer:" + (mech.Combat.Constants.Heat.GlobalHeatSinkMultiplier) + "\n");

      OnlineHeatDissipation = (float)Math.Round(((double)OnlineHeatDissipation * ((double)num * (double)mech.Combat.Constants.Heat.GlobalHeatSinkMultiplier)));*/
      stringBuilder.Append("Effective total heat dissipation:" + (mech.AdjustedHeatsinkCapacity) + "\n");
      Log.LogWrite(stringBuilder.ToString());
      return stringBuilder.ToString();
    }
  }
  public static partial class Core {
    public static float Epsilon = 0.01f;
    //public static Dictionary<string, GameObject> AdditinalFXObjects = new Dictionary<string, GameObject>();

    //public static List<ActivatableComponent> currentActiveComponentsDlg = new List<ActivatableComponent>();
    public static readonly string HeatSinkOfflineTagName = "offline";
    //public static Dictionary<string,List<ComponentToggle>>
    public static bool checkExistance(StatCollection statCollection, string statName) {
      return ((Dictionary<string, Statistic>)typeof(StatCollection).GetField("stats", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(statCollection)).ContainsKey(statName);
    }
    public static void ShowHeatDlg(Mech mech) {
      if (mech == null) {
        GenericPopupBuilder popup = GenericPopupBuilder.Create("Heat sinks", "No mech selected");
        popup.AddButton("Done", (Action)null, true, (PlayerAction)null);
        popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
      } else {
        GenericPopupBuilder popup = GenericPopupBuilder.Create("Heat sinks", HeatSinkToggle.MechHeatSinksInfo(mech));
        HeatSinkToggle HTT = new HeatSinkToggle(mech);
        popup.AddButton("Done", (Action)null, true, (PlayerAction)null);
        popup.AddButton("-1", new Action(HTT.RemoveHeatSink), false, (PlayerAction)null);
        popup.AddButton("+1", new Action(HTT.AddHeatSink), false, (PlayerAction)null);
        HTT.popup = popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
      }
    }
    public static void ShowEquipmentDlg(Mech mech,CombatHUD HUD) {
      List<string> activatables = new List<string>();
      List<ComponentToggle> actComps = new List<ComponentToggle>();
      //Core.currentActiveComponentsDlg.Clear();
      foreach (MechComponent component in mech.allComponents) {
        //if (component.IsFunctional == false) { return; };
        ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        if (activatable != null) {
          Log.LogWrite(component.defId + ":"+component.parent.GUID+":" + component.getCCGUID() + " is activatable\n");
          //if (activatable.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) {
            //Log.LogWrite(" but can't be activated manualy activatable\n");
          //} else {
            activatables.Add(activatable.ButtonName);
            actComps.Add(new ComponentToggle(component,activatable));
          //}
          //Core.currentActiveComponentsDlg.Add(activatable);
        } else {
          Log.LogWrite(component.defId + ":" + component.parent.GUID + ":" + component.getCCGUID() + " is not activatable\n");
        }
      }
      StringBuilder text = new StringBuilder();
      text.Append("Active components:");
      for (int index = 0; index < activatables.Count; ++index) {
        MechComponent component = actComps[index].component;
        text.Append("\n" + component.UIName);
        text.Append(" STATE:");
        if (component.IsFunctional == false) {
          text.Append(" NON FUNCTIONAL");
          continue;
        }
        if (ActivatableComponent.isOutOfCharges(component)) {
          text.Append(" OUT OF CHARGES");
          continue;
        }
        if(actComps[index].activatable.ChargesCount > 0) {
          text.Append(" CHARGES:"+ActivatableComponent.getChargesCount(component));
        }
        if (ActivatableComponent.isComponentActivated(component)) {
          text.Append(" " + actComps[index].activatable.ActivationMessage + " ");
          if (actComps[index].activatable.CanBeactivatedManualy == false) {
            if (component.parent is Mech) {
              float neededHeat = (actComps[index].activatable.AutoDeactivateOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? actComps[index].activatable.AutoDeactivateOverheatLevel * (float)(component.parent as Mech).OverheatLevel : actComps[index].activatable.AutoDeactivateOnHeat;
              text.Append("HEAT:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
            }
          }
        } else {
          text.Append(" " + actComps[index].activatable.DeactivationMessage + " ");
          if (actComps[index].activatable.AutoActivateOnHeat > CustomActivatableEquipment.Core.Epsilon) {
            if (component.parent is Mech) {
              float neededHeat = (actComps[index].activatable.AutoActivateOnOverheatLevel > CustomActivatableEquipment.Core.Epsilon) ? actComps[index].activatable.AutoActivateOnOverheatLevel * (float)(component.parent as Mech).OverheatLevel : actComps[index].activatable.AutoActivateOnHeat;
              text.Append("HEAT:" + (component.parent as Mech).CurrentHeat + "/" + neededHeat);
            }
          }
        }
        float failChance = ActivatableComponent.getEffectiveComponentFailChance(component);
        //ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
        //if (failChance < activatable.FailFlatChance) { failChance = activatable.FailFlatChance; };
        text.Append(" FAIL:" + Math.Round(failChance * 100f) + "%");
      }
      if(HUD.SelectedTarget != null) {
        text.Append("\nYou can't activate/deactivate equipment while having target selected");
      }
      GenericPopupBuilder popup = GenericPopupBuilder.Create("Components", text.ToString());
      popup.AddButton("Done", (Action)null, true, (PlayerAction)null);
      if (HUD.SelectedTarget == null) {
        for (int index = 0; index < activatables.Count; ++index) {
          if (actComps[index].component.IsFunctional == false) { continue; };
          if (actComps[index].activatable.CanBeactivatedManualy) {
            if (ActivatableComponent.isOutOfCharges(actComps[index].component) == false) {
              popup.AddButton(activatables[index], new Action(actComps[index].toggle), true, (PlayerAction)null);
            }
          }
        }
      }
      Log.LogWrite("Rendering popup:"+text.ToString()+"\n");
      popup.IsNestedPopupWithBuiltInFader().CancelOnEscape().Render();
    }

    public static Settings Settings = new Settings();
    public static void Init(string directory, string settingsJson) {
      CustomActivatableEquipment.Log.BaseDirectory = directory;
      CustomActivatableEquipment.Log.InitLog();
      Core.Settings = JsonConvert.DeserializeObject<CustomActivatableEquipment.Settings>(settingsJson);
      CustomActivatableEquipment.Log.LogWrite("Initing... " + directory + "\n");
      /*try {
        string apath = Path.Combine(directory, "assets");
        Log.LogWrite("additional assets:" + Core.Settings.AdditionalAssets.Count + "\n");
        foreach (string assetName in Core.Settings.AdditionalAssets) {
          string path = Path.Combine(apath, assetName);
          if (File.Exists(path)) {
            var assetBundle = AssetBundle.LoadFromFile(path);
            if (assetBundle != null) {
              Log.LogWrite("asset " + path + ":" + assetBundle.name + " loaded\n");
              UnityEngine.GameObject[] objects = assetBundle.LoadAllAssets<GameObject>();
              Log.LogWrite("FX objects:\n");
              foreach (var obj in objects) {
                Log.LogWrite(" " + obj.name + "\n");
                AdditinalFXObjects.Add(obj.name, obj);
              }
            } else {
              Log.LogWrite("fail to load:" + path + "\n");
            }
          } else {
            Log.LogWrite("not exists:" + path + "\n");
          }
        }
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n");
      }*/
      try {
        CustomComponents.Registry.RegisterSimpleCustomComponents(Assembly.GetExecutingAssembly());
        var harmony = HarmonyInstance.Create("io.mission.activatablecomponents");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
      } catch (Exception e) {
        CustomActivatableEquipment.Log.LogWrite(e.ToString() + "\n");
      }
    }
  }
}

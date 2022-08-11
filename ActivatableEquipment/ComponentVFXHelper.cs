using BattleTech;
using BattleTech.Rendering;
using CustAmmoCategories;
using CustomComponents;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatableEquipment {
  //[HarmonyPatch(typeof(Mech))]
  //[HarmonyPatch("InitGameRep")]
  //[HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { typeof(Transform) })]
  public static class Mech_InitGameRep {
    public static void Postfix(Mech __instance) {
      try {
        Log.Debug?.TWL(0, "Mech_InitGameRep.Postfix " + __instance.PilotableActorDef.Description.Id, true);
        __instance.registerComponentsForVFX();
      }catch(Exception e) {
        Log.Debug?.Write(e.ToString()+"\n");
      }
    }
  }
  //[HarmonyPatch(typeof(Vehicle))]
  //[HarmonyPatch("InitGameRep")]
  //[HarmonyPatch(MethodType.Normal)]
  //[HarmonyPatch(new Type[] { typeof(Transform) })]
  //public static class Vehicle_InitGameRep {
  //  public static void Postfix(Vehicle __instance) {
  //    try {
  //      Log.Debug?.Write("Vehicle.InitGameRep " + (__instance != null ? "not null" : "null") + "\n");
  //      __instance.registerComponentsForVFX();
  //    }catch(Exception e) {
  //      Log.Debug?.Write(e.ToString() + "\n");
  //    }
  //  }
  //}
  [HarmonyPatch(typeof(Turret))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Transform) })]
  public static class Turret_InitGameRep {
    public static void Postfix(Turret __instance) {
      try {
        __instance.registerComponentsForVFX();
      } catch (Exception e) {
        Log.Debug?.Write(e.ToString() + "\n");
      }
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("HandleDeath")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(string) })]
  public static class AbstractActor_HandleDeath {
    public static void Postfix(AbstractActor __instance) {
      if (__instance.IsDead) {
        foreach(MechComponent component in __instance.allComponents) {
          VFXObjects vfxObjects = component.VFXObjects();
          if (vfxObjects != null) { vfxObjects.CleanOnDead(); };
          component.UpdateAuras();
        }
        __instance.RemoveAllAuras();
      }
    }
  }
  [HarmonyPatch(typeof(PilotableActorRepresentation))]
  [HarmonyPatch("OnPlayerVisibilityChanged")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(VisibilityLevel) })]
  public static class PilotableActorRepresentation_OnPlayerVisibilityChanged {
    public static void Postfix(PilotableActorRepresentation __instance, VisibilityLevel newLevel) {
      try {
        Log.Debug?.Write("PilotableActorRepresentation.OnPlayerVisibilityChanged " + __instance.parentCombatant.DisplayName + " " + newLevel + "\n");
        if (__instance.parentActor == null) {
          Log.WriteCritical(" are you fucking seriously?? parentActor is null\n");
          return;
        }
        foreach (MechComponent component in __instance.parentActor.allComponents) {
          try {
            ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null) { continue; };
            if ((activatable.activateVFXOutOfLOSHide == false) && (activatable.presistantVFXOutOfLOSHide == false)) { continue; }
            if (activatable.activateVFXOutOfLOSHide == true) {
              ObjectSpawnDataSelf activeVFX = component.ActivateVFX();
              if (activeVFX != null) {
                if (newLevel != VisibilityLevel.LOSFull) { activeVFX.Hide(); } else { activeVFX.Show(); }
              }
            }
            if (activatable.presistantVFXOutOfLOSHide == true) {
              ObjectSpawnDataSelf presistantVFX = component.PresitantVFX();
              if (presistantVFX != null) {
                if (newLevel != VisibilityLevel.LOSFull) { presistantVFX.Hide(); } else { presistantVFX.Show(); }
              }
            }
          } catch (Exception e) {
            Log.WriteCritical(e.ToString() + "\n");
          }
        }
      }catch(Exception e) {
        Log.WriteCritical(e.ToString() + "\n");
      }
    }
  }
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("DamageComponent")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo),typeof(ComponentDamageLevel),typeof(bool) })]
  public static class MechComponent_DamageComponent {
    public static void Postfix(MechComponent __instance, WeaponHitInfo hitInfo, ComponentDamageLevel damageLevel, bool applyEffects) {
      try {
        Log.Debug?.TWL(0, "MechComponent.DamageComponent Postfix " + __instance.defId + " DamageLevel:" + __instance.DamageLevel + "/" + damageLevel);
        if ((__instance.DamageLevel >= ComponentDamageLevel.Destroyed) || (damageLevel >= ComponentDamageLevel.Destroyed)) {
          Statistic isCAEDestroyed = __instance.StatCollection.GetStatistic("CAEDestroyed");
          if (isCAEDestroyed == null) { isCAEDestroyed = __instance.StatCollection.AddStatistic("CAEDestroyed", false); }
          if (isCAEDestroyed.Value<bool>() == false) {
            isCAEDestroyed.SetValue(true);
            Log.Debug?.WL(1, "really destroyed at first time");
            ActivatableComponent activatable = __instance.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null) {
              Log.Debug?.WL(1, "not activatable");
              return;
            }
            ObjectSpawnDataSelf VFX = __instance.PresitantVFX();
            if (VFX != null) { VFX.CleanupSelf(); };
            VFX = __instance.ActivateVFX();
            if (VFX != null) { VFX.CleanupSelf(); };
            VFX = __instance.DestroyedVFX();
            if (VFX != null) { VFX.SpawnSelf(__instance.parent.Combat); };
            if (activatable.ExplodeOnDamage) { __instance.AoEExplodeComponent(); };
            __instance.playDestroySound();
          } else {
            Log.Debug?.WL(1, "not a really destroyed");
          }
          __instance.UpdateAuras();
        }
      } catch (Exception e) {
        Log.Debug?.TWL(0, e.ToString(), true);
      }
    }
  }

  public class VFXObjects {
    public MechComponent parent;
    public ObjectSpawnDataSelf presitantObject;
    public ObjectSpawnDataSelf activateObject;
    public ObjectSpawnDataSelf destroyedObject;
    public ObjectSpawnDataSelf explodeObject;
    public void Clean() {
      if (this.presitantObject != null) { this.presitantObject.CleanupSelf(); }
      if (this.activateObject != null) { this.activateObject.CleanupSelf(); }
      if (this.destroyedObject != null) { this.destroyedObject.CleanupSelf(); }
      if (this.explodeObject != null) { this.explodeObject.CleanupSelf(); }
    }
    public void CleanOnDead() {
      if (this.presitantObject != null) { this.presitantObject.CleanupSelf(); }
      if (this.activateObject != null) { this.activateObject.CleanupSelf(); }
    }
    public VFXObjects(MechComponent p) {
      this.parent = p;
      this.explodeObject = null;
      ActivatableComponent activatable = this.parent.componentDef.GetComponent<ActivatableComponent>();
      if (activatable != null) {
        GameObject parentObject = p.parent.GameRep.gameObject;
        Weapon weapon = p as Weapon;
        if(weapon != null) {
          if(weapon.weaponRep != null) {
            parentObject = weapon.weaponRep.gameObject;
          }
        }
        Log.Debug?.Write(p.defId + " is activatable \n");
        //PilotableActorRepresentation rep = parent.parent.GameRep as PilotableActorRepresentation;
        if (activatable.presistantVFX.isInited) {
          try {
            Log.Debug?.Write(p.defId + " spawning " + activatable.presistantVFX.VFXPrefab + " \n");
            presitantObject = new ObjectSpawnDataSelf(activatable.presistantVFX.VFXPrefab, parentObject,
              new Vector3(activatable.presistantVFX.VFXOffsetX, activatable.presistantVFX.VFXOffsetY, activatable.presistantVFX.VFXOffsetZ),
              new Vector3(activatable.presistantVFX.VFXScaleX, activatable.presistantVFX.VFXScaleY, activatable.presistantVFX.VFXScaleZ), true, false);
            presitantObject.SpawnSelf(parent.parent.Combat);
            //if(rep != null) {
            //  if (activatable.presistantVFXOutOfLOSHide == true) {
            //    if (rep.VisibleToPlayer == false) { presitantObject.Hide(); }
            //  }
            //}
          } catch (Exception e) {
            Log.Debug?.Write(" Fail to spawn vfx " + e.ToString() + "\n");
          }
        } else {
          presitantObject = null;
        }
        if (activatable.activateVFX.isInited) {
          Log.Debug?.Write(p.defId + " spawning " + activatable.activateVFX.VFXPrefab + " \n");
          activateObject = new ObjectSpawnDataSelf(activatable.activateVFX.VFXPrefab, parentObject,
            new Vector3(activatable.activateVFX.VFXOffsetX, activatable.activateVFX.VFXOffsetY, activatable.activateVFX.VFXOffsetZ),
            new Vector3(activatable.activateVFX.VFXScaleX, activatable.activateVFX.VFXScaleY, activatable.activateVFX.VFXScaleZ), true, false);
        } else {
          activateObject = null;
        }
        if (activatable.destroyedVFX.isInited) {
          Log.Debug?.Write(p.defId + " spawning " + activatable.destroyedVFX.VFXPrefab + " \n");
          destroyedObject = new ObjectSpawnDataSelf(activatable.destroyedVFX.VFXPrefab, parentObject,
            new Vector3(activatable.destroyedVFX.VFXOffsetX, activatable.destroyedVFX.VFXOffsetY, activatable.destroyedVFX.VFXOffsetZ),
            new Vector3(activatable.destroyedVFX.VFXScaleX, activatable.destroyedVFX.VFXScaleY, activatable.destroyedVFX.VFXScaleZ), true, false);
        } else {
          destroyedObject = null;
        }
      }
    }
  }

  public static class ComponentVFXHelper {
    public static Dictionary<string, VFXObjects> componentsVFXObjects = new Dictionary<string, VFXObjects>();
    public static void playActivateSound(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable != null) { activatable.playActivateSound(component.parent.GameRep.audioObject); }
    }
    public static void playDeactivateSound(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable != null) { activatable.playDeactivateSound(component.parent.GameRep.audioObject); }
    }
    public static void playDestroySound(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable != null) { activatable.playDestroySound(component.parent.GameRep.audioObject); }
    }
    public static void registerComponentsForVFX(this AbstractActor unit) {
      Log.Debug?.Write("registerComponentsForVFX "+unit.DisplayName+":"+unit.GUID+"\n");
      foreach (MechComponent component in unit.allComponents) {
        string wGUID;
        if (component.StatCollection.ContainsStatistic(CustomAmmoCategories.GUIDStatisticName) == false) {
          wGUID = Guid.NewGuid().ToString();
          component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
        } else {
          wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
        }
        Log.Debug?.Write(" " + component.defId + ":"+wGUID+"\n");
        ComponentVFXHelper.componentsVFXObjects[wGUID] = new VFXObjects(component);
      }
    }
    public static ObjectSpawnDataSelf ActivateVFX(this MechComponent component) {
      string wGUID;
      if (component.StatCollection.ContainsStatistic(CustomAmmoCategories.GUIDStatisticName) == false) {
        wGUID = Guid.NewGuid().ToString();
        component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
      } else {
        wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
      }
      Log.Debug?.Write("ActivateVFX(" + component.defId + ":" + wGUID + ")\n");
      if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(wGUID)) {
        return ComponentVFXHelper.componentsVFXObjects[wGUID].activateObject;
      }
      return null;
    }
    public static ObjectSpawnDataSelf PresitantVFX(this MechComponent component) {
      string wGUID;
      if (component.StatCollection.ContainsStatistic(CustomAmmoCategories.GUIDStatisticName) == false) {
        wGUID = Guid.NewGuid().ToString();
        component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
      } else {
        wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
      }
      Log.Debug?.Write("PresitantVFX(" + component.defId + ":" + wGUID + ")\n");
      if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(wGUID)) {
        return ComponentVFXHelper.componentsVFXObjects[wGUID].presitantObject;
      }
      return null;
    }
    public static ObjectSpawnDataSelf DestroyedVFX(this MechComponent component) {
      string wGUID;
      if (component.StatCollection.ContainsStatistic(CustomAmmoCategories.GUIDStatisticName) == false) {
        wGUID = Guid.NewGuid().ToString();
        component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
      } else {
        wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
      }
      Log.Debug?.Write("DestroyedVFX(" + component.defId + ":" + wGUID + ")\n");
      if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(wGUID)) {
        return ComponentVFXHelper.componentsVFXObjects[wGUID].destroyedObject;
      }
      return null;
    }
    public static VFXObjects VFXObjects(this MechComponent component) {
      string wGUID;
      if (component.StatCollection.ContainsStatistic(CustomAmmoCategories.GUIDStatisticName) == false) {
        wGUID = Guid.NewGuid().ToString();
        component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
      } else {
        wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
      }
      if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(wGUID)) {
        return ComponentVFXHelper.componentsVFXObjects[wGUID];
      }
      return null;
    }
  }

  public class VFXInfo {
    public string VFXPrefab { get; set; }
    public float VFXScaleX { get; set; }
    public float VFXScaleY { get; set; }
    public float VFXScaleZ { get; set; }
    public float VFXOffsetX { get; set; }
    public float VFXOffsetY { get; set; }
    public float VFXOffsetZ { get; set; }
    public bool isInited { get { return string.IsNullOrEmpty(this.VFXPrefab) == false; } }
    public VFXInfo() {
      VFXPrefab = string.Empty;
      VFXScaleX = 1f;
      VFXScaleY = 1f;
      VFXScaleZ = 1f;
      VFXOffsetX = 0f;
      VFXOffsetY = 0f;
      VFXOffsetZ = 0f;
    }
  }
  [CustomComponent("VFXComponent")]
  public class VFXComponent : SimpleCustomComponent {
    public VFXInfo StaticVFX { get; set; }
    public VFXInfo ActiveVFX { get; set; }

  }
  public class ObjectAncor : MonoBehaviour {
    public Transform ancor { get; set; }
    private Vector3 offset { get; set; }
    public void Awake() {
      offset = Vector3.zero;
      ancor = null;
    }
    public void Update() {
      if (ancor != null) { this.transform.position = ancor.position + offset; }
    }
  }
  public class ObjectSpawnDataSelf : ObjectSpawnData {
    public bool keepPrefabRotation;
    public Vector3 scale;
    public string prefabStringName;
    public CombatGameState Combat;
    public GameObject parentObject;
    public Vector3 localPos;
    public void Hide() {
      if (this.spawnedObject != null) {
        Log.Debug?.Write("ObjectSpawnDataSelf.Hide "+this.spawnedObject.name+"\n");
        this.spawnedObject.SetActive(false);
      }
    }
    public void Show() {
      if (this.spawnedObject != null) {
        Log.Debug?.Write("ObjectSpawnDataSelf.Show " + this.spawnedObject.name + "\n");
        this.spawnedObject.SetActive(true);
      }
    }
    public ObjectSpawnDataSelf(string prefabName,GameObject parent, Vector3 localPosition, Vector3 scale, bool playFX, bool autoPoolObject) :
      base(prefabName, Vector3.zero, Quaternion.identity, playFX, autoPoolObject) {
      keepPrefabRotation = false;
      this.scale = scale;
      this.Combat = null;
      this.parentObject = parent;
      this.localPos = localPosition;
    }
    public void CleanupSelf() {
      if (this == null) {
        Log.WriteCritical("Cleaning null?!!!\n");
        return;
      }
      Log.Debug?.Write("Cleaning up " + this.prefabName + "\n");
      if (Combat == null) {
        Log.WriteCritical("Trying cleanup object " + this.prefabName + " never spawned\n");
        return;
      }
      if (this.spawnedObject == null) {
        Log.WriteCritical("Trying cleanup object " + this.prefabName + " already cleaned\n");
        return;
      }
      try {
        //this.spawnedObject.SetActive(false);
        //this.Combat.DataManager.PoolGameObject(this.prefabName, this.spawnedObject);
        //ParticleSystem component = this.spawnedObject.GetComponent<ParticleSystem>();
        //if ((UnityEngine.Object)component != (UnityEngine.Object)null) {
        //component.Stop(true);
        //}
        GameObject.Destroy(this.spawnedObject);
      } catch (Exception e) {
        Log.WriteCritical("Cleanup exception: " + e.ToString() + "\n");
        Log.WriteCritical("nulling spawned object directly\n");
        this.spawnedObject = null;
      }
      this.spawnedObject = null;
      Log.Debug?.Write("Finish cleaning " + this.prefabName + "\n");
    }
    public void SpawnSelf(CombatGameState Combat) {
      this.Combat = Combat;
      GameObject gameObject = Combat.DataManager.PooledInstantiate(this.prefabName, BattleTechResourceType.Prefab, new Vector3?(), new Quaternion?(), (Transform)null);
      if ((UnityEngine.Object)gameObject == (UnityEngine.Object)null) {
        Log.Debug?.Write("Can't find " + prefabName + " in in-game prefabs\n");
        if (CACMain.Core.AdditinalFXObjects.ContainsKey(prefabName)) {
          Log.Debug?.Write("Found in additional prefabs\n");
          gameObject = GameObject.Instantiate(CACMain.Core.AdditinalFXObjects[prefabName]);
        } else {
          Log.WriteCritical(" can't spawn prefab " + this.prefabName + " it is absent in pool,in-game assets and external assets\n");
          return;
        }
      }
      Log.Debug?.Write("SpawnSelf: "+ this.prefabName+"\n");
      Component[] components = gameObject.GetComponentsInChildren<Component>();
      foreach(Component component in components) {
        if (component == null) { continue; };
        Log.Debug?.Write(" " + component.name + ":"+component.GetType().ToString()+"\n");
        ParticleSystem ps = component as ParticleSystem;
        if(ps != null) {
          var main = ps.main;
          main.scalingMode = ParticleSystemScalingMode.Hierarchy;
          Log.Debug?.Write("  " + ps.main.scalingMode.ToString() + "\n");
        }
      }
      //gameObject.transform.SetParent(this.parentObject.transform);
      gameObject.transform.position = parentObject.transform.position;
      ObjectAncor ancor = gameObject.AddComponent<ObjectAncor>();
      ancor.ancor = parentObject.transform;
      gameObject.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
      if (!this.keepPrefabRotation)
        gameObject.transform.rotation = this.worldRotation;
      if (this.playFX) {
        ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
        if (component != null) {
          //component.transform.localScale.Set(scale.x, scale.y, scale.z);
          gameObject.SetActive(true);
          component.Stop(true);
          component.Clear(true);
          //component.transform.transform.SetParent(this.parentObject.transform);
          //component.transform.localPosition = this.localPos;
          if (!this.keepPrefabRotation)
            component.transform.rotation = this.worldRotation;
          BTCustomRenderer.SetVFXMultiplier(component);
          component.Play(true);
        }
      }
      this.spawnedObject = gameObject;
    }
  }
}
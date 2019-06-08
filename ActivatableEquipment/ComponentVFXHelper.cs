using BattleTech;
using BattleTech.Rendering;
using CustAmmoCategories;
using CustomComponents;
using Harmony;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Transform) })]
  public static class Mech_InitGameRep {
    public static void Postfix(Mech __instance, Transform parentTransform) {
      __instance.registerComponentsForVFX();
    }
  }
  [HarmonyPatch(typeof(MechComponent))]
  [HarmonyPatch("DamageComponent")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(WeaponHitInfo),typeof(ComponentDamageLevel),typeof(bool) })]
  public static class MechComponent_DamageComponent {
    public static bool Prefix(MechComponent __instance, WeaponHitInfo hitInfo, ComponentDamageLevel damageLevel, bool applyEffects, ref bool __state) {
      __state = false;
      Log.LogWrite("MechComponent.DamageComponent "+ __instance.DamageLevel+"->"+ damageLevel+"\n");
      if (__instance.DamageLevel < ComponentDamageLevel.Destroyed) {
        if(damageLevel >= ComponentDamageLevel.Destroyed) {
          Log.LogWrite(" destroyed\n");
          __state = true;
        }
      }
      return false;
    }
    public static void Postfix(MechComponent __instance, WeaponHitInfo hitInfo, ComponentDamageLevel damageLevel, bool applyEffects, ref bool __state) {
      if (__state) {
        ActivatableComponent activatable = __instance.componentDef.GetComponent<ActivatableComponent>();
        if(activatable == null) {
          Log.LogWrite(" not activatable\n");
          return;
        }
        ObjectSpawnDataSelf VFX = __instance.PresitantVFX();
        if (VFX != null) { VFX.CleanupSelf(); };
        VFX = __instance.ActivateVFX();
        if (VFX != null) { VFX.CleanupSelf(); };
        VFX = __instance.DestroyedVFX();
        if (VFX != null) { VFX.SpawnSelf(__instance.parent.Combat); };
        if (activatable.ExplodeOnDamage) { __instance.AoEExplodeComponent(); };
      } else {
        Log.LogWrite(" no additional processing\n");
      }
    }
  }

  public class VFXObjects {
    public MechComponent parent;
    public ObjectSpawnDataSelf presitantObject;
    public ObjectSpawnDataSelf activateObject;
    public ObjectSpawnDataSelf destroyedObject;
    public void Clean() {
      if (this.presitantObject != null) { this.presitantObject.CleanupSelf(); }
      if (this.activateObject != null) { this.activateObject.CleanupSelf(); }
      if (this.destroyedObject != null) { this.destroyedObject.CleanupSelf(); }
    }
    public VFXObjects(MechComponent p) {
      this.parent = p;
      ActivatableComponent activatable = this.parent.componentDef.GetComponent<ActivatableComponent>();
      if (activatable != null) {
        GameObject parentObject = p.parent.GameRep.gameObject;
        Weapon weapon = p as Weapon;
        if(weapon != null) {
          if(weapon.weaponRep != null) {
            parentObject = weapon.weaponRep.gameObject;
          }
        }
        Log.LogWrite(p.defId + " is activatable \n");
        if (activatable.presistantVFX.isInited) {
          try {
            Log.LogWrite(p.defId + " spawning " + activatable.presistantVFX.VFXPrefab + " \n");
            presitantObject = new ObjectSpawnDataSelf(activatable.presistantVFX.VFXPrefab, parentObject,
              new Vector3(activatable.presistantVFX.VFXOffsetX, activatable.presistantVFX.VFXOffsetY, activatable.presistantVFX.VFXOffsetZ),
              new Vector3(activatable.presistantVFX.VFXScaleX, activatable.presistantVFX.VFXScaleY, activatable.presistantVFX.VFXScaleZ), true, false);
            presitantObject.SpawnSelf(parent.parent.Combat);
          } catch (Exception e) {
            Log.LogWrite(" Fail to spawn vfx " + e.ToString() + "\n");
          }
        } else {
          presitantObject = null;
        }
        if (activatable.activateVFX.isInited) {
          Log.LogWrite(p.defId + " spawning " + activatable.activateVFX.VFXPrefab + " \n");
          activateObject = new ObjectSpawnDataSelf(activatable.activateVFX.VFXPrefab, parentObject,
            new Vector3(activatable.activateVFX.VFXOffsetX, activatable.activateVFX.VFXOffsetY, activatable.activateVFX.VFXOffsetZ),
            new Vector3(activatable.activateVFX.VFXScaleX, activatable.activateVFX.VFXScaleY, activatable.activateVFX.VFXScaleZ), true, false);
        } else {
          activateObject = null;
        }
        if (activatable.destroyedVFX.isInited) {
          Log.LogWrite(p.defId + " spawning " + activatable.destroyedVFX.VFXPrefab + " \n");
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
    public static void registerComponentsForVFX(this AbstractActor unit) {
      Log.LogWrite("registerComponentsForVFX "+unit.DisplayName+":"+unit.GUID+"\n");
      foreach (MechComponent component in unit.allComponents) {
        string wGUID;
        if (CustomAmmoCategories.checkExistance(component.StatCollection, CustomAmmoCategories.GUIDStatisticName) == false) {
          wGUID = Guid.NewGuid().ToString();
          component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
        } else {
          wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
        }
        Log.LogWrite(" " + component.defId + ":"+wGUID+"\n");
        ComponentVFXHelper.componentsVFXObjects[wGUID] = new VFXObjects(component);
      }
    }
    public static ObjectSpawnDataSelf ActivateVFX(this MechComponent component) {
      string wGUID;
      if (CustomAmmoCategories.checkExistance(component.StatCollection, CustomAmmoCategories.GUIDStatisticName) == false) {
        wGUID = Guid.NewGuid().ToString();
        component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
      } else {
        wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
      }
      Log.LogWrite("ActivateVFX(" + component.defId + ":" + wGUID + ")\n");
      if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(wGUID)) {
        return ComponentVFXHelper.componentsVFXObjects[wGUID].activateObject;
      }
      return null;
    }
    public static ObjectSpawnDataSelf PresitantVFX(this MechComponent component) {
      string wGUID;
      if (CustomAmmoCategories.checkExistance(component.StatCollection, CustomAmmoCategories.GUIDStatisticName) == false) {
        wGUID = Guid.NewGuid().ToString();
        component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
      } else {
        wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
      }
      Log.LogWrite("PresitantVFX(" + component.defId + ":" + wGUID + ")\n");
      if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(wGUID)) {
        return ComponentVFXHelper.componentsVFXObjects[wGUID].presitantObject;
      }
      return null;
    }
    public static ObjectSpawnDataSelf DestroyedVFX(this MechComponent component) {
      string wGUID;
      if (CustomAmmoCategories.checkExistance(component.StatCollection, CustomAmmoCategories.GUIDStatisticName) == false) {
        wGUID = Guid.NewGuid().ToString();
        component.StatCollection.AddStatistic<string>(CustomAmmoCategories.GUIDStatisticName, wGUID);
      } else {
        wGUID = component.StatCollection.GetStatistic(CustomAmmoCategories.GUIDStatisticName).Value<string>();
      }
      Log.LogWrite("DestroyedVFX(" + component.defId + ":" + wGUID + ")\n");
      if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(wGUID)) {
        return ComponentVFXHelper.componentsVFXObjects[wGUID].destroyedObject;
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
  public class ObjectSpawnDataSelf : ObjectSpawnData {
    public bool keepPrefabRotation;
    public Vector3 scale;
    public string prefabStringName;
    public CombatGameState Combat;
    public GameObject parentObject;
    public Vector3 localPos;
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
        Log.LogWrite("Cleaning null?!!!\n", true);
        return;
      }
      Log.LogWrite("Cleaning up " + this.prefabName + "\n");
      if (Combat == null) {
        Log.LogWrite("Trying cleanup object " + this.prefabName + " never spawned\n", true);
        return;
      }
      if (this.spawnedObject == null) {
        Log.LogWrite("Trying cleanup object " + this.prefabName + " already cleaned\n", true);
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
        Log.LogWrite("Cleanup exception: " + e.ToString() + "\n", true);
        Log.LogWrite("nulling spawned object directly\n", true);
        this.spawnedObject = null;
      }
      this.spawnedObject = null;
      Log.LogWrite("Finish cleaning " + this.prefabName + "\n");
    }
    public void SpawnSelf(CombatGameState Combat) {
      this.Combat = Combat;
      GameObject gameObject = Combat.DataManager.PooledInstantiate(this.prefabName, BattleTechResourceType.Prefab, new Vector3?(), new Quaternion?(), (Transform)null);
      if ((UnityEngine.Object)gameObject == (UnityEngine.Object)null) {
        Log.LogWrite("Can't find " + prefabName + " in in-game prefabs\n");
        if (CACMain.Core.AdditinalFXObjects.ContainsKey(prefabName)) {
          Log.LogWrite("Found in additional prefabs\n");
          gameObject = GameObject.Instantiate(CACMain.Core.AdditinalFXObjects[prefabName]);
        } else {
          Log.LogWrite(" can't spawn prefab " + this.prefabName + " it is absent in pool,in-game assets and external assets\n", true);
          return;
        }
      }
      gameObject.transform.SetParent(this.parentObject.transform);
      gameObject.transform.localPosition = this.localPos;
      gameObject.transform.localScale.Set(scale.x, scale.y, scale.z);
      if (!this.keepPrefabRotation)
        gameObject.transform.rotation = this.worldRotation;
      if (this.playFX) {
        ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
        if (component != null) {
          component.transform.localScale.Set(scale.x, scale.y, scale.z);
          gameObject.SetActive(true);
          component.Stop(true);
          component.Clear(true);
          component.transform.transform.SetParent(this.parentObject.transform);
          component.transform.localPosition = this.localPos;
          if (!this.keepPrefabRotation)
            component.transform.rotation = this.worldRotation;
          BTCustomRenderer.SetVFXMultiplier(component);
          component.Play(true);
        }
      }
      this.spawnedObject = gameObject;
    }
  }
  [HarmonyPatch(typeof(CombatGameState))]
  [HarmonyPatch("OnCombatGameDestroyed")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class CombatGameState_OnCombatGameDestroyedMap {
    public static bool Prefix(CombatGameState __instance) {
      HashSet<string> clearComponents = new HashSet<string>();
      foreach (var avfx in ComponentVFXHelper.componentsVFXObjects) {
        clearComponents.Add(avfx.Key);
      }
      foreach (var compGUID in clearComponents) {
        if (ComponentVFXHelper.componentsVFXObjects.ContainsKey(compGUID)) {
          ComponentVFXHelper.componentsVFXObjects[compGUID].Clean();
        }
      }
      ComponentVFXHelper.componentsVFXObjects.Clear();
      return true;
    }
  }

}
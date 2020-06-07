using BattleTech;
using BattleTech.Rendering;
using BattleTech.Rendering.UrbanWarfare;
using BattleTech.UI;
using CustomActivatablePatches;
using CustomComponents;
using Harmony;
using HBS.Util;
using Localize;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CustomActivatableEquipment {
  [HarmonyPatch(typeof(DestructibleUrbanFlimsy))]
  [HarmonyPatch("OnTriggerEnter")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Collider) })]
  public static class DestructibleUrbanFlimsy_OnTriggerEnter {
    public static bool Prefix(DestructibleUrbanFlimsy __instance, Collider other) {
      int instanceId = other.gameObject.GetInstanceID();
      Log.LogWrite("DestructibleUrbanFlimsy.OnTriggerEnter Prefix " + other.gameObject.name + ":" + instanceId + "\n");
      AuraActorBody body = other.GetComponent<AuraActorBody>();
      if (body != null) {
        Log.LogWrite(" aura body\n");
        return false;
      }
      AuraBubble aura = other.GetComponent<AuraBubble>();
      if (aura != null) {
        Log.LogWrite(" aura bubble\n");
        return false;
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("InitEffectStats")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { })]
  public static class AbstractActor_InitEffectStatsAuras {
    public static void Postfix(AbstractActor __instance) {
      __instance.StatCollection.AddStatistic<bool>(Core.Settings.unaffectedByHeadHitStatName, false);
      foreach (MechComponent component in __instance.allComponents) {
        try {
          if (component == null) { Log.TWL(0, "WARNING!! null component in " + new Text(__instance.DisplayName).ToString(), true); continue; }
          if (component.componentDef == null) { Log.TWL(0, "WARNING!! null componentDef in " + new Text(__instance.DisplayName).ToString(),true); continue; }
          List<AuraDef> adefs = component.componentDef.GetAuras();
          foreach (AuraDef adef in adefs) {
            if (string.IsNullOrEmpty(adef.RangeStatistic) == false) {
              __instance.StatCollection.AddStatistic<float>(adef.RangeStatistic, adef.Range);
            }
          }
        }catch(Exception e) {
          Log.TWL(0, e.ToString(), true);
        }
      }
      if (string.IsNullOrEmpty(Core.Settings.sensorsAura.RangeStatistic) == false) {
        __instance.StatCollection.AddStatistic<float>(Core.Settings.sensorsAura.RangeStatistic, Core.Settings.sensorsAura.Range);
      }
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("ProcessAddedMark")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(Effect) })]
  public static class AbstractActor_ProcessAddedMark {
    public static void Postfix(AbstractActor __instance) {
      AuraActorBody body = __instance.bodyAura();
      List<AuraBubble> auras = body.affectedAurasEffects.Keys.ToList();
      foreach (AuraBubble aura in auras) {
        if (aura.Def.RemoveOnSensorLock) {
          body.RemoveAuraEffects(aura, false, true);
        }
      }
      body.ReapplyAllEffects();
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("ProcessAddedMark")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(Effect), typeof(Ability) })]
  public static class AbstractActor_ProcessAddedMark2 {
    public static void Postfix(AbstractActor __instance) {
      AuraActorBody body = __instance.bodyAura();
      List<AuraBubble> auras = body.affectedAurasEffects.Keys.ToList();
      foreach (AuraBubble aura in auras) {
        if (aura.Def.RemoveOnSensorLock) {
          body.RemoveAuraEffects(aura, false, true);
        }
      }
      body.ReapplyAllEffects();
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("OnEffectEnd")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPriority(Priority.First)]
  [HarmonyPatch(new Type[] { typeof(Effect) })]
  public static class AbstractActor_OnEffectEnd {
    public static void Postfix(AbstractActor __instance) {
      if (__instance.IsSensorLocked) { return; }
      AuraActorBody body = __instance.bodyAura();
      Collider[] colliders = Physics.OverlapSphere(body.transform.position, body.collider.radius);
      foreach (Collider collider in colliders) { body.OnTriggerEnter(collider); }
    }
  }
  public static partial class CAEAuraHelper {
    //private static Dictionary<CombatAuraReticle, AuraBubble> mainSensorsBubbles = new Dictionary<CombatAuraReticle, AuraBubble>();
    private static Dictionary<AbstractActor, AuraBubble> actorsSensorsBubbles = new Dictionary<AbstractActor, AuraBubble>();
    private static Dictionary<CombatAuraReticle, AuraBubble> reticleAuraBubbles = new Dictionary<CombatAuraReticle, AuraBubble>();
    private static Dictionary<AbstractActor, List<AuraBubble>> actorAuraBubbles = new Dictionary<AbstractActor, List<AuraBubble>>();
    private static Dictionary<MechComponent, List<AuraBubble>> componentAuraBubbles = new Dictionary<MechComponent, List<AuraBubble>>();
    private static Dictionary<MechComponentDef, List<AuraDef>> aurasDefinitions = new Dictionary<MechComponentDef, List<AuraDef>>();
    private static Dictionary<AbstractActor, AuraActorBody> actorAuraBodies = new Dictionary<AbstractActor, AuraActorBody>();
    private static List<AuraBubble> allAuras = new List<AuraBubble>();
    private static Dictionary<AuraActorBody, HashSet<AuraBubble>> movingAddFloaties = new Dictionary<AuraActorBody, HashSet<AuraBubble>>();
    private static Dictionary<AuraActorBody, HashSet<AuraBubble>> movingRemoveFloaties = new Dictionary<AuraActorBody, HashSet<AuraBubble>>();
    private static HashSet<AbstractActor> skipStealthChange = new HashSet<AbstractActor>();
    private static FieldInfo AbstractActorStealthPipsPrevious = null;
    public static void UpdateAurasWithSensors(this AbstractActor unit) {
      AuraBubble sensors = unit.sensorAura();
      if (sensors != null) { sensors.UpdateRadius(); }
      foreach(MechComponent component in unit.allComponents) {
        component.UpdateAuras();
      }
    }
    public static bool IsStealthFloatieSkip(this AbstractActor actor) {
      return skipStealthChange.Contains(actor);
    }
    public static void AddStealthFloatieSkip(this AbstractActor actor) {
      skipStealthChange.Add(actor);
    }
    public static void DelStealthFloatieSkip(this AbstractActor actor) {
      skipStealthChange.Remove(actor);
    }
    public static void CleanStealthFloatieSkip(this AbstractActor actor) {
      skipStealthChange.Clear();
    }
    public static int StealthPipsPrevious(this AbstractActor actor) {
      if (AbstractActorStealthPipsPrevious == null) { AbstractActorStealthPipsPrevious = typeof(AbstractActor).GetField("StealthPipsPrevious", BindingFlags.Instance | BindingFlags.NonPublic); };
      return (int)AbstractActorStealthPipsPrevious.GetValue(actor);
    }
    public static void StealthPipsPrevious(this AbstractActor actor, int val) {
      if (AbstractActorStealthPipsPrevious == null) { AbstractActorStealthPipsPrevious = typeof(AbstractActor).GetField("StealthPipsPrevious", BindingFlags.Instance | BindingFlags.NonPublic); };
      AbstractActorStealthPipsPrevious.SetValue(actor, val);
    }
    public static void FlushMovingAuraFloaties() {
      foreach (var body in movingAddFloaties) {
        foreach (var aura in body.Value) {
          body.Key.ShowAddFloatie(aura);
        }
      }
      movingAddFloaties.Clear();
      foreach (var body in movingRemoveFloaties) {
        foreach (var aura in body.Value) {
          body.Key.ShowDelFloatie(aura);
        }
      }
      movingRemoveFloaties.Clear();
    }
    public static void MovingAddFloatie(this AuraActorBody body, AuraBubble aura) {
      if (movingAddFloaties.ContainsKey(body) == false) { movingAddFloaties.Add(body, new HashSet<AuraBubble>()); }
      if (movingRemoveFloaties.ContainsKey(body) == false) { movingRemoveFloaties.Add(body, new HashSet<AuraBubble>()); }
      if (movingAddFloaties[body].Contains(aura) == false) { movingAddFloaties[body].Add(aura); };
      if (movingRemoveFloaties[body].Contains(aura)) { movingRemoveFloaties[body].Remove(aura); };
    }
    public static void MovingRemoveFloatie(this AuraActorBody body, AuraBubble aura) {
      if (movingAddFloaties.ContainsKey(body) == false) { movingAddFloaties.Add(body, new HashSet<AuraBubble>()); }
      if (movingRemoveFloaties.ContainsKey(body) == false) { movingRemoveFloaties.Add(body, new HashSet<AuraBubble>()); }
      if (movingAddFloaties[body].Contains(aura)) { movingAddFloaties[body].Remove(aura); };
      if (movingRemoveFloaties[body].Contains(aura) == false) { movingRemoveFloaties[body].Add(aura); };
    }
    public static void RemoveAllAuras(this AbstractActor unit) {
      if (actorAuraBodies.ContainsKey(unit) == false) { return; }
      AuraActorBody body = actorAuraBodies[unit];
      List<AuraBubble> auras = body.affectedAurasEffects.Keys.ToList();
      foreach (AuraBubble aura in auras) {
        body.RemoveAuraEffects(aura, false, true);
      }
      body.ReapplyAllEffects();
    }
    public static void ClearBubbles() {
      //mainSensorsBubbles.Clear();
      reticleAuraBubbles.Clear();
      componentAuraBubbles.Clear();
      List<GameObject> objToDestroy = new List<GameObject>();
      foreach (AuraBubble aura in allAuras) {
        if (aura == null) { continue; }
        aura.affectedTo.Clear();
        try { objToDestroy.Add(aura.gameObject); } finally { };
        foreach (var bVFX in aura.bubblesVFX) {
          if (bVFX.Key == null) { continue; }
          try { objToDestroy.Add(bVFX.Key.gameObject); } finally { };
        }
        try { objToDestroy.Add(aura.reticle.gameObject); } finally { };
        aura.bubblesVFX.Clear();
      }
      foreach (var body in actorAuraBodies) {
        if (body.Value == null) { continue; }
        body.Value.affectedAurasEffects.Clear();
        if (body.Value == null) { continue; }
        if (body.Value.gameObject == null) { continue; }
        objToDestroy.Add(body.Value.gameObject);
      }
      actorAuraBodies.Clear();
      allAuras.Clear();
      actorsSensorsBubbles.Clear();
      foreach (GameObject obj in objToDestroy) {
        GameObject.Destroy(obj);
      }
      objToDestroy.Clear();
    }
    public static AuraBubble sensorAura(this AbstractActor unit) {
      if (actorsSensorsBubbles.ContainsKey(unit) == false) { return null; };
      return actorsSensorsBubbles[unit];
    }
    public static AuraActorBody bodyAura(this AbstractActor unit) {
      if (actorAuraBodies.ContainsKey(unit) == false) { return null; };
      return actorAuraBodies[unit];
    }
    public static List<AuraBubble> actorAuras(this AbstractActor unit) {
      if (actorAuraBubbles.ContainsKey(unit) == false) { return new List<AuraBubble>(); };
      return actorAuraBubbles[unit];
    }
    public static void RegisterAuraBody(this AbstractActor unit, AuraActorBody body) {
      if (actorAuraBodies.ContainsKey(unit) == false) { actorAuraBodies.Add(unit, body); } else {
        actorAuraBodies[unit] = body;
      }
    }
    public static void UpdateAuras(this MechComponent component, bool now = false) {
      if (componentAuraBubbles.ContainsKey(component) == false) { return; }
      List<AuraBubble> auras = componentAuraBubbles[component];
      foreach (AuraBubble aura in auras) {
        aura.UpdateRadius(now);
      }
    }
    public static void UpdateAuras(this AbstractActor unit, bool now = false) {
      if (actorAuraBubbles.ContainsKey(unit) == false) { return; }
      List<AuraBubble> auras = actorAuraBubbles[unit];
      foreach (AuraBubble aura in auras) {
        aura.UpdateRadius(now);
      }
    }
    public static void InitBodyBubble(this AbstractActor unit) {
      try {
        GameObject body = new GameObject();
        body.SetActive(false);
        body.transform.localScale = new Vector3(1f, 1f, 1f);
        body.gameObject.transform.parent = unit.GameRep.gameObject.transform;
        body.gameObject.transform.localPosition = new Vector3(0, 0, 0);
        //body.GetComponent<Renderer>().material.color = Color.black;
        body.gameObject.AddComponent<Rigidbody>();
        body.gameObject.GetComponent<Rigidbody>().name = "RBODY:" + unit.DisplayName + ":" + unit.GUID;
        body.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        body.gameObject.GetComponent<Rigidbody>().useGravity = false;
        body.gameObject.layer = LayerMask.NameToLayer("Combatant");
        body.gameObject.AddComponent<SphereCollider>();
        body.gameObject.GetComponent<SphereCollider>().enabled = false;
        body.gameObject.GetComponent<SphereCollider>().isTrigger = true;
        body.gameObject.GetComponent<SphereCollider>().radius = 1f;
        body.gameObject.GetComponent<SphereCollider>().name = "SBODY:" + unit.DisplayName + ":" + unit.GUID;
        body.gameObject.GetComponent<SphereCollider>().enabled = true;
        body.gameObject.AddComponent<AuraActorBody>();
        body.gameObject.GetComponent<AuraActorBody>().owner = unit;
        body.gameObject.GetComponent<AuraActorBody>().StealthPipsPrev = 0;
        body.gameObject.GetComponent<AuraActorBody>().collider = body.gameObject.GetComponent<SphereCollider>();
        body.SetActive(true);
        unit.RegisterAuraBody(body.gameObject.GetComponent<AuraActorBody>());
      }catch(Exception e) {
        Log.LogWrite(e.ToString() + "\n",true);
      }
    }
    public static void UnregisterAura(this AuraBubble aura) {
      foreach (var body in actorAuraBodies) {
        if (body.Value.affectedAurasEffects.ContainsKey(aura)) {
          body.Value.RemoveAuraEffects(aura, true, true);
        }
      }
      aura.affectedTo.Clear();
      aura.owner.actorAuras().Remove(aura);
      if (aura.owner != null) {
        if (actorsSensorsBubbles.ContainsKey(aura.owner)) {
          if (actorsSensorsBubbles[aura.owner] == aura) {
            actorsSensorsBubbles.Remove(aura.owner);
          }
        }
      }
      if (aura.reticle != null) {
        if (reticleAuraBubbles.ContainsKey(aura.reticle)) {
          if (reticleAuraBubbles[aura.reticle] == aura) {
            reticleAuraBubbles.Remove(aura.reticle);
          }
        }
        //if (mainSensorsBubbles.ContainsKey(aura.reticle)) {
        //  if (mainSensorsBubbles[aura.reticle] == aura) {
        //    mainSensorsBubbles.Remove(aura.reticle);
        //  }
        //}
      }
      if (aura.source != null) {
        if (componentAuraBubbles.ContainsKey(aura.source)) {
          componentAuraBubbles[aura.source].Remove(aura);
        }
      }
      allAuras.Remove(aura);
      GameObject go = null;
      try { go = aura.gameObject; } finally { };
      GameObject.Destroy(aura);
      if (go != null) { GameObject.Destroy(go); };
    }
    public static void RegisterAuraBubble(this AbstractActor unit, MechComponent source, CombatAuraReticle reticle, AuraBubble aura) {
      if (actorAuraBubbles.ContainsKey(unit) == false) { actorAuraBubbles.Add(unit, new List<AuraBubble>()); };
      actorAuraBubbles[unit].Add(aura);
      if (source != null) {
        if (componentAuraBubbles.ContainsKey(source) == false) { componentAuraBubbles.Add(source, new List<AuraBubble>()); };
        componentAuraBubbles[source].Add(aura);
        if (reticleAuraBubbles.ContainsKey(reticle)) { reticleAuraBubbles[reticle].UnregisterAura(); }
        reticleAuraBubbles.Add(reticle, aura);
      } else {
        if (reticleAuraBubbles.ContainsKey(reticle)) { reticleAuraBubbles[reticle].UnregisterAura(); }
        if (actorsSensorsBubbles.ContainsKey(unit)) { actorsSensorsBubbles[unit].UnregisterAura(); }
        //mainSensorsBubbles.Add(reticle, aura);
        actorsSensorsBubbles.Add(unit, aura);
        reticleAuraBubbles.Add(reticle, aura);
      }
      allAuras.Add(aura);
    }
    public static void AddAuras(this MechComponentDef def, List<AuraDef> auras) {
      if (aurasDefinitions.ContainsKey(def) == false) { aurasDefinitions.Add(def, new List<AuraDef>()); }
      aurasDefinitions[def].AddRange(auras);
    }
    public static List<AuraDef> GetAuras(this MechComponentDef def) {
      if (aurasDefinitions.ContainsKey(def) == false) { aurasDefinitions.Add(def, new List<AuraDef>()); }
      return aurasDefinitions[def];
    }
    //public static bool isMainSensors(this CombatAuraReticle reticle) {
    //  return mainSensorsBubbles.ContainsKey(reticle);
    //}
    //public static AuraBubble MainSensors(this CombatAuraReticle reticle) {
    //  if (mainSensorsBubbles.ContainsKey(reticle) == false) { return null; }
    //  return mainSensorsBubbles[reticle];
    //}
    public static AuraBubble AuraBubble(this CombatAuraReticle reticle) {
      if (reticleAuraBubbles.ContainsKey(reticle) == false) { return null; }
      return reticleAuraBubbles[reticle];
    }
  }

  public class AuraChangeRequest {
    public bool isAdd { get; set; }
    public AuraBubble aura { get; set; }
    public AuraChangeRequest(bool isAdd, AuraBubble aura) {
      this.isAdd = isAdd;
      this.aura = aura;
    }
  }
  public class AuraActorBody : MonoBehaviour {
    public AbstractActor owner { get; set; }
    public SphereCollider collider { get; set; }
    public int StealthPipsPrev { get; set; }
    //public Dictionary<string, int> aurasVFXes { get; set; }
    public Dictionary<AuraBubble, HashSet<string>> affectedAurasEffects { get; set; }
    //public Queue<AuraChangeRequest> changeQueue;
    public Dictionary<AuraBubble,bool> aurasToChange;
    public void ShowAddFloatie(AuraBubble aura) {
      bool isAlly = false;
      AuraDef def = aura.Def;
      AbstractActor auraOwner = aura.owner;
      if (owner.GUID == auraOwner.GUID) { isAlly = true; } else
        if (owner.TeamId == auraOwner.TeamId) { isAlly = true; } else {
        if (owner.team == null) {
          Log.TWL(0, "!!!WARNING!!! "+new Text(owner.DisplayName).ToString() + " have no team. Fix this!!", true);
        } else {
          if (auraOwner.team == null) {
            Log.TWL(0, "!!!WARNING!!! " + new Text(auraOwner.DisplayName).ToString() + " have no team. Fix this!!", true);
          } else {
            try { isAlly = owner.team.IsFriendly(auraOwner.team); } catch (Exception e) { Log.TWL(0, e.ToString(), true); };
          }
        }
      }
      this.ShowAddFloatie(aura, isAlly);
    }
    public void ShowAddFloatie(AuraBubble aura, bool isAlly) {
      FloatieMessage.MessageNature nature = FloatieMessage.MessageNature.NotSet;
      if (owner.Combat == null) { return; }
      if (owner.Combat.MessageCenter == null) { return; }
      string action = "";
      if (isAlly && aura.Def.IsPositiveToAlly) { action = "PROTECTED"; nature = FloatieMessage.MessageNature.Buff; } else if ((isAlly == false) && aura.Def.IsNegativeToEnemy) { action = "AFFECTED"; nature = FloatieMessage.MessageNature.Debuff; } else if ((isAlly == false) && aura.Def.IsPositiveToEnemy) { action = "PROTECTED"; nature = FloatieMessage.MessageNature.Buff; } else if ((isAlly == true) && aura.Def.IsNegativeToAlly) { action = "AFFECTED"; nature = FloatieMessage.MessageNature.Debuff; }
      if (nature != FloatieMessage.MessageNature.NotSet) {
        owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(aura.owner.GUID, owner.GUID, new Text("{0} {1}", aura.Def.Name, action), nature));
      }
    }
    public void ShowDelFloatie(AuraBubble aura) {
      try {
        bool isAlly = false;
        AuraDef def = aura.Def;
        AbstractActor auraOwner = aura.owner;
        if (owner.GUID == auraOwner.GUID) { isAlly = true; } else
          if (owner.TeamId == auraOwner.TeamId) { isAlly = true; } else {
          if ((owner.team == null)||(auraOwner.team == null)) {
            isAlly = false;
            if (owner.team == null) {
              Log.TWL(0, "!!!WARNING!!! " + new Text(owner.DisplayName).ToString() + "  have no team. Fix this!!", true);
            } else {
              Log.TWL(0, "!!!WARNING!!! " + new Text(auraOwner.DisplayName).ToString() + "  have no team. Fix this!!", true);
            }
          } else {
            try { isAlly = owner.team.IsFriendly(auraOwner.team); } catch (Exception e) { Log.TWL(0, e.ToString(), true); isAlly = false; };
          }
        }
        this.ShowDelFloatie(aura, isAlly);
      } catch (Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
    public void ShowDelFloatie(AuraBubble aura, bool isAlly) {
      try {
        FloatieMessage.MessageNature nature = FloatieMessage.MessageNature.NotSet;
        string action = "";
        if (isAlly && aura.Def.IsPositiveToAlly) { action = "REMOVED"; nature = FloatieMessage.MessageNature.Buff; } else if ((isAlly == false) && aura.Def.IsNegativeToEnemy) { action = "REMOVED"; nature = FloatieMessage.MessageNature.Debuff; } else if ((isAlly == false) && aura.Def.IsPositiveToEnemy) { action = "REMOVED"; nature = FloatieMessage.MessageNature.Buff; } else if ((isAlly == true) && aura.Def.IsNegativeToAlly) { action = "REMOVED"; nature = FloatieMessage.MessageNature.Debuff; }
        if (this.owner.IsSensorLocked && aura.Def.RemoveOnSensorLock) { action = "SUPPRESSED"; }
        if (nature != FloatieMessage.MessageNature.NotSet) {
          if (owner.Combat == null) {
            Log.TWL(0, "This is reall fucking shit. Combatant without Combat inited", true);
          } else {
            if (owner.Combat.MessageCenter == null) {
              owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(aura.owner.GUID, owner.GUID, new Text("{0} {1}", aura.Def.Name, action), nature));
            } else {
              Log.TWL(0, "This is reall fucking shit. Combat without message center", true);
            }
          }
        }
      }catch(Exception e) {
        Log.TWL(0, e.ToString(), true);
      }
    }
    public void ApplyAuraEffects(AuraBubble aura, bool forcedFloatie) {
      try {
        bool isAlly = false;
        AuraDef def = aura.Def;
        AbstractActor auraOwner = aura.owner;
        if (owner.GUID == auraOwner.GUID) { isAlly = true; } else
          if (owner.TeamId == auraOwner.TeamId) { isAlly = true; } else {
          if (owner.team == null) {
            isAlly = false;
            Log.TWL(0, "!!!WARNING!!! " + new Text(owner.DisplayName).ToString() + " have no team. Fix this!!", true);
          } else {
            try { isAlly = owner.team.IsFriendly(auraOwner.team); } catch (Exception e) { Log.TWL(0, e.ToString(), true); };
          }
        }
        if (affectedAurasEffects.ContainsKey(aura) == false) {
          affectedAurasEffects.Add(aura, new HashSet<string>());
          aura.affectedTo.Add(this);
        };
        Log.LogWrite("Apply aura " + aura.Def.Id + " from " + aura.owner.DisplayName + " to " + this.owner.DisplayName + " ally:" + isAlly + "\n");
        foreach (string VFX in aura.Def.targetVFX) {
          //if (aurasVFXes.ContainsKey(VFX) == false) { aurasVFXes.Add(VFX, 0); };
          //if (aurasVFXes[VFX] == 0) {
          owner.GameRep.PlayVFXAt(owner.GameRep.thisTransform, Vector3.zero, VFX, true, Vector3.zero, true, -1f);
          //}
          //aurasVFXes[VFX] += 1;
        }
        foreach (string SFX in aura.Def.targetSFX) {
          int num2 = (int)WwiseManager.PostEvent(SFX, owner.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
        }
        if ((owner.GUID == auraOwner.GUID) && (aura.Def.ApplySelf == false)) { return; };
        for (int i = 0; i < def.statusEffects.Count; ++i) {
          EffectData statusEffect = def.statusEffects[i];
          if (statusEffect.targetingData.effectTriggerType == EffectTriggerType.Passive) {
            if (((isAlly == true) && (statusEffect.targetingData.effectTargetType == EffectTargetType.AlliesWithinRange))
              || ((isAlly == false) && (statusEffect.targetingData.effectTargetType == EffectTargetType.EnemiesWithinRange))) {
              string effectID = string.Format("AuraEffect_{0}_{1}_{2}_{3}_{4}", auraOwner.GUID, owner.GUID, def.Id, statusEffect.Description.Id, i);
              if (affectedAurasEffects[aura].Contains(effectID) == false) {
                List<Effect> createdEffects = owner.Combat.EffectManager.CreateEffect(statusEffect, effectID, -1, (ICombatant)auraOwner, owner, new WeaponHitInfo(), 0, false);
                affectedAurasEffects[aura].Add(effectID);
                Log.LogWrite(" Applied effect:" + effectID + ":" + createdEffects.Count + "\n");
                this.owner.Combat.MessageCenter.PublishMessage(new AuraAddedMessage(aura.owner.GUID, this.owner.GUID, effectID, statusEffect));
              }
            }
          }
        }
        if (this.StealthPipsPrev != owner.StealthPipsCurrent) {
          Log.LogWrite(" StealthChangedMessage(" + owner.GUID + "," + this.StealthPipsPrev + " -> " + owner.StealthPipsCurrent + ")\n");
          owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new StealthChangedMessage(owner.GUID, owner.StealthPipsCurrent));
          this.StealthPipsPrev = owner.StealthPipsCurrent;
        }
        if ((aura.Def.FloatieAtEndOfMove == false) || (forcedFloatie == true) || (owner.Combat.TurnDirector.IsInterleaved == false)) {
          Log.LogWrite(" Floatie add(" + new Text(owner.DisplayName) + " FloatieAtEndOfMove:" + aura.Def.FloatieAtEndOfMove + " forcedFloatie:" + forcedFloatie + " IsInterleaved:" + owner.Combat.TurnDirector.IsInterleaved + "\n");
          this.ShowAddFloatie(aura, isAlly);
        } else {
          Log.LogWrite(" Floatie add cache(" + new Text(owner.DisplayName) + " FloatieAtEndOfMove:" + aura.Def.FloatieAtEndOfMove + " forcedFloatie:" + forcedFloatie + " IsInterleaved:" + owner.Combat.TurnDirector.IsInterleaved + "\n");
          this.MovingAddFloatie(aura);
        }
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
    public void ReapplyAllEffects() {
      try {
        owner.AddStealthFloatieSkip();
        foreach (var aura in affectedAurasEffects) {
          HashSet<string> effectsIds = affectedAurasEffects[aura.Key];
          foreach (string effectId in effectsIds) {
            List<Effect> allEffectsWithId = owner.Combat.EffectManager.GetAllEffectsWithID(effectId);
            for (int t = 0; t < allEffectsWithId.Count; ++t) {
              owner.CancelEffect(allEffectsWithId[t], false);
              owner.StealthPipsPrevious(owner.StealthPipsCurrent);
            }
          }
        }
        foreach (var aura in affectedAurasEffects) {
          bool isAlly = false;
          AuraDef def = aura.Key.Def;
          AbstractActor auraOwner = aura.Key.owner;
          if (owner.GUID == auraOwner.GUID) { isAlly = true; } else
            if (owner.TeamId == auraOwner.TeamId) { isAlly = true; } else {
            if (owner.team == null) {
              Log.TWL(0, "!!!WARNING!!! " + new Text(owner.DisplayName).ToString() + " have no team. Fix this!!", true);
            } else {
              try { isAlly = owner.team.IsFriendly(auraOwner.team); } catch (Exception e) { Log.TWL(0, e.ToString(), true); };
            }
          }
          if ((owner.GUID == auraOwner.GUID) && (def.ApplySelf == false)) { continue; };
          for (int i = 0; i < def.statusEffects.Count; ++i) {
            EffectData statusEffect = def.statusEffects[i];
            if (statusEffect.targetingData.effectTriggerType == EffectTriggerType.Passive) {
              if (((isAlly == true) && (statusEffect.targetingData.effectTargetType == EffectTargetType.AlliesWithinRange))
                || ((isAlly == false) && (statusEffect.targetingData.effectTargetType == EffectTargetType.EnemiesWithinRange))) {
                string effectID = string.Format("AuraEffect_{0}_{1}_{2}_{3}_{4}", auraOwner.GUID, owner.GUID, def.Id, statusEffect.Description.Id, i);
                if (aura.Value.Contains(effectID)) {
                  List<Effect> createdEffects = owner.Combat.EffectManager.CreateEffect(statusEffect, effectID, -1, (ICombatant)auraOwner, owner, new WeaponHitInfo(), 0, false);
                  owner.StealthPipsPrevious(owner.StealthPipsCurrent);
                }
              }
            }
          }
        }
        owner.StealthPipsPrevious(owner.StealthPipsCurrent);
        owner.DelStealthFloatieSkip();
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
    public void RemoveAuraEffects(AuraBubble aura, bool reApplyEffects, bool forcedFloatie) {
      try {
        if (affectedAurasEffects.ContainsKey(aura) == false) { return; }
        //foreach (string VFX in aura.Def.targetVFX) {
        //  if (aurasVFXes.ContainsKey(VFX) == false) { continue; };
        //  if (aurasVFXes[VFX] <= 1) {
        //    owner.GameRep.StopManualPersistentVFX(VFX);
        //    aurasVFXes[VFX] = 0;
        //  } else {
        //    aurasVFXes[VFX] -= 1;
        //  }
        //}
        foreach (string VFX in aura.Def.removeTargetVFX) {
          owner.GameRep.PlayVFXAt(owner.GameRep.thisTransform, Vector3.zero, VFX, true, Vector3.zero, true, -1f);
        }
        foreach (string SFX in aura.Def.removeTargetSFX) {
          int num2 = (int)WwiseManager.PostEvent(SFX, owner.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
        }
        HashSet<string> effectsIds = affectedAurasEffects[aura];
        Log.LogWrite("Remove aura " + aura.Def.Id + " from " + aura.owner.DisplayName + " to " + this.owner.DisplayName + "\n");
        if (effectsIds.Count > 0) {
          foreach (string effectId in effectsIds) {
            List<Effect> allEffectsWithId = owner.Combat.EffectManager.GetAllEffectsWithID(effectId);
            Log.LogWrite(" Removed effect:" + effectId + ":" + allEffectsWithId.Count + "\n");
            for (int t = 0; t < allEffectsWithId.Count; ++t) {
              owner.CancelEffect(allEffectsWithId[t], false);
              this.owner.Combat.MessageCenter.PublishMessage(new AuraRemovedMessage(aura.owner.GUID, this.owner.GUID, effectId, allEffectsWithId[t].EffectData));
            }
          }
          if (this.StealthPipsPrev != owner.StealthPipsCurrent) {
            Log.LogWrite(" StealthChangedMessage(" + owner.GUID + "," + this.StealthPipsPrev + " -> " + owner.StealthPipsCurrent + ")\n");
            owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new StealthChangedMessage(owner.GUID, owner.StealthPipsCurrent));
            this.StealthPipsPrev = owner.StealthPipsCurrent;
          }
          if ((aura.Def.FloatieAtEndOfMove == false) || (forcedFloatie == true) || (owner.Combat.TurnDirector.IsInterleaved == false)) {
            Log.LogWrite(" Floatie del(" + new Text(owner.DisplayName) + " FloatieAtEndOfMove:" + aura.Def.FloatieAtEndOfMove + " forcedFloatie:" + forcedFloatie + " IsInterleaved:" + owner.Combat.TurnDirector.IsInterleaved + "\n");
            this.ShowDelFloatie(aura);
          } else {
            Log.LogWrite(" Floatie del cache(" + new Text(owner.DisplayName) + " FloatieAtEndOfMove:" + aura.Def.FloatieAtEndOfMove + " forcedFloatie:" + forcedFloatie + " IsInterleaved:" + owner.Combat.TurnDirector.IsInterleaved + "\n");
            this.MovingRemoveFloatie(aura);
          }
        }
        affectedAurasEffects.Remove(aura);
        aura.affectedTo.Remove(this);
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
      if (reApplyEffects) { ReapplyAllEffects(); };
    }
    public AuraActorBody() {
      affectedAurasEffects = new Dictionary<AuraBubble, HashSet<string>>();
      aurasToChange = new Dictionary<AuraBubble, bool>();
      //changeQueue = new Queue<AuraChangeRequest>();
      //aurasVFXes = new Dictionary<string, int>();
    }
    void Awake() {
      Log.LogWrite("Body collider awake " + owner.DisplayName + ":" + owner.GUID + "\n");
    }
    public void OnTriggerEnter(Collider other) {
      AuraBubble aura = other.gameObject.GetComponent<AuraBubble>();
      if (aura == null) { return; }
      if (aura.collider.radius < 1.0f) { return; };
      if (owner.IsDead) { return; }
      if (aura.owner.IsDead) { return; }
      if (affectedAurasEffects.ContainsKey(aura)) { return; }
      if (owner.IsSensorLocked && aura.Def.RemoveOnSensorLock) { return; };
      if (aurasToChange.TryGetValue(aura, out bool isAdd)) {
        if (isAdd == true) { return; }
        aurasToChange[aura] = true;
      } else {
        aurasToChange.Add(aura, true);
      }
      //ApplyAuraEffects(aura, false);
      //aura.owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(owner.GUID, owner.GUID,
      //"AURA FROM " + aura.owner.DisplayName + " ADD",
      //FloatieMessage.MessageNature.Buff
      //));
      Log.LogWrite("Collision enter " + owner.DisplayName + ":" + owner.GUID + " -> " + aura.owner.DisplayName + ":" + aura.owner.GUID + " r:" + aura.collider.radius + "/" + Vector3.Distance(owner.CurrentPosition, aura.owner.CurrentPosition) + "\n");
    }
    public void OnTriggerExit(Collider other) {
      AuraBubble aura = other.gameObject.GetComponent<AuraBubble>();
      if (aura == null) { return; }
      if (affectedAurasEffects.ContainsKey(aura) == false) { return; }
      if (aurasToChange.TryGetValue(aura, out bool isAdd)) {
        if (isAdd == false) { return; }
        aurasToChange[aura] = false;
      } else {
        aurasToChange.Add(aura, false);
      }
      //changeQueue.Enqueue(new AuraChangeRequest(false, aura));

      //RemoveAuraEffects(aura, true, false);
      //aura.owner.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(owner.GUID, owner.GUID,
      //"AURA FROM " + aura.owner.DisplayName + " REMOVE",
      //FloatieMessage.MessageNature.Debuff
      //));
      Log.LogWrite("Collision exit " + owner.DisplayName + ":" + owner.GUID + " -> " + aura.owner.DisplayName + ":" + aura.owner.GUID + " r:" + aura.collider.radius + "/" + Vector3.Distance(owner.CurrentPosition, aura.owner.CurrentPosition) + "\n");
    }
    public void Update() {
      if (aurasToChange.Count == 0) { return; };
      KeyValuePair<AuraBubble, bool> auraChangeRec = aurasToChange.First();
      aurasToChange.Remove(auraChangeRec.Key);
      //AuraChangeRequest request = changeQueue.Dequeue();
      if (auraChangeRec.Value) {
        if (affectedAurasEffects.ContainsKey(auraChangeRec.Key) == false) {
          ApplyAuraEffects(auraChangeRec.Key, false);
        }
      } else {
        if (affectedAurasEffects.ContainsKey(auraChangeRec.Key)) {
          RemoveAuraEffects(auraChangeRec.Key, true, false);
        }
      }
    }
  }
  public class AuraBubble : MonoBehaviour {
    public AbstractActor owner { get; set; }
    public MechComponent source { get; set; }
    public CombatAuraReticle reticle { get; set; }
    public SphereCollider collider { get; set; }
    public HashSet<AuraActorBody> affectedTo { get; set; }
    public AuraDef Def { get; set; }
    public Dictionary<ParticleSystem, AuraBubbleVFXDef> bubblesVFX { get; set; }
    private float FRadius { get; set; }
    private float Speed { get; set; }
    private float StartupCounter { get; set; }
    public bool isMainSensors { get { return source == null; } }
    public ParticleSystem PlayVFXAt(Vector3 offset, string vfxName) {
      if (string.IsNullOrEmpty(vfxName))
        return (ParticleSystem)null;
      GameObject gameObject = this.owner.Combat.DataManager.PooledInstantiate(vfxName, BattleTechResourceType.Prefab, new Vector3?(), new Quaternion?(), (Transform)null);
      if ((UnityEngine.Object)gameObject == (UnityEngine.Object)null) {
        GameRepresentation.initLogger.LogError((object)("Error instantiating VFX " + vfxName), (UnityEngine.Object)this);
        return (ParticleSystem)null;
      }
      ParticleSystem component = gameObject.GetComponent<ParticleSystem>();
      component.Stop(true);
      component.Clear(true);
      Transform transform = gameObject.transform;
      transform.SetParent((Transform)null);
      BTWindZone componentInChildren1 = gameObject.GetComponentInChildren<BTWindZone>(true);
      if ((UnityEngine.Object)componentInChildren1 != (UnityEngine.Object)null && componentInChildren1.enabled)
        componentInChildren1.ResetZero();
      BTLightAnimator componentInChildren2 = gameObject.GetComponentInChildren<BTLightAnimator>(true);
      transform.SetParent(owner.GameRep.thisTransform, false);
      transform.localPosition = offset;
      transform.localRotation = Quaternion.identity;
      transform.localScale = Vector3.one;
      BTCustomRenderer.SetVFXMultiplier(component);
      component.Play(true);
      if ((UnityEngine.Object)componentInChildren1 != (UnityEngine.Object)null)
        componentInChildren1.PlayAnimCurve();
      if ((UnityEngine.Object)componentInChildren2 != (UnityEngine.Object)null)
        componentInChildren2.PlayAnimation();
      return component;
    }
    public float Radius {
      get {
        return FRadius;
      }
      set {
        FRadius = value;
        Speed = (FRadius - this.collider.radius) / 5f;
      }
    }
    private float GetRadius() {
      if (Def == null) { return 0f; }
      if (owner == null) { return Def.Range; }
      if ((Def.Id == "AMS") && (source != null)) {
        Weapon weapon = source as Weapon;
        if (weapon != null) {
          return weapon.MaxRange;
        }
      }
      if (string.IsNullOrEmpty(Def.RangeStatistic)) { return Def.Range; };
      return owner.StatCollection.GetStatistic(Def.RangeStatistic).Value<float>();
    }
    public void UpdateRadius(bool now = false) {
      float radius = this.GetRadius();
      if (this.owner.IsDead || this.owner.IsShutDown) { radius = 0.1f; } else
      if (source != null) {
        if (source.IsFunctional == false) { radius = 0.1f; } else {
          if (Def.State != AuraState.Persistent) {
            if ((source.isActive() == false) && (Def.State == AuraState.Online)) { radius = 0.1f; } else
            if ((source.isActive() == true) && (Def.State == AuraState.Offline)) { radius = 0.1f; } // исключающее или?
          }
        }
      }
      if (now) { this.collider.radius = radius; };
      Radius = radius;
      Log.LogWrite("Aura:" + owner.DisplayName + ":" + this.Def.Id + " src:" + (source != null?source.isActive().ToString():"null") + " state:" + Def.State + " " + this.collider.radius + "->" + Radius + " speed:" + Speed + "\n");
    }
    public void Init(AbstractActor owner, MechComponent source, AuraDef def, CombatAuraReticle reticle) {
      try {
        this.owner = owner;
        this.source = source;
        this.Def = def;
        this.reticle = reticle;
        this.StartupCounter = Core.Settings.auraStartupTime;
        this.gameObject.AddComponent<Rigidbody>();
        this.gameObject.GetComponent<Rigidbody>().name = "RBODY:" + owner.DisplayName + ":" + owner.GUID;
        this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        this.gameObject.GetComponent<Rigidbody>().useGravity = false;
        this.gameObject.layer = LayerMask.NameToLayer("VFXPhysics");
        this.collider = this.gameObject.AddComponent<SphereCollider>();
        this.collider.enabled = false;
        this.collider.isTrigger = true;
        this.collider.radius = 0f;
        if (source != null) {
          if (source.IsFunctional) {
            ActivatableComponent activatable = source.componentDef.GetComponent<ActivatableComponent>();
            if (activatable == null) {
              this.Radius = this.GetRadius();
            } else {
              if (activatable.ActiveByDefault == true) {
                this.Radius = this.GetRadius();
              } else {
                this.Radius = 0.1f;
              }
            }
          } else {
            this.Radius = 0.1f;
          }
        } else {
          this.Radius = this.GetRadius();
        }
        this.collider.name = "ABODY:" + owner.DisplayName + ":" + owner.GUID;
        Color bc = def.AuraColor;
        bc.a = 1.0f;
        Color dc = def.AuraColor;
        dc.a = 0.6f;
        if (def.LineType == AuraLineType.Dashes) {
          reticle.auraRangeMatBright = Material.Instantiate(reticle.auraRangeMatBright);
          reticle.auraRangeMatDim = Material.Instantiate(reticle.auraRangeMatDim);
          reticle.auraRangeMatBrightEnemy = Material.Instantiate(reticle.auraRangeMatBright);
          reticle.auraRangeMatDimEnemy = Material.Instantiate(reticle.auraRangeMatDim);
          reticle.activeProbeMatBright = Material.Instantiate(reticle.auraRangeMatBright);
          reticle.activeProbeMatDim = Material.Instantiate(reticle.auraRangeMatDim);
          reticle.auraRangeDecal.DecalMaterial = reticle.auraRangeMatBright;
          reticle.activeProbeDecal.DecalMaterial = reticle.activeProbeMatBright;
        } else {
          reticle.auraRangeMatBright = Material.Instantiate(reticle.activeProbeMatBright);
          reticle.auraRangeMatDim = Material.Instantiate(reticle.activeProbeMatDim);
          reticle.auraRangeMatBrightEnemy = Material.Instantiate(reticle.activeProbeMatBright);
          reticle.auraRangeMatDimEnemy = Material.Instantiate(reticle.activeProbeMatDim);
          reticle.activeProbeMatBright = Material.Instantiate(reticle.activeProbeMatBright);
          reticle.activeProbeMatDim = Material.Instantiate(reticle.activeProbeMatDim);
          reticle.auraRangeDecal.DecalMaterial = reticle.auraRangeMatBright;
          reticle.activeProbeDecal.DecalMaterial = reticle.activeProbeMatBright;
        }

        reticle.auraRangeMatBrightEnemy.color = bc;
        reticle.auraRangeMatBright.color = bc;
        reticle.activeProbeMatBright.color = bc;

        reticle.auraRangeMatDimEnemy.color = dc;
        reticle.auraRangeMatDim.color = dc;
        reticle.activeProbeMatDim.color = dc;
        owner.RegisterAuraBubble(source, reticle, this);
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n",true);
      }
    }
    //public GameObject dbgvisual { get; set; }
    //private float speed;
    public AuraBubble() {
      collider = null;
      reticle = null;
      source = null;
      owner = null;
      Speed = 0f;
      affectedTo = new HashSet<AuraActorBody>();
      bubblesVFX = new Dictionary<ParticleSystem, AuraBubbleVFXDef>();
      //speed = 10f;
    }
    void Awake() {
      Log.LogWrite("Aura collider awake " + owner.DisplayName + ":" + owner.GUID + "\n");
      //speed = 2.5f;
    }
    public void PlayOnlineFX() {
      try {
        foreach (AuraBubbleVFXDef vfxdef in this.Def.onlineVFX) {
          ParticleSystem ps = this.PlayVFXAt(Vector3.zero, vfxdef.VFXname);
          if (vfxdef.scale) {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ps.gameObject.transform.localScale = Vector3.one * this.collider.radius / vfxdef.scaleToRangeFactor;
          }
          bubblesVFX.Add(ps, vfxdef);
          Log.LogWrite("PlayOnlineFX:" + vfxdef.VFXname + "\n");
          Component[] components = ps.gameObject.GetComponentsInChildren<Component>();
          foreach (Component component in components) {
            Log.LogWrite(" " + component.name + ":" + component.GetType().ToString() + "\n");
            ps = component as ParticleSystem;
            if (ps != null) {
              var main = ps.main;
              main.scalingMode = ParticleSystemScalingMode.Hierarchy;
              Log.LogWrite("  " + ps.name + ":" + ps.main.scalingMode + "\n");
            }
          }
        }
        foreach (string SFX in this.Def.ownerSFX) {
          int num2 = (int)WwiseManager.PostEvent(SFX, owner.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
        }
      }catch(Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
    public void StopOnlineFX() {
      List<ParticleSystem> pss = bubblesVFX.Keys.ToList();
      bubblesVFX.Clear();
      foreach (var ps in pss) { GameObject.Destroy(ps.gameObject); }
      foreach (string SFX in this.Def.removeOwnerSFX) {
        int num2 = (int)WwiseManager.PostEvent(SFX, owner.GameRep.audioObject, (AkCallbackManager.EventCallback)null, (object)null);
      }
    }
    public void Update() {
      try {
        if (collider == null) { return; };
        //if (this.StartupCounter > 0f) {
          //collider.enabled = false;
          //return;
        //}
        if ((collider.enabled == false) && (collider.radius > 1f)) { collider.enabled = true; this.PlayOnlineFX(); };
        if ((collider.enabled == true) && (collider.radius < 1f)) {
          List<AuraActorBody> restBodies = this.affectedTo.ToList();
          collider.enabled = false;
          foreach (AuraActorBody body in restBodies) {
            body.RemoveAuraEffects(this, true, true);
          }
          this.StopOnlineFX();
        };
        if (Mathf.Abs(Speed) <= Core.Epsilon) { return; }
        float sdelta = Mathf.Abs(Speed * Time.deltaTime);
        float delta = Mathf.Abs(collider.radius - FRadius);
        if (sdelta >= delta) { collider.radius = FRadius; Speed = 0f; } else {
          collider.radius += Speed * Time.deltaTime;
        }
        foreach (var vfx in bubblesVFX) {
          vfx.Key.gameObject.transform.localScale = Vector3.one * this.collider.radius / vfx.Value.scaleToRangeFactor;
        }
        reticle.auraRangeScaledObject().transform.Rotate(0f, 10f, 0f, Space.Self);
      }catch(Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
  }
  [HarmonyPatch(typeof(CombatHUDInWorldElementMgr))]
  [HarmonyPatch("AddInWorldActorElements")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(ICombatant) })]
  public static class CombatAuraReticle_Init_Aura {
    public static void Postfix(CombatHUDInWorldElementMgr __instance, ICombatant combatant, ref List<CombatAuraReticle> ___AuraReticles, CombatHUD ___HUD) {
      try {
        Log.LogWrite("AddInWorldActorElements " + combatant.DisplayName + ":" + combatant.GUID + "\n");
        AbstractActor owner = combatant as AbstractActor;
        if (owner == null) { return; };
        //CombatAuraReticle sensorsReticle = null;
        //foreach (CombatAuraReticle reticle in ___AuraReticles) {
        //if (reticle.owner().GUID == owner.GUID) { sensorsReticle = reticle; break; }
        //}
        //if (sensorsReticle == null) { return; };
        CombatAuraReticle sensorsReticle = owner.Combat.DataManager.PooledInstantiate(CombatAuraReticle.PrefabName, BattleTechResourceType.UIModulePrefabs, new Vector3?(), new Quaternion?(), (Transform)null).GetComponent<CombatAuraReticle>();
        sensorsReticle.Init(owner, ___HUD);
        GameObject aura = new GameObject("BODY:" + owner.DisplayName + ":" + owner.GUID);
        aura.SetActive(false);
        aura.transform.localScale = new Vector3(1f, 1f, 1f);
        aura.gameObject.transform.parent = owner.GameRep.gameObject.transform;
        aura.gameObject.transform.position = owner.CurrentPosition;
        aura.gameObject.AddComponent<AuraBubble>().Init(owner, null, Core.Settings.sensorsAura, sensorsReticle);
        //aura.gameObject.GetComponent<AuraBubble>().dbgvisual = __instance.auraRangeDecal.gameObject;
        ___AuraReticles.Add(sensorsReticle);
        aura.SetActive(true);
        foreach (MechComponent source in owner.allComponents) {
          Log.LogWrite(" component: " + source.defId + " functional:"+source.IsFunctional+"\n");
          if (source.IsFunctional == false) { continue; }
          List<AuraDef> auraDefs = source.componentDef.GetAuras();
          foreach (AuraDef auraDef in auraDefs) {
            Log.LogWrite("  aura: " + auraDef.Id + "\n");
            CombatAuraReticle reticle = owner.Combat.DataManager.PooledInstantiate(CombatAuraReticle.PrefabName, BattleTechResourceType.UIModulePrefabs, new Vector3?(), new Quaternion?(), (Transform)null).GetComponent<CombatAuraReticle>();
            reticle.Init(owner, ___HUD);
            aura = new GameObject("BODY:" + owner.DisplayName + ":" + owner.GUID);
            aura.SetActive(false);
            aura.transform.localScale = new Vector3(1f, 1f, 1f);
            aura.gameObject.transform.parent = owner.GameRep.gameObject.transform;
            aura.gameObject.transform.position = owner.CurrentPosition;
            aura.gameObject.AddComponent<AuraBubble>().Init(owner, source, auraDef, reticle);
            ___AuraReticles.Add(reticle);
            //aura.gameObject.GetComponent<AuraBubble>().dbgvisual = __instance.auraRangeDecal.gameObject;
            aura.SetActive(true);
          }
        }
      }catch(Exception e) {
        Log.LogWrite(e.ToString() + "\n",true);
      }
    }
  }
  [HarmonyPatch(typeof(AbstractActor))]
  [HarmonyPatch("EvaluateStealthState")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class AbstractActor_EvaluateStealthState {
    public static bool Prefix(AbstractActor __instance) {
      try {
        if (__instance.IsStealthFloatieSkip()) { __instance.StealthPipsPrevious(__instance.StealthPipsCurrent); };
      }catch(Exception e) {
        Log.LogWrite(e.ToString() + "\n",true);
      }
      return true;
    }
  }
  [HarmonyPatch(typeof(ActorMovementSequence))]
  [HarmonyPatch("CompleteMove")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class ActorMovementSequence_CompleteMoveAuraFloatie {
    public static void Postfix(MechJumpSequence __instance) {
      CAEAuraHelper.FlushMovingAuraFloaties();
    }
  }
  [HarmonyPatch(typeof(MechJumpSequence))]
  [HarmonyPatch("CompleteJump")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { })]
  public static class MechJumpSequence_CompleteJumpAuraFloatie {
    public static void Postfix(MechJumpSequence __instance) {
      CAEAuraHelper.FlushMovingAuraFloaties();
    }
  }
  [HarmonyPatch(typeof(Mech))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Transform) })]
  public static class Mech_InitGameRep_Aura {
    public static void Postfix(Mech __instance, Transform parentTransform) {
      try {
        __instance.InitBodyBubble();
      }catch(Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
  }
  [HarmonyPatch(typeof(Vehicle))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Transform) })]
  public static class Vehicle_InitGameRep_Aura {
    public static void Postfix(Vehicle __instance, Transform parentTransform) {
      try {
        __instance.InitBodyBubble();
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
  }
  [HarmonyPatch(typeof(Turret))]
  [HarmonyPatch("InitGameRep")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(Transform) })]
  public static class Turret_InitGameRep_Aura {
    public static void Postfix(Turret __instance, Transform parentTransform) {
      try {
        __instance.InitBodyBubble();
      } catch (Exception e) {
        Log.LogWrite(e.ToString() + "\n", true);
      }
    }
  }
}

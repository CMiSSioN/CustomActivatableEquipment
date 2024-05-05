using BattleTech;
using System.Collections.Generic;
using UnityEngine;

namespace CustomActivatableEquipment {
  public enum StealthAffection { None, Nullify, PositiveOne, PositiveTwo, PositiveThree, PositiveFour, PositiveFive, NegativeOne, NegativeTwo, NegativeThree, NegativeFour, NegativeFive }
  public class AuraPreview {
    public AbstractActor proj;
    public AbstractActor recv;
    public AuraDef def;
    public bool ProjAllyToRecv;
    public AuraPreview(AbstractActor proj, AbstractActor recv, AuraDef def) {
      this.proj = proj;
      this.recv = recv;
      this.def = def;
      ProjAllyToRecv = false;
      if(proj.GUID == recv.GUID) {
        ProjAllyToRecv = true;
      } else
      if(proj.TeamId == recv.TeamId) {
        ProjAllyToRecv = true;
      }else
      if (proj.team.IsFriendly(recv.team)) {
        ProjAllyToRecv = true;
      }
    }
    public void DBGLogPrint() {
      Log.Debug?.Write("  proj:"+proj.DisplayName+"-"+proj.GUID+"\n");
      Log.Debug?.Write("  recv:" + recv.DisplayName + "-" + recv.GUID + "\n");
      Log.Debug?.Write("  def:" + def.Id + "\n");
      Log.Debug?.Write("  ProjAllyToRecv:" + ProjAllyToRecv + "\n");
    }
  }
  public class AuraPreviewRecord {
    public AbstractActor movingActor;
    public Dictionary<AbstractActor, List<AuraPreview>> aurasAdded;
    public Dictionary<AbstractActor, List<AuraPreview>> aurasRemoved;
    public Dictionary<AbstractActor, int> stealthPipsPreview;
    public void DBGLogPrint() {
      Log.Debug?.Write(" movingActor:"+movingActor.DisplayName+"-"+movingActor.GUID+"\n");
      Log.Debug?.Write(" aurasAdded:\n");
      foreach(var auras in aurasAdded) {
        foreach(AuraPreview aura in auras.Value) {
          aura.DBGLogPrint();
        }
      }
      Log.Debug?.Write(" aurasRemoved:\n");
      foreach (var auras in aurasRemoved) {
        foreach (AuraPreview aura in auras.Value) {
          aura.DBGLogPrint();
        }
      }
      Log.Debug?.Write(" stealthPipsPreview:\n");
      foreach (var st in stealthPipsPreview) {
        Log.Debug?.Write("  "+st.Key.DisplayName+":"+st.Value+"\n");
      }
    }
    public AuraPreviewRecord(AbstractActor movingActor) {
      aurasAdded = new Dictionary<AbstractActor, List<AuraPreview>>();
      aurasRemoved = new Dictionary<AbstractActor, List<AuraPreview>>();
      stealthPipsPreview = new Dictionary<AbstractActor, int>();
      this.movingActor = movingActor;
    }
    public void RecalculateStealthPips() {
      List<AbstractActor> actors = movingActor.Combat.AllActors;
      //Log.LogWrite("RecalculateStealthPips\n");
      foreach(AbstractActor actor in actors) {
        int pips = actor.StealthPipsTotal;
        //Log.LogWrite(" "+actor.DisplayName+":"+pips+"\n");
        bool nofurthercalc = false;
        List<AuraPreview> auras = previewAurasAdded(actor);
        foreach(AuraPreview aura in auras) {
          //Log.LogWrite(" add aura:"+aura.def.Id+"\n");
          if (aura.recv.GUID != actor.GUID) {
            //Log.LogWrite(" wrong aura:"+ aura.recv.GUID + " != "+actor.GUID+"\n");
            break;
          }
          if (aura.ProjAllyToRecv) {
            switch (aura.def.AllyStealthAffection) {
              case StealthAffection.Nullify: nofurthercalc = true; pips = 0; break;
              case StealthAffection.PositiveOne: pips += 1; break;
              case StealthAffection.PositiveTwo: pips += 2; break;
              case StealthAffection.PositiveThree: pips += 3; break;
              case StealthAffection.PositiveFour: pips += 4; break;
              case StealthAffection.PositiveFive: pips += 5; break;
              case StealthAffection.NegativeOne: pips -= 1; break;
              case StealthAffection.NegativeTwo: pips -= 2; break;
              case StealthAffection.NegativeThree: pips -= 3; break;
              case StealthAffection.NegativeFour: pips -= 4; break;
              case StealthAffection.NegativeFive: pips -= 5; break;
            }
            //Log.LogWrite("  pips altered:"+pips+"\n");
          } else {
            switch (aura.def.EnemyStealthAffection) {
              case StealthAffection.Nullify: nofurthercalc = true; pips = 0; break;
              case StealthAffection.PositiveOne: pips += 1; break;
              case StealthAffection.PositiveTwo: pips += 2; break;
              case StealthAffection.PositiveThree: pips += 3; break;
              case StealthAffection.PositiveFour: pips += 4; break;
              case StealthAffection.PositiveFive: pips += 5; break;
              case StealthAffection.NegativeOne: pips -= 1; break;
              case StealthAffection.NegativeTwo: pips -= 2; break;
              case StealthAffection.NegativeThree: pips -= 3; break;
              case StealthAffection.NegativeFour: pips -= 4; break;
              case StealthAffection.NegativeFive: pips -= 5; break;
            }
            //Log.LogWrite("  pips altered:" + pips + "\n");
          }
          if (nofurthercalc) { break; }
        }
        if (nofurthercalc) {
          //Log.LogWrite(" no further calc\n");
          stealthPipsPreview.Add(actor, System.Math.Max(pips, 0)); continue;
        }
        auras = previewAurasRemoved(actor);
        foreach (AuraPreview aura in auras) {
          //Log.LogWrite(" remove aura:" + aura.def.Id + "\n");
          if (aura.recv.GUID != actor.GUID) {
            //Log.LogWrite(" wrong aura:" + aura.recv.GUID + " != " + actor.GUID + "\n");
            break;
          }
          if (aura.ProjAllyToRecv) {
            switch (aura.def.AllyStealthAffection) {
              case StealthAffection.PositiveOne: pips -= 1; break;
              case StealthAffection.PositiveTwo: pips -= 2; break;
              case StealthAffection.PositiveThree: pips -= 3; break;
              case StealthAffection.PositiveFour: pips -= 4; break;
              case StealthAffection.PositiveFive: pips -= 5; break;
              case StealthAffection.NegativeOne: pips += 1; break;
              case StealthAffection.NegativeTwo: pips += 2; break;
              case StealthAffection.NegativeThree: pips += 3; break;
              case StealthAffection.NegativeFour: pips += 4; break;
              case StealthAffection.NegativeFive: pips += 5; break;
            }
            //Log.LogWrite("  pips altered:" + pips + "\n");
          } else {
            switch (aura.def.EnemyStealthAffection) {
              case StealthAffection.PositiveOne: pips -= 1; break;
              case StealthAffection.PositiveTwo: pips -= 2; break;
              case StealthAffection.PositiveThree: pips -= 3; break;
              case StealthAffection.PositiveFour: pips -= 4; break;
              case StealthAffection.PositiveFive: pips -= 5; break;
              case StealthAffection.NegativeOne: pips += 1; break;
              case StealthAffection.NegativeTwo: pips += 2; break;
              case StealthAffection.NegativeThree: pips += 3; break;
              case StealthAffection.NegativeFour: pips += 4; break;
              case StealthAffection.NegativeFive: pips += 5; break;
            }
            //Log.LogWrite("  pips altered:" + pips + "\n");
          }
        }
        stealthPipsPreview.Add(actor, System.Math.Max(pips, 0));
      }
    }
    public int getStealthPipsPreview(AbstractActor unit) {
      if (stealthPipsPreview.ContainsKey(unit) == false) { return unit.StealthPipsCurrent; };
      return stealthPipsPreview[unit];
    }
    public List<AuraPreview> previewAurasRemoved(AbstractActor unit) {
      if (aurasRemoved.ContainsKey(unit)) { return aurasRemoved[unit]; }
      return new List<AuraPreview>();
    }
    public List<AuraPreview> previewAurasAdded(AbstractActor unit) {
      if (aurasAdded.ContainsKey(unit)) { return aurasAdded[unit]; }
      return new List<AuraPreview>();
    }
  }
  public static partial class CAEAuraHelper {
    private static int auraCacheTurn = -1;
    private static int auraCachePhase = -1;
    public static Dictionary<AbstractActor, Dictionary<Vector3, AuraPreviewRecord>> auraPreviewCache = new Dictionary<AbstractActor, Dictionary<Vector3, AuraPreviewRecord>>();
    public static void ClearAuraPreviewCache() {
      auraPreviewCache.Clear();
    }
    public static AuraPreviewRecord getPreviewCache(this AbstractActor movingActor, Vector3 position) {
      int currentTurn = movingActor.Combat.TurnDirector.CurrentRound;
      int currentPhase = movingActor.Combat.TurnDirector.CurrentPhase;
      if ((CAEAuraHelper.auraCachePhase != currentPhase) || (currentTurn != CAEAuraHelper.auraCacheTurn)) {
        auraPreviewCache = new Dictionary<AbstractActor, Dictionary<Vector3, AuraPreviewRecord>>();
        CAEAuraHelper.auraCacheTurn = currentTurn; auraCachePhase = currentPhase;
      };
      if (auraPreviewCache.ContainsKey(movingActor)) {
        if (auraPreviewCache[movingActor].ContainsKey(position)) {
          return auraPreviewCache[movingActor][position];
        }
      } else {
        auraPreviewCache.Add(movingActor, new Dictionary<Vector3, AuraPreviewRecord>());
      }
      AuraPreviewRecord result = movingActor.fillPreviewCache(position);
      auraPreviewCache[movingActor].Add(position, result);
      return result;
    }
    public static AuraPreviewRecord fillPreviewCache(this AbstractActor movingActor, Vector3 position) {
      AuraActorBody body = movingActor.bodyAura();
      if (body == null) {
        Log.WriteCritical("WARNING!" + movingActor.DisplayName + ":" + movingActor.GUID + " has no body to apply auras\n");
        return new AuraPreviewRecord(movingActor);
      }
      HashSet<AuraBubble> newAuras = new HashSet<AuraBubble>();
      HashSet<AuraBubble> remAuras = new HashSet<AuraBubble>();
      foreach(var aura in body.affectedAurasEffects) { if (aura.Key.owner.GUID != movingActor.GUID) remAuras.Add(aura.Key);}
      Collider[] hitColliders = Physics.OverlapSphere(position, 1f);
      foreach (Collider collider in hitColliders) {
        AuraBubble aura = collider.gameObject.GetComponent<AuraBubble>();
        if (aura == null) { continue; }
        if (aura.Def.RemoveOnSensorLock && movingActor.IsSensorLocked) { continue; }
        if (aura.Def.NotApplyMoving && Vector3.Distance(movingActor.CurrentPosition, position) > 1.0f) { continue; }
        if (aura.owner.GUID == movingActor.GUID) { continue; }
        if (body.affectedAurasEffects.ContainsKey(aura) == false) {
          newAuras.Add(aura);
        } else {
          remAuras.Remove(aura);
        }
      }
      AuraPreviewRecord result = new AuraPreviewRecord(movingActor);
      result.aurasAdded.Add(movingActor, new List<AuraPreview>());
      result.aurasRemoved.Add(movingActor, new List<AuraPreview>());
      List<AuraPreview> tmpList = result.aurasAdded[movingActor];
      foreach (AuraBubble aura in newAuras) {
        if ((aura.owner.GUID == movingActor.GUID) && (aura.Def.ApplySelf == false)) { continue; }
        tmpList.Add(new AuraPreview(aura.owner,movingActor,aura.Def));
      }
      tmpList = result.aurasRemoved[movingActor];
      foreach (AuraBubble aura in remAuras) { tmpList.Add(new AuraPreview(aura.owner, movingActor, aura.Def)); }
      List<AuraBubble> actorAuras = movingActor.actorAuras();
      foreach(AuraBubble aura in actorAuras) {
        if (aura.collider.enabled == false) { continue; }
        if (aura.collider.radius < 1f) { continue; }
        hitColliders = Physics.OverlapSphere(position, aura.collider.radius);
        HashSet<AbstractActor> newUnits = new HashSet<AbstractActor>();
        HashSet<AbstractActor> remUnits = new HashSet<AbstractActor>();
        foreach (var unit in aura.affectedTo) { if (unit.owner.GUID != movingActor.GUID) { remUnits.Add(unit.owner); } }
        foreach (Collider collider in hitColliders) {
          AuraActorBody otherBody = collider.gameObject.GetComponent<AuraActorBody>();
          if (otherBody == null) { continue; }
          if (movingActor.GUID == otherBody.owner.GUID) { continue; }
          if (aura.Def.RemoveOnSensorLock && otherBody.owner.IsSensorLocked) { continue; }
          if (aura.affectedTo.Contains(otherBody) == false) {
            newUnits.Add(otherBody.owner);
          } else {
            remUnits.Remove(otherBody.owner);
          }
        }
        foreach(AbstractActor unit in newUnits) {
          if (result.aurasAdded.ContainsKey(unit) == false) { result.aurasAdded.Add(unit, new List<AuraPreview>()); };
          result.aurasAdded[unit].Add(new AuraPreview(aura.owner, unit, aura.Def));
        }
        foreach (AbstractActor unit in remUnits) {
          if (result.aurasRemoved.ContainsKey(unit) == false) { result.aurasRemoved.Add(unit, new List<AuraPreview>()); };
          result.aurasRemoved[unit].Add(new AuraPreview(aura.owner, unit, aura.Def));
        }
      }
      result.RecalculateStealthPips();
      //Log.LogWrite("fillPreviewCache:"+movingActor.DisplayName+" "+position.ToString()+"\n");
      //result.DBGLogPrint();
      return result;
    }
  }
}
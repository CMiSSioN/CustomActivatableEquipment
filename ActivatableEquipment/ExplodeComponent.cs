using BattleTech;
using CustomComponents;
using Localize;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Building = BattleTech.Building;

namespace CustomActivatableEquipment {
  public class AoEComponentExplosionHitRecord {
    public Vector3 hitPosition;
    public float Damage;
    public AoEComponentExplosionHitRecord(Vector3 pos, float dmg) {
      this.hitPosition = pos;
      this.Damage = dmg;
    }
  }
  public class AoEComponentExplosionRecord {
    public ICombatant target;
    public float HeatDamage;
    public float StabDamage;
    public Dictionary<int, AoEComponentExplosionHitRecord> hitRecords;
    public AoEComponentExplosionRecord(ICombatant trg) {
      this.target = trg;
      this.HeatDamage = 0f;
      this.StabDamage = 0f;
      this.hitRecords = new Dictionary<int, AoEComponentExplosionHitRecord>();
    }
  }
  public static class AoEComponentExplodeHelper {
    public static Dictionary<int, float> MechHitLocations = null;
    public static Dictionary<int, float> VehicleLocations = null;
    public static Dictionary<int, float> OtherLocations = null;
    public static readonly float AOEHitIndicator = -10f;
    public static Vector3 GetBuildingHitPosition(this LineOfSight LOS, AbstractActor attacker, BattleTech.Building target, Vector3 attackPosition, float weaponRange, Vector3 origHitPosition) {
      Vector3 a = origHitPosition;
      Vector3 vector3_1 = attackPosition + attacker.HighestLOSPosition;
      string guid = target.GUID;
      Vector3 collisionWorldPos = Vector3.zero;
      bool flag = false;
      if ((UnityEngine.Object)target.BuildingRep == (UnityEngine.Object)null)
        return a;
      foreach (Collider allRaycastCollider in target.GameRep.AllRaycastColliders) {
        if (LOS.HasLineOfFire(vector3_1, allRaycastCollider.bounds.center, guid, weaponRange, out collisionWorldPos)) {
          a = allRaycastCollider.bounds.center;
          flag = true;
          break;
        }
      }
      for (int index1 = 0; index1 < target.LOSTargetPositions.Length; ++index1) {
        if (LOS.HasLineOfFire(vector3_1, target.LOSTargetPositions[index1], guid, weaponRange, out collisionWorldPos)) {
          if (flag) {
            Vector3 end = Vector3.Lerp(a, target.LOSTargetPositions[index1], UnityEngine.Random.Range(0.0f, 0.15f));
            if (LOS.HasLineOfFire(vector3_1, end, guid, weaponRange, out collisionWorldPos))
              a = end;
          } else {
            Vector3 vector3_2 = a;
            for (int index2 = 0; index2 < 10; ++index2) {
              vector3_2 = Vector3.Lerp(vector3_2, target.LOSTargetPositions[index1], UnityEngine.Random.Range(0.1f, 0.6f));
              if (LOS.HasLineOfFire(vector3_1, vector3_2, guid, weaponRange, out collisionWorldPos)) {
                a = vector3_2;
                flag = true;
                break;
              }
            }
            if (!flag) {
              a = target.LOSTargetPositions[index1];
              flag = true;
            }
          }
        }
      }
      Ray ray = new Ray(vector3_1, a - vector3_1);
      foreach (Collider allRaycastCollider in target.GameRep.AllRaycastColliders) {
        GameObject gameObject = allRaycastCollider.gameObject;
        bool activeSelf = gameObject.activeSelf;
        gameObject.SetActive(true);
        RaycastHit hitInfo;
        if (allRaycastCollider.Raycast(ray, out hitInfo, 1000f)) {
          gameObject.SetActive(activeSelf);
          return hitInfo.point;
        }
        gameObject.SetActive(activeSelf);
      }
      return a;

    }
    public static Vector3 getImpactPositionSimple(this ICombatant initialTarget, AbstractActor attacker, Vector3 attackPosition, int hitLocation) {
      Vector3 impactPoint = initialTarget.CurrentPosition;
      AttackDirection attackDirection = AttackDirection.FromFront;
      if ((UnityEngine.Object)initialTarget.GameRep != (UnityEngine.Object)null) {
        impactPoint = initialTarget.GameRep.GetHitPosition(hitLocation);
        attackDirection = initialTarget.Combat.HitLocation.GetAttackDirection(attackPosition, initialTarget);
        if (initialTarget.UnitType == UnitType.Building) {
          impactPoint = attacker.Combat.LOS.GetBuildingHitPosition(attacker, initialTarget as BattleTech.Building, attackPosition, 100f, impactPoint);
        } else {
          Vector3 origin = attackPosition + attacker.HighestLOSPosition;
          Vector3 vector3_2 = impactPoint - origin;
          Ray ray2 = new Ray(origin, vector3_2.normalized);
          foreach (Collider allRaycastCollider in initialTarget.GameRep.AllRaycastColliders) {
            RaycastHit hitInfo;
            if (allRaycastCollider.Raycast(ray2, out hitInfo, vector3_2.magnitude)) {
              impactPoint = hitInfo.point;
              break;
            }
          }
        }
      }
      return impactPoint;
    }
    public static void InitHitLocationsAOE() {
      AoEComponentExplodeHelper.MechHitLocations = new Dictionary<int, float>();
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.CenterTorso] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.CenterTorsoRear] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftTorso] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftTorsoRear] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightTorso] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightTorsoRear] = 100f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftArm] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightArm] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.LeftLeg] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.RightLeg] = 50f;
      AoEComponentExplodeHelper.MechHitLocations[(int)ArmorLocation.Head] = 0f;
      AoEComponentExplodeHelper.VehicleLocations = new Dictionary<int, float>();
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Front] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Rear] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Left] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Right] = 100f;
      AoEComponentExplodeHelper.VehicleLocations[(int)VehicleChassisLocations.Turret] = 80f;
      AoEComponentExplodeHelper.OtherLocations = new Dictionary<int, float>();
      AoEComponentExplodeHelper.OtherLocations[1] = 100f;
    }
    public static void AoEExplodeComponent(this MechComponent component) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      if (component.parent == null) { return; }
      if (activatable.Explosion.Damage <= Core.Epsilon) { return; }
      if (activatable.Explosion.Range <= Core.Epsilon) { return; }
      List<AoEComponentExplosionRecord> AoEDamage = new List<AoEComponentExplosionRecord>();
      foreach(ICombatant target in component.parent.Combat.GetAllLivingCombatants()) {
        if (target.GUID == component.parent.GUID) { continue; };
        float distance = Vector3.Distance(target.CurrentPosition, component.parent.CurrentPosition);
        if (distance > activatable.Explosion.Range) { continue; };
        float HeatDamage = activatable.Explosion.Heat * (activatable.Explosion.Range - distance) / activatable.Explosion.Range;
        float Damage = activatable.Explosion.Damage * (activatable.Explosion.Range - distance) / activatable.Explosion.Range;
        float StabDamage = activatable.Explosion.Stability * (activatable.Explosion.Range - distance) / activatable.Explosion.Range;
        Mech mech = target as Mech;
        Vehicle vehicle = target as Vehicle;
        if (mech == null) {
          Damage += HeatDamage;
        };
        List<int> hitLocations = null;
        Dictionary<int, float> AOELocationDict = null;
        if (mech != null) {
          hitLocations = component.parent.Combat.HitLocation.GetPossibleHitLocations(component.parent.CurrentPosition, mech);
          if (AoEComponentExplodeHelper.MechHitLocations == null) { AoEComponentExplodeHelper.InitHitLocationsAOE(); };
          AOELocationDict = AoEComponentExplodeHelper.MechHitLocations;
          int HeadIndex = hitLocations.IndexOf((int)ArmorLocation.Head);
          if ((HeadIndex >= 0) && (HeadIndex < hitLocations.Count)) { hitLocations.RemoveAt(HeadIndex); };
        } else
        if (target is Vehicle) {
          hitLocations = component.parent.Combat.HitLocation.GetPossibleHitLocations(component.parent.CurrentPosition, target as Vehicle);
          if (AoEComponentExplodeHelper.VehicleLocations == null) { AoEComponentExplodeHelper.InitHitLocationsAOE(); };
          AOELocationDict = AoEComponentExplodeHelper.VehicleLocations;
        } else {
          hitLocations = new List<int>() { 1 };
          if (AoEComponentExplodeHelper.OtherLocations == null) { AoEComponentExplodeHelper.InitHitLocationsAOE(); };
          AOELocationDict = AoEComponentExplodeHelper.OtherLocations;
        }
        float fullLocationDamage = 0.0f;
        foreach (int hitLocation in hitLocations) {
          if (AOELocationDict.ContainsKey(hitLocation)) {
            fullLocationDamage += AOELocationDict[hitLocation];
          } else {
            fullLocationDamage += 100f;
          }
        }
        Log.LogWrite(" hitLocations: ");
        foreach (int hitLocation in hitLocations) {
          Log.LogWrite(" " + hitLocation);
        }
        Log.LogWrite("\n");
        Log.LogWrite(" full location damage coeff " + fullLocationDamage + "\n");
        AoEComponentExplosionRecord AoERecord = new AoEComponentExplosionRecord(target);
        AoERecord.HeatDamage = HeatDamage;
        AoERecord.StabDamage = StabDamage;
        foreach (int hitLocation in hitLocations) {
          float currentDamageCoeff = 100f;
          if (AOELocationDict.ContainsKey(hitLocation)) {
            currentDamageCoeff = AOELocationDict[hitLocation];
          }
          currentDamageCoeff /= fullLocationDamage;
          float CurrentLocationDamage = Damage * currentDamageCoeff;
          if(AoERecord.hitRecords.ContainsKey(hitLocation)) {
            AoERecord.hitRecords[hitLocation].Damage += CurrentLocationDamage;
          } else {
            Vector3 pos = target.getImpactPositionSimple(component.parent, component.parent.CurrentPosition, hitLocation);
            AoERecord.hitRecords[hitLocation] = new AoEComponentExplosionHitRecord(pos, CurrentLocationDamage);
          }
          Log.LogWrite("  location " + hitLocation + " damage " + AoERecord.hitRecords[hitLocation].Damage + "\n");
        }
        AoEDamage.Add(AoERecord);
      }
      Log.LogWrite("AoE Damage result:\n");
      Weapon fakeWeapon = new Weapon();
      fakeWeapon.parent = component.parent;
      typeof(MechComponent).GetProperty("componentDef", BindingFlags.Instance | BindingFlags.Public).GetSetMethod(true).Invoke(fakeWeapon,new object[1] { (object)component.componentDef });
      var fakeHit = new WeaponHitInfo(-1, -1, -1, -1, component.parent.GUID, component.parent.GUID, -1, null, null, null, null, null, null, new AttackImpactQuality[1] { AttackImpactQuality.Solid }, new AttackDirection[1] { AttackDirection.FromArtillery }, null, null, null);
      for (int index = 0; index < AoEDamage.Count; ++index) {
        Log.LogWrite(" "+ AoEDamage[index].target.DisplayName+":"+ AoEDamage[index].target.GUID+"\n");
        Log.LogWrite(" Heat:" + AoEDamage[index].HeatDamage+ "\n");
        Log.LogWrite(" Instability:" + AoEDamage[index].StabDamage + "\n");
        fakeHit.targetId = AoEDamage[index].target.GUID;
        foreach (var AOEHitRecord in AoEDamage[index].hitRecords) {
          Log.LogWrite("  location:" + AOEHitRecord.Key + " pos:" + AOEHitRecord.Value.hitPosition + " dmg:" + AOEHitRecord.Value.Damage + "\n");
          float LocArmor = AoEDamage[index].target.ArmorForLocation(AOEHitRecord.Key);
          if ((double)LocArmor < (double)AOEHitRecord.Value.Damage) {
            component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, AoEDamage[index].target.GUID, new Text("{0}", new object[1]
            {
                      (object) (int) Mathf.Max(1f, AOEHitRecord.Value.Damage)
            }), component.parent.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.StructureDamage, AOEHitRecord.Value.hitPosition.x, AOEHitRecord.Value.hitPosition.y, AOEHitRecord.Value.hitPosition.z));
          } else {
            component.parent.Combat.MessageCenter.PublishMessage((MessageCenterMessage)new FloatieMessage(component.parent.GUID, AoEDamage[index].target.GUID, new Text("{0}", new object[1]
            {
                      (object) (int) Mathf.Max(1f, AOEHitRecord.Value.Damage)
            }), component.parent.Combat.Constants.CombatUIConstants.floatieSizeMedium, FloatieMessage.MessageNature.ArmorDamage, AOEHitRecord.Value.hitPosition.x, AOEHitRecord.Value.hitPosition.y, AOEHitRecord.Value.hitPosition.z));
          }
          AoEDamage[index].target.TakeWeaponDamage(fakeHit,AOEHitRecord.Key,fakeWeapon, AOEHitRecord.Value.Damage,0,DamageType.AmmoExplosion);
        }
        AoEDamage[index].target.HandleDeath(component.parent.GUID);
        Mech mech = AoEDamage[index].target as Mech;
        if(mech != null) {
          mech.HandleKnockdown(-1, component.parent.GUID, Vector2.one, (SequenceFinished)null);
        }
      }
    }
  }
}
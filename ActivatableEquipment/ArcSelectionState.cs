using BattleTech;
using BattleTech.Data;
using BattleTech.Rendering;
using BattleTech.UI;
using HarmonyLib;
using HBS.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CustomActivatableEquipment {
  public class SelectionStateActiveProbeArc : SelectionState {
    private bool _positionLocked = false;
    private bool hasActivated = false;
    public override bool ConsumesMovement => false;
    public override bool ConsumesFiring => true;
    public override Vector3 PreviewPos => this.SelectedActor.CurrentPosition;
    public override Quaternion PreviewRot => this.SelectedActor.CurrentRotation;
    protected virtual Settings.ArcDecalTexture arc { get; set; } = Settings.ArcDecalTexture.arc90;
    protected override bool ShouldShowWeaponsUI => true;
    protected override bool ShouldShowFiringArcs => false;
    protected override bool ShouldShowTargetingLines => false;
    protected override bool showHeatWarnings => true;
    protected virtual CombatHUDFireButton.FireMode FireMode => CombatHUDFireButton.FireMode.ActiveProbe;
    protected virtual string FireButtonString {
      get {
        if (!this.HasTarget)
          return Ability.ProcessDetailString(this.FromButton.Ability).ToString(true);
        return this.FromButton != null && this.FromButton.Ability != null ? Ability.ProcessDetailString(this.FromButton.Ability).ToString(true) : "TODO: active probe description. This string is in SelectionStateActiveProbe.";
      }
    }
    public override int ProjectedHeatForState => (int)this.FromButton.Ability.Def.FloatParam2;
    public override float ProjectedStabilityForState {
      get {
        Mech selectedMech = this.SelectedMech;
        if (selectedMech == null)
          return 0.0f;
        return !selectedMech.HasMovedThisRound && !selectedMech.CanMoveAfterShooting ? selectedMech.GetMinStability(StabilityChangeSource.RemainingStationary, this.CurrentStability) : this.CurrentStability;
      }
    }
    public override void RefreshPossibleTargets() {
      this.AllPossibleTargets = this.CalcPossibleTargets(this.FiringPreview, this.SelectedActor, this.PreviewPos, this.PreviewRot);
      this.ShowFireButton(this.FireMode, this.FireButtonString);
    }
    public virtual List<ICombatant> CalcPossibleTargets(FiringPreviewManager firingPreview, AbstractActor actor, Vector3 position, Quaternion rotation) {
      List<ICombatant> combatantList = new List<ICombatant>();
      float radius = (float)this.FromButton.Ability.Def.IntParam1;
      for (int index = 0; index < this.AllDetectedEnemies.Count; ++index) {
        if ((actor.CurrentPosition - this.AllDetectedEnemies[index].CurrentPosition).magnitude <= radius)
          combatantList.Add((ICombatant)this.AllDetectedEnemies[index]);
      }
      return combatantList;
    }
    protected bool positionLocked {
      get => this._positionLocked;
      set {
        this._positionLocked = value;
        if (value == false)
          return;
        this.HUD.SelectionHandler.NotifyChange(CombatSelectionHandler.SelectionChange.StateData);
      }
    }
    public override bool ProcessTargetNext() => false;
    public override bool ProcessTargetPrevious() => false;
    public override bool ProcessFaceTarget() => false;
    public override bool CanActorUseThisState(AbstractActor actor) => true;
    public override bool ProcessSelectNext() {
      if (this.Orders != null || this.AllPossibleTargets.Count == 0)
        return false;
      ICombatant targetedCombatant = this.TargetedCombatant;
      int num = -1;
      if (targetedCombatant != null)
        num = this.AllPossibleTargets.IndexOf(targetedCombatant);
      int index = num + 1;
      if (index >= this.AllPossibleTargets.Count)
        index = 0;
      ICombatant allPossibleTarget = this.AllPossibleTargets[index];
      if (this.HUD.SelectionHandler.TrySelectTarget(allPossibleTarget))
        this.SetTargetedCombatant(allPossibleTarget);
      if (targetedCombatant == this.TargetedCombatant)
        return false;
      this.HUD.SelectionHandler.ClearInfoTarget();
      return true;
    }
    public override bool ProcessSelectPrevious() {
      if (this.Orders != null || this.AllPossibleTargets.Count == 0)
        return false;
      ICombatant targetedCombatant = this.TargetedCombatant;
      int num = -1;
      if (targetedCombatant != null)
        num = this.AllPossibleTargets.IndexOf(targetedCombatant);
      int index = num - 1;
      if (index < 0)
        index = this.AllPossibleTargets.Count - 1;
      ICombatant allPossibleTarget = this.AllPossibleTargets[index];
      if (this.HUD.SelectionHandler.TrySelectTarget(allPossibleTarget))
        this.SetTargetedCombatant(allPossibleTarget);
      if (targetedCombatant == this.TargetedCombatant)
        return false;
      this.HUD.SelectionHandler.ClearInfoTarget();
      return true;
    }
    public SelectionStateActiveProbeArc(
      CombatGameState Combat,
      CombatHUD HUD,
      CombatHUDActionButton FromButton, AbstractActor actor)
      : base(Combat, HUD, FromButton, actor) {
      this.SelectionType = SelectionType.ActiveProbe;
      this.PriorityLevel = SelectionPriorityLevel.BasicCommand;
      this.targetedActors = new List<AbstractActor>();
      this.SelectedActor = actor;
      if (Enum.TryParse<Settings.ArcDecalTexture>(FromButton.Ability.Def.StringParam2, out var incArc)) {
        this.arc = incArc;
      }
    }
    protected Vector3 targetPosition { get; set; }
    protected List<AbstractActor> targetedActors { get; set; }
    public override bool ProcessLeftClick(Vector3 worldPos) {
      try {
        if (this.positionLocked)
          return false;
        this.targetPosition = this.GetValidTargetPosition(worldPos);
        this.RefreshPossibleTargets();
        this.GetTargetedActors(this.targetPosition, (float)this.FromButton.Ability.Def.IntParam1);
        this.positionLocked = true;
        //this.ShowFireButton(this.FireMode, this.FireButtonString);
        if (this.SelectedActor.GameRep != null) {
          MechRepresentation gameRep = this.SelectedActor.GameRep as MechRepresentation;
          if (gameRep != null) {
            gameRep.ToggleRandomIdles(false);
            this.SelectedActor.GameRep.FacePoint(true, this.targetPosition, false, 0.5f, -1, -1, false, (GameRepresentation.RotationCompleteDelegate)null);
          }
        }
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
      return true;
    }
    public override bool ProcessClickedCombatant(ICombatant actor) => this.ProcessLeftClick(CameraControl.Instance.ScreenToGroundPosition);
    public override bool CanBackOut => this.positionLocked;
    public override void BackOut() {
      if (this.positionLocked) {
        this.HideFireButton(false);
        positionLocked = false;
        this.SetTargetHighlights(false);
        this.SetTargetMortarIndicators(false);
        targetedActors.Clear();
      }
    }
    public override void ProcessMousePos(Vector3 worldPos) {
      if (this.hasActivated)
        return;
      float raduis = (float)this.FromButton.Ability.Def.IntParam1;
      worldPos = this.GetValidTargetPosition(worldPos);
      if (this.positionLocked) {
        TargetArcReticle.Instance?.UpdatePosition(this.SelectedActor.CurrentPosition, this.targetPosition, raduis, this.arc);
      } else {
        TargetArcReticle.Instance?.UpdatePosition(this.SelectedActor.CurrentPosition, worldPos, raduis, this.arc);
      }
      TargetArcReticle.Instance?.Show();
    }
    public override bool CanDeselect {
      get {
        if (!base.CanDeselect)
          return false;
        return this.SelectedActor.HasMovedThisRound || !this.SelectedActor.CanMoveAfterShooting;
      }
    }
    protected virtual bool CreateFiringOrders(string button) {
      if (button != "BTN_FireConfirm") {
        return this.ProcessPressedButton(button);
      }
      MessageCenterMessage activeProbeInvokation = (MessageCenterMessage)new ActiveProbeInvocation(this.SelectedActor, this.targetedActors.ToList<ICombatant>());
      if (activeProbeInvokation == null) {
        Log.Error?.TWL(0, "No invocation created");
        return false;
      }
      TargetArcReticle.Instance?.Hide();
      this.SetTargetHighlights(false);
      this.SetTargetMortarIndicators(false);
      targetedActors.Clear();
      this.hasActivated = true;
      this.HUD.MechWarriorTray.ConfirmAbilities(AbilityDef.ActivationTiming.ConsumedByMovement);
      ReceiveMessageCenterMessage subscriber = (ReceiveMessageCenterMessage)(message => this.Orders = (message as AddSequenceToStackMessage).sequence);
      this.Combat.MessageCenter.AddSubscriber(MessageCenterMessageType.AddSequenceToStackMessage, subscriber);
      this.Combat.MessageCenter.PublishMessage(activeProbeInvokation);
      this.Combat.MessageCenter.RemoveSubscriber(MessageCenterMessageType.AddSequenceToStackMessage, subscriber);
      WeaponRangeIndicators.Instance.HideTargetingLines(this.Combat);
      return true;
    }

    protected Vector3 GetValidTargetPosition(Vector3 worldPos) {
      Vector3 result = new Vector3(worldPos.x, worldPos.y, worldPos.z);
      float radius = (float)this.FromButton.Ability.Def.IntParam1;
      float distance = Vector3.Distance(this.SelectedActor.CurrentPosition, worldPos);
      if (distance > radius) {
        result = this.SelectedActor.CurrentPosition + (worldPos - this.SelectedActor.CurrentPosition).normalized * radius;
      }
      return result;
    }

    protected void GetTargetedActors(Vector3 targetPos, float radius) {
      this.targetedActors.Clear();
      Vector3 targetLookDirection = targetPos - this.SelectedActor.CurrentPosition;
      targetLookDirection.y = 0f;
      float maxAngle = 45f;
      switch (this.arc) {
        case Settings.ArcDecalTexture.arc60: maxAngle = 30f; break;
        case Settings.ArcDecalTexture.arc90: maxAngle = 45f; break;
      }
      Log.Debug?.TWL(0,$"GetTargetedActors {targetPos} targetLookDirection:{targetLookDirection} AllPossibleTargets:{AllPossibleTargets.Count}");
      foreach (AbstractActor unit in this.AllPossibleTargets) {
        PilotableActorRepresentation gameRep = null;
        //if (unit.IsDead) { goto untarget; }
        //if (unit.IsFlaggedForDeath) { goto untarget; }
        //f (Vector3.Distance(this.SelectedActor.CurrentPosition, unit.CurrentPosition) > radius) { goto untarget; }
        Vector3 unitLookDirection = unit.CurrentPosition - this.SelectedActor.CurrentPosition;
        unitLookDirection.y = 0f;
        float angle = Mathf.Abs(NvMath.AngleSigned(targetLookDirection, unitLookDirection, Vector3.up));
        Log.Debug?.WL(1,$"{unit.PilotableActorDef.ChassisID} distance:{Vector3.Distance(this.SelectedActor.CurrentPosition, unit.CurrentPosition)} unitLookDirection:{unitLookDirection} angle:{angle}");
        if (angle > maxAngle) { goto untarget; }
        this.targetedActors.Add(unit);
        unit.GameRep.IsTargeted = true;
        gameRep = unit.GameRep as PilotableActorRepresentation;
        if (gameRep != null && gameRep.AuraReticle != null)
          gameRep.AuraReticle.ToggleMortarIndicator(true);
        continue;
      untarget:
        {
          unit.GameRep.IsTargeted = false;
          gameRep = unit.GameRep as PilotableActorRepresentation;
          if (gameRep != null && gameRep.AuraReticle != null)
            gameRep.AuraReticle.ToggleMortarIndicator(false);
        }
      }
    }

    protected void SetTargetHighlights(bool shouldHighlight) {
      foreach (AbstractActor targetedActor in this.targetedActors)
        targetedActor.GameRep.IsTargeted = shouldHighlight;
    }

    protected void SetTargetMortarIndicators(bool shouldShow) {
      foreach (AbstractActor targetedActor in this.targetedActors) {
        PilotableActorRepresentation gameRep = targetedActor.GameRep as PilotableActorRepresentation;
        if (gameRep != null && gameRep.AuraReticle != null)
          gameRep.AuraReticle.ToggleMortarIndicator(shouldShow);
      }
    }

    public override void OnInactivate() {
      base.OnInactivate();
      int num1 = (int)WwiseManager.PostEvent<AudioEventList_activeProbe>(AudioEventList_activeProbe.activeProbe_priming_off, WwiseManager.GlobalAudioObject);
      int num2 = (int)WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_target_sensor_lock_soft_off, WwiseManager.GlobalAudioObject);
      int num3 = (int)WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_target_sensor_sweep_stop, WwiseManager.GlobalAudioObject);
      TargetArcReticle.Instance.Hide();
      this.SetTargetHighlights(false);
      this.SetTargetMortarIndicators(false);
      if (this.SelectedActor.GameRep == null)
        return;
      MechRepresentation gameRep = this.SelectedActor.GameRep as MechRepresentation;
      if (gameRep == null)
        return;
      gameRep.ToggleRandomIdles(true);
    }
    public override bool ProcessPressedButton(string button) {
      if (!(button == "BTN_FireConfirm"))
        return base.ProcessPressedButton(button);
      if (this.Orders != null)
        return false;
      this.HideFireButton(false);
      return this.CreateFiringOrders(button);
    }
    public override void OnAddToStack() {
      base.OnAddToStack();
      WeaponRangeIndicators.Instance.HideFiringArcs();
      int num1 = (int)WwiseManager.PostEvent<AudioEventList_activeProbe>(AudioEventList_activeProbe.activeProbe_priming_on, WwiseManager.GlobalAudioObject);
      int num2 = (int)WwiseManager.PostEvent<AudioEventList_ui>(AudioEventList_ui.ui_target_sensor_sweep_start, WwiseManager.GlobalAudioObject);
    }
    public virtual void SetTargetedCombatant(ICombatant target) {
    }
    protected virtual void ClearTargetedActor() {
    }
  }

  public class TargetArcReticle : MonoBehaviour {
    private static TargetArcReticle _instance = null;
    public Dictionary<Settings.ArcDecalTexture, Material> arcMaterials = new Dictionary<Settings.ArcDecalTexture, Material>();
    public BTUIDecal decal = null;
    public Settings.ArcDecalTexture currentArc { get; set; } = Settings.ArcDecalTexture.arc60;
    public static TargetArcReticle Instance {
      get {
        if ((UnityEngine.Object)TargetArcReticle._instance == (UnityEngine.Object)null)
          TargetArcReticle._instance = UnityEngine.Object.FindObjectOfType<TargetArcReticle>();
        return TargetArcReticle._instance;
      }
    }
    public void UpdatePosition(Vector3 center, Vector3 target, float radius, Settings.ArcDecalTexture arc) {
      try {
        if (arc != this.currentArc) { this.decal.DecalMaterial = arcMaterials[arc]; this.currentArc = arc; }
        this.transform.position = center;
        decal.transform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
        target.y = center.y;
        Vector3 desiredLookDirection = target - center;
        this.transform.rotation = Quaternion.LookRotation(desiredLookDirection);
        //Log.Debug?.TWL(0, $"TargetArcReticle.Update center:{center} target:{target} arc:{arc} rotation:{this.transform.rotation}");
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
    public void Show() {
      if (decal.gameObject.activeSelf == false) this.decal.gameObject.SetActive(true);
    }
    public void Hide() {
      if (decal.gameObject.activeSelf) this.decal.gameObject.SetActive(false);
    }
    public static void Create(CombatGameState __instance) {
      GameObject src = __instance.DataManager.PooledInstantiate(CombatAuraReticle.PrefabName, BattleTechResourceType.UIModulePrefabs);
      if (src == null) { return; }
      CombatAuraReticle auraReticle = src.GetComponent<CombatAuraReticle>();
      if (auraReticle != null) {
        GameObject go = new GameObject("TargetArcReticle");
        _instance = go.AddComponent<TargetArcReticle>();
        GameObject StartReticle = new GameObject("StartReticle");
        StartReticle.transform.SetParent(go.transform);
        StartReticle.transform.localScale = new Vector3(2f, 2f, 2f);
        GameObject RangeHolder = new GameObject("RangeHolder");
        RangeHolder.transform.SetParent(StartReticle.transform);
        RangeHolder.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        GameObject RangeIndicator = new GameObject("RangeIndicator");
        RangeIndicator.transform.SetParent(RangeHolder.transform);
        RangeIndicator.transform.localScale = new Vector3(1f, 1024f, 1f);
        foreach (var arcTextureName in Core.Settings.arcTextures) {
          if (string.IsNullOrEmpty(arcTextureName.Value)) { continue; }
          if (!__instance.DataManager.Exists(BattleTechResourceType.Texture2D, arcTextureName.Value)) { continue; };
          Material arcMaterial = GameObject.Instantiate(auraReticle.activeProbeMatBright);
          arcMaterial.mainTexture = __instance.DataManager.GetObjectOfType<Texture2D>(arcTextureName.Value, BattleTechResourceType.Texture2D);
          arcMaterial.SetVector("_DecalAffects", new Vector4(1f, 1f, 0f, 0f));
          _instance.arcMaterials.Add(arcTextureName.Key, arcMaterial);
        }
        _instance.decal = GameObject.Instantiate(auraReticle.activeProbeDecal.gameObject).GetComponent<BTUIDecal>();
        _instance.decal.gameObject.transform.SetParent(RangeIndicator.transform);
        _instance.decal.DecalMaterial = _instance.arcMaterials[_instance.currentArc];
        _instance.decal.gameObject.SetActive(false);
      }
      __instance.DataManager.PoolGameObject(CombatAuraReticle.PrefabName, src);
    }
  }
  [HarmonyPatch(typeof(SelectionState), "GetNewSelectionStateByType")]
  public static class SelectionState_GetNewSelectionStateByType {
    public static void Postfix(SelectionType type, CombatGameState Combat, CombatHUD HUD, CombatHUDActionButton FromButton, AbstractActor actor, ref SelectionState __result) {
      try {
        if (type != SelectionType.ActiveProbe) { return; }
        if (FromButton == null) { return; }
        if (FromButton.Ability == null) { return; }
        if (string.IsNullOrEmpty(FromButton.Ability.Def.StringParam2)) { return; }
        Log.Debug?.TWL(0, "SelectionState.GetNewSelectionStateByType SelectionStateActiveProbeArc");
        __result = new SelectionStateActiveProbeArc(Combat, HUD, FromButton, actor);
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

  [HarmonyPatch(typeof(Contract), "RequestConversations")]
  public static class Contract_RequestConversations {
    public static void Postfix(Contract __instance, LoadRequest loadRequest) {
      Log.Debug?.TWL(0, "Contract.RequestConversations");
      foreach (var arcTexture in Core.Settings.arcTextures) {
        Log.Debug?.WL(1, $"{arcTexture.Key} texture:{arcTexture.Value}");
        if (string.IsNullOrEmpty(arcTexture.Value)) { continue; }
        if (!__instance.DataManager.Exists(BattleTechResourceType.Texture2D, arcTexture.Value)) { continue; };
        if (__instance.DataManager.ResourceLocator.EntryByID(arcTexture.Value, BattleTechResourceType.Texture2D) != null) {
          Log.Debug?.WL(2, "exist in manifest but not loaded");
          loadRequest.AddBlindLoadRequest(BattleTechResourceType.Texture2D, arcTexture.Value);
        } else {
          Log.Debug?.WL(2, "not exist in manifest");
        }
      }
    }
  }
  [HarmonyPatch(typeof(CombatHUDInWorldElementMgr))]
  [HarmonyPatch("Init")]
  [HarmonyPatch(MethodType.Normal)]
  [HarmonyPatch(new Type[] { typeof(CombatGameState), typeof(CombatHUD) })]
  public static class CombatTargetingReticle_Create {
    public static Texture2D DeCompress(this Texture2D source) {
      RenderTexture renderTex = RenderTexture.GetTemporary(
                  source.width,
                  source.height,
                  0,
                  RenderTextureFormat.Default,
                  RenderTextureReadWrite.Linear);

      Graphics.Blit(source, renderTex);
      RenderTexture previous = RenderTexture.active;
      RenderTexture.active = renderTex;
      Texture2D readableText = new Texture2D(source.width, source.height);
      readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
      readableText.Apply();
      RenderTexture.active = previous;
      RenderTexture.ReleaseTemporary(renderTex);
      return readableText;
    }
    public static void Postfix(CombatHUDInWorldElementMgr __instance, CombatGameState combat, CombatHUD HUD) {
      try {
        Log.Debug?.TWL(0, "CombatGameState._Init");
        //GameObject go = combat.DataManager.PooledInstantiate(CombatAuraReticle.PrefabName, BattleTechResourceType.UIModulePrefabs);
        //BTUIDecal[] decals = go.GetComponentsInChildren<BTUIDecal>(true);
        //foreach (BTUIDecal decal in decals) {
        //  Log.Debug?.WL(1, $"{decal.DecalMaterial.mainTexture.name}");
        //  File.WriteAllBytes(Path.Combine(Log.BaseDirectory, $"decal_{decal.DecalMaterial.mainTexture.name}.png"), (decal.DecalMaterial.mainTexture as Texture2D).DeCompress().EncodeToPNG());
        //}
        //combat.DataManager.PoolGameObject(CombatAuraReticle.PrefabName, go);
        TargetArcReticle.Create(combat);
      } catch (Exception e) {
        Log.Error?.TWL(0, e.ToString(), true);
      }
    }
  }

}

using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomComponents;

namespace CustomActivatableEquipment {
  public class LinkageStateRecord {
    public List<string> Activate;
    public List<string> Deactivate;
    public LinkageStateRecord() {
      Activate = new List<string>();
      Deactivate = new List<string>();
    }
  }
  public class LinkageRecord {
    public LinkageStateRecord OnActivate;
    public LinkageStateRecord OnDeactivate;
    public LinkageRecord() {
      OnActivate = new LinkageStateRecord();
      OnDeactivate = new LinkageStateRecord();
    }
  }
  public partial class ActivatableComponent {
    public LinkageRecord Linkage { get; set; }
  }
  public static class LinkageHelper {
    public static void LinkageActivate(this MechComponent component, bool isInital) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if(activatable == null) { return; }
      Log.Debug?.TWL(0, "LinkageActivate " + component.defId);
      foreach (string toActivateBtn in activatable.Linkage.OnActivate.Activate) {
        Log.Debug?.WL(1, "searching " + toActivateBtn);
        foreach (MechComponent CompToActivate in component.parent.allComponents) {
          ActivatableComponent tactivatable = CompToActivate.componentDef.GetComponent<ActivatableComponent>();
          if (tactivatable == null) { continue; }
          //if (tactivatable.CanBeactivatedManualy == false) { continue; };
          if (tactivatable.ButtonName != toActivateBtn) { continue; }
          if (ActivatableComponent.isComponentActivated(CompToActivate) == true) { continue; };
          ActivatableComponent.activateComponent(CompToActivate, true, isInital);
        }
      }
      foreach (string toActivateBtn in activatable.Linkage.OnActivate.Deactivate) {
        foreach (MechComponent CompToDeactivate in component.parent.allComponents) {
          ActivatableComponent tactivatable = CompToDeactivate.componentDef.GetComponent<ActivatableComponent>();
          if (tactivatable == null) { continue; }
          //if (tactivatable.CanBeactivatedManualy == false) { continue; };
          if (tactivatable.ButtonName != toActivateBtn) { continue; }
          if (ActivatableComponent.isComponentActivated(CompToDeactivate) == false) { continue; };
          ActivatableComponent.deactivateComponent(CompToDeactivate);
        }
      }
    }
    public static void LinkageDectivate(this MechComponent component, bool isInital) {
      ActivatableComponent activatable = component.componentDef.GetComponent<ActivatableComponent>();
      if (activatable == null) { return; }
      Log.Debug?.TWL(0,"LinkageDectivate "+component.defId);
      foreach (string toActivateBtn in activatable.Linkage.OnDeactivate.Activate) {
        foreach (MechComponent CompToActivate in component.parent.allComponents) {
          ActivatableComponent tactivatable = CompToActivate.componentDef.GetComponent<ActivatableComponent>();
          if (tactivatable == null) { continue; }
          //if (tactivatable.CanBeactivatedManualy == false) { continue; };
          if (tactivatable.ButtonName != toActivateBtn) { continue; }
          if (ActivatableComponent.isComponentActivated(CompToActivate) == true) { continue; };
          ActivatableComponent.activateComponent(CompToActivate, true, isInital);
        }
      }
      foreach (string toActivateBtn in activatable.Linkage.OnDeactivate.Deactivate) {
        foreach (MechComponent CompToDeactivate in component.parent.allComponents) {
          ActivatableComponent tactivatable = CompToDeactivate.componentDef.GetComponent<ActivatableComponent>();
          if (tactivatable == null) { continue; }
          //if (tactivatable.CanBeactivatedManualy == false) { continue; };
          if (tactivatable.ButtonName != toActivateBtn) { continue; }
          if (ActivatableComponent.isComponentActivated(CompToDeactivate) == false) { continue; };
          ActivatableComponent.deactivateComponent(CompToDeactivate);
        }
      }
    }
  }
}

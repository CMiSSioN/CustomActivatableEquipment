using BattleTech;
using CustomComponents;

namespace CustomActivatableEquipment {
  [CustomComponent("AutoReplentish")]
  public class AutoReplentish : SimpleCustomComponent {
    public int ReplentishAmount { get; set; } = 0;
  }
  public static class AutoReplentishHelper {
    public static void Replentish(this AbstractActor unit) {
      foreach(var component in unit.allComponents) {
        if(component.IsFunctional == false) { continue; }
        if (component.componentType != ComponentType.AmmunitionBox) { continue; }
        AutoReplentish autoReplentish = component.componentDef.GetComponent<AutoReplentish>();
        if (autoReplentish == null) { continue; }
        AmmunitionBox box = component as AmmunitionBox;
        if (box == null) { continue; }
        Statistic CurrentAmmo = box.StatCollection.GetOrCreateStatisic<int>("CurrentAmmo", box.ammunitionBoxDef.Capacity);
        int newCurrentAmmo = CurrentAmmo.Value<int>() + autoReplentish.ReplentishAmount;
        if (newCurrentAmmo > box.ammunitionBoxDef.Capacity) { newCurrentAmmo = box.ammunitionBoxDef.Capacity; }
        if (CurrentAmmo.Value<int>() != newCurrentAmmo) {
          CurrentAmmo.SetValue<int>(newCurrentAmmo);
        }
      }
    }
  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using Harmony;
using HBS;
using CustomComponents;

namespace CustomActivatableEquipment.DamageHelpers
{
    [HarmonyPatch(typeof(Mech), "AddExternalHeat")]
    public class Mech_AddExternalHeat_Patch
    {
        internal static int heatDamage;

        private static void Postfix(int amt)
        {
            heatDamage += amt;
        }
    }

    // for reasons, AttackStackSequence et al have no?? way to tell if support weapons are firing so we stash a global
    [HarmonyPatch(typeof(MechMeleeSequence), "FireWeapons")]
    public static class MechMeleeSequence_FireWeapons_Patch
    {
        internal static bool meleeHasSupportWeapons;

        public static void Postfix(MechMeleeSequence __instance)
        {
            meleeHasSupportWeapons =
                Traverse.Create(__instance).Field("CAErequestedWeapons").GetValue<List<Weapon>>().Count > 0;
        }
    }

    // save the pre-attack condition
    [HarmonyPatch(typeof(AttackStackSequence), "OnAttackBegin")]
    public static class AttackStackSequence_OnAttackBegin_Patch
    {
        internal static float armorBeforeAttack;
        internal static float structureBeforeAttack;

        public static void Prefix(AttackStackSequence __instance)
        {
            if (__instance.directorSequences == null || __instance.directorSequences.Count == 0)
            {
                return;
            }

            var target = __instance.directorSequences[0].chosenTarget;
            armorBeforeAttack = target.SummaryArmorCurrent;
            structureBeforeAttack = target.SummaryStructureCurrent;
        }
    }

    [HarmonyPatch(typeof(AttackStackSequence), "OnAttackComplete")]
    public static class AttackStackSequence_OnAttackComplete_Patch
    {
        private static readonly Stopwatch stopwatch = new Stopwatch();

        public static void Prefix(AttackStackSequence __instance, MessageCenterMessage message)
        {
            stopwatch.Restart();
            var attackCompleteMessage = (AttackCompleteMessage)message;
            if (attackCompleteMessage == null || attackCompleteMessage.stackItemUID != __instance.SequenceGUID)
            {
                return;
            }

            // can't do stuff with buildings or Vehicles
            if (!(__instance.directorSequences[0].chosenTarget is Mech))
            {
                Log.LogWrite("Not a mech.");
                return;
            }

            if (__instance.directorSequences[0].chosenTarget?.GUID == null)
            {
                return;
            }

            var director = __instance.directorSequences;
            if (director == null) return;

            Log.LogWrite(new string('═', 46));
            Log.LogWrite($"{director[0].attacker.DisplayName} attacks {director[0].chosenTarget.DisplayName}");

            AbstractActor defender = null;
            switch (director[0]?.chosenTarget)
            {
                case Mech _:
                    defender = (Mech)director[0]?.chosenTarget;
                    break;
            }

            // a building , vehicle or turret?
            if (defender == null)
            {
                Log.LogWrite("Not a mech.");
                return;
            }

            if (defender.IsDead || defender.IsFlaggedForDeath || defender.IsShutDown)
            {
                Log.LogWrite("defender dead or shutdown.");//<check> do we need to handle incoming damage when shutdown on startup?
                return;
            }

            DamageHelper.ActivateComponentsBasedOnDamage(defender, attackCompleteMessage.attackSequence);

        }

        public class DamageHelper
        {

            // values for combining melee with support weapon fire
            private static float initialArmorMelee = 0f;
            private static float initialStructureMelee = 0f;
            private static float armorDamageMelee = 0f;
            private static float structureDamageMelee = 0f;
            private static bool hadMeleeAttack = false;
            internal static float totalDamage = 0f;

            internal static float MaxArmorForLocation(Mech mech, int Location)
            {
                if (mech != null)
                {
                    Statistic stat = mech.StatCollection.GetStatistic(mech.GetStringForArmorLocation((ArmorLocation)Location));
                    if (stat == null)
                    {
                        Log.LogWrite($"Can't get armor stat  { mech.DisplayName } location:{ Location.ToString()}");
                        return 0;
                    }
                    //Log.LogWrite($"armor stat  { mech.DisplayName } location:{ Location.ToString()} :{stat.DefaultValue<float>()}");
                    return stat.DefaultValue<float>();
                }
                Log.LogWrite($"Mech null");
                return 0;
            }
            internal static float MaxStructureForLocation(Mech mech, int Location)
            {
                if (mech != null)
                {
                    Statistic stat = mech.StatCollection.GetStatistic(mech.GetStringForStructureLocation((ChassisLocations)Location));
                    if (stat == null)
                    {
                        Log.LogWrite($"Can't get structure stat  { mech.DisplayName } location:{ Location.ToString()}");
                        return 0;
                    }
                    //Log.LogWrite($"structure stat  { mech.DisplayName } location:{ Location.ToString()}:{stat.DefaultValue<float>()}");
                    return stat.DefaultValue<float>();
                }
                Log.LogWrite($"Mech null");
                return 0;
            }

            public static void ActivateComponentsBasedOnDamage(AbstractActor defender, AttackDirector.AttackSequence attackSequence)
            {
                bool gotdamagevalues = false;
                float armorDamage = 0f;
                float structureDamage = 0f;
                int heatDamage = 0;

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

                foreach (MechComponent component in defender.allComponents)
                {
                    ActivatableComponent tactivatable = component.componentDef.GetComponent<ActivatableComponent>();
                    if (tactivatable == null) { continue; }
                    if (ActivatableComponent.canBeDamageActivated(component) == false) { continue; };
                    if (!gotdamagevalues)
                    {//have atleast 1 damage activateable component get the damage values
                        var id = attackSequence.chosenTarget.GUID;
                        if (!attackSequence.GetAttackDidDamage(id) && !hadMeleeAttack)
                        {
                            Log.LogWrite("No damage");
                            return;
                        }

                        // Account for melee attacks so separate activate checks not triggered.
                        if (attackSequence.isMelee && MechMeleeSequence_FireWeapons_Patch.meleeHasSupportWeapons)
                        {
                            initialArmorMelee = AttackStackSequence_OnAttackBegin_Patch.armorBeforeAttack;
                            initialStructureMelee = AttackStackSequence_OnAttackBegin_Patch.structureBeforeAttack;
                            armorDamageMelee = attackSequence.GetArmorDamageDealt(id);
                            structureDamageMelee = attackSequence.GetStructureDamageDealt(id);
                            hadMeleeAttack = true;
                            Log.LogWrite("Stashing melee damage for support weapon firing");
                            return;
                        }

                        var previousArmor = AttackStackSequence_OnAttackBegin_Patch.armorBeforeAttack;
                        var previousStructure = AttackStackSequence_OnAttackBegin_Patch.structureBeforeAttack;

                        if (hadMeleeAttack)
                        {
                            Log.LogWrite("Adding stashed melee damage");
                            previousArmor = initialArmorMelee;
                            previousStructure = initialStructureMelee;
                        }
                        else
                        {
                            armorDamageMelee = 0;
                            structureDamageMelee = 0;
                        }

                        armorDamage = attackSequence.GetArmorDamageDealt(id) + armorDamageMelee;
                        structureDamage = attackSequence.GetStructureDamageDealt(id) + structureDamageMelee;
                        heatDamage = Mech_AddExternalHeat_Patch.heatDamage;
                        totalDamage = armorDamage + structureDamage;

                        // clear melee values
                        initialArmorMelee = 0;
                        initialStructureMelee = 0;
                        armorDamageMelee = 0;
                        structureDamageMelee = 0;
                        hadMeleeAttack = false;
                        gotdamagevalues = true;
                        if (defender is Mech mech)
                        {
                            Log.LogWrite($"Damage >>> A: {armorDamage:F3} S: {structureDamage:F3} H: {heatDamage}");
                            Log.LogWrite(new string('-', 46));
                            Log.LogWrite($"{"Location",-20} | {"Armor Damage",12} | {"Structure Damage",12}");
                            Log.LogWrite(new string('-', 46));
                            Head_s = MaxStructureForLocation(mech, (int)ChassisLocations.Head) - mech.HeadStructure;
                            Head_a = MaxArmorForLocation(mech, (int)ChassisLocations.Head) - mech.HeadArmor;
                            Log.LogWrite($"{ChassisLocations.Head.ToString(),-20} | {Head_a,12:F3} | {Head_s,12:F3}");
                            CenterTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.CenterTorso) - mech.CenterTorsoStructure;
                            CenterTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.CenterTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.CenterTorsoRear) - mech.CenterTorsoFrontArmor - mech.CenterTorsoRearArmor;
                            Log.LogWrite($"{ChassisLocations.CenterTorso.ToString(),-20} |  {CenterTorso_a,12:F3} | {CenterTorso_s,12:F3}");
                            LeftTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftTorso) - mech.LeftTorsoStructure;
                            LeftTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.LeftTorsoRear) - mech.LeftTorsoFrontArmor - mech.LeftTorsoRearArmor;
                            Log.LogWrite($"{ChassisLocations.LeftTorso.ToString(),-20} |  {LeftTorso_a,12:F3} | {LeftTorso_s,12:F3}");
                            RightTorso_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightTorso) - mech.RightTorsoStructure;
                            RightTorso_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightTorso) + MaxArmorForLocation(mech, (int)ArmorLocation.RightTorsoRear) - mech.RightTorsoFrontArmor - mech.RightTorsoRearArmor;
                            Log.LogWrite($"{ChassisLocations.RightTorso.ToString(),-20} |  {RightTorso_a,12:F3} | {RightTorso_s,12:F3}");
                            LeftLeg_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftLeg) - mech.LeftLegStructure;
                            LeftLeg_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftLeg) - mech.LeftLegArmor;
                            Log.LogWrite($"{ChassisLocations.LeftLeg.ToString(),-20} |  {LeftLeg_a,12:F3} | {LeftLeg_s,12:F3}");
                            RightLeg_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightLeg) - mech.RightLegStructure;
                            RightLeg_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightLeg) - mech.RightLegArmor;
                            Log.LogWrite($"{ChassisLocations.RightLeg.ToString(),-20} |  {RightLeg_a,12:F3} | {RightLeg_s,12:F3}");
                            LeftArm_s = MaxStructureForLocation(mech, (int)ChassisLocations.LeftArm) - mech.LeftArmStructure;
                            LeftArm_a = MaxArmorForLocation(mech, (int)ArmorLocation.LeftArm) - mech.LeftArmArmor;
                            Log.LogWrite($"{ChassisLocations.LeftArm.ToString(),-20} |  {LeftArm_a,12:F3} | {LeftArm_s,12:F3}");
                            RightArm_s = MaxStructureForLocation(mech, (int)ChassisLocations.RightArm) - mech.RightArmStructure;
                            RightArm_a = MaxArmorForLocation(mech, (int)ArmorLocation.RightArm) - mech.RightArmArmor;
                            Log.LogWrite($"{ChassisLocations.RightArm.ToString(),-20} |  {RightArm_a,12:F3} | {RightArm_s,12:F3}");

                            Log.LogWrite($"{ChassisLocations.Torso.ToString(),-20} |  {CenterTorso_a + LeftTorso_a + RightTorso_a,12:F3} | {CenterTorso_s + LeftTorso_s + RightTorso_s,12:F3}");
                            Log.LogWrite($"{ChassisLocations.Legs.ToString(),-20} |  {LeftLeg_a + RightLeg_a,12:F3} | { LeftLeg_s + RightLeg_s,12:F3}");
                            Log.LogWrite($"{ChassisLocations.Arms.ToString(),-20} |  {LeftArm_a + RightArm_a,12:F3} | { LeftArm_s + RightArm_s,12:F3}");
                            Log.LogWrite($"{ChassisLocations.All.ToString(),-20} |  {CenterTorso_a + LeftTorso_a + RightTorso_a + LeftLeg_a + RightLeg_a + LeftArm_a + RightArm_a,12:F3} | {CenterTorso_s + LeftTorso_s + RightTorso_s + LeftLeg_s + RightLeg_s + LeftArm_s + RightArm_s,12:F3}");
                        }
                        else
                        {
                            Log.LogWrite($"Not a mech, somethings broken");
                        }

                    }
                    // we stop trying to activate the component if any of these return true i.e activated;
                    //ignore the damage from this hit and use the current damage levels.
                    //Not handling ChassisLocation MainBody as i dont know what locations it covers.
                    if (ActivatableComponent.ActivateOnIncomingHeat(component, heatDamage) ||
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
                      )
                    {
                        continue;
                    }


                }

            }
        }
    }
}


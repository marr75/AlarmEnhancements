using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UniLinq;
using UnityEngine;

namespace AlarmEnhancements
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class AlarmEnhancements : MonoBehaviour
    {
        void Start() {
            var harmony = new Harmony("com.marr75.alarmEnhancements");
            harmony.PatchAll();
            Debug.Log("Alarm Enhancements applied patches.");
        }
    }

    [HarmonyPatch]
    class ManeuverAlarmTitlePatch {

        static IEnumerable<MethodBase> GetMethods() {
            var types = new[] {
                typeof(AlarmTypeManeuver),
                typeof(AlarmTypeApoapsis),
                typeof(AlarmTypePeriapsis),
                typeof(AlarmTypeSOI)
            };
            var methods = types.Select(t => t.GetMethod("GetDefaultTitle"));
            return methods;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AlarmTypeManeuver), "GetDefaultTitle")]
        static void ManeuverPostFix(AlarmTypeManeuver __instance, ref string __result) {
            __result = GetDefaultTitle(__result);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AlarmTypeApoapsis), "GetDefaultTitle")]
        static void ApoapsisPostFix(AlarmTypeApoapsis __instance, ref string __result) {
            __result = GetDefaultTitle(__result);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AlarmTypePeriapsis), "GetDefaultTitle")]
        static void PeriapsisPostFix(AlarmTypePeriapsis __instance, ref string __result) {
            __result = GetDefaultTitle(__result);
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AlarmTypeSOI), "GetDefaultTitle")]
        static void SOIPostFix(AlarmTypeSOI __instance, ref string __result) {
            __result = GetDefaultTitle(__result);
        }

        static string GetDefaultTitle(string localizedAlarmTypeName) {
            if (!AlarmClockScenario.IsVesselAvailable) {
                return localizedAlarmTypeName;
            }
            else {
                var vessel = AlarmClockScenario.AvailableVessel;
                var vesselName = vessel.vesselName;
                return $"{vesselName} {localizedAlarmTypeName}";
            }
        }
    }
}
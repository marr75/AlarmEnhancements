using System;
using System.Collections.Generic;
using KSPAchievements;
using UniLinq;
using UnityEngine;

namespace AlarmEnhancements
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AlarmEnhancements : MonoBehaviour
    {
        private void Start()
        {
            InvokeRepeating(nameof(RunCoroutine), 2.0f, 0.5f);
            InvokeRepeating(nameof(CheckForAtmosphericReentry), 2.0f, 1.0f);
            GameEvents.onAlarmAdded.Add(OnAlarmAdded);
            GameEvents.onManeuverAdded.Add(UpdateManeuvers);
            GameEvents.onManeuverRemoved.Add(UpdateManeuvers);
        }

        private void RemoveSOIAlarm(Vessel v)
        {
            var alarms = AlarmClockScenario.Instance.alarms.Values;
            for (int i = 0; i < alarms.Count; i++)
            {
                AlarmTypeSOI al = alarms.ElementAt(i) as AlarmTypeSOI;
                if (al == null) continue;
                if (al.vesselId != v.persistentId) continue;
                AlarmClockScenario.DeleteAlarm(al);
            }
        }
        
        private void RemoveManeuverAlarms(Vessel v)
        {
            var alarms = AlarmClockScenario.Instance.alarms.Values;
            for (int i = 0; i < alarms.Count; i++)
            {
                AlarmTypeManeuver al = alarms.ElementAt(i) as AlarmTypeManeuver;
                if (al == null) continue;
                if (al.vesselId != v.persistentId) continue;
                AlarmClockScenario.DeleteAlarm(al);
            }
        }

        private void UpdateManeuvers(Vessel v, PatchedConicSolver solver)
        {
            if (FlightGlobals.ActiveVessel == null) return;
            if (v != FlightGlobals.ActiveVessel) return;
            if (solver == null || solver.maneuverNodes == null) return;
            if (solver.maneuverNodes.Count == 0)
            {
                RemoveManeuverAlarms(FlightGlobals.ActiveVessel);
                return;
            }
            if (!ShouldSetManeuverAlarm(solver.maneuverNodes[0], v.persistentId)) return;
            AlarmTypeManeuver alarmToSet = new AlarmTypeManeuver
            {
                title = v.vesselName,
                description = v.vesselName + " approaching Maneuver",
                actions =
                {
                    warp = GetAlarmAction("Maneuver"),
                    message = AlarmActions.MessageEnum.Yes
                },
                ut = solver.maneuverNodes[0].UT,
                Maneuver = solver.maneuverNodes[0],
                vesselId = FlightGlobals.ActiveVessel.persistentId
            };
            AlarmClockScenario.AddAlarm(alarmToSet);

        }

        private bool ShouldSetManeuverAlarm(ManeuverNode m, uint vesselId)
        {
            var alarms = AlarmClockScenario.Instance.alarms.Values;
            for (int i = 0; i < alarms.Count; i++)
            {
                AlarmTypeManeuver al = alarms.ElementAt(i) as AlarmTypeManeuver;
                if (al == null) continue;
                if (al.vesselId != FlightGlobals.ActiveVessel.persistentId) continue;
                return false;
            }
            return true;
        }

        private void OnAlarmAdded(AlarmTypeBase alarm)
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().AutoRenameAlarms) return;
            if (alarm.Vessel == null) return;
            alarm.title = GetVesselName(alarm.vesselId);
            //Alarm Clock App doesn't seem to update titles on the fly and only when the list is refreshed, so let's force it to refresh. 
            Invoke(nameof(CreateFakeAlarm), 0.5f);
        }

        private void CreateFakeAlarm()
        {
            AlarmTypeRaw alarmToSet = new AlarmTypeRaw
            {
                title = "This is a fake alarm",
                description = "If you are seeing this something has gone seriously wrong. Please report this error on the AlarmEnhancements forum thread",
                actions =
                {
                    warp = AlarmActions.WarpEnum.DoNothing,
                    message = AlarmActions.MessageEnum.No,
                    deleteWhenDone = true
                },
                ut = Planetarium.GetUniversalTime()+99999,
            };
            AlarmClockScenario.AddAlarm(alarmToSet);
            AlarmClockScenario.DeleteAlarm(alarmToSet);
        }
        private string GetVesselName(uint alarmVesselId)
        {
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vessel v = FlightGlobals.Vessels.ElementAt(i);
                if (v.persistentId != alarmVesselId) continue;
                return v.vesselName;
            }

            return "ERROR";
        }

        private void RunCoroutine()
        {
            if (FlightGlobals.ActiveVessel == null) return;
            if (FlightGlobals.ActiveVessel.orbit == null) return;
            if(FlightGlobals.ActiveVessel.patchedConicRenderer == null) return;
            if(HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().AutoSoiAlarms)CheckForSoiChanges();
        }


        private void CheckForAtmosphericReentry()
        {
            if (!HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().AutoAtmoAlarms) return;
            if (FlightGlobals.ActiveVessel == null) return;
            if (!FlightGlobals.ActiveVessel.mainBody.atmosphere) return;
            if (FlightGlobals.ActiveVessel.orbit == null) return;
            if (FlightGlobals.ActiveVessel.altitude < FlightGlobals.ActiveVessel.mainBody.atmosphereDepth) return;
            if (FlightGlobals.ActiveVessel.situation == Vessel.Situations.ESCAPING) return;
            if (FlightGlobals.ActiveVessel.orbit.PeA > FlightGlobals.ActiveVessel.mainBody.atmosphereDepth)
            {
                ClearAtmoAlarms();
                return;
            }
            double latestTime = FlightGlobals.ActiveVessel.orbit.timeToPe + Planetarium.GetUniversalTime();
            double alarmTime = Planetarium.GetUniversalTime();
            for (alarmTime = Planetarium.GetUniversalTime(); alarmTime < latestTime; alarmTime++)
            {
                if (FlightGlobals.ActiveVessel.orbit.GetRadiusAtUT(alarmTime)-FlightGlobals.ActiveVessel.mainBody.Radius < FlightGlobals.ActiveVessel.mainBody.atmosphereDepth) break;
            }
            alarmTime -= HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().AtmoMargin;
            if (!RefreshAlarm(alarmTime)) return;
            if (alarmTime - Planetarium.GetUniversalTime() < HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().AtmoMargin)  return;
            AlarmTypeRaw alarmToSet = new AlarmTypeRaw
            {
                title = FlightGlobals.ActiveVessel.vesselName,
                description = "Entering atmosphere of" + FlightGlobals.ActiveVessel.mainBody.bodyName,
                actions =
                {
                    warp = GetAlarmAction("Atmosphere"),
                    message = AlarmActions.MessageEnum.Yes
                },
                ut = alarmTime,
                vesselId = FlightGlobals.ActiveVessel.persistentId
            };
            AlarmClockScenario.AddAlarm(alarmToSet);
        }

        private void ClearAtmoAlarms()
        {
            var alarms = AlarmClockScenario.Instance.alarms.Values;
            for (int i = 0; i < alarms.Count; i++)
            {
                AlarmTypeRaw al = alarms.ElementAt(i) as AlarmTypeRaw;
                if (al == null) continue;
                if (al.vesselId != FlightGlobals.ActiveVessel.persistentId) continue;
                if (al.description != "Entering atmosphere of" + FlightGlobals.ActiveVessel.mainBody.bodyName) continue;
                AlarmClockScenario.DeleteAlarm(al);
            }
        }

        private bool RefreshAlarm(double alarmTime)
        {
            var alarms = AlarmClockScenario.Instance.alarms.Values;
            for (int i = 0; i < alarms.Count; i++)
            {
                AlarmTypeRaw al = alarms.ElementAt(i) as AlarmTypeRaw;
                if (al == null) continue;
                if (al.vesselId != FlightGlobals.ActiveVessel.persistentId) continue; 
                if (al.description != "Entering atmosphere of" + FlightGlobals.ActiveVessel.mainBody.bodyName) continue;
                if (CloseEnough(al.ut, alarmTime)) return false;
                AlarmClockScenario.DeleteAlarm(al);
                return true;
            }
            return true;
        }

        private void CheckForSoiChanges()
        {
            if (FlightGlobals.ActiveVessel.orbit == null) return;
            if (FlightGlobals.ActiveVessel.orbit.patchEndTransition != Orbit.PatchTransitionType.ESCAPE && FlightGlobals.ActiveVessel.orbit.patchEndTransition != Orbit.PatchTransitionType.ENCOUNTER && TimeWarp.CurrentRate == 1) RemoveSOIAlarm(FlightGlobals.ActiveVessel);
            if (ShouldSetSOIAlarm(FlightGlobals.ActiveVessel)) AddSOIAlarm(FlightGlobals.ActiveVessel.vesselName, FlightGlobals.ActiveVessel.orbit.nextPatch.UTsoi);
        }

        private bool ShouldSetSOIAlarm(Vessel v)
        {
            if (v.orbit.patchEndTransition != Orbit.PatchTransitionType.ESCAPE && v.orbit.patchEndTransition != Orbit.PatchTransitionType.ENCOUNTER) return false;
            if (v.orbit.nextPatch.StartUT - Planetarium.GetUniversalTime() < HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().SoiMargin) return false;
            var alarms = AlarmClockScenario.Instance.alarms.Values;
            for (int i = 0; i < alarms.Count; i++)
            {
                AlarmTypeSOI al = alarms.ElementAt(i) as AlarmTypeSOI;
                if (al == null) continue;
                if (al.vesselId != v.persistentId) continue;
                return false;
            }
            return true;
        }


        private bool CloseEnough(double firstValue, double secondValue)
        {
            return Math.Abs(firstValue - secondValue) < 1;
        }

        private void AddSOIAlarm(string alarmTitle, double alarmTime)
        {
            AlarmTypeSOI alarmToSet = new AlarmTypeSOI
            {
                title = alarmTitle,
                description = alarmTitle + " approaching new SOI",
                actions =
                {
                    warp = GetAlarmAction("SOI"),
                    message = AlarmActions.MessageEnum.Yes
                },
                ut = alarmTime + Planetarium.GetUniversalTime(),
                marginEntry = HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().SoiMargin,
                vesselId = FlightGlobals.ActiveVessel.persistentId
            };
            AlarmClockScenario.AddAlarm(alarmToSet);


        }

        private AlarmActions.WarpEnum GetAlarmAction(string situation)
        {
            int selectedEnum;
            switch (situation)
            {
                case "SOI":
                    selectedEnum = HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().SoiAction;
                    break;
                case "Maneuver":
                    selectedEnum = HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().ManeuverAction;
                    break;
                case "Atmosphere":
                    selectedEnum = HighLogic.CurrentGame.Parameters.CustomParams<AlarmEnhancementSettings>().AtmoAction;
                    break;
                default:
                    selectedEnum = 1;
                    break;
            }

            switch (selectedEnum)
            {
                case 0:
                    return AlarmActions.WarpEnum.DoNothing;
                case 1:
                    return AlarmActions.WarpEnum.KillWarp;
                case 2:
                    return AlarmActions.WarpEnum.PauseGame;
            }
            return AlarmActions.WarpEnum.DoNothing;
        }

        private void OnDisable()
        {
            GameEvents.onManeuverAdded.Remove(UpdateManeuvers);
            GameEvents.onManeuverRemoved.Remove(UpdateManeuvers);
            GameEvents.onAlarmAdded.Remove(OnAlarmAdded);
        }
    }
}
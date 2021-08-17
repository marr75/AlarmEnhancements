namespace AlarmEnhancements
{
    public class AlarmEnhancementSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Alarm Enhancements";

        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;

        public override string Section => "Alarm Enhancements";

        public override string DisplaySection => Section;

        public override int SectionOrder => 1;

        public override bool HasPresets => false;
        
        public bool AutoPersistance = true;
        
        public bool NewGameOnly = false;
        
        [GameParameters.CustomParameterUI("Auto Rename Alarms", toolTip = "Automatically rename alarms to match the vessel name")]
        public bool AutoRenameAlarms = true;

        [GameParameters.CustomParameterUI("Automatic SOI Alarms")]
        public bool AutoSoiAlarms = true;

        [GameParameters.CustomIntParameterUI("SOI Alarm Margin", minValue = 5, maxValue = 600, stepSize = 5, toolTip = "Don't set auto alarm if within this many seconds of the event")]
        public int SoiMargin = 60;
        
        [GameParameters.CustomIntParameterUI("SOI Alarm Action", minValue = 0, maxValue = 2, stepSize = 1, toolTip = "0 = Do Nothing, 1 = Kill Warp, 2 = Pause Game")]
        public int SoiAction = 1;
        
        [GameParameters.CustomParameterUI("Automatic Maneuver Alarms")]
        public bool AutoManeuverAlarms = true;

        [GameParameters.CustomIntParameterUI("Maneuver Alarm Margin", minValue = 60, maxValue = 600, stepSize = 5, toolTip = "Don't set auto alarm if within this many seconds of the event")]
        public int ManeuverMargin = 60;

        [GameParameters.CustomIntParameterUI("Maneuver Alarm Action", minValue = 0, maxValue = 2, stepSize = 1, toolTip = "0 = Do Nothing, 1 = Kill Warp, 2 = Pause Game")]
        public int ManeuverAction = 1;
        
        [GameParameters.CustomParameterUI("Automatic Reentry Alarms")]
        public bool AutoAtmoAlarms = true;

        [GameParameters.CustomIntParameterUI("Reentry Alarm Margin", minValue = 5, maxValue = 600, stepSize = 5, toolTip = "Don't set auto alarm if within this many seconds of the event")]
        public int AtmoMargin = 300;

        [GameParameters.CustomIntParameterUI("Reentry Alarm Action", minValue = 0, maxValue = 2, stepSize = 1, toolTip = "0 = Do Nothing, 1 = Kill Warp, 2 = Pause Game")]
        public int AtmoAction = 1;


    }
}
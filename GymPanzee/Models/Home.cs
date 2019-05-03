using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GymPanzee.Models
{
    public class Rootobject
    {
        public int IDjson { get; set; }
        public string ExerciseCategoryjson { get; set; }
        public string Typejson { get; set; }
    }

    public class Summary
    {
        public List<string> UpperBody { get; set; }
        public List<string> LowerBody { get; set; }
        public List<SummaryActivity> SActivity {get; set;}
    }

    public class SummaryActivity
    {
        public string ExerciseMachineValue { get; set; }
        public int? Reps { get; set; }
        public int? Weights { get; set; }
        public int? Sets { get; set; }
        public string Other { get; set; }
        public string BodyHalf { get; set; }
        public string BodyPartTarget { get; set; }
    }

    public class ExcerciseMachineChartModel
    {
        public string label { get; set; }
        public int value { get; set; }
        public string tooltext { get; set; }
        //public string anchorBorderColor { get; set; }
        //public string anchorBgColor { get; set; }
        //public string anchorHoverScale { get; set; }
    }

    public class ChartSummary
    {
        public List<BodyHalfPieChart> piechart {get;set;}
        public List<TargetBodyPartRadarChart> upperbodylabel { get; set; }
        public List<TargetBodyPartCount> upperbodycount { get; set; }
        public List<TargetBodyPartRadarChart> lowerbodylabel { get; set; }
        public List<TargetBodyPartCount> lowerbodycount { get; set; }
    }

    public class BodyHalfPieChart
    {
        public string label { get; set; }
        public int value { get; set; }
    }

    public class TargetBodyPartRadarChart
    {
        public string label { get; set; }
    }

    public class TargetBodyPartCount
    {
        public string value { get; set; }
    }
}
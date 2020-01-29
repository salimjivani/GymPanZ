using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using GymPanzee.Models;
using System.Web.Services;
using System.Web.Script.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Core;

namespace GymPanzee.Controllers
{
    
    public class HomeController : Controller
    {
        GympanzeeDBDataContext _dt = new GympanzeeDBDataContext();

        public void InserttheActivity()
        {
            Activity act = new Activity();
            act.Date = DateTime.Now;
            act.UserID = 1;
            act.FacilityID = 1;
            act.ExerciseMachineID = 18;
            act.Reps = 3;
            act.Weights = 1999;
            act.Time = 5;
            act.Sets = 10;
            act.Other = "tryingsomethingnew";
            _dt.Activities.InsertOnSubmit(act);
            _dt.SubmitChanges();
            Console.WriteLine("worked");
        }
       
        public ActionResult Index()
        {
            if (Request.Cookies["username"] != null && Request.QueryString["exercisemachineid"] != null && Request.QueryString["facilityid"] != null)
            {
                return RedirectToAction("Activity", new { exercisemachineid = Request.QueryString["exercisemachineid"], username = Request.Cookies["username"].Value, facility = Request.QueryString["facilityid"] });
            }
            else
            {
                //InserttheActivity();
                return View();
            }

            //Request.Cookies["username"].Expires = DateTime.Now.AddDays(-1);

        }

        [HttpPost]
        public JsonResult ExerciseMachines(int exercisemachineid)
        {

            var exercisecategory = (from em in _dt.ExerciseMachines
                                    join ec in _dt.ExerciseCategories on em.ExerciseCategoryID equals ec.ID
                                    join eq in _dt.ExerciseEquipmentCategories on ec.ExerciseEquipmentCategory equals eq.ID
                                    select new { em, ec, eq }).ToList();

            var exercisemachine = exercisecategory.Where(x => x.em.ID == exercisemachineid).ToList();

            var exerciselist = new List<Rootobject>();

            if (exercisemachine[0].eq.ID == 1)
            {
                Rootobject exercisemodel = new Rootobject()
                {
                    IDjson = exercisemachine[0].em.ID,
                    ExerciseCategoryjson = exercisemachine[0].em.ExerciseCategoryID.ToString(),
                    Typejson = exercisemachine[0].em.Type
                };
                exerciselist.Add(exercisemodel);
            }
            else
            {
                foreach (var ee in exercisecategory)
                {
                    if (ee.ec.ID == exercisemachine[0].ec.ID)
                    {
                        Rootobject exercisemodel = new Rootobject()
                        {
                            IDjson = ee.em.ID,
                            ExerciseCategoryjson = ee.em.ExerciseCategoryID.ToString(),
                            Typejson = ee.em.Type
                        };
                        exerciselist.Add(exercisemodel);
                    }
                }
            }


            return Json(exerciselist, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public JsonResult InsertActivities(Activity model)
        {
            _dt.insertactivity(model.UserID, model.FacilityID, model.ExerciseMachineID, model.Reps, model.Weights, model.Time, model.Other, model.Sets, model.Notes);

            return Json("saved", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Activities(int User, int Machine)
        {
            var ActivityDataSet = (from a in _dt.Activities
                                   where a.UserID == User && a.ExerciseMachineID == Machine
                                   orderby a.Date ascending
                                   select new { a.UserID, a.ExerciseMachineID, a.FacilityID, a.Reps, a.Weights, a.Time, a.Other, a.Date, a.Sets}).ToList();

            var exercisemachinechartlist = new List<ExcerciseMachineChartModel>();
            var timelist = ActivityDataSet.Select(x => x.Date.Date).Distinct().OrderBy(x => x.Date).ToList();

            foreach (var a in timelist)
            {
                ExcerciseMachineChartModel exercisemachineobj = new ExcerciseMachineChartModel()
                {
                    label = a.Date.ToString("MM/dd/yyyy"),
                    value = (Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Sum(x => x.Reps) * Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Sum(x => x.Weights))) * Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Sum(x => x.Sets))),
                    tooltext = "Avg Reps: " + Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Reps)) + "{br}" + "Avg Weights: " + Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Weights)) + "{br}" + "Avg Sets: " + Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Sets)),
                    weights = Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Weights)),
                    reps = Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Reps)), 
                    sets = Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Sum(x => x.Sets))

                };
                exercisemachinechartlist.Add(exercisemachineobj);
            }

            return Json(exercisemachinechartlist, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ActivityTable(int User, int Machine)
        {
            var ActivityDataSet = (from a in _dt.Activities
                                   where a.UserID == User && a.ExerciseMachineID == Machine
                                   select new { a.Date, a.Reps, a.Weights, a.Sets, a.Other }).ToList();

            return Json(ActivityDataSet, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public JsonResult GetUserID(string useremail)
        {
            _dt.Login(useremail);

            var UsersTable = (from a in _dt.Users where a.Username == useremail select new { a.ID }).FirstOrDefault();

            var Username = new Activity()
            {
                UserID = UsersTable.ID
            };

            return Json(Username, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SummaryInformation(int userid)
        {
            var activitydata = (from act in _dt.Activities
                                join em in _dt.ExerciseMachines on act.ExerciseMachineID equals em.ID
                                join tb in _dt.TargetBodyParts on em.TargetBodyPartID equals tb.ID
                                join bh in _dt.BodyHalfs on tb.BodyHalfID equals bh.ID
                                where act.UserID == userid
                                select new { act, em, tb, bh }).ToList();

            var upperbodydata = (from tb in _dt.TargetBodyParts
                                 join bh in _dt.BodyHalfs on tb.BodyHalfID equals bh.ID
                                 where bh.ID == 1
                                 select new { tb }).ToList();

            var lowerbodydata = (from tb in _dt.TargetBodyParts
                                 join bh in _dt.BodyHalfs on tb.BodyHalfID equals bh.ID
                                 where bh.ID == 2
                                 select new { tb }).ToList();

            var SummaryData = new Summary()
            {
                UpperBody = new List<string>(),
                LowerBody = new List<string>(),
                SActivity = new List<SummaryActivity>()
            };

            foreach (var a in upperbodydata)
            {
                SummaryData.UpperBody.Add(a.tb.Value);
            }

            foreach (var a in lowerbodydata)
            {
                SummaryData.LowerBody.Add(a.tb.Value);
            }

            foreach (var a in activitydata)
            {
                SummaryActivity data = new SummaryActivity()
                {
                    ExerciseMachineValue = a.em.Type,
                    Reps = a.act.Reps,
                    Weights = a.act.Weights,
                    Sets = a.act.Sets,
                    Other = a.act.Other,
                    BodyHalf = a.bh.Value,
                    BodyPartTarget = a.tb.Value
                };
                SummaryData.SActivity.Add(data);
            }

            //piechart
            var bodyhalfs = activitydata.Select(x => x.bh.Value).Distinct();
            var piechartlist = new List<BodyHalfPieChart>();
            foreach (var a in bodyhalfs)
            {
                BodyHalfPieChart piechartobj = new BodyHalfPieChart
                {
                    label = a,
                    value = activitydata.Where(x => x.bh.Value == a).Count()
                };
                piechartlist.Add(piechartobj);
            }

            //Check Error if only one body half has been used
            if (piechartlist.Count() == 1 && piechartlist[0].label == "Lower")
            {
                BodyHalfPieChart piecharobj = new BodyHalfPieChart
                {
                    label = "Upper",
                    value = 0
                };
                piechartlist.Add(piecharobj);
            }
            else if (piechartlist.Count() == 1 && piechartlist[0].label == "Upper")
            {
                BodyHalfPieChart piecharobj = new BodyHalfPieChart
                {
                    label = "Lower",
                    value = 0
                };
                piechartlist.Add(piecharobj);
            }

            //summary
            var summarycharts = new ChartSummary()
            {
                upperbodylabel = new List<TargetBodyPartRadarChart>(),
                upperbodycount = new List<TargetBodyPartCount>(),
                lowerbodylabel = new List<TargetBodyPartRadarChart>(),
                lowerbodycount = new List<TargetBodyPartCount>()
            };
            summarycharts.piechart = piechartlist;
            

            //upperbody
            foreach (var a in upperbodydata)
            {
                int count = 0;
                foreach (var b in activitydata)
                {
                    if (b.tb.Value == a.tb.Value)
                    {
                        count++;
                    }
                }
                TargetBodyPartCount frequency = new TargetBodyPartCount
                {
                    value = count
                };

                summarycharts.upperbodycount.Add(frequency);

                TargetBodyPartRadarChart upperbodylist = new TargetBodyPartRadarChart
                {
                    label = a.tb.Value
                };
                summarycharts.upperbodylabel.Add(upperbodylist);
            }


            //lowerbody
            foreach (var a in lowerbodydata)
            {
                int count = 0;
                foreach (var b in activitydata)
                {
                    if (b.tb.Value == a.tb.Value)
                    {
                        count++;
                    }
                }
                TargetBodyPartCount frequency = new TargetBodyPartCount
                {
                    value = count
                };

                summarycharts.lowerbodycount.Add(frequency);

                TargetBodyPartRadarChart lowerbodylist = new TargetBodyPartRadarChart
                {
                    label = a.tb.Value
                };
                summarycharts.lowerbodylabel.Add(lowerbodylist);
            }

            return Json(summarycharts, JsonRequestBehavior.AllowGet);
       }

        public ActionResult Activity()
        {
            var username = Request.QueryString["username"];
            HttpCookie cookie = new HttpCookie("username");
            cookie.Value = username;
            cookie.Expires = DateTime.Now.AddHours(1);
            Response.Cookies.Add(cookie);
            return View();
        }

        public JsonResult SummaryInformationFilter(int userid, string startrange, string endrange)
        {
            DateTime start = DateTime.Parse(startrange);
            DateTime end = DateTime.Parse(endrange);

            var activitydata = (from act in _dt.Activities
                                join em in _dt.ExerciseMachines on act.ExerciseMachineID equals em.ID
                                join tb in _dt.TargetBodyParts on em.TargetBodyPartID equals tb.ID
                                join bh in _dt.BodyHalfs on tb.BodyHalfID equals bh.ID
                                where act.UserID == userid && act.Date >= start && act.Date <= end
                                select new { act, em, tb, bh }).ToList();

            var upperbodydata = (from tb in _dt.TargetBodyParts
                                 join bh in _dt.BodyHalfs on tb.BodyHalfID equals bh.ID
                                 where bh.ID == 1
                                 select new { tb }).ToList();

            var lowerbodydata = (from tb in _dt.TargetBodyParts
                                 join bh in _dt.BodyHalfs on tb.BodyHalfID equals bh.ID
                                 where bh.ID == 2
                                 select new { tb }).ToList();

            var SummaryData = new Summary()
            {
                UpperBody = new List<string>(),
                LowerBody = new List<string>(),
                SActivity = new List<SummaryActivity>()
            };

            foreach (var a in upperbodydata)
            {
                SummaryData.UpperBody.Add(a.tb.Value);
            }

            foreach (var a in lowerbodydata)
            {
                SummaryData.LowerBody.Add(a.tb.Value);
            }

            foreach (var a in activitydata)
            {
                SummaryActivity data = new SummaryActivity()
                {
                    ExerciseMachineValue = a.em.Type,
                    Reps = a.act.Reps,
                    Weights = a.act.Weights,
                    Sets = a.act.Sets,
                    Other = a.act.Other,
                    BodyHalf = a.bh.Value,
                    BodyPartTarget = a.tb.Value
                };
                SummaryData.SActivity.Add(data);
            }

            //piechart
            var bodyhalfs = activitydata.Select(x => x.bh.Value).Distinct();
            var piechartlist = new List<BodyHalfPieChart>();
            foreach (var a in bodyhalfs)
            {
                BodyHalfPieChart piechartobj = new BodyHalfPieChart
                {
                    label = a,
                    value = activitydata.Where(x => x.bh.Value == a).Count()
                };
                piechartlist.Add(piechartobj);
            }

            //Check Error if only one body half has been used
            if (piechartlist.Count() == 1 && piechartlist[0].label == "Lower")
            {
                BodyHalfPieChart piecharobj = new BodyHalfPieChart
                {
                    label = "Upper",
                    value = 0
                };
                piechartlist.Add(piecharobj);
            }
            else if(piechartlist.Count() == 1 && piechartlist[0].label == "Upper")
            {
                BodyHalfPieChart piecharobj = new BodyHalfPieChart
                {
                    label = "Lower",
                    value = 0
                };
                piechartlist.Add(piecharobj);
            }

            //summary
            var summarycharts = new ChartSummary()
            {
                upperbodylabel = new List<TargetBodyPartRadarChart>(),
                upperbodycount = new List<TargetBodyPartCount>(),
                lowerbodylabel = new List<TargetBodyPartRadarChart>(),
                lowerbodycount = new List<TargetBodyPartCount>()
            };
            summarycharts.piechart = piechartlist;


            //upperbody
            foreach (var a in upperbodydata)
            {
                int count = 0;
                foreach (var b in activitydata)
                {
                    if (b.tb.Value == a.tb.Value)
                    {
                        count++;
                    }
                }
                TargetBodyPartCount frequency = new TargetBodyPartCount
                {
                    value = count
                };

                summarycharts.upperbodycount.Add(frequency);

                TargetBodyPartRadarChart upperbodylist = new TargetBodyPartRadarChart
                {
                    label = a.tb.Value
                };
                summarycharts.upperbodylabel.Add(upperbodylist);
            }


            //lowerbody
            foreach (var a in lowerbodydata)
            {
                int count = 0;
                foreach (var b in activitydata)
                {
                    if (b.tb.Value == a.tb.Value)
                    {
                        count++;
                    }
                }
                TargetBodyPartCount frequency = new TargetBodyPartCount
                {
                    value = count
                };

                summarycharts.lowerbodycount.Add(frequency);

                TargetBodyPartRadarChart lowerbodylist = new TargetBodyPartRadarChart
                {
                    label = a.tb.Value
                };
                summarycharts.lowerbodylabel.Add(lowerbodylist);
            }

            return Json(summarycharts, JsonRequestBehavior.AllowGet);
        }
        
        public ActionResult Summary()
        {

            return View();
        }
    }
}
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
            GympanzeeDBDataContext DB = new GympanzeeDBDataContext();
            DB.Activities.InsertOnSubmit(act);
            DB.SubmitChanges();
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

        }

        [HttpPost]
        public JsonResult ExerciseMachines(int exercisemachineid)
        {

            GympanzeeDBDataContext userstable = new GympanzeeDBDataContext();

            var exercisecategory = (from em in userstable.ExerciseMachines
                                    join ec in userstable.ExerciseCategories on em.ExerciseCategoryID equals ec.ID
                                    join eq in userstable.ExerciseEquipmentCategories on ec.ExerciseEquipmentCategory equals eq.ID
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
            GympanzeeDBDataContext insertactivitiesDB = new GympanzeeDBDataContext();

            insertactivitiesDB.insertactivity(model.UserID, model.FacilityID, model.ExerciseMachineID, model.Reps, model.Weights, model.Time, model.Other, model.Sets, model.Notes);


            return Json("saved", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Activities(int User, int Machine)
        {
            GympanzeeDBDataContext Pastactivity = new GympanzeeDBDataContext();

            var ActivityDataSet = (from a in Pastactivity.Activities
                                   where a.UserID == User && a.ExerciseMachineID == Machine
                                   orderby a.Date ascending
                                   select new { a.UserID, a.ExerciseMachineID, a.FacilityID, a.Reps, a.Weights, a.Time, a.Other, a.Date, a.Sets}).ToList();

            var exercisemachinechartlist = new List<ExcerciseMachineChartModel>();
            var timelist = ActivityDataSet.Select(x => x.Date.Date).Distinct().ToList();

            foreach (var a in timelist)
            {
                ExcerciseMachineChartModel exercisemachineobj = new ExcerciseMachineChartModel()
                {
                    label = a.Date.ToString("MM/dd/yyyy"),
                    value = (Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Sum(x => x.Reps) * Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Sum(x => x.Weights))) * Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Sum(x => x.Sets))),
                    tooltext = "Avg Reps: " + Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Reps)) + "{br}" + "Avg Weights: " + Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Weights)) + "{br}" + "Avg Sets: " + Convert.ToInt32(ActivityDataSet.Where(x => x.Date.Date == a).Average(x => x.Sets))

                };
                exercisemachinechartlist.Add(exercisemachineobj);
            }

            return Json(exercisemachinechartlist, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetUserID(string useremail)
        {
            //HttpCookie gymuser = new HttpCookie("userid", "2");
            //Response.Cookies.Add(gymuser);
            //gymuser.Expires = DateTime.Now.AddHours(1);

            GympanzeeDBDataContext Table = new GympanzeeDBDataContext();

            Table.Login(useremail);

            var UsersTable = (from a in Table.Users where a.Username == useremail select new { a.ID }).FirstOrDefault();

            var Username = new Activity()
            {
                UserID = UsersTable.ID
            };

            return Json(Username, JsonRequestBehavior.AllowGet);
        }

        public JsonResult SummaryInformation(int userid)
        {
            GympanzeeDBDataContext table = new GympanzeeDBDataContext();

            var activitydata = (from act in table.Activities
                                join em in table.ExerciseMachines on act.ExerciseMachineID equals em.ID
                                join tb in table.TargetBodyParts on em.TargetBodyPartID equals tb.ID
                                join bh in table.BodyHalfs on tb.BodyHalfID equals bh.ID
                                where act.UserID == userid
                                select new { act, em, tb, bh }).ToList();

            var upperbodydata = (from tb in table.TargetBodyParts
                                 join bh in table.BodyHalfs on tb.BodyHalfID equals bh.ID
                                 where bh.ID == 1
                                 select new { tb }).ToList();

            var lowerbodydata = (from tb in table.TargetBodyParts
                                 join bh in table.BodyHalfs on tb.BodyHalfID equals bh.ID
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
                    value = count.ToString()
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
                    value = count.ToString()
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

        public ActionResult Summary()
        {

            return View();
        }
    }
}
﻿#region copyright
/*
    OurPlace is a mobile learning platform, designed to support communities
    in creating and sharing interactive learning activities about the places they care most about.
    https://github.com/GSDan/OurPlace
    Copyright (C) 2018 Dan Richardson

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see https://www.gnu.org/licenses.
*/
#endregion
using Newtonsoft.Json;
using OurPlace.API.Models;
using OurPlace.Common;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OurPlace.API.Controllers.Site
{
    public class AppController : OurPlaceSiteController
    {
        public async Task<ActionResult> Index()
        {
            await MakeLog();
            return Redirect("intent://activity#Intent;scheme=parklearn;package=com.park.learn;end");
        }

        // If the task QR codes are scanned outside of the app, just treat them like they were the parent activity's
        public async Task<ActionResult> Task(int id)
        {
            LearningTask matchingTask = await db.LearningActivityTasks.Where(t => t.Id == id).FirstOrDefaultAsync();
            if(matchingTask == null)
            {
                return new HttpNotFoundResult();
            }

            return RedirectToAction("Activity", "App", new { code = matchingTask.LearningActivity.InviteCode } );
        }

        public async Task<ActionResult> Activity(string code)
        {
            if (code == "SALTWELLSTATUE") code = "CHARLTONSTATUE";

            LearningActivity found = await db.LearningActivities.Where(act => act.InviteCode == code).FirstOrDefaultAsync();

            if (found == null)
            {
                return HttpNotFound();
            }

            string img = found.ImageUrl;

            if (string.IsNullOrWhiteSpace(img))
            {
                img = "icons/OurPlaceLogo.png";
            }

            Dictionary<string, string> logData = new Dictionary<string, string>() { { "code", code } };
            string logJson = JsonConvert.SerializeObject(logData);
            int logCount = await db.UsageLogs.CountAsync(log => log.Data == logJson && (log.User == null || !ConfidentialData.TestEmails.Contains(log.User.Email)));

            List<KeyValuePair<string, string>> scanTasks = new List<KeyValuePair<string, string>>();

            foreach (LearningTask task in found.LearningTasks)
            {
                if(task.TaskType.IdName == "SCAN_QR")
                {
                    scanTasks.Add(new KeyValuePair<string, string>(
                        ConfidentialData.storage + task.JsonData, task.Description));
                }
                if (task.ChildTasks == null) continue;
                foreach(LearningTask child in task.ChildTasks)
                {
                    if (child.TaskType.IdName == "SCAN_QR")
                    {
                        scanTasks.Add(new KeyValuePair<string, string>(
                        ConfidentialData.storage + child.JsonData, child.Description));
                    }
                }
            }

            ViewData["actName"] = found.Name;
            ViewData["actDesc"] = found.Description;
            ViewData["actShare"] = found.InviteCode;
            ViewData["actImg"] = ConfidentialData.storage + img;
            ViewData["viewCount"] = logCount;
            ViewData["intent"] = string.Format("intent://activity?code={0}#Intent;scheme=parklearn;package=com.park.learn;end", code);
            ViewData["scanTasks"] = scanTasks;

            if(!string.IsNullOrWhiteSpace(found.QRCodeUrl))
            {
                ViewData["qrCode"] = ConfidentialData.storage + found.QRCodeUrl;
            }

            await MakeLog( logData);

            return View();
        }
    }
}
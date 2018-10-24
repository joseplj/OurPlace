#region copyright
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
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OurPlace.Android.Activities;
using OurPlace.Android.Activities.Create;
using OurPlace.Android.Adapters;
using OurPlace.Common.Models;
using System;
using System.Collections.Generic;
using Android.Runtime;
using Android.Support.V4.App;
using OurPlace.Common;
using Android.Text;
using System.Linq;
using OurPlace.Common.LocalData;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;

namespace OurPlace.Android.Fragments
{
    public class MainMyActivitiesFragment : global::Android.Support.V4.App.Fragment
    {
        private LearningActivitiesAdapter adapter;
        private RecyclerView recyclerView;
        private GridLayoutManager layoutManager;
        private TextView fabPrompt;
        private SwipeRefreshLayout refresher;
        private const int PermReqId = 111;
        private List<LearningActivity> unsubmittedActivities;
        private TextView uploadsHint;
        private bool refreshingData = true;
        private bool viewLoaded;
        public static bool ForceRefresh;

        public override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Load from cached data from the database if available, 
            // just in case we can't contact the server
            List<ActivityFeedSection> cached = await ((MainActivity)Activity).GetCachedActivities(false);

            var metrics = Resources.DisplayMetrics;
            var widthInDp = AndroidUtils.ConvertPixelsToDp(metrics.WidthPixels, Activity);
            int cols = Math.Max(widthInDp / 300, 1);

            adapter = new LearningActivitiesAdapter(cached, await ((MainActivity)Activity).GetDbManager());
            adapter.ItemClick += OnItemClick;
            layoutManager = new GridLayoutManager(Activity, cols);
            layoutManager.SetSpanSizeLookup(new GridSpanner(adapter, cols));
        }

        public override async void OnResume()
        {
            base.OnResume();

            if (viewLoaded && !refreshingData && ForceRefresh)
            {
                LoadRemoteData();
            }

            uploadsHint.Visibility = ((await ((MainActivity)Activity).GetDbManager()).GetUploadQueue().Any()) ?
                    ViewStates.Visible : ViewStates.Gone;
        }

        public override bool UserVisibleHint
        {
            get => base.UserVisibleHint;

            set
            {
                if (value && viewLoaded && !refreshingData && ForceRefresh)
                {
                    LoadRemoteData();
                }

                base.UserVisibleHint = value;
            }
        }

        private async void LoadRemoteData()
        {
            try
            {
                refreshingData = true;
                refresher.Refreshing = true;

                DatabaseManager dbManager = await ((MainActivity)Activity).GetDbManager();

                ServerResponse<List<LearningActivity>> results =
                    await ServerUtils.Get<List<LearningActivity>>(
                        "/api/learningactivities/getfromuser/?creatorId=" + dbManager.currentUser.Id);
                refresher.Refreshing = false;

                if (results == null)
                {
                    var suppress = AndroidUtils.ReturnToSignIn(Activity);
                    Toast.MakeText(Activity, Resource.String.ForceSignOut, ToastLength.Long).Show();
                    return;
                }

                if (!results.Success)
                {
                    Toast.MakeText(Activity, Resource.String.ConnectionError, ToastLength.Long).Show();
                    return;
                }

                // Save this in the offline cache
                dbManager.currentUser.RemoteCreatedActivitiesJson = JsonConvert.SerializeObject(results.Data,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 5
                    });
                dbManager.AddUser(dbManager.currentUser);

                LoadIntoFeed(results.Data.OrderByDescending(act => act.CreatedAt).ToList());

                refreshingData = false;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private async Task LoadIntoFeed(List<LearningActivity> remoteData)
        {
            List<ActivityFeedSection> feed = new List<ActivityFeedSection>();

            string unsubmittedActivitiesJson = (await ((MainActivity)Activity).GetCurrentUser()).LocalCreatedActivitiesJson;
            unsubmittedActivities = null;

            if (!string.IsNullOrWhiteSpace(unsubmittedActivitiesJson))
            {
                unsubmittedActivities = JsonConvert.DeserializeObject<List<LearningActivity>>(
                    unsubmittedActivitiesJson,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                        MaxDepth = 10
                    });
            }

            // Add a section to the feed if the user has activities which they didn't finish creating
            if (unsubmittedActivities != null && unsubmittedActivities.Count > 0)
            {
                feed.Add(new ActivityFeedSection
                {
                    Title = Resources.GetString(Resource.String.createdLocalTitle),
                    Description = Resources.GetString(Resource.String.createdLocalDesc),
                    Activities = unsubmittedActivities
                });
            }

            // Add a section to the feed if the user has activities stored on the remote server
            if (remoteData != null && remoteData.Count > 0)
            {
                feed.Add(new ActivityFeedSection
                {
                    Title = Resources.GetString(Resource.String.createdFeedTitle),
                    Description = Resources.GetString(Resource.String.createdFeedDesc),
                    Activities = remoteData
                });
            }

            // Set up the adapter if needed, adding the feed data
            adapter.Data = feed;
            adapter.NotifyDataSetChanged();

            if(fabPrompt != null)
            {
                // Hide the fab tutorial if the user has already created an activity
                fabPrompt.Visibility = (adapter.Data.Count > 0) ? ViewStates.Gone : ViewStates.Visible;
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.MainMyActivities, container, false);
            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            uploadsHint = view.FindViewById<TextView>(Resource.Id.uploadsHint);
            fabPrompt = view.FindViewById<TextView>(Resource.Id.fabPrompt);
            refresher = view.FindViewById<SwipeRefreshLayout>(Resource.Id.refresher);
            refresher.Refresh += (a, e) => { LoadRemoteData(); };
            refresher.SetColorSchemeResources(
                Resource.Color.app_darkgreen,
                Resource.Color.app_green,
                Resource.Color.app_lightgreen,
                Resource.Color.app_lightergreen
            );

            FloatingActionButton fab = view.FindViewById<FloatingActionButton>(Resource.Id.createActivityFab);
            fab.Click += Fab_Click;

            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.recyclerView);
            recyclerView.SetLayoutManager(layoutManager);
            recyclerView.SetAdapter(adapter);

            viewLoaded = true;

            LoadRemoteData();
        }

        private void Fab_Click(object sender, EventArgs e)
        {
            const string permission = global::Android.Manifest.Permission.ReadExternalStorage;
            Permission currentPerm = ContextCompat.CheckSelfPermission(Activity, permission);

            const string writePerm = global::Android.Manifest.Permission.WriteExternalStorage;
            Permission currentWritePerm = ContextCompat.CheckSelfPermission(Activity, writePerm);

            if (currentPerm != Permission.Granted || currentWritePerm != Permission.Granted)
            {
                // Show an explanation of why it's needed if necessary
                if (ActivityCompat.ShouldShowRequestPermissionRationale(Activity, permission) || ActivityCompat.ShouldShowRequestPermissionRationale(Activity, writePerm))
                {
                    global::Android.Support.V7.App.AlertDialog dialog = new global::Android.Support.V7.App.AlertDialog.Builder(Activity)
                        .SetTitle(Resources.GetString(Resource.String.permissionFilesTitle))
                        .SetMessage(Resources.GetString(Resource.String.permissionFilesExplanation))
                        .SetPositiveButton("Got it", (s, o) => {
                            RequestPermissions(new string[] { permission, writePerm }, PermReqId);
                        })
                        .Create();
                    dialog.Show();
                }
                else
                {
                    // No explanation needed, just ask
                    RequestPermissions(new string[] { permission, writePerm }, PermReqId);
                }
            }
            else
            {
                StartCreate();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == PermReqId && grantResults.All((p) => p == Permission.Granted))
            {
                StartCreate();
            }
        }

        private void StartCreate()
        {
            Analytics.TrackEvent("MainMyActivitiesFragment_StartCreate");
            Intent intent = new Intent(Activity, typeof(CreateNewActivity));
            StartActivity(intent);
        }

        private void OnItemClick(object sender, int position)
        {
            LearningActivity chosen = adapter.GetItem(position);

            if (chosen == null)
            {
                Toast.MakeText(Activity, "ERROR", ToastLength.Short).Show();
                return;
            }

            bool inProgress = unsubmittedActivities != null && unsubmittedActivities.Exists((la) => chosen.Id == la.Id);

            if(inProgress)
            {
                new AlertDialog.Builder(Activity)
                    .SetTitle(chosen.Name)
                    .SetMessage(Resource.String.createdActivityDialogUnfinishedMessage)
                    .SetPositiveButton(Resource.String.EditBtn, (a, b) => { EditLocalActivity(chosen); })
                    .SetNegativeButton(Resource.String.createdActivityDialogDelete, (a, b) => { DeleteLocalActivity(chosen); })
                    .SetNeutralButton(Resource.String.dialog_cancel, (a, b) => { })
                    .SetCancelable(true)
                    .Show();
            }
            else
            {
                new AlertDialog.Builder(Activity)
                    .SetTitle(chosen.Name)
                    .SetMessage(Html.FromHtml(string.Format(Resources.GetString(Resource.String.createdActivityDialogMessage), chosen.InviteCode)))
                    .SetPositiveButton(Resource.String.createdActivityDialogOpen, (a, b) => { ((MainActivity)Activity).LaunchActivity(chosen); })
                    .SetNegativeButton(Resource.String.createdActivityDialogDelete, (a, b) => { DeleteServerActivity(chosen); })
                    .SetNeutralButton(Resource.String.dialog_cancel, (a, b) => { })
                    .SetCancelable(true)
                    .Show();
            }
        }

        private void EditLocalActivity(LearningActivity chosen)
        {
            Intent addTasksActivity = new Intent(Activity, typeof(CreateManageTasksActivity));
            string json = JsonConvert.SerializeObject(chosen, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                MaxDepth = 5
            });
            addTasksActivity.PutExtra("JSON", json);
            StartActivity(addTasksActivity);
        }

        private void DeleteLocalActivity(LearningActivity chosen)
        {
            Analytics.TrackEvent("MainMyActivitiesFragment_DeleteLocalActivity");

            new AlertDialog.Builder(Activity)
            .SetTitle(Resource.String.deleteTitle)
            .SetMessage(Resource.String.deleteMessage)
            .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => { })
            .SetCancelable(true)
            .SetPositiveButton(Resource.String.DeleteBtn, async (a, b) =>
            {
                // Remove this activity from the user's inprogress cache
                if (unsubmittedActivities != null)
                {
                    unsubmittedActivities.Remove(chosen);
                    DatabaseManager dbManager = await ((MainActivity)Activity).GetDbManager();
                    dbManager.currentUser.LocalCreatedActivitiesJson = JsonConvert.SerializeObject(unsubmittedActivities);
                    dbManager.AddUser(dbManager.currentUser);
                }
                LoadRemoteData();
            })
            .Show();
        }

        private void DeleteServerActivity(LearningActivity chosen)
        {
            new AlertDialog.Builder(Activity)
                .SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, b) => {})
                .SetCancelable(true)
                .SetPositiveButton(Resource.String.DeleteBtn, (a, b) => {
                    Delete(chosen);
                })
                .Show();
        }

        private async void Delete(LearningActivity chosen)
        {
            ProgressDialog prog = new ProgressDialog(Activity);
            prog.SetMessage(Resources.GetString(Resource.String.PleaseWait));
            prog.Show();
            ServerResponse<string> resp = await ServerUtils.Delete<string>("/api/learningactivities?id=" + chosen.Id);
            prog.Dismiss();

            if (resp == null)
            {
                var suppress = AndroidUtils.ReturnToSignIn(Activity);
                Toast.MakeText(Activity, Resource.String.ForceSignOut, ToastLength.Long).Show();
                return;
            }

            if (resp.Success)
            {
                Toast.MakeText(Activity, Resource.String.uploadsUploadSuccessTitle, ToastLength.Long).Show();
            }
            else
            {
                Toast.MakeText(Activity, Resource.String.ConnectionError, ToastLength.Long).Show();
            }

            LoadRemoteData();
        }
    }
}
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

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using FFImageLoading;
using FFImageLoading.Views;
using Newtonsoft.Json;
using OurPlace.Common.Models;
using System.Linq;

namespace OurPlace.Android.Activities
{
    [Activity(Theme = "@style/OurPlaceActionBar", ParentActivity = typeof(ActTaskListActivity))]
    public class MediaViewerActivity : AppCompatActivity, MediaPlayer.IOnPreparedListener
    {
        private int taskId;
        private int resIndex;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MediaViewerActivity);

            string json = Intent.GetStringExtra("JSON") ?? "";
            resIndex = Intent.GetIntExtra("RES_INDEX", -1);

            if (string.IsNullOrWhiteSpace(json) || resIndex == -1) return;

            AppTask thisTask = JsonConvert.DeserializeObject<AppTask>(json);

            taskId = thisTask.Id;

            SupportActionBar.Show();
            SupportActionBar.Title = thisTask.Description;

            string[] results = JsonConvert.DeserializeObject<string[]>(thisTask.CompletionData.JsonData);

            if(thisTask.TaskType.IdName == "TAKE_VIDEO")
            {
                VideoView videoView = FindViewById<VideoView>(Resource.Id.videoView);
                videoView.Visibility = ViewStates.Visible;
                var uri = global::Android.Net.Uri.Parse(results[resIndex]);
                videoView.SetOnPreparedListener(this);
                videoView.SetVideoURI(uri);

                MediaController mediaController = new MediaController(this);
                mediaController.SetAnchorView(videoView);
                videoView.SetMediaController(mediaController);

                videoView.Start();
            }
            else if(new string[] { "DRAW", "DRAW_PHOTO", "TAKE_PHOTO", "MATCH_PHOTO" }.Contains(thisTask.TaskType.IdName))
            {
                ImageViewAsync imageView = FindViewById<ImageViewAsync>(Resource.Id.imageView);
                imageView.Visibility = ViewStates.Visible;
                ImageService.Instance.LoadFile(results[resIndex]).FadeAnimation(true).Into(imageView);
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MediaViewerMenu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Resource.Id.menudelete)
            {
                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetTitle(Resource.String.deleteTitle)
                .SetMessage(Resource.String.deleteMessage)
                .SetNegativeButton(Resource.String.dialog_cancel, (a, e) =>
                {
                })
                .SetPositiveButton(Resource.String.DeleteBtn, (a, e) =>
                {
                    ReturnToDelete();
                })
                .Show();

                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public void OnPrepared(MediaPlayer mp)
        {
            mp.Looping = true;
        }

        public void ReturnToDelete()
        {
            // add location to EXIF if it's known
            Intent myIntent = new Intent(this, typeof(ActTaskListActivity));
            myIntent.PutExtra("IS_DELETE", true);
            myIntent.PutExtra("TASK_ID", taskId);
            myIntent.PutExtra("RES_INDEX", resIndex);
            SetResult(Result.Ok, myIntent);
            Finish();
        }
    }
}
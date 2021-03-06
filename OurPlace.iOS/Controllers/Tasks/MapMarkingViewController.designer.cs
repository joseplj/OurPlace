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

// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace OurPlace.iOS
{
    [Register ("MapMarkingViewController")]
    partial class MapMarkingViewController
    {
        [Outlet]
        UIKit.UILabel AvailableLabel { get; set; }


        [Outlet]
        Google.Maps.MapView mapView { get; set; }


        [Outlet]
        UIKit.UIButton MarkLocButton { get; set; }


        [Outlet]
        UIKit.UIButton ProgressFinishButton { get; set; }


        [Outlet]
        UIKit.UILabel TaskDescLabel { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (AvailableLabel != null) {
                AvailableLabel.Dispose ();
                AvailableLabel = null;
            }

            if (mapView != null) {
                mapView.Dispose ();
                mapView = null;
            }

            if (MarkLocButton != null) {
                MarkLocButton.Dispose ();
                MarkLocButton = null;
            }

            if (ProgressFinishButton != null) {
                ProgressFinishButton.Dispose ();
                ProgressFinishButton = null;
            }

            if (TaskDescLabel != null) {
                TaskDescLabel.Dispose ();
                TaskDescLabel = null;
            }
        }
    }
}
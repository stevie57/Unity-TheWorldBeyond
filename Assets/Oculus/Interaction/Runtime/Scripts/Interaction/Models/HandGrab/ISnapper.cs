/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.HandPosing
{

    /// <summary>
    /// Defines the strategy for aligning the object to the hand.
    /// The hand can go towards the object or vice-versa.
    /// </summary>
    public enum SnapType
    {
        AttractToHand,
        AnchorAtHand,
        Custom
    }

    public enum HandAlignType
    {
        AlignOnGrab,
        AttractOnHover,
        None
    }

    /// <summary>
    /// Interface for interactors that allow aligning to an object.
    /// Contains information to drive the HandGrabVisual moving
    /// the fingers and wrist.
    /// </summary>
    public interface ISnapper
    {
        bool IsSnapping { get; }
        float SnapStrength { get; }
        Pose WristToSnapOffset { get; }
        HandFingerFlags SnappingFingers();
        ISnapData SnapData { get; }
        System.Action<ISnapper> WhenSnapStarted { get; set; }
        System.Action<ISnapper> WhenSnapEnded { get; set; }
    }

    /// <summary>
    /// Interface for interactables that allow
    /// being visually aligned to by an interactor.
    /// </summary>
    public interface ISnappable
    {
        Transform RelativeTo { get; }
        HandAlignType HandAlignment { get; }
        Collider[] Colliders { get; }

        bool UsesHandPose();
    }

    /// <summary>
    /// Interface containing specific data regarding
    /// the pose of the hand as it aligns to an ISnappable
    /// </summary>
    public interface ISnapData
    {
        HandAlignType HandAlignment { get; }
        HandPose HandPose { get; }
        Pose WorldSnapPose { get; }
    }
}

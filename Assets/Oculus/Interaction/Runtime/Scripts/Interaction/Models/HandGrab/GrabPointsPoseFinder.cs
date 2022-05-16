﻿/*
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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction.HandPosing
{
    /// <summary>
    /// Utility class used by the grab interactors to find the best matching
    /// pose from a provided list of HandGrabPoints in an object.
    /// </summary>
    public class GrabPointsPoseFinder
    {
        /// <summary>
        /// List of HandGrabPoints that can move the provided _relativeTo Transform
        /// </summary>
        private List<HandGrabPoint> _handGrabPoints;
        /// <summary>
        /// Target Transform that is grabbed
        /// </summary>
        private Transform _relativeTo;
        /// <summary>
        /// When no HandGrabPoints are provided. This transform is used
        /// as a HandGrabPoint without HandPose or Handedness.
        /// </summary>
        private Transform _fallbackTransform;
        /// <summary>
        /// Cached relative pose from the target object to the fallbacktransform
        /// to save computation when the _fallbackTransform is used.
        /// </summary>
        private Pose _cachedFallbackPose;

        private InterpolationCache _interpolationCache = new InterpolationCache();

        public GrabPointsPoseFinder(List<HandGrabPoint> handGrabPoints, Transform relativeTo, Transform fallbackTransform)
        {
            _handGrabPoints = handGrabPoints;
            _relativeTo = relativeTo;
            _fallbackTransform = fallbackTransform;

            _cachedFallbackPose = _relativeTo.RelativeOffset(fallbackTransform);
        }

        public bool UsesHandPose()
        {
            return _handGrabPoints.Count > 0 && _handGrabPoints[0].HandPose != null;
        }

        /// <summary>
        /// Finds the best valid hand-pose at this HandGrabInteractable.
        /// Remember that a HandGrabPoint can actually have a whole surface the user can snap to.
        /// </summary>
        /// <param name="userPose">Pose to compare to the snap point in world coordinates.</param>
        /// <param name="handScale">The scale of the tracked hand.</param>
        /// <param name="handedness">The handedness of the tracked hand.</param>
        /// <param name="bestHandPose">The most similar valid HandPose at this HandGrabInteractable.</param>
        /// <param name="bestSnapPoint">The point of the valid snap.</param>
        /// <param name="scoringModifier">Parameters indicating how to score the different poses.</param>
        /// <param name="usesHandPose">True if the resultHandPose was populated.</param>
        /// <param name="score">The score of the best pose found.</param>
        /// <returns>True if a good pose was found</returns>
        public bool FindBestPose(Pose userPose, float handScale, Handedness handedness,
            ref HandPose bestHandPose, ref Pose bestSnapPoint, in PoseMeasureParameters scoringModifier, out bool usesHandPose, out float score)
        {
            if (_handGrabPoints.Count == 1)
            {
                return _handGrabPoints[0].CalculateBestPose(userPose, handedness,
                    ref bestHandPose, ref bestSnapPoint, scoringModifier, out usesHandPose, out score);
            }
            else if (_handGrabPoints.Count > 1)
            {
                return CalculateBestScaleInterpolatedPose(userPose, handedness, handScale,
                    ref bestHandPose, ref bestSnapPoint, scoringModifier, out usesHandPose, out score);
            }
            else
            {
                usesHandPose = false;
                bestSnapPoint = new Pose(_cachedFallbackPose.position, Quaternion.Inverse(_relativeTo.rotation) * userPose.rotation);
                score = PoseUtils.Similarity(userPose, _fallbackTransform.GetPose(), scoringModifier);
                return true;
            }
        }

        private bool CalculateBestScaleInterpolatedPose(Pose userPose, Handedness handedness, float handScale,
            ref HandPose result, ref Pose snapPoint, in PoseMeasureParameters scoringModifier, out bool usesHandPose, out float score)
        {
            usesHandPose = false;
            score = float.NaN;

            FindInterpolationRange(handScale, _handGrabPoints, out HandGrabPoint under, out HandGrabPoint over, out float t);

            bool underFound = under.CalculateBestPose(userPose, handedness,
               ref _interpolationCache.underHandPose, ref _interpolationCache.underSnapPoint, scoringModifier,
               out bool underPoseWritten, out float underScore);

            bool overFound = over.CalculateBestPose(userPose, handedness,
                ref _interpolationCache.overHandPose, ref _interpolationCache.overSnapPoint, scoringModifier,
                out bool overPoseWritten, out float overScore);

            if (underPoseWritten && overPoseWritten)
            {
                usesHandPose = true;
                result.CopyFrom(_interpolationCache.underHandPose);
                HandPose.Lerp(_interpolationCache.underHandPose, _interpolationCache.overHandPose, t, ref result);
                PoseUtils.Lerp(_interpolationCache.underSnapPoint, _interpolationCache.overSnapPoint, t, ref snapPoint);
            }
            else if (underPoseWritten)
            {
                usesHandPose = true;
                result.CopyFrom(_interpolationCache.underHandPose);
                snapPoint.CopyFrom(_interpolationCache.underSnapPoint);
            }
            else if (overPoseWritten)
            {
                usesHandPose = true;
                result.CopyFrom(_interpolationCache.overHandPose);
                snapPoint.CopyFrom(_interpolationCache.overSnapPoint);
            }

            if (underFound && overFound)
            {
                score = Mathf.Lerp(underScore, overScore, t);
                return true;
            }

            if (underFound)
            {
                score = underScore;
                return true;
            }

            if (overFound)
            {
                score = overScore;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the two nearest HandGrabPoints to interpolate from given a scale.
        /// The result can require an unclamped interpolation (t can be bigger than 1 or smaller than 0).
        /// </summary>
        /// <param name="scale">The user scale</param>
        /// <param name="grabPoints">The list of grabpoints to interpolate from</param>
        /// <param name="from">The HandGrabInteractable with nearest scale recorder that is smaller than the provided one</param>
        /// <param name="to">The HandGrabInteractable with nearest scale recorder that is bigger than the provided one</param>
        /// <param name="t">The progress between from and to variables at which the desired scale resides</param>
        /// <returns>The HandGrabPoint near under and over the scale, and the interpolation factor between them.</returns>
        public static void FindInterpolationRange(float scale, List<HandGrabPoint> grabPoints, out HandGrabPoint from, out HandGrabPoint to, out float t)
        {
            from = grabPoints[0];
            to = grabPoints[1];

            for (int i = 2; i < grabPoints.Count; i++)
            {
                HandGrabPoint point = grabPoints[i];

                if (point.Scale <= scale
                    && point.Scale > from.Scale)
                {
                    from = point;
                }
                else if (point.Scale >= scale
                    && point.Scale < to.Scale)
                {
                    to = point;
                }
            }

            t = (scale - from.Scale) / (to.Scale - from.Scale);
        }

        private class InterpolationCache
        {
            public HandPose underHandPose = new HandPose();
            public HandPose overHandPose = new HandPose();
            public Pose underSnapPoint = Pose.identity;
            public Pose overSnapPoint = Pose.identity;
        }
    }
}

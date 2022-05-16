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

using UnityEngine;

/// <summary>
/// Represents a Scene entity created during Room Capture.
/// </summary>
public class OVRSceneEntity : MonoBehaviour
{
  /// <summary>
  /// The unique handle used to identify this entity in the low-level runtime.
  /// </summary>
  public OVRSpace Space
  {
    get => _space;
    internal set => _space = value;
  }

  private OVRSpace _space;

  private static readonly Quaternion RotateY180 = Quaternion.Euler(0, 180, 0);

  internal bool TryUpdateTransform()
  {
    if (!_space.Valid) return false;

    if (!OVRPlugin.TryLocateSpace(_space, OVRPlugin.GetTrackingOriginType(), out var pose)) return false;

    // NOTE: This transformation performs the following steps:
    // 1. Flip Z to convert from OpenXR's right-handed to Unity's left-handed coordinate system.
    //    OpenXR             Unity
    //       | y          y |  / z
    //       |              | /
    //       +----> x       +----> x
    //      /
    //    z/ (normal)
    //
    // 2. (1) means that Z now points in the opposite direction from OpenXR. However, the design is such that a
    //    plane's normal should coincide with +Z, so we rotate 180 degrees around the +Y axis to make Z now point
    //    in the intended direction.
    //    OpenXR           Unity
    //       | y           y |
    //       |               |
    //       +---->  x  <----+
    //      /               /
    //    z/             z / (normal)
    //
    // 3. Convert from tracking space to world space.
    var worldSpacePose = new OVRPose
    {
      position = pose.Position.FromFlippedZVector3f(),
      orientation = pose.Orientation.FromFlippedZQuatf() * RotateY180
    }.ToWorldSpacePose(Camera.main);
    transform.SetPositionAndRotation(worldSpacePose.position, worldSpacePose.orientation);
    return true;
  }

  private void Update() => TryUpdateTransform();
}

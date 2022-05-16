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

using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandPosing.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandGrabInteractable))]
    public class HandGrabInteractableEditor : UnityEditor.Editor
    {
        private HandGrabInteractable _interactable;

        private void Awake()
        {
            _interactable = target as HandGrabInteractable;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawGrabPointsMenu();
            GUILayout.Space(20f);
            DrawGenerationMenu();
        }

        private void DrawGrabPointsMenu()
        {
            if (GUILayout.Button("Refresh HandGrab Points"))
            {
                _interactable.GrabPoints.Clear();
                HandGrabPoint[] handGrabPoints = _interactable.GetComponentsInChildren<HandGrabPoint>();
                _interactable.GrabPoints.AddRange(handGrabPoints);
            }

            if (GUILayout.Button("Add HandGrab Point"))
            {
                if (_interactable.GrabPoints.Count > 0)
                {
                    AddHandGrabPoint(_interactable.GrabPoints[0]);
                }
                else
                {
                    AddHandGrabPoint();
                }
            }

            if (GUILayout.Button("Replicate Default Scaled HandGrab Points"))
            {
                if (_interactable.GrabPoints.Count > 0)
                {
                    AddHandGrabPoint(_interactable.GrabPoints[0], 0.8f);
                    AddHandGrabPoint(_interactable.GrabPoints[0], 1.2f);
                }
                else
                {
                    Debug.LogError("You have to provide a default HandGrabPoint first!");
                }
            }
        }

        private void AddHandGrabPoint(HandGrabPoint copy = null, float? scale = null)
        {
            HandGrabPoint point = _interactable.CreatePoint();
            if (copy != null)
            {
                HandGrabPointEditor.CloneHandGrabPoint(copy, point);
                if (scale.HasValue)
                {
                    HandGrabPointData scaledData = point.SaveData();
                    scaledData.scale = scale.Value;
                    point.LoadData(scaledData, copy.RelativeTo);
                }
            }
            _interactable.GrabPoints.Add(point);
        }

        private void DrawGenerationMenu()
        {
            if (GUILayout.Button("Create Mirrored HandGrabInteractable"))
            {
                HandGrabInteractable mirrorInteractable = 
                    HandGrabInteractable.Create(_interactable.RelativeTo, 
                        $"{_interactable.gameObject.name}_mirror");

                HandGrabInteractableData data = _interactable.SaveData();
                data.points = null;
                mirrorInteractable.LoadData(data);

                foreach (HandGrabPoint point in _interactable.GrabPoints)
                {
                    HandGrabPoint mirrorPoint = mirrorInteractable.CreatePoint();
                    HandGrabPointEditor.MirrorHandGrabPoint(point, mirrorPoint);
                    mirrorPoint.transform.SetParent(mirrorInteractable.transform);
                    mirrorInteractable.GrabPoints.Add(mirrorPoint);
                }
            }
        }
    }
}

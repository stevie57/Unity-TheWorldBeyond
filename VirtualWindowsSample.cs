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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VirtualWindowsSample : MonoBehaviour
{
    [HideInInspector]
    public OVRSceneAnchor[] _sceneAnchors;
    bool _sceneModelLoaded = false;

    // all virtual content is a child of this
    public Transform _envRoot;

    // drop the virtual world this far below the floor anchor
    const float _groundDelta = 0.02f;

    void Start()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
        OVRManager.eyeFovPremultipliedAlphaModeEnabled = false;
#endif
    }

    void Update()
    {
        if (!_sceneModelLoaded)
        {
            _sceneAnchors = FindObjectsOfType<OVRSceneAnchor>();
            if (_sceneAnchors.Length > 0)
            {
                Initialize(_sceneAnchors);
                _sceneModelLoaded = true;
            }
            else
            {
                return;
            }
        }
    }

    void Initialize(OVRSceneAnchor[] sceneAnchors)
    {
        for (int i = 0; i < sceneAnchors.Length; i++)
        {
            OVRSceneAnchor instance = sceneAnchors[i];
            OVRSemanticClassification classification = instance.GetComponent<OVRSemanticClassification>();

            if (classification.Contains(OVRSceneManager.Classification.Floor))
            {
                // move the world slightly below the ground floor, so the virtual floor doesn't Z-fight
                if (_envRoot)
                {
                    Vector3 envPos = _envRoot.transform.position;
                    float groundHeight = instance.transform.position.y - _groundDelta;
                    _envRoot.transform.position = new Vector3(envPos.x, groundHeight, envPos.z);
                }
            }
        }
    }
}

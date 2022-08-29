﻿/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using Facebook.WitAi.Data;
using Facebook.WitAi.Interfaces;
using UnityEngine;

namespace Facebook.WitAi.Lib
{
    // Voice SDK abstract class for handling mic elsewhere
    public abstract class MicBase : MonoBehaviour, IAudioInputSource
    {
        // Abstract getters for Unity Mic data
        public abstract string GetMicName();
        public abstract int GetMicSampleRate();
        public abstract AudioClip GetMicClip();

        // All Mic callbacks
        public event Action OnStartRecording;
        public event Action OnStartRecordingFailed;
        public event Action OnStopRecording;
        public event Action<int, float[], float> OnSampleReady;

        // Mic states
        public bool IsRecording { get; private set; }
        public bool IsInputAvailable => GetMicClip() != null;

        // Encoding settings for wit
        // Warning: Changes may not work
        public AudioEncoding AudioEncoding { get; set; } = new AudioEncoding();

        // Used for reading
        private int _sampleCount = 0;
        private Coroutine _reader;

        // Can be overriden for refreshing mic lists
        public virtual void CheckForInput()
        {

        }

        // Records at a specified sample duration in ms
        public virtual void StartRecording(int sampleDurationMS)
        {
            // Stop previous
            if (IsRecording)
            {
                StopRecording();
            }

            // Cannot start
            if (!IsInputAvailable)
            {
                OnStartRecordingFailed.Invoke();
                return;
            }

            // Recording
            IsRecording = true;

            // Available
            _reader = StartCoroutine(ReadRawAudio(sampleDurationMS));
        }

        // Read raw audio
        protected virtual IEnumerator ReadRawAudio(int sampleDurationMS)
        {
            // Start recording
            Debug.Log("Mic Ref - Start Recording");
            OnStartRecording?.Invoke();

            // Get data
            AudioClip micClip = GetMicClip();
            string micDevice = GetMicName();
            int micSampleRate = GetMicSampleRate();

            // Setup sample
            int sampleTotal = AudioEncoding.samplerate / 1000 * sampleDurationMS * micClip.channels;
            float[] sample = new float[sampleTotal];

            // All needed data
            int loops = 0;
            int readAbsPos = Microphone.GetPosition(micDevice);
            int prevPos = readAbsPos;
            int micTempTotal = micSampleRate / 1000 * sampleDurationMS * micClip.channels;
            int micDif = micTempTotal / sampleTotal;
            float[] temp = new float[micTempTotal];

            // Continue reading
            while (micClip != null && Microphone.IsRecording(micDevice) && IsRecording)
            {
                bool isNewDataAvailable = true;

                while (isNewDataAvailable && micClip != null)
                {
                    int currPos = Microphone.GetPosition(micDevice);
                    if (currPos < prevPos)
                        loops++;
                    prevPos = currPos;

                    var currAbsPos = loops * micClip.samples + currPos;
                    var nextReadAbsPos = readAbsPos + micTempTotal;

                    if (nextReadAbsPos < currAbsPos)
                    {
                        micClip.GetData(temp, readAbsPos % micClip.samples);

                        // Fill sample & get level max
                        float levelMax = 0;
                        int sampleIndex = 0;
                        for (int i = 0; i < temp.Length; i++)
                        {
                            float wavePeak = temp[i] * temp[i];
                            if (levelMax < wavePeak)
                            {
                                levelMax = wavePeak;
                            }
                            if (i % micDif == 0 && sampleIndex < sample.Length)
                            {
                                sample[sampleIndex] = temp[i];
                                sampleIndex++;
                            }
                        }

                        _sampleCount++;
                        OnSampleReady?.Invoke(_sampleCount, sample, levelMax);

                        readAbsPos = nextReadAbsPos;
                    }
                    else
                    {
                        isNewDataAvailable = false;
                    }
                }

                // Wait a moment
                yield return null;
            }

            // Stop
            if (IsRecording)
            {
                StopRecording();
            }
        }

        // Stop recording
        public virtual void StopRecording()
        {
            // Ignore
            if (!IsRecording)
            {
                return;
            }

            // Stop Recording
            IsRecording = false;

            // Stop reading
            if (_reader != null)
            {
                StopCoroutine(_reader);
                _reader = null;
            }

            // Stop recording
            Debug.Log("Mic Ref - Stop Recording");
            OnStopRecording?.Invoke();
        }
    }
}

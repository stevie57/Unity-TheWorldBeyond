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

Shader "Oculus/Interaction/StencilWriter"{
	Properties{
		[IntRange] _StencilRef("Stencil Reference Value", Range(0,255)) = 0
	}

	SubShader{

		Tags {   "Queue" = "Geometry+501"} // stuck way up here so that it sorts with transparent objects

		ZWrite Off

		ColorMask 0 // Don't write to any colour channels
		//Blend Zero One
		Stencil{
			Ref[_StencilRef]
			Comp Always
			Pass Replace
		}



		Pass {



			CGPROGRAM

			// pragmas
			#pragma vertex vert
			#pragma fragment frag

			// base input structs
			struct vertexInput {
				half4 vertex : POSITION;
			};
			struct vertexOutput {
				half4 pos : SV_POSITION;
			};

			// vertex function
			vertexOutput vert(vertexInput v) {
				vertexOutput o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}

			// fragment function
			fixed4 frag(vertexOutput i) : COLOR
			{
				return 0;
			}

			ENDCG
		}
	}
}

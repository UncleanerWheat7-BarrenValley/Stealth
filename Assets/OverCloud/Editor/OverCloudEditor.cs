using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace OC
{
	// http://tips.hecomi.com/entry/2016/10/15/004144
	public static class CustomUI
	{
		public static bool Foldout(string title, bool display)
		{
			var style = new GUIStyle("ShurikenModuleTitle");
			style.font = new GUIStyle(EditorStyles.label).font;
			style.border = new RectOffset(15, 7, 4, 4);
			style.fixedHeight = 22;
			style.contentOffset = new Vector2(20f, -2f);

			var rect = GUILayoutUtility.GetRect(16f, 22f, style);
			GUI.Box(rect, title, style);

			var e = Event.current;

			var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
			if (e.type == EventType.Repaint) {
				EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
			}

			if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) {
				display = !display;
				e.Use();
			}

			return display;
		}
	}

	[CustomEditor(typeof(OverCloud)), CanEditMultipleObjects]
	public class OverCloudEditor : Editor
	{
		private static GUIStyle ListStyle		= null;
		private static GUIStyle ButtonStyle		= null;
		private static GUIStyle ButtonStyleToggle = null;
		private static Color buttonColorUp		= new Color(0.8f, 0.8f, 0.8f, 1);
		private static Color buttonColorDown	= new Color(0.6f, 0.6f, 0.6f, 1);

		float inspectorWidth;

		#region Properties
		SerializedProperty
			p_sun,
			p_moon,

			p_cloudMaterial,
			p_skyMaterial,
			p_compositorRes,
			p_noiseTex,
			p_noiseScale,
			p_noiseMacroScale,
			p_compositorBlur,
			p_timescale,
			p_radius,
			p_lodMultiplier,
			
			p_particleCount,
			p_lodSize,

			p_noiseTiling_A,
			p_noiseIntensity_A,

			p_noiseTiling_B,
			p_noiseIntensity_B,

			p_shapeCenter,
			p_baseDensityIncrease,
			p_noiseErosion,

			p_alphaEdgeLower,
			p_alphaEdgeUpper,

			p_noiseTurbulence,
			p_noiseRiseFactor,
			
			p_3DNoiseResolution,
			p_perlinPeriods,
			p_perlinOctaves,
			p_perlinBrightness,
			p_perlinContrast,
			p_worleyPeriods,
			p_worleyOctaves,
			p_worleyBrightness,
			p_worleyContrast,
			
			p_scatteringMaskRange,
			p_scatteringMaskIntensity,
			p_scatteringMaskSoftness,
			p_scatteringMaskFloor,
			
			p_rainRippleTex,
			p_rainFlowTex,
			p_rainMaskOffsetTex,
			p_rainMaskResolution,
			p_rainMaskLayers,
			p_rainMaskRadius,
			p_rainMaskFalloff,
			p_rainMaskBlur,
			p_rainMaskOffset,

			p_rainMaskAlbedoDarken,
			p_rainMaskRoughnessDecrease,
			p_rainRippleIntensity,
			p_rainRippleScale,
			p_rainRippleTimescale,
			p_rainFlowIntensity,
			p_rainFlowScale,
			p_rainFlowTimescale,

			p_lightningObject,
			p_lightningDistanceMin,
			p_lightningDistanceMax,
			p_lightningCameraBias,
			p_lightningMinimumDensity,
			p_lightningSpanMin,
			p_lightningSpanMax,
			p_lightningRestrikeChance,
			p_enableLightningInEditor,
			
			p_activePreset,
			p_fadeDuration,
			p_editorFadeDuration,

			p_lightingAlbedo,
			p_lightingPrecipitationAlbedo,
			p_lightingEccentricity,
			p_lightingSilverIntensity,
			p_lightingSilverSpread,
			p_lightingDirect,
			p_lightingDirectAbsorption,
			p_lightingIndirect,
			p_lightingIndirectAbsorption,
			//p_lightingIndirectSoftness,
			p_lightingAmbient,
			p_lightingAmbientAbsorption,
			p_lightingAmbientDesaturation,
			p_lightingPowderSize,
			p_lightingPowderIntensity,

			p_ambientSky,
			p_ambientEquator,
			p_ambientGround,
			p_ambientMultiplier,
			p_nightScattering,
			p_lunarEclipseInfluence,
			p_cloudShadowsEnabled,
			p_cloudShadowsMode,
			p_cloudShadowsResolution,
			p_cloudShadowsCoverage,
			p_cloudShadowsBlur,
			p_cloudShadowsSharpen,
			p_cloudShadowsEdgeTex,
			p_cloudShadowsEdgeTexScale,
			p_cloudShadowsEdgeTexIntensity,
			p_cloudAOIntensity,
			p_cloudAOHeight,
			
			p_overrideSkyboxMaterial,
			p_planetScale,
			p_atmosphereHeightScale,
			//p_atmosphereMie,
			p_atmosphereRayleigh,
			p_atmosphereOzone,
			//p_atmospherePhase,
			p_atmosphereComputeShader,
			p_atmosphereExposure,
			p_atmosphereDensity,
			p_atmosphereFarClipFade,
			p_skyActualSunColor,
			p_skyActualMoonColor,
			p_skySunSize,
			p_skyMoonSize,
			p_skySunIntensity,
			p_skyMoonIntensity,
			p_skyMoonCube,
			p_skyStarsCube,
			p_skyStarsIntensity,
			p_mieScatteringIntensity,
			p_mieScatteringPhase,
			p_mieScatteringFogPhase,
			p_mieScatteringDistanceFadeA,
			p_mieScatteringDistanceFadeB,
			p_solarEclipseColor,
			p_lunarEclipseColor,

			p_earthColor,
			
			p_useTimeOfDay,
			p_useLocalTime,
			p_todPlay,
			p_todPlayInEditor,
			p_todPlaySpeed,
			p_todLatitude,
			p_todLongitude,
			p_todYear,
			p_todMonth,
			p_todDay,
			p_todTime,
			p_todDynamicMoon;

		private void OnEnable()
		{
			// ---------- Components ----------
			var componentsProp					= serializedObject.FindProperty("m_Components");
			p_sun								= componentsProp.FindPropertyRelative("sun");
			p_moon								= componentsProp.FindPropertyRelative("moon");
			p_cloudMaterial						= componentsProp.FindPropertyRelative("cloudMaterial");
			p_skyMaterial						= componentsProp.FindPropertyRelative("skyMaterial");

			// ---------- Volumetric clouds ----------
			var volumetricCloudsProp			= serializedObject.FindProperty("m_VolumetricClouds");

			p_radius							= volumetricCloudsProp.FindPropertyRelative("cloudPlaneRadius");
			p_compositorRes						= volumetricCloudsProp.FindPropertyRelative("compositorResolution");
			p_compositorBlur					= volumetricCloudsProp.FindPropertyRelative("compositorBlur");
			p_noiseTex							= volumetricCloudsProp.FindPropertyRelative("noiseTexture");
			p_noiseScale						= volumetricCloudsProp.FindPropertyRelative("noiseScale");
			p_noiseMacroScale					= volumetricCloudsProp.FindPropertyRelative("noiseMacroScale");
			p_particleCount						= volumetricCloudsProp.FindPropertyRelative("particleCount");
			p_lodMultiplier						= volumetricCloudsProp.FindPropertyRelative("lodRadiusMultiplier");
			p_lodSize							= volumetricCloudsProp.FindPropertyRelative("lodParticleSize");

			// Shape (3D Noise)
			var noiseProperty					= volumetricCloudsProp.FindPropertyRelative("m_NoiseSettings");
			p_noiseTiling_A						= noiseProperty.FindPropertyRelative("noiseTiling_A");
			p_noiseIntensity_A					= noiseProperty.FindPropertyRelative("noiseIntensity_A");
			p_noiseTiling_B						= noiseProperty.FindPropertyRelative("noiseTiling_B");
			p_noiseIntensity_B					= noiseProperty.FindPropertyRelative("noiseIntensity_B");
			p_shapeCenter						= noiseProperty.FindPropertyRelative("shapeCenter");
			p_baseDensityIncrease				= noiseProperty.FindPropertyRelative("baseDensityIncrease");
			p_noiseErosion						= noiseProperty.FindPropertyRelative("erosion");
			p_alphaEdgeLower					= noiseProperty.FindPropertyRelative("alphaEdgeLower");
			p_alphaEdgeUpper					= noiseProperty.FindPropertyRelative("alphaEdgeUpper");
			p_noiseTurbulence					= noiseProperty.FindPropertyRelative("turbulence");
			p_noiseRiseFactor					= noiseProperty.FindPropertyRelative("riseFactor");

			// 3D Noise Generator
			var noiseGenProperty				= volumetricCloudsProp.FindPropertyRelative("m_NoiseGeneration");
			var perlinProperty					= noiseGenProperty.FindPropertyRelative("perlin");
			var worleyProperty					= noiseGenProperty.FindPropertyRelative("worley");
			p_3DNoiseResolution					= noiseGenProperty.FindPropertyRelative("resolution");
			p_perlinPeriods						= perlinProperty.FindPropertyRelative("periods");
			p_perlinOctaves						= perlinProperty.FindPropertyRelative("octaves");
			p_perlinBrightness					= perlinProperty.FindPropertyRelative("brightness");
			p_perlinContrast					= perlinProperty.FindPropertyRelative("contrast");
			p_worleyPeriods						= worleyProperty.FindPropertyRelative("periods");
			p_worleyOctaves						= worleyProperty.FindPropertyRelative("octaves");
			p_worleyBrightness					= worleyProperty.FindPropertyRelative("brightness");
			p_worleyContrast					= worleyProperty.FindPropertyRelative("contrast");

			// ---------- Atmosphere ----------
			var atmosphereProp					= serializedObject.FindProperty("m_Atmosphere");
			p_overrideSkyboxMaterial			= atmosphereProp.FindPropertyRelative("overrideSkyboxMaterial");
			p_atmosphereExposure				= atmosphereProp.FindPropertyRelative("exposure");
			p_atmosphereDensity					= atmosphereProp.FindPropertyRelative("density");
			p_atmosphereFarClipFade				= atmosphereProp.FindPropertyRelative("farClipFade");
			// Precomputation parameters
			var precomputationProperty			= atmosphereProp.FindPropertyRelative("m_Precomputation");
			p_planetScale						= precomputationProperty.FindPropertyRelative("planetScale");
			p_atmosphereHeightScale				= precomputationProperty.FindPropertyRelative("heightScale");
			p_atmosphereComputeShader			= precomputationProperty.FindPropertyRelative("shader");
			//p_atmosphereMie					= precomputationProperty.FindPropertyRelative("mie");
			p_atmosphereRayleigh				= precomputationProperty.FindPropertyRelative("rayleigh");
			p_atmosphereOzone					= precomputationProperty.FindPropertyRelative("ozone");
			//p_atmospherePhase					= precomputationProperty.FindPropertyRelative("phase");
			// Sun
			p_skyActualSunColor					= atmosphereProp.FindPropertyRelative("actualSunColor");
			p_skySunSize						= atmosphereProp.FindPropertyRelative("sunSize");
			p_skySunIntensity					= atmosphereProp.FindPropertyRelative("sunIntensity");
			p_solarEclipseColor					= atmosphereProp.FindPropertyRelative("solarEclipseColor");
			// Moon
			p_skyMoonCube						= atmosphereProp.FindPropertyRelative("moonAlbedo");
			p_skyActualMoonColor				= atmosphereProp.FindPropertyRelative("actualMoonColor");
			p_skyMoonSize						= atmosphereProp.FindPropertyRelative("moonSize");
			p_skyMoonIntensity					= atmosphereProp.FindPropertyRelative("moonIntensity");
			p_lunarEclipseColor					= atmosphereProp.FindPropertyRelative("lunarEclipseColor");
			// Earth
			p_earthColor						= atmosphereProp.FindPropertyRelative("earthColor");
			// Mie Scattering
			p_mieScatteringIntensity			= atmosphereProp.FindPropertyRelative("mieScatteringIntensity");
			p_mieScatteringPhase				= atmosphereProp.FindPropertyRelative("mieScatteringPhase");
			p_mieScatteringFogPhase				= atmosphereProp.FindPropertyRelative("mieScatteringFogPhase");
			p_mieScatteringDistanceFadeA		= atmosphereProp.FindPropertyRelative("mieScatteringDistanceFadeA");
			p_mieScatteringDistanceFadeB		= atmosphereProp.FindPropertyRelative("mieScatteringDistanceFadeB");
			// Night
			p_nightScattering					= atmosphereProp.FindPropertyRelative("nightScattering");
			// Stars
			p_skyStarsCube						= atmosphereProp.FindPropertyRelative("starsCubemap");
			p_skyStarsIntensity					= atmosphereProp.FindPropertyRelative("starsIntensity");
			// Scattering Mask
			var scatteringMaskProperty			= atmosphereProp.FindPropertyRelative("m_ScatteringMask");
			p_scatteringMaskRange				= scatteringMaskProperty.FindPropertyRelative("range");
			p_scatteringMaskIntensity			= scatteringMaskProperty.FindPropertyRelative("intensity");
			p_scatteringMaskSoftness			= scatteringMaskProperty.FindPropertyRelative("softness");
			p_scatteringMaskFloor				= scatteringMaskProperty.FindPropertyRelative("floor");

			// ---------- Lighting ----------
			// Cloud Lighting
			var lightingProperty				= serializedObject.FindProperty("m_Lighting");
			var cloudLightingProperty			= lightingProperty.FindPropertyRelative("m_CloudLighting");
			p_lightingAlbedo					= cloudLightingProperty.FindPropertyRelative("albedo");
			p_lightingPrecipitationAlbedo		= cloudLightingProperty.FindPropertyRelative("precipitationAlbedo");
			p_lightingEccentricity				= cloudLightingProperty.FindPropertyRelative("eccentricity");
			p_lightingSilverIntensity			= cloudLightingProperty.FindPropertyRelative("silverIntensity");
			p_lightingSilverSpread				= cloudLightingProperty.FindPropertyRelative("silverSpread");
			p_lightingDirect					= cloudLightingProperty.FindPropertyRelative("direct");
			p_lightingDirectAbsorption			= cloudLightingProperty.FindPropertyRelative("directAbsorption");
			p_lightingIndirect					= cloudLightingProperty.FindPropertyRelative("indirect");
			p_lightingIndirectAbsorption		= cloudLightingProperty.FindPropertyRelative("indirectAbsorption");
			//p_lightingIndirectSoftness		= cloudLightingProperty.FindPropertyRelative("indirectSoftness");
			p_lightingAmbient					= cloudLightingProperty.FindPropertyRelative("ambient");
			p_lightingAmbientAbsorption			= cloudLightingProperty.FindPropertyRelative("ambientAbsorption");
			p_lightingAmbientDesaturation		= cloudLightingProperty.FindPropertyRelative("ambientDesaturation");
			p_lightingPowderSize				= cloudLightingProperty.FindPropertyRelative("powderSize");
			p_lightingPowderIntensity			= cloudLightingProperty.FindPropertyRelative("powderIntensity");

			// Cloud Shadows
			var cloudShadowsProperty			= lightingProperty.FindPropertyRelative("m_CloudShadows");
			p_cloudShadowsEnabled				= cloudShadowsProperty.FindPropertyRelative("enabled");
			p_cloudShadowsMode					= cloudShadowsProperty.FindPropertyRelative("mode");
			p_cloudShadowsResolution			= cloudShadowsProperty.FindPropertyRelative("resolution");
			p_cloudShadowsCoverage				= cloudShadowsProperty.FindPropertyRelative("coverage");
			p_cloudShadowsBlur					= cloudShadowsProperty.FindPropertyRelative("blur");
			p_cloudShadowsSharpen				= cloudShadowsProperty.FindPropertyRelative("sharpen");
			p_cloudShadowsEdgeTex				= cloudShadowsProperty.FindPropertyRelative("edgeTexture");
			p_cloudShadowsEdgeTexScale			= cloudShadowsProperty.FindPropertyRelative("edgeTextureScale");
			p_cloudShadowsEdgeTexIntensity		= cloudShadowsProperty.FindPropertyRelative("edgeTextureIntensity");
			// Cloud Ambient Occlusion
			var cloudAmbientOcclusionProperty	= lightingProperty.FindPropertyRelative("m_CloudAmbientOcclusion");
			p_cloudAOIntensity					= cloudAmbientOcclusionProperty.FindPropertyRelative("intensity");
			p_cloudAOHeight						= cloudAmbientOcclusionProperty.FindPropertyRelative("heightFalloff");
			// Ambient Lighting
			var ambientProperty					= lightingProperty.FindPropertyRelative("m_Ambient");
			p_ambientSky						= ambientProperty.FindPropertyRelative("sky");
			p_ambientEquator					= ambientProperty.FindPropertyRelative("equator");
			p_ambientGround						= ambientProperty.FindPropertyRelative("ground");
			p_lunarEclipseInfluence				= ambientProperty.FindPropertyRelative("lunarEclipseLightingInfluence");
			p_ambientMultiplier					= ambientProperty.FindPropertyRelative("multiplier");

			// ---------- Weather Effects ----------
			var weatherProperty					= serializedObject.FindProperty("m_Weather");
			// Wind
			p_timescale							= weatherProperty.FindPropertyRelative("windTimescale");
			// Rain
			var rainProperty					= weatherProperty.FindPropertyRelative("m_Rain");
			// Rain Mask Rendering
			p_rainMaskResolution				= rainProperty.FindPropertyRelative("maskResolution");
			p_rainMaskLayers					= rainProperty.FindPropertyRelative("maskLayers");
			p_rainMaskRadius					= rainProperty.FindPropertyRelative("maskRadius");
			// Rain Mask Sampling
			p_rainMaskFalloff					= rainProperty.FindPropertyRelative("maskFalloff");
			p_rainMaskBlur						= rainProperty.FindPropertyRelative("maskBlur");
			p_rainMaskOffsetTex					= rainProperty.FindPropertyRelative("maskOffsetTexture");
			p_rainMaskOffset					= rainProperty.FindPropertyRelative("maskOffset");
			// Rain Normals, Albedo & Gloss
			p_rainRippleTex						= rainProperty.FindPropertyRelative("rippleTexture");
			p_rainFlowTex						= rainProperty.FindPropertyRelative("flowTexture");
			p_rainMaskAlbedoDarken				= rainProperty.FindPropertyRelative("albedoDarken");
			p_rainMaskRoughnessDecrease			= rainProperty.FindPropertyRelative("roughnessDecrease");
			p_rainRippleIntensity				= rainProperty.FindPropertyRelative("rippleIntensity");
			p_rainRippleScale					= rainProperty.FindPropertyRelative("rippleScale");
			p_rainRippleTimescale				= rainProperty.FindPropertyRelative("rippleTimescale");
			p_rainFlowIntensity					= rainProperty.FindPropertyRelative("flowIntensity");
			p_rainFlowScale						= rainProperty.FindPropertyRelative("flowScale");
			p_rainFlowTimescale					= rainProperty.FindPropertyRelative("flowTimescale");
			// Lightning
			var lightningProperty				= weatherProperty.FindPropertyRelative("m_Lightning");
			p_lightningObject					= lightningProperty.FindPropertyRelative("gameObject");
			p_lightningDistanceMin				= lightningProperty.FindPropertyRelative("distanceMin");
			p_lightningDistanceMax				= lightningProperty.FindPropertyRelative("distanceMax");
			p_lightningCameraBias				= lightningProperty.FindPropertyRelative("cameraBias");
			p_lightningMinimumDensity			= lightningProperty.FindPropertyRelative("minimumDensity");
			p_lightningSpanMin					= lightningProperty.FindPropertyRelative("intervalMin");
			p_lightningSpanMax					= lightningProperty.FindPropertyRelative("intervalMax");
			p_lightningRestrikeChance			= lightningProperty.FindPropertyRelative("restrikeChance");
			p_enableLightningInEditor			= lightningProperty.FindPropertyRelative("enableInEditor");

			// ---------- Time of Day ----------
			var todProperty						= serializedObject.FindProperty("m_TimeOfDay");
			p_useTimeOfDay						= todProperty.FindPropertyRelative("enable");
			p_useLocalTime						= todProperty.FindPropertyRelative("useLocalTime");
			p_todDynamicMoon					= todProperty.FindPropertyRelative("affectsMoon");
			p_todPlay							= todProperty.FindPropertyRelative("play");
			p_todPlayInEditor					= todProperty.FindPropertyRelative("playInEditor");
			p_todPlaySpeed						= todProperty.FindPropertyRelative("playSpeed");
			p_todLatitude						= todProperty.FindPropertyRelative("latitude");
			p_todLongitude						= todProperty.FindPropertyRelative("longitude");
			p_todYear							= todProperty.FindPropertyRelative("year");
			p_todMonth							= todProperty.FindPropertyRelative("month");
			p_todDay							= todProperty.FindPropertyRelative("day");
			p_todTime							= todProperty.FindPropertyRelative("time");

			// ---------- Weather Presets ----------
			p_activePreset						= serializedObject.FindProperty("activePreset");
			p_fadeDuration						= serializedObject.FindProperty("fadeDuration");
			p_editorFadeDuration				= serializedObject.FindProperty("editorFadeDuration");
		}
		#endregion

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			OverCloud oc = (OverCloud)target;

			//DrawDefaultInspector();

			if ( ListStyle == null )
			{
				ButtonStyle = "Button";
				ButtonStyleToggle = new GUIStyle(ButtonStyle);
				ButtonStyleToggle.normal.background = ButtonStyleToggle.active.background;
				ListStyle = new GUIStyle(ButtonStyle);
				ListStyle.normal.background = Texture2D.whiteTexture;
				ListStyle.margin = new RectOffset(0, 0, 0, 0);
				ListStyle.alignment = TextAnchor.MiddleLeft;
			}
			
			if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Skybox && OverCloud.timeOfDay.play && OverCloud.timeOfDay.playSpeed > Mathf.Epsilon)
			{
				EditorGUILayout.HelpBox("The Environment Ambient mode is set to \"Skybox\" and Time Of Day is set to play. \n" +
					"This might/will force an update of the environment lighting at some point, which can cause performance spikes. \n" +
					"Consider switching the Environment Ambient mode to \"Gradient\" or disable dynamic Time Of Day to improve performance.", MessageType.Warning);
			}

			GUILayout.Label("Components", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(p_sun);
			EditorGUILayout.PropertyField(p_moon);
			EditorGUILayout.PropertyField(p_cloudMaterial);
			EditorGUILayout.PropertyField(p_skyMaterial);

			EditorGUILayout.Space();

			oc.showDrawerCloud = CustomUI.Foldout("Volumetric Clouds", oc.showDrawerCloud);
			if (oc.showDrawerCloud)
			{
				EditorGUI.indentLevel = 1;

				EditorGUILayout.PropertyField(p_radius, new GUIContent("Cloud Plane Radius"));

				EditorGUILayout.Space();

				EditorGUI.indentLevel = 2;
				oc.showDrawerCloudCompositorTexture = EditorGUILayout.Foldout(oc.showDrawerCloudCompositorTexture, new GUIContent("Compositor Preview", "Show a preview of the compositor texture."), true);
				if (oc.showDrawerCloudCompositorTexture && OverCloud.compositorTexture)
				{
					//GUILayout.Label("Compositor Texture:");
			
					var rect = GUILayoutUtility.GetLastRect();
					if (rect.width > 1)
						inspectorWidth = rect.width - 16;

					inspectorWidth = Screen.width - 70;

					GUILayout.BeginHorizontal();
					GUILayout.Label(Texture2D.blackTexture, GUILayout.MaxWidth(18), GUILayout.MaxHeight(18)); // Really stupid way of indenting a label
					GUILayout.Label(OverCloud.compositorTexture, GUILayout.MaxWidth(inspectorWidth), GUILayout.MaxHeight(inspectorWidth));
					GUILayout.EndHorizontal();
				}
				EditorGUI.indentLevel = 1;

				EditorGUILayout.PropertyField(p_compositorRes, new GUIContent("Compositor Resolution"));
				EditorGUILayout.PropertyField(p_compositorBlur, new GUIContent("Compositor Blur"));
			
				EditorGUILayout.PropertyField(p_noiseTex);
				EditorGUILayout.PropertyField(p_noiseScale);
				EditorGUILayout.PropertyField(p_noiseMacroScale);

				EditorGUILayout.PropertyField(p_particleCount);
				EditorGUILayout.PropertyField(p_lodMultiplier, new GUIContent("LOD Radius Multiplier"));
				EditorGUILayout.PropertyField(p_lodSize, new GUIContent("LOD Particle Size"));

				EditorGUILayout.Space();

				oc.showDrawer3DNoise = EditorGUILayout.Foldout(oc.showDrawer3DNoise, new GUIContent("Shape (3D Noise)", ""), true);
				if (oc.showDrawer3DNoise)
				{
					GUILayout.Label("Noise Pass 1", EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(p_noiseTiling_A,    new GUIContent("Tiling"));
					EditorGUILayout.PropertyField(p_noiseIntensity_A, new GUIContent("Intensity"));

					GUILayout.Label("Noise Pass 2", EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(p_noiseTiling_B,    new GUIContent("Tiling"));
					EditorGUILayout.PropertyField(p_noiseIntensity_B, new GUIContent("Intensity"));

					GUILayout.Label("Noise Shape Settings", EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(p_shapeCenter, new GUIContent("Shape Center"));
					EditorGUILayout.PropertyField(p_baseDensityIncrease, new GUIContent("Base Density Increase"));
					EditorGUILayout.PropertyField(p_noiseErosion, new GUIContent("Erosion"));

					GUILayout.Label("Noise Alpha Settings", EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(p_alphaEdgeLower, new GUIContent("Lower Edge"));
					EditorGUILayout.PropertyField(p_alphaEdgeUpper, new GUIContent("Upper Edge"));

					GUILayout.Label("Noise Wind Settings", EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(p_noiseTurbulence);
					EditorGUILayout.PropertyField(p_noiseRiseFactor);

					EditorGUILayout.Space();

					EditorGUI.indentLevel = 2;
					oc.showDrawerNoiseGenerator = EditorGUILayout.Foldout(oc.showDrawerNoiseGenerator, new GUIContent("3D Noise Generator", "Can be used to generate a new tiling 3D noise texture. It runs on the CPU and so is quite slow. Eventually it will be replaced by a GPU variant. A higher resolution will give you better quality clouds at the cost of performance. Recommended to leave at 64x64 or 128x128."), true);
					if (oc.showDrawerNoiseGenerator)
					{
						//GUILayout.Label("3D Noise Generator", EditorStyles.boldLabel);

						GUILayout.Label("Slice Preview:");
			
						var rect = GUILayoutUtility.GetLastRect();
						if (rect.width > 1)
							inspectorWidth = rect.width - 16;

						inspectorWidth = Screen.width - 40;

						//GUILayout.Label(oc._3DNoiseSlice, GUILayout.MaxWidth(inspectorWidth), GUILayout.MaxHeight(inspectorWidth), GUILayout.ExpandWidth(true));
						float size = inspectorWidth / 3f;
						oc.UpdateNoisePreview();
						GUILayout.BeginHorizontal();
						GUI.DrawTexture(new Rect(16, rect.max.y, size, size), oc._3DNoiseSlice[1]);
						GUI.DrawTexture(new Rect(16 + size, rect.max.y, size, size), oc._3DNoiseSlice[0]);
						GUI.DrawTexture(new Rect(16 + size * 2f, rect.max.y, size, size), oc._3DNoiseSlice[2]);

						EditorGUI.DrawRect(new Rect(16, rect.max.y + size - 16, 40, 16), new Color(0, 0, 0, 0.6f));
						EditorGUI.DrawRect(new Rect(16 + size, rect.max.y + size - 16, 84, 16), new Color(0, 0, 0, 0.6f));
						EditorGUI.DrawRect(new Rect(16 + size * 2, rect.max.y + size - 16, 48, 16), new Color(0, 0, 0, 0.6f));

						GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
						labelStyle.normal.textColor = Color.white;
						GUI.Label(new Rect(16, rect.max.y + size - 16, size, 16), "Perlin", labelStyle);
						GUI.Label(new Rect(16 + size, rect.max.y + size - 16, size, 16), "Perlin-Worley", labelStyle);
						GUI.Label(new Rect(16 + size * 2, rect.max.y + size - 16, size, 16), "Worley", labelStyle);
						GUILayout.EndHorizontal();

						EditorGUILayout.LabelField("", GUILayout.MinHeight(size));

						EditorGUILayout.PropertyField(p_3DNoiseResolution);
						GUILayout.Label("Perlin", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(p_perlinPeriods, new GUIContent("Base Period"));
						EditorGUILayout.PropertyField(p_perlinOctaves);
						EditorGUILayout.PropertyField(p_perlinBrightness);
						EditorGUILayout.PropertyField(p_perlinContrast);
						GUILayout.Label("Worley", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(p_worleyPeriods, new GUIContent("Base Period"));
						EditorGUILayout.PropertyField(p_worleyOctaves);
						EditorGUILayout.PropertyField(p_worleyBrightness);
						EditorGUILayout.PropertyField(p_worleyContrast);
						if(GUILayout.Button("Generate 3D Noise Texture"))
						{
							oc.InitializeNoise(true);
						}
						if(GUILayout.Button("Load 3D Noise Texture"))
						{
							oc.InitializeNoise(false);
						}
					}
				}

				EditorGUI.indentLevel = 1;
			}

			EditorGUILayout.Space();

			oc.showDrawerCirrus = CustomUI.Foldout("2D Clouds", oc.showDrawerCirrus);
			if (oc.showDrawerCirrus)
			{
				EditorGUI.indentLevel = 1;

				GUILayout.Label("Layers", EditorStyles.boldLabel);

				EditorGUILayout.Space();

				if (OverCloud.cloudPlanes.Length > 0)
				{
					// List border
					var rectPrev = GUILayoutUtility.GetLastRect();
					EditorGUI.DrawRect(new Rect(rectPrev.x-1, rectPrev.y+rectPrev.height-1, Screen.width-32, OverCloud.cloudPlanes.Length * 18 + 3), Color.black);

					// List
					for (int i = 0; i < OverCloud.cloudPlanes.Length; i++)
					{
						var tmp = GUI.backgroundColor;
						GUI.backgroundColor = i == oc.drawerSelectedCloudPlane ? buttonColorDown : buttonColorUp;
						if (GUILayout.Button(OverCloud.cloudPlanes[i].name, ListStyle))
						{
							if (i != oc.drawerSelectedCloudPlane)
							{
								Undo.RecordObject(oc, "Change selected cloud plane");

								oc.drawerSelectedCloudPlane = i;

								SceneView.RepaintAll();

								GUI.FocusControl("");
							}
						}
						GUI.backgroundColor = tmp;
					}

					GUILayout.BeginHorizontal();

					if (GUILayout.Button("+"))
					{
						Undo.RecordObject(oc, "Add cloud plane");
						oc.AddCloudPlane();
						PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
					}

					GUI.enabled = OverCloud.cloudPlanes.Length > 0;
					if (GUILayout.Button("-"))
					{
						Undo.RecordObject(oc, "Remove cloud plane");
						oc.DeleteCloudPlane();
						PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
					}
					
					GUI.enabled = true;
					GUILayout.EndHorizontal();

					// Cloud plane parameters
					if (oc.drawerSelectedCloudPlane > -1 && oc.drawerSelectedCloudPlane < OverCloud.cloudPlanes.Length)
					{
						var cloudPlane = OverCloud.cloudPlanes[oc.drawerSelectedCloudPlane];
						GUILayout.Space(6);

						Texture2D texture		= (Texture2D)EditorGUILayout.ObjectField("Texture", cloudPlane.texture, typeof(Texture2D), false);
						var color				= EditorGUILayout.ColorField("Color", cloudPlane.color);
						var name				= EditorGUILayout.TextField("Name", cloudPlane.name);
						var scale				= EditorGUILayout.FloatField("Scale", cloudPlane.scale);
						var detailScale			= EditorGUILayout.FloatField("Detail Scale", cloudPlane.detailScale);
						var height				= EditorGUILayout.FloatField("Height", cloudPlane.height);
						var opacity				= EditorGUILayout.Slider("Opacity", cloudPlane.opacity, 0, 2);
						var lightPenetration	= EditorGUILayout.Slider("Light Penetration", cloudPlane.lightPenetration, 0, 4);
						var lightAbsorption		= EditorGUILayout.Slider("Light Absorption", cloudPlane.lightAbsorption, 0, 8);
						var windTimescale		= EditorGUILayout.FloatField("Wind Timescale", cloudPlane.windTimescale);

						// Update values
						bool modified = false;
						if (texture != cloudPlane.texture)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.texture = texture;
							modified = true;
						}
						if (color != cloudPlane.color)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.color = color;
							modified = true;
						}
						if (name != cloudPlane.name)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.name = name;
							modified = true;
						}
						if (scale != cloudPlane.scale)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.scale = scale;
							modified = true;
						}
						if (detailScale != cloudPlane.detailScale)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.detailScale = detailScale;
							modified = true;
						}
						if (height != cloudPlane.height)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.height = height;
							modified = true;
						}
						if (opacity != cloudPlane.opacity)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.opacity = opacity;
							modified = true;
						}
						if (lightPenetration != cloudPlane.lightPenetration)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.lightPenetration = lightPenetration;
							modified = true;
						}
						if (lightAbsorption != cloudPlane.lightAbsorption)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.lightAbsorption = lightAbsorption;
							modified = true;
						}
						if (windTimescale != cloudPlane.windTimescale)
						{
							Undo.RecordObject(oc, "Change cloud plane parameter");
							cloudPlane.windTimescale = windTimescale;
							modified = true;
						}

						if (modified)
						{
							EditorUtility.SetDirty(oc);
							PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
						}
					}
				}
				else
				{
					GUILayout.Label("No cloud planes. Press + to add one.");
					if (GUILayout.Button("+"))
					{
						Undo.RecordObject(oc, "Add cloud plane");
						oc.AddCloudPlane();
						PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
					}
				}

				EditorGUILayout.Space();

				EditorGUI.indentLevel = 0;
			}

			EditorGUILayout.Space();

			oc.showDrawerAtmosphere = CustomUI.Foldout("Atmosphere", oc.showDrawerAtmosphere);
			if (oc.showDrawerAtmosphere)
			{
				EditorGUI.indentLevel = 1;
				EditorGUILayout.PropertyField(p_overrideSkyboxMaterial, new GUIContent("Override Skybox Material"), true);
				EditorGUILayout.PropertyField(p_atmosphereExposure);
				EditorGUILayout.PropertyField(p_atmosphereDensity);
				EditorGUILayout.PropertyField(p_atmosphereFarClipFade);

				GUILayout.Label("Precomputation Parameters", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("Changing the precomputation parameters will recompute the atmosphere lookup tables. This is a slow operation, and thus it is strongly recommended not to do every frame.", MessageType.Info);
				EditorGUILayout.PropertyField(p_atmosphereComputeShader, new GUIContent("Compute Shader"));
				EditorGUILayout.PropertyField(p_planetScale);
				EditorGUILayout.PropertyField(p_atmosphereHeightScale, new GUIContent("Height Scale"));
				// The precomputed mie scattering suffers from precision issues and has been substituted for a realtime variant below
				//EditorGUILayout.PropertyField(p_atmosphereMie, new GUIContent("Mie Density"));
				//EditorGUILayout.PropertyField(p_atmospherePhase, new GUIContent("Mie Phase"));
				EditorGUILayout.PropertyField(p_atmosphereRayleigh,	new GUIContent("Rayleigh Density"));
				EditorGUILayout.PropertyField(p_atmosphereOzone,	new GUIContent("Ozone Density"));
				

				GUILayout.Label("Sun", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_skyActualSunColor);
				EditorGUILayout.PropertyField(p_skySunSize);
				EditorGUILayout.PropertyField(p_skySunIntensity);
				EditorGUILayout.PropertyField(p_solarEclipseColor);

				GUILayout.Label("Moon", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_skyMoonCube);
				EditorGUILayout.PropertyField(p_skyActualMoonColor);
				EditorGUILayout.PropertyField(p_skyMoonSize);
				EditorGUILayout.PropertyField(p_skyMoonIntensity);
				EditorGUILayout.PropertyField(p_lunarEclipseColor);

				GUILayout.Label("Earth", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_earthColor);

				EditorGUILayout.Space();

				GUILayout.Label("Mie Scattering", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_mieScatteringIntensity,		new GUIContent("Intensity"));
				EditorGUILayout.PropertyField(p_mieScatteringPhase,			new GUIContent("Phase"));
				EditorGUILayout.PropertyField(p_mieScatteringFogPhase,		new GUIContent("Fog Phase"));
				EditorGUILayout.PropertyField(p_mieScatteringDistanceFadeA,	new GUIContent("Distance Fade A"));
				EditorGUILayout.PropertyField(p_mieScatteringDistanceFadeB,	new GUIContent("Distance Fade B"));

				EditorGUILayout.Space();

				GUILayout.Label("Night", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_nightScattering);

				EditorGUILayout.Space();

				GUILayout.Label("Stars", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_skyStarsCube);
				EditorGUILayout.PropertyField(p_skyStarsIntensity);

				EditorGUILayout.Space();

				GUILayout.Label("Scattering Mask", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_scatteringMaskRange,			new GUIContent("Range"));
				EditorGUILayout.PropertyField(p_scatteringMaskIntensity,		new GUIContent("Intensity"));
				EditorGUILayout.PropertyField(p_scatteringMaskSoftness,			new GUIContent("Softness"));
				EditorGUILayout.PropertyField(p_scatteringMaskFloor,			new GUIContent("Floor Height"));

				EditorGUILayout.Space();
				EditorGUI.indentLevel = 0;
			}

			EditorGUILayout.Space();

			oc.showDrawerLighting = CustomUI.Foldout("Lighting", oc.showDrawerLighting);
			if (oc.showDrawerLighting)
			{
				EditorGUI.indentLevel = 1;
				GUILayout.Label("Cloud Lighting", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_lightingAlbedo);
				EditorGUILayout.PropertyField(p_lightingPrecipitationAlbedo);
				EditorGUILayout.PropertyField(p_lightingEccentricity);
				EditorGUILayout.PropertyField(p_lightingSilverIntensity);
				EditorGUILayout.PropertyField(p_lightingSilverSpread);
				EditorGUILayout.PropertyField(p_lightingDirect);
				EditorGUILayout.PropertyField(p_lightingDirectAbsorption);
				EditorGUILayout.PropertyField(p_lightingIndirect);
				EditorGUILayout.PropertyField(p_lightingIndirectAbsorption);
				//EditorGUILayout.PropertyField(p_lightingIndirectSoftness);
				EditorGUILayout.PropertyField(p_lightingAmbient);
				EditorGUILayout.PropertyField(p_lightingAmbientAbsorption);
				EditorGUILayout.PropertyField(p_lightingAmbientDesaturation);
				EditorGUILayout.PropertyField(p_lightingPowderSize);
				EditorGUILayout.PropertyField(p_lightingPowderIntensity);

				GUILayout.Label("Cloud Shadows", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_cloudShadowsEnabled,			new GUIContent("Enabled"));
				EditorGUILayout.PropertyField(p_cloudShadowsMode,				new GUIContent("Mode"));
				EditorGUILayout.PropertyField(p_cloudShadowsResolution,			new GUIContent("Resolution"));
				EditorGUILayout.PropertyField(p_cloudShadowsCoverage,			new GUIContent("Coverage"));
				EditorGUILayout.PropertyField(p_cloudShadowsBlur,				new GUIContent("Blur"));
				EditorGUILayout.PropertyField(p_cloudShadowsEdgeTex,			new GUIContent("Edge Texture"));
				EditorGUILayout.PropertyField(p_cloudShadowsEdgeTexScale,		new GUIContent("Edge Texture Scale"));
				EditorGUILayout.PropertyField(p_cloudShadowsEdgeTexIntensity,	new GUIContent("Edge Texture Intensity"));
				EditorGUILayout.PropertyField(p_cloudShadowsSharpen,			new GUIContent("Sharpen"));

				GUILayout.Label("Cloud AO", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_cloudAOIntensity,	new GUIContent("Intensity"));
				EditorGUILayout.PropertyField(p_cloudAOHeight,		new GUIContent("Height Falloff"));

				GUILayout.Label("Ambient Lighting", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_ambientSky);
				EditorGUILayout.PropertyField(p_ambientEquator);
				EditorGUILayout.PropertyField(p_ambientGround);
				EditorGUILayout.PropertyField(p_lunarEclipseInfluence);
				EditorGUILayout.PropertyField(p_ambientMultiplier);

				EditorGUI.indentLevel = 0;
			}

			EditorGUILayout.Space();

			oc.showDrawerWeather = CustomUI.Foldout("Weather Effects", oc.showDrawerWeather);
			if (oc.showDrawerWeather)
			{
				EditorGUI.indentLevel = 1;
				GUILayout.Label("Wind", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_timescale, new GUIContent("Wind Timescale"));

				GUILayout.Label("Rain Mask Rendering", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_rainMaskResolution,			new GUIContent("Resolution"));
				EditorGUILayout.PropertyField(p_rainMaskLayers,				new GUIContent("Layers"));
				EditorGUILayout.PropertyField(p_rainMaskRadius,				new GUIContent("Radius"));

				GUILayout.Label("Rain Mask Sampling", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_rainMaskFalloff,			new GUIContent("Height Falloff"));
				EditorGUILayout.PropertyField(p_rainMaskBlur,				new GUIContent("Blur"));
				EditorGUILayout.PropertyField(p_rainMaskOffsetTex,			new GUIContent("Offset Texture"));
				EditorGUILayout.PropertyField(p_rainMaskOffset,				new GUIContent("Offset Amount"));

				GUILayout.Label("Rain Normals, Albedo & Gloss", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_rainRippleTex,				new GUIContent("Ripple Texture"));
				EditorGUILayout.PropertyField(p_rainFlowTex,				new GUIContent("Flow Texture"));

				EditorGUILayout.PropertyField(p_rainMaskAlbedoDarken,		new GUIContent("Albedo Darken"));
				EditorGUILayout.PropertyField(p_rainMaskRoughnessDecrease,	new GUIContent("Roughness Decrease"));
				EditorGUILayout.PropertyField(p_rainRippleIntensity,		new GUIContent("Ripple Intensity"));
				EditorGUILayout.PropertyField(p_rainRippleScale,			new GUIContent("Ripple Scale"));
				EditorGUILayout.PropertyField(p_rainRippleTimescale,		new GUIContent("Ripple Timescale"));
				EditorGUILayout.PropertyField(p_rainFlowIntensity,			new GUIContent("Flow Intensity"));
				EditorGUILayout.PropertyField(p_rainFlowScale,				new GUIContent("Flow Scale"));
				EditorGUILayout.PropertyField(p_rainFlowTimescale,			new GUIContent("Flow Timescale"));

				GUILayout.Label("Lightning", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_lightningObject);
				EditorGUILayout.PropertyField(p_lightningDistanceMin);
				EditorGUILayout.PropertyField(p_lightningDistanceMax);
				EditorGUILayout.PropertyField(p_lightningCameraBias);
				EditorGUILayout.PropertyField(p_lightningMinimumDensity);
				EditorGUILayout.PropertyField(p_lightningSpanMin);
				EditorGUILayout.PropertyField(p_lightningSpanMax);
				EditorGUILayout.PropertyField(p_lightningRestrikeChance);
				EditorGUILayout.PropertyField(p_enableLightningInEditor, new GUIContent("Enable In Editor"));
				EditorGUI.indentLevel = 0;
			}

			EditorGUILayout.Space();

			oc.showDrawerTimeOfDay = CustomUI.Foldout("Time Of Day", oc.showDrawerTimeOfDay);
			if (oc.showDrawerTimeOfDay)
			{
				EditorGUI.indentLevel = 1;
				EditorGUILayout.PropertyField(p_useTimeOfDay, true);
				EditorGUILayout.PropertyField(p_todDynamicMoon, new GUIContent("Affect Moon"));
				EditorGUILayout.PropertyField(p_useLocalTime, true);
				EditorGUILayout.PropertyField(p_todPlay, true);
				EditorGUILayout.PropertyField(p_todPlayInEditor, true);
				EditorGUILayout.PropertyField(p_todPlaySpeed);
				GUILayout.Label("Location Coordinates", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_todLatitude);
				EditorGUILayout.PropertyField(p_todLongitude);
				GUILayout.Label("Time & Date", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(p_todYear);
				EditorGUILayout.PropertyField(p_todMonth);
				EditorGUILayout.PropertyField(p_todDay);
				EditorGUILayout.PropertyField(p_todTime);
				EditorGUI.indentLevel = 0;
			}

			EditorGUILayout.Space();

			oc.showDrawerWeatherPresets = CustomUI.Foldout("Weather Presets", oc.showDrawerWeatherPresets);
			if (oc.showDrawerWeatherPresets)
			{
				EditorGUI.indentLevel = 1;
				EditorGUILayout.PropertyField(p_activePreset);
				EditorGUILayout.PropertyField(p_fadeDuration);
				EditorGUILayout.PropertyField(p_editorFadeDuration);
				
				oc.showDrawerCustomFloats = EditorGUILayout.Foldout(oc.showDrawerCustomFloats, new GUIContent("Custom Floats", "Custom Floats enable you to add custom data to the weather presets. These can then be accessed with OverCloud.current.GetCustomFloat(). Additionally, they can be automatically set as global shader paramters if the shaderParameter string is set."), true);
				if (oc.showDrawerCustomFloats)
				{
					if (oc.customFloatsCount > 0)
					{
						// List border
						var rectPrev = GUILayoutUtility.GetLastRect();
						EditorGUI.DrawRect(new Rect(rectPrev.x-1, rectPrev.y+rectPrev.height+1, Screen.width-17, oc.customFloatsCount * 18 + 3), Color.black);

						for (int i = 0; i < oc.customFloatsCount; i++)
						{
							var tmp = GUI.backgroundColor;
							GUI.backgroundColor = i == oc.drawerSelectedCustomFloat ? buttonColorDown : buttonColorUp;
							var name = oc.GetCustomFloatName(i);//path.name != "" ? path.name + " (" + path.type.ToString() + ")" : path.type.ToString() + " " + i;
							if (GUILayout.Button(name, ListStyle))
							{
								if (i != oc.drawerSelectedCustomFloat)
								{
									Undo.RecordObject(oc, "Change selected custom float");

									oc.drawerSelectedCustomFloat = i;

									SceneView.RepaintAll();

									GUI.FocusControl("");
								}
							}
							GUI.backgroundColor = tmp;
						}

						GUILayout.BeginHorizontal();

						if (GUILayout.Button("+"))
						{
							Undo.RecordObject(oc, "Add custom float");
							oc.AddCustomFloat();
							// Need to call this because the changes to the prefab are not done via the serialized object
							PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
							//serializedObject.Update();
						}

						GUI.enabled = oc.customFloatsCount > 0;
						if (GUILayout.Button("-"))
						{
							Undo.RecordObject(oc, "Remove custom float");
							//if (Event.current.shift || EditorUtility.DisplayDialog("Delete custom float",
							//	"Are you sure? This is an undoable operation.\n(Hold shift to skip prompt)", "Yes", "No"))
							{
								oc.DeleteCustomFloat();
								// Need to call this because the changes to the prefab are not done via the serialized object
								PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
								//serializedObject.Update();
							}
						}
						GUI.enabled = true;
						GUILayout.EndHorizontal();

						if (oc.drawerSelectedCustomFloat > -1 && oc.drawerSelectedCustomFloat < oc.customFloatsCount)
						{
							GUILayout.Space(6);

							string  name			= oc.GetCustomFloatName(oc.drawerSelectedCustomFloat);
							string  shaderParameter = oc.GetCustomFloatShaderParameter(oc.drawerSelectedCustomFloat);

							name			= EditorGUILayout.TextField("Name", name);
							shaderParameter = EditorGUILayout.TextField("Shader Parameter", shaderParameter);

							// Update values
							bool modified = false;
							if (name != oc.GetCustomFloatName(oc.drawerSelectedCustomFloat))
							{
								Undo.RecordObject(oc, "Change custom float parameter");
								oc.SetCustomFloatName(oc.drawerSelectedCustomFloat, name);
								modified = true;
							}
							if (shaderParameter != oc.GetCustomFloatShaderParameter(oc.drawerSelectedCustomFloat))
							{
								Undo.RecordObject(oc, "Change custom float parameter");
								oc.SetCustomFloatShaderParameter(oc.drawerSelectedCustomFloat, shaderParameter);
								modified = true;
							}

							if (modified)
								PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
						}
					}
					else
					{
						GUILayout.Label("No custom floats. Press + to add one.");

						if (GUILayout.Button("+"))
						{
							Undo.RecordObject(oc, "Add custom float");
							oc.AddCustomFloat();
							// Need to call this because the changes to the prefab are not done via the serialized object
							PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
							//serializedObject.Update();
						}
					}

					EditorGUILayout.Space();
				}

				//EditorGUILayout.PropertyField(p_presets, true);

				EditorGUILayout.Space();

				GUILayout.Label("Presets", EditorStyles.boldLabel);

				var presetsProperty	= serializedObject.FindProperty("m_Presets");
				for (int i = 0; i < presetsProperty.arraySize; i++)
				{
					var presetProperty = presetsProperty.GetArrayElementAtIndex(i);
					EditorGUILayout.PropertyField(presetProperty);
					if (presetProperty.isExpanded)
					{
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("name"));

						GUILayout.Label("Clouds", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("cloudPlaneAltitude"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("cloudPlaneHeight"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("cloudiness"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("macroCloudiness"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("sharpness"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("macroSharpness"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("opticalDensity"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("lightingDensity"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("cloudShadowsDensity"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("cloudShadowsOpacity"));

						GUILayout.Label("Weather", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("windMultiplier"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("precipitation"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("lightningChance"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("wetnessRemap"), new GUIContent("Wetness Sharpness"));
						//EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("wetnessDarken"));
						//EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("wetnessGloss"));

						GUILayout.Label("Fog", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogDensity"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogBlend"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogAlbedo"));
						//EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogDirectIntensity"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogAmbientIntensity"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogShadow"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogHeight"));
						EditorGUILayout.PropertyField(presetProperty.FindPropertyRelative("fogFalloff"));

						if (oc.customFloatsCount > 0)
						{
							GUILayout.Label("Custom floats", EditorStyles.boldLabel);
							var floatsProperty = presetProperty.FindPropertyRelative("customFloats");
							for (int u = 0; u < Mathf.Min(floatsProperty.arraySize, oc.customFloatsCount); u++)
							{
								EditorGUILayout.PropertyField(floatsProperty.GetArrayElementAtIndex(u), new GUIContent(oc.GetCustomFloatName(u)));
							}
						}
					}
				}

				EditorGUILayout.Space();

				if (GUILayout.Button( new GUIContent("Add Preset", "Add a new weather preset. (To delete an existing preset, right-click it and select \"Delete Array Element\")")))
				{
					Undo.RecordObject(oc, "Add weather preset");
					oc.AddWeatherPreset();
					// Need to call this because the changes to the prefab are not done via the serialized object
					PrefabUtility.RecordPrefabInstancePropertyModifications(oc);
				}

				EditorGUI.indentLevel = 0;
			}

			EditorGUILayout.Space();

			serializedObject.ApplyModifiedProperties();
		}
	}
}
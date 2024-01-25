//#define POST_PROCESSING_STACK_V1

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if POST_PROCESSING_STACK_V1
using UnityEngine.PostProcessing;
#endif

namespace OC.ExampleContent
{
	public class Demo : MonoBehaviour
	{
		[SerializeField]
		OverCloudCamera			m_OverCloudCamera	 = null;

		#if POST_PROCESSING_STACK_V1
		[SerializeField]
		PostProcessingBehaviour	m_PostProcessing	 = null;

		[SerializeField]
		PostProcessingProfile	m_EffectsOn			 = null;

		[SerializeField]
		PostProcessingProfile	m_EffectsOff		 = null;
		#endif

		[SerializeField]
		Text					m_ControlsText		 = null;

		[SerializeField]
		Text					m_OutputText		 = null;

		[SerializeField]
		Text					m_FPSText			 = null;

		[SerializeField]
		ReflectionProbe			m_DynamicReflectionProbe  = null;

		[SerializeField]
		OverCloudProbe			m_CloudProbe		 = null;

		[SerializeField]
		AudioLowPassFilter		m_PropellerFilter	 = null;

		string					m_CachedString;
		int						m_CloudQuality = 2;

		private void Start()
		{
			m_CachedString = m_ControlsText.text;
			UpdateText();
			Application.targetFrameRate = 60;
		}

		void UpdateText ()
		{
			m_ControlsText.text = m_CachedString + "\n";
			m_ControlsText.text += "Cloud Quality: ";
			switch (m_CloudQuality)
			{
				case 0:
					m_ControlsText.text += "Low";
				break;
				case 1:
					m_ControlsText.text += "Medium";
				break;
				case 2:
					m_ControlsText.text += "High";
				break;
			}
		}

		void Update ()
		{
			// Toggle cloud quality
			if (Input.GetKeyDown("q"))
			{
				m_CloudQuality++;
				if (m_CloudQuality > 2)
					m_CloudQuality = 0;
				switch (m_CloudQuality)
				{
					case 0:
						m_OverCloudCamera.downsampleFactor = DownSampleFactor.Quarter;
						m_OverCloudCamera.renderScatteringMask = false;
						m_OverCloudCamera.highQualityClouds = false;
						m_OverCloudCamera.lightSampleCount = SampleCount.Low;
					break;
					case 1:
						m_OverCloudCamera.downsampleFactor = DownSampleFactor.Quarter;
						m_OverCloudCamera.renderScatteringMask = true;
						m_OverCloudCamera.highQualityClouds = true;
						m_OverCloudCamera.lightSampleCount = SampleCount.Normal;
					break;
					case 2:
						m_OverCloudCamera.downsampleFactor = DownSampleFactor.Half;
						m_OverCloudCamera.renderScatteringMask = true;
						m_OverCloudCamera.highQualityClouds = true;
						m_OverCloudCamera.lightSampleCount = SampleCount.High;
					break;
				}

				UpdateText();
			}

			m_FPSText.text = "FPS: " + Mathf.CeilToInt(1f / Time.smoothDeltaTime);

			// Weather presets
			if (Input.GetKeyDown("1")) OverCloud.SetWeatherPreset("Clear");
			if (Input.GetKeyDown("2")) OverCloud.SetWeatherPreset("Broken");
			if (Input.GetKeyDown("3")) OverCloud.SetWeatherPreset("Overcast");
			if (Input.GetKeyDown("4")) OverCloud.SetWeatherPreset("Foggy");
			if (Input.GetKeyDown("5")) OverCloud.SetWeatherPreset("Rain");
			if (Input.GetKeyDown("6")) OverCloud.SetWeatherPreset("Storm");

			// Toggle controls text
			if (Input.GetKeyDown("h"))
			{
				m_OutputText.enabled	= !m_ControlsText.enabled;
				m_FPSText.enabled		= !m_ControlsText.enabled;
				m_ControlsText.enabled	= !m_ControlsText.enabled;
			}

			// Toggle dynamic reflection probe
			if (Input.GetKeyDown("r"))
			{
				m_DynamicReflectionProbe.enabled	= !m_DynamicReflectionProbe.enabled;
			}

			#if POST_PROCESSING_STACK_V1
			// Toggle post processing
			if (Input.GetKeyDown("p"))
			{
				if (m_PostProcessing.profile == m_EffectsOn)
					m_PostProcessing.profile = m_EffectsOff;
				else
					m_PostProcessing.profile = m_EffectsOn;
			}
			#endif

			// Apply a low pass filter to the propeller when inside the clouds
			m_PropellerFilter.cutoffFrequency = Mathf.Lerp(22000, 800, m_CloudProbe.density);

			// Change time of day
			float scroll = Input.GetAxis("Mouse ScrollWheel");
			if (scroll > Mathf.Epsilon)
			{
				OverCloud.timeOfDay.time += 0.2f;
			}
			else if (scroll < -Mathf.Epsilon)
			{
				OverCloud.timeOfDay.time -= 0.2f;
			}

			// Toggle controls text
			if (Input.GetKeyDown("space"))
				OverCloud.timeOfDay.play = !OverCloud.timeOfDay.play;

			m_OutputText.text =  "Time of Day - "	+ (OverCloud.timeOfDay.play ? "Playing" : "Paused") + "\n";
			m_OutputText.text += "Timescale - "		+ OverCloud.timeOfDay.playSpeed + "\n";
			m_OutputText.text += "Year - "			+ OverCloud.timeOfDay.year		+ "\n";
			m_OutputText.text += "Month - "			+ OverCloud.timeOfDay.month	+ "\n";
			m_OutputText.text += "Day - "			+ OverCloud.timeOfDay.day		+ "\n";
			int h = OverCloud.timeOfDay.hour;
			int m = OverCloud.timeOfDay.minute;
			int s = OverCloud.timeOfDay.second;
			m_OutputText.text += "Time - "			+ (h < 10 ? "0" : "") + h + ":" + (m < 10 ? "0" : "") + m + ":" + (s < 10 ? "0" : "") + s + "\n";
		}
	}
}
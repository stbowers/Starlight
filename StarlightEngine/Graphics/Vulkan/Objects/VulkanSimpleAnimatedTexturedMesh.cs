using System.Diagnostics;
using StarlightEngine.Graphics.Objects;
using StarlightEngine.Graphics.Math;

namespace StarlightEngine.Graphics.Vulkan.Objects
{
	public class VulkanSimpleAnimatedTexturedMesh: VulkanTexturedMesh, IAnimatedObject
	{
		public struct AnimationKeyframe
		{
			public FMat4 Translation;
			public Quaternion Rotation;
			public float time;
		}

		VulkanAPIManager m_apiManager;

		AnimationKeyframe[] m_keyframes;

		AnimationState m_animationState = new AnimationState();
		Stopwatch m_stopwatch = new Stopwatch();

		public VulkanSimpleAnimatedTexturedMesh(VulkanAPIManager apiManager, string objFile, string textureFile, AnimationKeyframe[] keyframes, FMat4 view, FMat4 proj, FVec4 lightPosition, FVec4 lightColor, float ambientLight, float shineDamper, float reflectivity):
		base(apiManager, objFile, textureFile, keyframes[0].Translation * keyframes[0].Rotation.GetRotationMatrix(), view, proj, lightPosition, lightColor, ambientLight, shineDamper, reflectivity)
		{
			m_apiManager = apiManager;

			m_keyframes = keyframes;

			m_animationState.playing = false;
			m_animationState.length = keyframes[keyframes.Length - 1].time;
			m_animationState.position = 0.0f;

			m_stopwatch.Reset();
			m_stopwatch.Stop();
		}

		public override void Update()
		{
			if (m_animationState.playing)
			{
				if (!m_stopwatch.IsRunning)
				{
					m_stopwatch.Start();
				}

				m_animationState.position = (m_stopwatch.ElapsedMilliseconds / 1000.0f);

				int keyframeIndex = 0;
				foreach (AnimationKeyframe keyframe in m_keyframes)
				{
					if (m_animationState.position >= keyframe.time)
					{
						if (keyframeIndex + 1 < m_keyframes.Length)
						{
							keyframeIndex++;
						}
					}
				}

				AnimationKeyframe lastFrame = m_keyframes[keyframeIndex - 1];
				AnimationKeyframe thisFrame = m_keyframes[keyframeIndex];

				float interpolationFactor = (m_animationState.position - lastFrame.time) / (thisFrame.time - lastFrame.time);

				FMat4 translation = FMat4.Interpolate(lastFrame.Translation, thisFrame.Translation, interpolationFactor);
				Quaternion rotation = Quaternion.Slerp(lastFrame.Rotation, thisFrame.Rotation, interpolationFactor);

				base.UpdateModelMatrix(translation * rotation.GetRotationMatrix());

				if (m_animationState.position >= m_animationState.length)
				{
					m_animationState.playing = false;
					m_stopwatch.Stop();
				}
			}
			else
			{
				if (!m_stopwatch.IsRunning)
				{
					m_stopwatch.Stop();
				}
			}
		}

		public AnimationState GetAnimationState()
		{
			return m_animationState;
		}

		public void PauseAnimation()
		{
			m_animationState.playing = false;
		}

		public void PlayAnimation()
		{
			m_animationState.playing = true;
		}

		public void SetAnimationPosition(float newPosition)
		{
			m_animationState.position = newPosition;
		}
	}
}

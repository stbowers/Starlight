using System;
namespace StarlightEngine.Graphics.Objects
{
	public struct AnimationState
	{
		public bool playing;
		public float length;
		public float position;
	}

	public interface IAnimatedObject
	{
		AnimationState GetAnimationState();
		void PlayAnimation();
		void PauseAnimation();
		void SetAnimationPosition(float newPosition);
	}

	public static class AnimatedObjectExtensions
	{
		public static void PlayToPosition(this IAnimatedObject animation, float position)
		{
			AnimationState state = animation.GetAnimationState();
			float playTime = position - state.position;
			animation.PlayAnimation();
			if (playTime < 0)
			{
				animation.PauseAnimation();
				return;
			}

			System.Threading.Thread.Sleep((int)(playTime * 1000.0f));
			while (position - animation.GetAnimationState().position > 0)
			{
				// spin lock until animation is done playing
			}
			animation.PauseAnimation();
		}

		public static void PlayToEnd(this IAnimatedObject animation)
		{
			AnimationState state = animation.GetAnimationState();
			animation.PlayToPosition(state.length);
		}
	}
}

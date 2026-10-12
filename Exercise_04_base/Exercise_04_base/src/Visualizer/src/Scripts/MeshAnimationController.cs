using Renderer.Components;

namespace Visualizer.Scripts
{
	public sealed class MeshAnimationController
	{
		private Mesh[] _frames = [];
		private float _accumulator;

		public Mesh[] Frames => _frames;
		public int FrameCount => _frames.Length;
		public bool HasAnimation => _frames.Length > 0;

		public bool IsPlaying { get; set; }
		public bool IsReverse { get; set; }
		public float Speed    { get; set; } = 1.0f;
		public float FPS      { get; set; } = 24.0f;

		/// <summary>0-based float frame position, clamped to [0, FrameCount).</summary>
		public float FramePosition { get; private set; }
		public int CurrentFrameIndex => HasAnimation ? (int)FramePosition % _frames.Length : 0;
		public Mesh? CurrentMesh => HasAnimation ? _frames[CurrentFrameIndex] : null;

		public void LoadSequence(Mesh[] frames)
		{
			_frames = frames;
			_accumulator = 0f;
			FramePosition = 0f;
			IsPlaying = false;
		}

		public void Unload()
		{
			_frames = [];
			_accumulator = 0f;
			FramePosition = 0f;
			IsPlaying = false;
		}

		public void Play()    => IsPlaying = true;
		public void Pause()   => IsPlaying = false;

		public void Reset()
		{
			IsPlaying = false;
			FramePosition = 0f;
			_accumulator = 0f;
		}

		/// <summary>Seeks to a specific frame by index.</summary>
		public void SeekTo(int frameIndex)
		{
			if (!HasAnimation) return;
			FramePosition = Math.Clamp(frameIndex, 0, _frames.Length - 1);
			_accumulator = 0f;
		}

		/// <summary>
		/// Advances playback by <paramref name="deltaTime"/> seconds.
		/// Returns true if the current frame index changed (caller should update the entity mesh).
		/// </summary>
		public bool Update(float deltaTime)
		{
			if (!HasAnimation || !IsPlaying)
				return false;

			int prevIndex = CurrentFrameIndex;

			_accumulator += deltaTime * FPS * Speed;

			while (_accumulator >= 1f)
			{
				_accumulator -= 1f;
				Step();
			}

			return CurrentFrameIndex != prevIndex;
		}

		private void Step()
		{
			if (!HasAnimation) return;

			if (IsReverse)
			{
				FramePosition -= 1f;
				if (FramePosition < 0f)
					FramePosition = _frames.Length - 1;
			}
			else
			{
				FramePosition += 1f;
				if (FramePosition >= _frames.Length)
					FramePosition = 0f;
			}
		}
	}
}

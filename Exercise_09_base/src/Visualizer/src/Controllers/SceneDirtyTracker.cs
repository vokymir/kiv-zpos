using System.Numerics;
using Visualizer.Scripts;

namespace Visualizer.Controllers
{
	/// <summary>
	/// Detects whether view settings, the camera, or the active selection changed since the
	/// last frame, so <see cref="AppWindow"/> can mark the scene as having unsaved changes.
	/// </summary>
	internal sealed class SceneDirtyTracker
	{
		private ViewState _prevViewState = new();
		private Vector3 _prevCamTarget;
		private float _prevCamDistance, _prevCamYaw, _prevCamPitch;
		private int? _prevSelVertex, _prevSelFace;

		public void SnapshotCamera(OrbitCameraController cam)
		{
			_prevCamTarget   = cam.DesiredTarget;
			_prevCamDistance = cam.DesiredDistance;
			_prevCamYaw      = cam.DesiredYaw;
			_prevCamPitch    = cam.DesiredPitch;
		}

		/// <summary>
		/// Compares the current frame's state against the previous frame, updates the snapshot,
		/// and returns true if anything tracked for dirty-detection has changed.
		/// </summary>
		public bool Update(ViewState state, OrbitCameraController cam, int? selectedVertex, int? selectedFace)
		{
			bool dirty = false;

			if (state.LayoutChangedThisFrame || state.GenerateNormalsMetaChangedThisFrame)
				dirty = true;

			// View settings changed
			if (state.RenderMode != _prevViewState.RenderMode ||
			    state.SelectionMode != _prevViewState.SelectionMode ||
			    state.ShowNormals != _prevViewState.ShowNormals ||
			    state.NormalsScale != _prevViewState.NormalsScale ||
			    state.BackfaceCulling != _prevViewState.BackfaceCulling ||
			    state.SelectedShader != _prevViewState.SelectedShader)
				dirty = true;

			// Camera moved
			if (cam.DesiredTarget   != _prevCamTarget   ||
			    cam.DesiredDistance != _prevCamDistance ||
			    cam.DesiredYaw      != _prevCamYaw      ||
			    cam.DesiredPitch    != _prevCamPitch)
				dirty = true;

			// Selection changed
			if (selectedVertex != _prevSelVertex || selectedFace != _prevSelFace)
				dirty = true;

			_prevViewState = state;
			_prevSelVertex = selectedVertex;
			_prevSelFace   = selectedFace;
			SnapshotCamera(cam);

			return dirty;
		}
	}
}

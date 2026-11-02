using System.Linq;
using System.Numerics;
using Core;
using Renderer.Abstractions;
using Renderer.Components;
using Renderer.Objects;
using Renderer.Wrappers.Shaders;
using Visualizer.Scripts;

namespace Visualizer.Controllers
{
	/// <summary>
	/// Maps the live scene (entity transforms, camera, lighting, selection, view settings,
	/// animation) to and from a <see cref="SceneConfig"/> stored in an asset's <c>.scene</c>
	/// sidecar via <see cref="AssetMetadata"/>.
	/// </summary>
	internal sealed class SceneConfigController
	{
		private readonly MeshLibrary _meshLibrary;
		private readonly AnimationLibrary _animLibrary;
		private readonly ShaderLibrary _shaderLibrary;
		private readonly UiController _ui;
		private readonly Entity _entity;
		private readonly Camera _camera;
		private readonly OrbitCameraController _cameraController;
		private readonly SceneLighting _lighting;
		private readonly MeshAnimationController _animController;
		private readonly Action _rebuildSceneHierarchy;
		private readonly Action _snapshotCameraState;

		public SceneConfigController(
			MeshLibrary meshLibrary,
			AnimationLibrary animLibrary,
			ShaderLibrary shaderLibrary,
			UiController ui,
			Entity entity,
			Camera camera,
			OrbitCameraController cameraController,
			SceneLighting lighting,
			MeshAnimationController animController,
			Action rebuildSceneHierarchy,
			Action snapshotCameraState)
		{
			_meshLibrary = meshLibrary;
			_animLibrary = animLibrary;
			_shaderLibrary = shaderLibrary;
			_ui = ui;
			_entity = entity;
			_camera = camera;
			_cameraController = cameraController;
			_lighting = lighting;
			_animController = animController;
			_rebuildSceneHierarchy = rebuildSceneHierarchy;
			_snapshotCameraState = snapshotCameraState;
		}

		public static Material CreateDefaultMaterial(ShaderProgram shader)
		{
			Material mat = new(shader);
			mat.SetParameter(MaterialParams.Tint,             MaterialParams.DefaultTint);
			mat.SetParameter(MaterialParams.SpecularColor,    MaterialParams.DefaultSpecularColor);
			mat.SetParameter(MaterialParams.SpecularStrength, MaterialParams.DefaultSpecularStrength);
			mat.SetParameter(MaterialParams.Shininess,        MaterialParams.DefaultShininess);
			return mat;
		}

		/// <summary>Copies the carry-over material parameters (tint, specular, shininess) from one material to another.</summary>
		public static void CarryOverMaterialParameters(Material from, Material to)
		{
			if (from.TryGetParameter(MaterialParams.Tint,             out Vector3 tint)) to.SetParameter(MaterialParams.Tint,             tint);
			if (from.TryGetParameter(MaterialParams.SpecularColor,    out Vector3 sCol)) to.SetParameter(MaterialParams.SpecularColor,    sCol);
			if (from.TryGetParameter(MaterialParams.SpecularStrength, out float   sSt))  to.SetParameter(MaterialParams.SpecularStrength, sSt);
			if (from.TryGetParameter(MaterialParams.Shininess,        out float   shi))  to.SetParameter(MaterialParams.Shininess,        shi);
		}

		public void ApplyMaterialOverrides(string modelName)
		{
			if (_entity.Material is null) return;
			AssetMetadata? meta = _meshLibrary.GetMetadata(modelName);
			SceneMaterialConfig? ov = meta?.Scene?.Model?.Material;
			if (ov is null) return;

			if (ov.Tint is { Length: >= 3 } t)
				_entity.Material.SetParameter(MaterialParams.Tint, new Vector3(t[0], t[1], t[2]));
			if (ov.SpecularColor is { Length: >= 3 } sc)
				_entity.Material.SetParameter(MaterialParams.SpecularColor, new Vector3(sc[0], sc[1], sc[2]));
			if (ov.SpecularStrength.HasValue)
				_entity.Material.SetParameter(MaterialParams.SpecularStrength, ov.SpecularStrength.Value);
			if (ov.Shininess.HasValue)
				_entity.Material.SetParameter(MaterialParams.Shininess, ov.Shininess.Value);
		}

		public void SaveSceneConfig(SceneState sceneState)
		{
			AssetMetadata? meta = _meshLibrary.GetMetadata(sceneState.CurrentModel);
			if (meta is null) return;

			ViewState state = _ui.State;

			SceneModelConfig model = meta.EnsureModel();
			if (_entity.Material is not null)
			{
				Vector3 tint = MaterialParams.DefaultTint;
				_entity.Material.TryGetParameter(MaterialParams.Tint, out tint);
				Vector3 specColor = MaterialParams.DefaultSpecularColor;
				_entity.Material.TryGetParameter(MaterialParams.SpecularColor, out specColor);
				float specStr = MaterialParams.DefaultSpecularStrength;
				_entity.Material.TryGetParameter(MaterialParams.SpecularStrength, out specStr);
				float shininess = MaterialParams.DefaultShininess;
				_entity.Material.TryGetParameter(MaterialParams.Shininess, out shininess);
				model.Material = new SceneMaterialConfig
				{
					Tint             = [tint.X, tint.Y, tint.Z],
					SpecularColor    = [specColor.X, specColor.Y, specColor.Z],
					SpecularStrength = specStr,
					Shininess        = shininess,
				};
			}

			var rot = _entity.Transforms.Rotation;
			meta.Scene = new SceneConfig
			{
				Model = model,
				Transforms = new SceneTransformsConfig
				{
					Position = [_entity.Transforms.Position.X, _entity.Transforms.Position.Y, _entity.Transforms.Position.Z],
					Rotation = [rot.X, rot.Y, rot.Z, rot.W],
					Scale    = [_entity.Transforms.Scale.X,    _entity.Transforms.Scale.Y,    _entity.Transforms.Scale.Z],
				},
				Camera = new SceneCameraConfig
				{
					Target   = [_cameraController.DesiredTarget.X, _cameraController.DesiredTarget.Y, _cameraController.DesiredTarget.Z],
					Distance = _cameraController.DesiredDistance,
					Yaw      = _cameraController.DesiredYaw,
					Pitch    = _cameraController.DesiredPitch,
				},
				Lighting = new SceneLightingConfig
				{
					Ambient = new SceneAmbientConfig
					{
						Color     = [_lighting.Ambient.Color.X, _lighting.Ambient.Color.Y, _lighting.Ambient.Color.Z],
						Intensity = _lighting.Ambient.Intensity,
					},
					DirectionalLights = _lighting.DirectionalLights.Select(l => new SceneDirectionalLightConfig
					{
						Color     = [l.Color.X, l.Color.Y, l.Color.Z],
						Intensity = l.Intensity,
						Direction = [l.Direction.X, l.Direction.Y, l.Direction.Z],
					}).ToList(),
				},
				Selection = new SceneSelectionConfig
				{
					VertexIndex = _ui.Selection.SelectedVertexIndex,
					FaceIndex   = _ui.Selection.SelectedFaceIndex,
					Mode        = state.SelectionMode.ToString(),
				},
				View = new SceneViewConfig
				{
					RenderMode      = state.RenderMode.ToString(),
					Shader          = state.SelectedShader,
					ShowNormals     = state.ShowNormals,
					NormalsScale    = state.NormalsScale,
					BackfaceCulling = state.BackfaceCulling,
				},
				Animation = sceneState.CurrentAnimation is not null || _animController.HasAnimation ? new SceneAnimationConfig
				{
					// Null for API-provided animations (no named sequence to reload by name).
					SequenceName = sceneState.CurrentAnimation,
					FrameIndex   = _animController.CurrentFrameIndex,
					FPS          = _animController.FPS,
					Speed        = _animController.Speed,
					IsReverse    = _animController.IsReverse,
					IsPlaying    = _animController.IsPlaying,
				} : null,
			};

			string assetPath = AssetPaths.ModelPath(sceneState.CurrentModel);
			meta.Save(assetPath);
		}

		public void ReloadSceneConfig(SceneState sceneState)
		{
			string assetPath = AssetPaths.ModelPath(sceneState.CurrentModel);
			AssetMetadata fresh = AssetMetadata.Load(assetPath);
			_meshLibrary.SetMetadata(sceneState.CurrentModel, fresh);
			if (fresh.Scene is SceneConfig scene)
				ApplySceneConfig(scene, sceneState);
			_snapshotCameraState();
		}

		/// <summary>Applies the loaded scene's material/camera/lighting to the current API mesh.</summary>
		public void ApplyApiSceneConfig(SceneState sceneState)
		{
			_ui.CustomModelPath = sceneState.CurrentModel;
			_ui.RevertModel(sceneState.CurrentModel);

			ApplyMaterialOverrides(sceneState.CurrentModel);
			AssetMetadata? meta = _meshLibrary.GetMetadata(sceneState.CurrentModel);
			if (meta?.Scene is SceneConfig scene)
				ApplySceneConfig(scene, sceneState);
			else
				_cameraController.FocusOnEntity(_entity);
			sceneState.Modified = false;
			_snapshotCameraState();
		}

		public void ApplySceneConfig(SceneConfig cfg, SceneState sceneState)
		{
			ApplyTransforms(cfg);
			ApplyCamera(cfg);
			ApplyLighting(cfg);
			_rebuildSceneHierarchy();
			ApplySelection(cfg);
			ApplyView(cfg);
			ApplyAnimation(cfg, sceneState);

			_snapshotCameraState();
		}

		// Entity transforms — default: identity
		private void ApplyTransforms(SceneConfig cfg)
		{
			var xforms = cfg.Transforms;
			_entity.Transforms.Position = xforms?.Position is { Length: >= 3 } p  ? new Vector3(p[0], p[1], p[2])            : Vector3.Zero;
			_entity.Transforms.Rotation = xforms?.Rotation is { Length: >= 4 } rq ? new Quaternion(rq[0], rq[1], rq[2], rq[3]) : Quaternion.Identity;
			_entity.Transforms.Scale    = xforms?.Scale    is { Length: >= 3 } sc ? new Vector3(sc[0], sc[1], sc[2])           : Vector3.One;
		}

		// Camera — default: same as OnLoad, then focus on entity
		private void ApplyCamera(SceneConfig cfg)
		{
			var cam = cfg.Camera;
			_cameraController.DesiredTarget   = cam?.Target is { Length: >= 3 } t ? new Vector3(t[0], t[1], t[2]) : Vector3.Zero;
			_cameraController.DesiredDistance = cam?.Distance ?? 10f;
			_cameraController.DesiredYaw      = cam?.Yaw      ?? Mathematics.DegreesToRadians(135f);
			_cameraController.DesiredPitch    = cam?.Pitch     ?? Mathematics.DegreesToRadians(-30f);
			if (cam is not null)
			{
				_cameraController.SnapToDesired();
				_cameraController.Apply(_camera);
			}
			else
			{
				_cameraController.FocusOnEntity(_entity);
			}
		}

		// Lighting — default: same two lights as OnLoad
		private void ApplyLighting(SceneConfig cfg)
		{
			_lighting.Ambient.Color     = cfg.Lighting?.Ambient?.Color is { Length: >= 3 } ac ? new Vector3(ac[0], ac[1], ac[2]) : Vector3.One;
			_lighting.Ambient.Intensity = cfg.Lighting?.Ambient?.Intensity ?? 0.1f;

			_lighting.ClearDirectionalLights();
			if (cfg.Lighting?.DirectionalLights is { } dirLights)
			{
				foreach (SceneDirectionalLightConfig dl in dirLights)
					_lighting.AddDirectionalLight(new DirectionalLight
					{
						Color     = dl.Color     is { Length: >= 3 } c ? new Vector3(c[0], c[1], c[2])    : Vector3.One,
						Intensity = dl.Intensity,
						Direction = dl.Direction is { Length: >= 3 } d ? new Vector3(d[0], d[1], d[2])    : new Vector3(0f, -1f, 0f),
					});
			}
			else
			{
				_lighting.AddDirectionalLight(new() { Color = Vector3.One, Intensity = 1.0f, Direction = new Vector3(1f,  -1f, 1f) });
				_lighting.AddDirectionalLight(new() { Color = Vector3.One, Intensity = 0.5f, Direction = new Vector3(-1f, -1f, -1f) });
			}
		}

		// Selection — default: no selection, Vertex mode
		private void ApplySelection(SceneConfig cfg)
		{
			var sel  = cfg.Selection;
			SelectionMode mode = sel?.Mode == nameof(SelectionMode.Face) ? SelectionMode.Face : SelectionMode.Vertex;
			Mesh? mesh = _entity.Mesh;
			int? vertex = sel?.VertexIndex is int vi && mesh is not null && vi >= 0 && vi < mesh.Vertices.Length    ? vi : null;
			int? face   = sel?.FaceIndex   is int fi && mesh is not null && fi >= 0 && fi < mesh.Indices.Length / 3 ? fi : null;
			_ui.RestoreSelection(vertex, face, mode);
		}

		// View settings — defaults: Solid, "default" shader, no normals, backface on
		private void ApplyView(SceneConfig cfg)
		{
			var view = cfg.View;
			_ui.SetRenderMode(view?.RenderMode is string rm && Enum.TryParse<ViewRenderMode>(rm, out var parsed) ? parsed : ViewRenderMode.Solid);

			string targetShader = view?.Shader ?? "default";
			{
				ShaderProgram shader = _shaderLibrary.Get(targetShader);
				Material prev = _entity.Material!;
				Material mat  = CreateDefaultMaterial(shader);
				CarryOverMaterialParameters(prev, mat);
				_entity.Material = mat;
			}

			_ui.SetNormals(view?.ShowNormals ?? false, view?.NormalsScale ?? 1f);
			_ui.SetBackfaceCulling(view?.BackfaceCulling ?? true);
		}

		private void ApplyAnimation(SceneConfig cfg, SceneState sceneState)
		{
			var anim = cfg.Animation;
			if (anim?.SequenceName is string seq)
			{
				Mesh[] frames = _animLibrary.Get(seq);
				_animController.LoadSequence(frames);
				_animController.FPS       = anim.FPS;
				_animController.Speed     = anim.Speed;
				_animController.IsReverse = anim.IsReverse;
				_animController.SeekTo(anim.FrameIndex);
				if (anim.IsPlaying) _animController.Play();
				sceneState.CurrentAnimation = seq;
				_entity.Mesh = _animController.CurrentMesh;
			}
			else if (anim is not null && _animController.HasAnimation)
			{
				// API-provided animation frames are already loaded; just restore playback state.
				_animController.FPS       = anim.FPS;
				_animController.Speed     = anim.Speed;
				_animController.IsReverse = anim.IsReverse;
				_animController.SeekTo(anim.FrameIndex);
				if (anim.IsPlaying) _animController.Play();
				sceneState.CurrentAnimation = null;
				_entity.Mesh = _animController.CurrentMesh;
			}
			else
			{
				// Only unload a previously active named sequence. If the controller already
				// holds API-provided frames (loaded by the caller before this call), leave
				// them in place — there's simply no saved animation state to apply on top.
				if (sceneState.CurrentAnimation is not null)
					_animController.Unload();
				sceneState.CurrentAnimation = null;
			}
			_ui.RestoreAnimation(sceneState.CurrentAnimation);
		}
	}
}

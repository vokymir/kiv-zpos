using OpenTK.Graphics.OpenGL;
using Renderer.Components;
using Renderer.Objects;
using Renderer.Wrappers.Model;
using Renderer.Wrappers.Shaders;
using Renderer.Wrappers.Textures;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Renderer
{
	/// <summary>Static entry points for the render pipeline: frame setup, offscreen targets, and entity drawing.</summary>
	public sealed class Render
	{
		public static Vector4 ClearColor { get; set; } = new(0f, 0f, 0f, 1f);

		private static ShaderStorageBuffer? _lightSsbo;

		[StructLayout(LayoutKind.Sequential)]
		private struct GpuDirectionalLight
		{
			public Vector3 Color;
			public float   Intensity;
			public Vector3 Direction;
			public float   Pad;
		}

		/// <summary>Sets the OpenGL viewport rectangle.</summary>
		public static void Viewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, width, height);
		}

		/// <summary>Binds <paramref name="target"/> and sets the viewport to its dimensions. Call <see cref="EndTarget"/> when done.</summary>
		public static void BeginTarget(RenderTarget target)
		{
			ArgumentNullException.ThrowIfNull(target);
			target.Bind();
			Viewport(0, 0, target.Width, target.Height);
		}

		/// <summary>Unbinds the render target and restores the viewport to the window dimensions.</summary>
		public static void EndTarget(int width, int height)
		{
			RenderTarget.Unbind();
			Viewport(0, 0, width, height);
		}

		/// <summary>Clears the active framebuffer. Each buffer can be toggled independently.</summary>
		public static void BeginFrame(bool clearColor = true, bool clearDepth = true, bool clearStencil = true)
		{
			ClearBufferMask mask = 0;

			if (clearColor)
			{
				GL.ClearColor(ClearColor.X, ClearColor.Y, ClearColor.Z, ClearColor.W);
				mask |= ClearBufferMask.ColorBufferBit;
			}

			if (clearDepth)
				mask |= ClearBufferMask.DepthBufferBit;

			if (clearStencil)
				mask |= ClearBufferMask.StencilBufferBit;

			if (mask != 0)
				GL.Clear(mask);
		}

		/// <summary>Draws a single entity with the given camera, lighting, and viewport aspect ratio.</summary>
		public static void Draw(Entity entity, Camera camera, SceneLighting lighting, float aspectRatio)
		{
			ArgumentNullException.ThrowIfNull(entity);
			ArgumentNullException.ThrowIfNull(camera);
			ArgumentNullException.ThrowIfNull(lighting);

			if (entity.Mesh is null)
				return;

			if (entity.Material is null)
				throw new InvalidOperationException("Entity has no material assigned.");

			ShaderProgram shader = entity.Material.Shader;

			shader.Bind();
			SetMaterialDefaults(shader);
			entity.Material.Apply();

			Matrix4x4 model = entity.Transforms.Matrix;
			Matrix4x4.Invert(model, out Matrix4x4 modelInv);
			// Transpose-of-inverse keeps normals perpendicular to surfaces under non-uniform scale.
			Matrix4x4 normalMatrix = Matrix4x4.Transpose(modelInv);

			shader.SetUniformImmediate("uModel", model);
			shader.SetUniformImmediate("uView", camera.GetViewMatrix());
			shader.SetUniformImmediate("uProjection", camera.GetProjectionMatrix(aspectRatio));
			shader.SetUniformImmediate("uNormalMatrix", normalMatrix);

			ApplyLighting(shader, lighting, camera.Transforms.Position);

			entity.Mesh.Bind();
			entity.Mesh.Draw();
		}

		/// <summary>Draws a sequence of entities, each sharing the same camera, lighting, and aspect ratio.</summary>
		public static void Draw(IEnumerable<Entity> entities, Camera camera, SceneLighting lighting, float aspectRatio)
		{
			ArgumentNullException.ThrowIfNull(entities);

			foreach (Entity entity in entities)
				Draw(entity, camera, lighting, aspectRatio);
		}

		private static void SetMaterialDefaults(ShaderProgram shader)
		{
			shader.SetUniformImmediate(MaterialParams.Shininess, MaterialParams.DefaultShininess);
			shader.SetUniformImmediate(MaterialParams.SpecularStrength, MaterialParams.DefaultSpecularStrength);
			shader.SetUniformImmediate(MaterialParams.SpecularColor, MaterialParams.DefaultSpecularColor);
		}

		private static void ApplyLighting(ShaderProgram shader, SceneLighting lighting, Vector3 viewPos)
		{
			ArgumentNullException.ThrowIfNull(shader);
			ArgumentNullException.ThrowIfNull(lighting);

			shader.SetUniformImmediate("uAmbientColor", lighting.Ambient.Color);
			shader.SetUniformImmediate("uAmbientIntensity", lighting.Ambient.Intensity);
			shader.SetUniformImmediate("uDirectionalLightCount", lighting.DirectionalLights.Count);
			shader.SetUniformImmediate("uViewPos", viewPos);

			_lightSsbo ??= new ShaderStorageBuffer();

			GpuDirectionalLight[] gpuLights = new GpuDirectionalLight[Math.Max(lighting.DirectionalLights.Count, 1)];
			for (int i = 0; i < lighting.DirectionalLights.Count; i++)
			{
				DirectionalLight l = lighting.DirectionalLights[i];
				gpuLights[i] = new GpuDirectionalLight { Color = l.Color, Intensity = l.Intensity, Direction = l.Direction };
			}

			_lightSsbo.SetData(BufferUsageHint.DynamicDraw, gpuLights);
			GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _lightSsbo.Handle);
		}
	}
}

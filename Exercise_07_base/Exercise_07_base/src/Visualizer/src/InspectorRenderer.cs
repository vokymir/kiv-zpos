using Core;
using ImGuiNET;
using Renderer.Components;
using Renderer.Objects;
using System.Numerics;
using System.Reflection;
using Visualizer.Inspector;

/// <summary>
/// Draws an ImGui property editor for arbitrary objects using reflection.
/// Typed drawers are registered in <c>_drawers</c>; unknown types are expanded recursively.
/// </summary>
public static class InspectorRenderer
{
	private delegate bool Drawer(string name, object value, Type type, out object? newValue);

	// Rotation (Euler degrees) uses a coarser drag speed than position/scale so a small mouse
	// movement doesn't spin the object through hundreds of degrees.
	private const float PositionDragSpeed = 0.01f;
	private const float RotationDragSpeed = 0.25f;

	private static readonly Dictionary<Type, (PropertyInfo[] Properties, FieldInfo[] Fields)> _memberCache = [];
	// Preserves user-edited euler angles between frames to avoid gimbal-lock discontinuities
	// that arise when converting quaternion → euler on every redraw.

	private static (PropertyInfo[] Properties, FieldInfo[] Fields) GetMembers(Type type)
	{
		if (_memberCache.TryGetValue(type, out var cached))
			return cached;

		var entry = (
			type.GetProperties(BindingFlags.Instance | BindingFlags.Public),
			type.GetFields(BindingFlags.Instance | BindingFlags.Public)
		);
		_memberCache[type] = entry;
		return entry;
	}

	private static readonly Dictionary<Type, Drawer> _drawers = new()
	{
		[typeof(float)] = DrawFloat,
		[typeof(int)] = DrawInt,
		[typeof(bool)] = DrawBool,
		[typeof(Vector2)] = DrawVector2,
		[typeof(Vector3)] = DrawVector3,
		[typeof(Vector4)] = DrawVector4,
		[typeof(Quaternion)] = DrawQuaternion,
		[typeof(Transforms)] = DrawTransforms,
		[typeof(AmbientLight)] = DrawObjectFallback,
		[typeof(DirectionalLight)] = DrawObjectFallback
	};

	/// <summary>Draws an ImGui header and property list for <paramref name="obj"/>.</summary>
	public static void DrawObject(object obj)
	{
		Type type = obj.GetType();

		if (InspectorRegistry.TryGet(type, out ComponentDrawer? drawer))
		{
			drawer!.DrawHeader(obj);
			DrawLayout(obj, drawer.BuildLayout(obj));
			return;
		}

		ImGui.Text(type.Name);
		ImGui.Separator();

		DrawMembers(obj, type);
	}

	private static void DrawLayout(object target, InspectorLayout layout)
	{
		foreach (InspectorSection section in layout.Sections)
		{
			bool visible = section.Header is null
				|| ImGui.CollapsingHeader(section.Header, section.TreeNodeFlags);

			if (!visible)
				continue;

			foreach (IInspectorField field in section.Fields)
				DrawField(target, field);
		}
	}

	private static void DrawField(object target, IInspectorField field)
	{
		ImGui.PushID(field.Label);

		object? value = field.Get(target);

		if (field.ReadOnly)
		{
			ImGui.TextDisabled($"{field.Label}: {value}");
			ImGui.PopID();
			return;
		}

		DrawValue(field.Label, value, field.ValueType, newValue => field.Set(target, newValue));

		ImGui.PopID();
	}

	private static void DrawMembers(object obj, Type type)
	{
		var (properties, fields) = GetMembers(type);

		foreach (PropertyInfo property in properties)
		{
			if (!property.CanRead)
				continue;

			object? value = property.GetValue(obj);

			DrawValue(property.Name, value, property.PropertyType, newValue =>
				{
					if (property.CanWrite)
					property.SetValue(obj, newValue);
				}
			);
		}

		foreach (FieldInfo field in fields)
		{
			object? value = field.GetValue(obj);

			DrawValue(field.Name, value, field.FieldType, newValue =>
				{
					field.SetValue(obj, newValue);
				}
			);
		}
	}

	private static void DrawValue(string name, object? value, Type type, Action<object?> setValue)
	{
		ImGui.PushID(name);

		if (value is null)
		{
			ImGui.TextDisabled($"{name}: null");
			ImGui.PopID();
			return;
		}

		if (_drawers.TryGetValue(type, out Drawer? drawer))
		{
			if (drawer(name, value, type, out object? newValue))
				setValue(newValue);

			ImGui.PopID();
			return;
		}

		if (type.IsEnum)
		{
			if (DrawEnum(name, value, type, out object? newEnumValue))
				setValue(newEnumValue);

			ImGui.PopID();
			return;
		}

		if (IsExpandable(type))
		{
			if (ImGui.TreeNode(name))
			{
				DrawMembers(value, type);
				ImGui.TreePop();
			}

			ImGui.PopID();
			return;
		}

		ImGui.TextDisabled($"{name}: {value}");
		ImGui.PopID();
	}

	private static bool DrawFloat(string name, object value, Type type, out object? newValue)
	{
		float v = (float)value;
		bool changed = ImGui.DragFloat(name, ref v, PositionDragSpeed);
		newValue = v;
		return changed;
	}

	private static bool DrawInt(string name, object value, Type type, out object? newValue)
	{
		int v = (int)value;
		bool changed = ImGui.DragInt(name, ref v);
		newValue = v;
		return changed;
	}

	private static bool DrawBool(string name, object value, Type type, out object? newValue)
	{
		bool v = (bool)value;
		bool changed = ImGui.Checkbox(name, ref v);
		newValue = v;
		return changed;
	}

	private static bool DrawVector2(string name, object value, Type type, out object? newValue)
	{
		Vector2 v = (Vector2)value;
		bool changed = ImGui.DragFloat2(name, ref v, PositionDragSpeed);
		newValue = v;
		return changed;
	}

	private static bool DrawVector3(string name, object value, Type type, out object? newValue)
	{
		Vector3 v = (Vector3)value;

		bool isColor =
			name.Contains("Color", StringComparison.OrdinalIgnoreCase) ||
			name.Contains("Tint", StringComparison.OrdinalIgnoreCase);

		bool changed = isColor
			? ImGui.ColorEdit3(name, ref v)
			: ImGui.DragFloat3(name, ref v, PositionDragSpeed);

		newValue = v;
		return changed;
	}

	private static bool DrawVector4(string name, object value, Type type, out object? newValue)
	{
		Vector4 v = (Vector4)value;

		bool isColor =
			name.Contains("Color", StringComparison.OrdinalIgnoreCase) ||
			name.Contains("Tint", StringComparison.OrdinalIgnoreCase);

		bool changed = isColor
			? ImGui.ColorEdit4(name, ref v)
			: ImGui.DragFloat4(name, ref v, PositionDragSpeed);

		newValue = v;
		return changed;
	}

	private static bool DrawQuaternion(string name, object value, Type type, out object? newValue)
	{
		Quaternion q = Quaternion.Normalize((Quaternion)value);
		Vector3 euler = EulerCache.ToEuler(q);
		bool changed = ImGui.DragFloat3($"{name} (Euler)", ref euler, RotationDragSpeed);
		newValue = changed ? EulerCache.FromEuler(euler) : q;
		return changed;
	}

	// Transforms is a sealed reference type exposed via a get-only property (Entity.Transforms),
	// so edits are applied in place on the same instance rather than via setValue. `changed`
	// still reflects whether any field was edited, matching the other drawers' contract.
	private static bool DrawTransforms(string name, object value, Type type, out object? newValue)
	{
		Transforms transforms = (Transforms)value;
		bool changed = false;

		if (ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.DefaultOpen))
		{
			Vector3 position = transforms.Position;
			if (ImGui.DragFloat3("Position", ref position, PositionDragSpeed))
			{
				transforms.Position = position;
				changed = true;
			}

			Vector3 euler = EulerCache.GetEulerDegrees(transforms);
			if (ImGui.DragFloat3("Rotation", ref euler, RotationDragSpeed))
			{
				EulerCache.SetEulerDegrees(transforms, euler);
				changed = true;
			}

			Vector3 scale = transforms.Scale;
			if (ImGui.DragFloat3("Scale", ref scale, PositionDragSpeed))
			{
				transforms.Scale = scale;
				changed = true;
			}

			if (ImGui.Button("Reset Transform"))
			{
				transforms.Position = Vector3.Zero;
				transforms.Rotation = Quaternion.Identity;
				transforms.Scale = Vector3.One;
				EulerCache.Evict(transforms);
				changed = true;
			}

			ImGui.TreePop();
		}

		newValue = transforms;
		return changed;
	}


	private static bool DrawEnum(string name, object value, Type type, out object? newValue)
	{
		string preview = value.ToString() ?? "";

		bool changed = false;
		object selected = value;

		ImGui.SetNextItemWidth(160f);

		if (ImGui.BeginCombo(name, preview))
		{
			foreach (object option in Enum.GetValues(type))
			{
				bool isSelected = Equals(option, value);

				if (ImGui.Selectable(option.ToString(), isSelected))
				{
					selected = option;
					changed = true;
				}

				if (isSelected)
					ImGui.SetItemDefaultFocus();
			}

			ImGui.EndCombo();
		}

		newValue = selected;
		return changed;
	}

	private static bool DrawObjectFallback(string name, object value, Type type, out object? newValue)
	{
		if (ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.DefaultOpen))
		{
			DrawMembers(value, type);
			ImGui.TreePop();
		}

		newValue = value;
		return false;
	}

	private static bool IsExpandable(Type type)
	{
		if (type.IsPrimitive || type.IsEnum)
			return false;

		if (type == typeof(string))
			return false;

		if (type.Namespace?.StartsWith("System") == true)
			return false;

		return true;
	}

}

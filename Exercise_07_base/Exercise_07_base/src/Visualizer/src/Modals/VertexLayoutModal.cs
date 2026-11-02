using Core.Structures;
using ImGuiNET;
using Renderer.Wrappers.Model;
using System.Numerics;

namespace Visualizer.Modals
{
	/// <summary>
	/// Modal popup for editing <see cref="SceneModelConfig.VertexLayout"/>.
	/// Each row maps a GPU attribute index to a named vertex channel.
	/// </summary>
	public sealed class VertexLayoutModal
	{
		private const string PopupId = "Vertex Attribute Mapping###VertexLayoutModal";

		private bool _pendingOpen;
		private AssetMetadata? _target;

		private readonly List<RowEntry> _rows = [];
		private string[] _availableChannels = [];

		// Validation error shown inline; cleared when the user fixes duplicates.
		private string? _validationError;

		public bool SavedThisFrame { get; private set; }

		private static readonly string[] _normalizationLabels = ["None", "[-1, 1]", "[0, 1]"];

		private sealed class RowEntry
		{
			public int  AttributeIndex;
			public int  ChannelComboIndex;
			public ChannelNormalization Normalization;
			public bool MarkedForRemoval;
			public bool MoveUp;
			public bool MoveDown;
		}

			/// <summary>Opens the modal on the next <see cref="Draw"/> call.</summary>
		/// <param name="meta">Metadata to edit (mutated in-memory on confirm).</param>
		/// <param name="availableChannels">Channel names present in the mesh vertices.</param>
		public void Open(AssetMetadata meta, string[] availableChannels)
		{
			_target            = meta;
			_availableChannels = availableChannels;
			_validationError   = null;

			_rows.Clear();
			List<VertexChannelSpec> existing = meta.Scene?.Model?.VertexLayout ?? DefaultLayout();
			foreach (VertexChannelSpec spec in existing)
			{
				int chIdx = Array.IndexOf(availableChannels, spec.Name);
				if (chIdx < 0 && availableChannels.Length > 0)
					chIdx = 0;
				_rows.Add(new RowEntry
				{
					AttributeIndex    = spec.AttributeIndex,
					ChannelComboIndex = Math.Max(0, chIdx),
					Normalization     = spec.Normalization,
				});
			}

			_pendingOpen = true;
		}

		/// <summary>Call once per frame inside an ImGui frame.</summary>
		public void Draw()
		{
			SavedThisFrame = false;

			if (_pendingOpen)
			{
				ImGui.OpenPopup(PopupId);
				_pendingOpen = false;
			}

			Vector2 center = ImGui.GetMainViewport().GetCenter();
			ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
			ImGui.SetNextWindowSize(new Vector2(500, 0), ImGuiCond.Appearing);

			bool open = true;
			if (!ImGui.BeginPopupModal(PopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
				return;

			ImGui.TextDisabled("Map GPU attribute slots to vertex channel names.");
			ImGui.Spacing();

			DrawTable();
			ApplyDeferredMutations();

			ImGui.Spacing();

			if (ImGui.Button("+ Add Row"))
			{
				_validationError = null;
				_rows.Add(new RowEntry
				{
					AttributeIndex    = _rows.Count,
					ChannelComboIndex = 0,
				});
			}

			ImGui.SameLine();
			if (ImGui.Button("Reset to Default"))
			{
				_validationError = null;
				LoadDefaultRows();
			}

			ImGui.SameLine();
			if (ImGui.Button("Clear"))
			{
				_validationError = null;
				_rows.Clear();
			}

			if (_validationError is not null)
			{
				ImGui.Spacing();
				ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.35f, 0.35f, 1f));
				ImGui.TextUnformatted(_validationError);
				ImGui.PopStyleColor();
			}

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			if (ImGui.Button("Save", new Vector2(100, 0)))
			{
				if (TryCommit())
					ImGui.CloseCurrentPopup();
			}
			ImGui.SameLine();
			if (ImGui.Button("Cancel", new Vector2(100, 0)))
				ImGui.CloseCurrentPopup();

			ImGui.EndPopup();
		}

		private void DrawTable()
		{
			ImGuiTableFlags flags =
				ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit;

			// 6 columns: reorder-up | reorder-down | attr-index | channel | normalize | remove
			if (!ImGui.BeginTable("##layoutTable", 6, flags))
				return;

			ImGui.TableSetupColumn("##up",       ImGuiTableColumnFlags.WidthFixed,   22);
			ImGui.TableSetupColumn("##down",     ImGuiTableColumnFlags.WidthFixed,   22);
			ImGui.TableSetupColumn("Attr Index", ImGuiTableColumnFlags.WidthFixed,   90);
			ImGui.TableSetupColumn("Channel",    ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn("Normalize",  ImGuiTableColumnFlags.WidthFixed,   90);
			ImGui.TableSetupColumn("##rm",       ImGuiTableColumnFlags.WidthFixed,   22);
			ImGui.TableHeadersRow();

			for (int i = 0; i < _rows.Count; i++)
			{
				RowEntry row = _rows[i];
				bool hasDuplicate = HasDuplicateIndex(i);

				ImGui.TableNextRow();
				ImGui.PushID(i);

				// ▲ move up
				ImGui.TableSetColumnIndex(0);
				if (i == 0) ImGui.BeginDisabled();
				if (ImGui.SmallButton("^")) row.MoveUp = true;
				if (i == 0) ImGui.EndDisabled();

				// ▼ move down
				ImGui.TableSetColumnIndex(1);
				if (i == _rows.Count - 1) ImGui.BeginDisabled();
				if (ImGui.SmallButton("v")) row.MoveDown = true;
				if (i == _rows.Count - 1) ImGui.EndDisabled();

				// Attribute index — highlighted red if duplicate
				ImGui.TableSetColumnIndex(2);
				if (hasDuplicate)
					ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.6f, 0.1f, 0.1f, 1f));
				ImGui.SetNextItemWidth(-1);
				ImGui.InputInt("##attr", ref row.AttributeIndex);
				row.AttributeIndex = Math.Max(0, row.AttributeIndex);
				if (hasDuplicate)
					ImGui.PopStyleColor();

				// Channel dropdown
				ImGui.TableSetColumnIndex(3);
				ImGui.SetNextItemWidth(-1);
				if (_availableChannels.Length > 0)
				{
					string preview = row.ChannelComboIndex < _availableChannels.Length
						? _availableChannels[row.ChannelComboIndex]
						: "";
					if (ImGui.BeginCombo("##ch", preview))
					{
						for (int c = 0; c < _availableChannels.Length; c++)
						{
							bool sel = c == row.ChannelComboIndex;
							if (ImGui.Selectable(_availableChannels[c], sel))
								row.ChannelComboIndex = c;
							if (sel)
								ImGui.SetItemDefaultFocus();
						}
						ImGui.EndCombo();
					}
				}
				else
				{
					ImGui.TextDisabled("(no channels)");
				}

				// Normalization mode
				ImGui.TableSetColumnIndex(4);
				ImGui.SetNextItemWidth(-1);
				int normIndex = (int)row.Normalization;
				if (ImGui.Combo("##norm", ref normIndex, _normalizationLabels, _normalizationLabels.Length))
					row.Normalization = (ChannelNormalization)normIndex;

				// Remove
				ImGui.TableSetColumnIndex(5);
				if (ImGui.SmallButton("x"))
					row.MarkedForRemoval = true;

				ImGui.PopID();
			}

			ImGui.EndTable();
		}

		private void ApplyDeferredMutations()
		{
			_rows.RemoveAll(r => r.MarkedForRemoval);

			for (int i = 0; i < _rows.Count; i++)
			{
				if (_rows[i].MoveUp && i > 0)
				{
					(_rows[i], _rows[i - 1]) = (_rows[i - 1], _rows[i]);
					_rows[i].MoveUp     = false;
					_rows[i - 1].MoveUp = false;
					break;
				}
				if (_rows[i].MoveDown && i < _rows.Count - 1)
				{
					(_rows[i], _rows[i + 1]) = (_rows[i + 1], _rows[i]);
					_rows[i].MoveDown     = false;
					_rows[i + 1].MoveDown = false;
					break;
				}
			}
		}

		private bool HasDuplicateIndex(int rowIndex)
		{
			int idx = _rows[rowIndex].AttributeIndex;
			for (int i = 0; i < _rows.Count; i++)
				if (i != rowIndex && _rows[i].AttributeIndex == idx)
					return true;
			return false;
		}

		/// <summary>Returns true when validation passes and the metadata was saved.</summary>
		private bool TryCommit()
		{
			if (_target is null) return false;

			var duplicates = _rows
				.GroupBy(r => r.AttributeIndex)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key)
				.ToList();

			if (duplicates.Count > 0)
			{
				_validationError = $"Duplicate attribute index: {string.Join(", ", duplicates)}. Each index must be unique.";
				return false;
			}

			_validationError = null;

			// Null means "use the built-in default"; an empty list is valid (no attributes mapped).
			_target.EnsureModel().VertexLayout = _rows.Count == 0
				? null
				: [.. _rows
					.Select(r => new VertexChannelSpec
					{
						AttributeIndex = r.AttributeIndex,
						Name           = r.ChannelComboIndex < _availableChannels.Length
							? _availableChannels[r.ChannelComboIndex]
							: "",
						Normalization  = r.Normalization,
					})];

			SavedThisFrame = true;
			return true;
		}

		private void LoadDefaultRows()
		{
			_rows.Clear();
			foreach (VertexChannelSpec spec in DefaultLayout())
			{
				int chIdx = Array.IndexOf(_availableChannels, spec.Name);
				_rows.Add(new RowEntry
				{
					AttributeIndex    = spec.AttributeIndex,
					ChannelComboIndex = Math.Max(0, chIdx),
				});
			}
		}

		private static List<VertexChannelSpec> DefaultLayout() =>
		[
			new() { Name = "position", AttributeIndex = 0 },
			new() { Name = "normal",   AttributeIndex = 1 },
		];
	}
}

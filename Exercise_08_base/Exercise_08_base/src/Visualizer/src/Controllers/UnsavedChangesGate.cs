using Visualizer.Modals;

namespace Visualizer.Controllers
{
	/// <summary>
	/// Tracks the action deferred behind the "unsaved changes" modal (exit or model change)
	/// and resolves it once the user picks Save / Discard / Cancel.
	/// </summary>
	internal sealed class UnsavedChangesGate
	{
		public enum Resolution { None, Exit, ChangeModel, RevertModel }

		private enum PendingAction { None, Exit, ModelChange }

		private PendingAction _pending = PendingAction.None;
		private string? _pendingModel;

		/// <summary>Returns true if the exit must wait for the unsaved-changes prompt.</summary>
		public bool RequestExit(bool sceneModified)
		{
			if (!sceneModified) return false;
			_pending = PendingAction.Exit;
			return true;
		}

		/// <summary>Returns true if the model change must wait for the unsaved-changes prompt.</summary>
		public bool RequestModelChange(string model, bool sceneModified)
		{
			if (!sceneModified) return false;
			_pending = PendingAction.ModelChange;
			_pendingModel = model;
			return true;
		}

		/// <summary>Resolves the pending action given the modal's result, clearing the pending state.</summary>
		public (Resolution Resolution, string? Model) Resolve(UnsavedChangesResult result)
		{
			Resolution resolution;
			string? model = null;

			if (result == UnsavedChangesResult.None)
			{
				resolution = Resolution.None;
			}
			else if (result != UnsavedChangesResult.Cancel)
			{
				if (_pending == PendingAction.Exit)
					resolution = Resolution.Exit;
				else if (_pending == PendingAction.ModelChange && _pendingModel is not null)
				{
					resolution = Resolution.ChangeModel;
					model = _pendingModel;
				}
				else
					resolution = Resolution.None;
			}
			else
			{
				resolution = _pending == PendingAction.ModelChange ? Resolution.RevertModel : Resolution.None;
			}

			_pending = PendingAction.None;
			_pendingModel = null;
			return (resolution, model);
		}
	}
}

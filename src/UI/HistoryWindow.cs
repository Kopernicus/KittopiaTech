using System.Linq;
using KittopiaTech.Model;
using KittopiaTech.UI.Debug;
using KittopiaTech.UI.Editors;
using KittopiaTech.UI.Inspector;
using TMPro;
using UnityEngine;

namespace KittopiaTech.UI;

/// <summary>
/// A body's applied revisions, newest first. Clicking one restores the body to it; with edits
/// pending, a second click confirms discarding them.
/// </summary>
public sealed class HistoryWindow : MonoBehaviour
{
    const float ConfirmSeconds = 3f;

    EditSession session;
    Transform list;
    int armed = -1;
    float armedUntil;

    public static Popout Open(InspectorWindow owner, EditSession session)
    {
        string name = session.Context.CelestialBody.displayName.LocalizeRemoveGender();
        var popout = Popout.Open(owner, $"History · {name}", new Vector2(380f, 420f));
        var window = popout.gameObject.AddComponent<HistoryWindow>();
        window.session = session;
        DebugSkin.CreateScrollView(popout.Content, out window.list);
        session.Changed += window.Rebuild;
        window.Rebuild();
        return popout;
    }

    void OnDestroy() => session.Changed -= Rebuild;

    void Update()
    {
        if (armed >= 0 && Time.unscaledTime > armedUntil)
        {
            armed = -1;
            Rebuild();
        }
    }

    void Rebuild()
    {
        InspectorBuilder.Clear(list);
        var history = session.History;
        for (int i = history.Revisions.Count - 1; i >= 0; i--)
        {
            var revision = history.Revisions[i];
            int index = i;
            var edited = revision.Edited.Distinct().ToList();
            string summary = edited.Count == 0 ? ""
                : edited.Count <= 3 ? string.Join(", ", edited)
                : $"{string.Join(", ", edited.Take(3))} +{edited.Count - 3} more";
            string text = armed == i
                ? $"Discard {session.PendingCount} pending and restore?"
                : $"{revision.Time:HH:mm:ss}  {revision.Label}  <color=#a3a7ab>{summary}</color>";

            var button = DebugSkin.CreateButton(list, text, () => OnClick(index));
            EditorLayout.Compact(button.gameObject);
            DebugSkin.StyleAsSidebarItem(button);
            DebugSkin.SetSidebarItemSelected(button, i == history.Current);
            button.interactable = history.CanRestore(i);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.richText = true;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.margin = new Vector4(6f, 0f, 6f, 0f);
            label.fontStyle = i == history.Current ? FontStyles.Bold : FontStyles.Normal;

            string state = i == history.Current ? "The body's current state"
                : revision.Sealed ? "Can't be restored: a later edit can't be undone"
                : "Click to restore the body to this state";
            Tooltip.Attach(button.gameObject, edited.Count == 0 ? state : $"{state}\n\n{string.Join("\n", edited)}");
        }
    }

    void OnClick(int index)
    {
        if (session.PendingCount > 0 && armed != index)
        {
            armed = index;
            armedUntil = Time.unscaledTime + ConfirmSeconds;
            Rebuild();
            return;
        }
        armed = -1;
        session.History.Restore(index);
    }
}

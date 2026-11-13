using System.Drawing;
using System.Windows.Forms;

namespace AutoClickSelector.UI.Components;

/// <summary>
/// Chuyển sự kiện bánh xe chuột tới vùng đang nằm dưới con trỏ, dù control đó không có tiêu điểm.
/// Khi có một cửa sổ "modal" (ví dụ danh sách thả xuống) đang mở, chỉ cửa sổ đó nhận bánh xe;
/// phần còn lại của ứng dụng đứng yên.
/// </summary>
internal static class WheelRouter
{
    private const int WmMouseWheel = 0x020A;

    private static readonly List<(Control Target, Action<int> Scroll)> Targets = new();
    private static (Control Target, Action<int> Scroll)? modal;
    private static bool registered;

    public static void Register(Control target, Action<int> scroll)
    {
        Targets.Add((target, scroll));
        EnsureRegistered();
    }

    public static void SetModal(Control target, Action<int> scroll)
    {
        modal = (target, scroll);
        EnsureRegistered();
    }

    public static void ClearModal(Control target)
    {
        if (modal is { } active && ReferenceEquals(active.Target, target))
            modal = null;
    }

    private static void EnsureRegistered()
    {
        if (registered)
            return;

        Application.AddMessageFilter(new Filter());
        registered = true;
    }

    private sealed class Filter : IMessageFilter
    {
        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WmMouseWheel)
                return false;

            Point cursor = Cursor.Position;

            // HIWORD của wParam là delta có dấu (120 cho mỗi nấc).
            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);

            if (modal is { } active)
            {
                if (IsUnder(active.Target, cursor))
                    active.Scroll(delta);

                // Khi đang có modal, bánh xe không cuộn gì khác.
                return true;
            }

            foreach (var (target, scroll) in Targets)
            {
                if (!target.IsHandleCreated || !target.Visible || !IsUnder(target, cursor))
                    continue;

                scroll(delta);
                return true;
            }

            return false;
        }

        private static bool IsUnder(Control target, Point cursor)
        {
            if (!target.IsHandleCreated || !target.Visible)
                return false;

            return target.RectangleToScreen(target.ClientRectangle).Contains(cursor);
        }
    }
}

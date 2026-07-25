#if HAS_UNO
using Tmds.DBus.Protocol;
using DesktopFlyouts.DBus;

namespace DesktopFlyouts;

class X11StatusNotifierItemHandler : DBusHandler, IStatusNotifierItemHandler, IStatusNotifierItemProperties
{
    public X11StatusNotifierItemHandler(DBusConnection connection, string id, string title)
        : base(connection, path: "/StatusNotifierItem", handlesChildPaths: false)
    {
        Category = "ApplicationStatus";
        Id = id;
        Title = title;
        Status = "Active";
        IconName = "";
        IconPixmap = Array.Empty<(int, int, byte[])>();
        OverlayIconName = "";
        OverlayIconPixmap = Array.Empty<(int, int, byte[])>();
        AttentionIconName = "";
        AttentionIconPixmap = Array.Empty<(int, int, byte[])>();
        AttentionMovieName = "";
        IconThemePath = "";
        ItemIsMenu = false;
        ToolTip = ("", Array.Empty<(int, int, byte[])>(), "", "");
        WindowId = 0;
    }

    public event Action<int, int>? ActivationDelegate;
    public event Action<int, int>? ContextMenuDelegate;
    public event Action<int, int>? SecondaryActivateDelegate;
    public event Action<int, string>? ScrollDelegate;

    public ValueTask ContextMenuAsync(int x, int y)
    {
        ContextMenuDelegate?.Invoke(x, y);
        return default;
    }

    public ValueTask ActivateAsync(int x, int y)
    {
        ActivationDelegate?.Invoke(x, y);
        return default;
    }

    public ValueTask SecondaryActivateAsync(int x, int y)
    {
        SecondaryActivateDelegate?.Invoke(x, y);
        return default;
    }

    public ValueTask ScrollAsync(int delta, string orientation)
    {
        ScrollDelegate?.Invoke(delta, orientation);
        return default;
    }

    public ValueTask HandleGetPropertyAsync(IStatusNotifierItemHandler.GetPropertyContext context)
        => context.Handle(this);

    public ValueTask HandleGetAllPropertiesAsync(IStatusNotifierItemHandler.GetAllPropertiesContext context)
        => context.Handle(this);

    public void SetIcon((int, int, byte[]) dbusPixmap)
    {
        IconPixmap = new[] { dbusPixmap };
        IconName = "";
        Status = "Active";
        Connection.EmitNewIcon(new ObjectPath(Path));
        Connection.EmitNewStatus(new ObjectPath(Path), Status);
    }

    public void SetAttentionIcon((int, int, byte[]) dbusPixmap)
    {
        AttentionIconPixmap = new[] { dbusPixmap };
        AttentionIconName = "";
        Status = "NeedsAttention";
        Connection.EmitNewAttentionIcon(new ObjectPath(Path));
        Connection.EmitNewStatus(new ObjectPath(Path), Status);
    }

    public void SetTitleAndTooltip(string text)
    {
        Title = text;
        ToolTip = ("", Array.Empty<(int, int, byte[])>(), text, "");
        Connection.EmitNewTitle(new ObjectPath(Path));
        Connection.EmitNewToolTip(new ObjectPath(Path));
    }

    public void SetStatus(string status)
    {
        Status = status;
        Connection.EmitNewStatus(new ObjectPath(Path), status);
    }

    public string Category { get; set; }
    public string Id { get; set; }
    public string Title { get; set; }
    public string Status { get; set; }
    public int WindowId { get; set; }
    public string IconThemePath { get; set; }
    public bool ItemIsMenu { get; set; }
    public string IconName { get; set; }
    public (int, int, byte[])[] IconPixmap { get; set; }
    public string OverlayIconName { get; set; }
    public (int, int, byte[])[] OverlayIconPixmap { get; set; }
    public string AttentionIconName { get; set; }
    public (int, int, byte[])[] AttentionIconPixmap { get; set; }
    public string AttentionMovieName { get; set; }
    public (string, (int, int, byte[])[], string, string) ToolTip { get; set; }
}
#endif

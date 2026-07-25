#if HAS_UNO
using Tmds.DBus.Protocol;
using DesktopFlyouts.DBus;
using SkiaSharp;
using Svg.Skia;

namespace DesktopFlyouts;

/// <summary>
/// Provides functionality for displaying and managing a system tray icon on Linux via D-Bus StatusNotifierItem.
/// </summary>
/// <remarks>
/// <see cref="SystemTrayIcon"/> wraps the StatusNotifierItem D-Bus protocol and raises click events
/// with screen coordinates that can be passed to the point-based flyout and menu show methods.
/// Call <see cref="Show"/> to register the icon with the StatusNotifierWatcher and
/// <see cref="Destroy"/> to remove it. Call <see cref="Dispose()"/> explicitly when the object
/// is no longer needed to release its owned resources.
/// </remarks>
public class SystemTrayIcon : IDisposable
{
    readonly string _id;
    DBusConnection? _connection;
    DBus.DBus? _dBus;
    StatusNotifierWatcher? _statusNotifierWatcher;
    X11StatusNotifierItemHandler? _sniHandler;
    IDisposable? _serviceWatchDisposable;
    string? _sysTrayServiceName;
    bool _isDisposed;
    bool _serviceConnected;
    bool _isVisible;

    (int, int, byte[]) _currentIcon = (1, 1, new byte[] { 255, 0, 0, 0 });

    const int GnomeShellInitialDelayMs = 100;
    const int GnomeShellSecondDelayMs = 400;

    string _tooltip;
    string _iconPath;

    public SystemTrayIcon(string iconPath, string tooltip, System.Guid id)
        : this(iconPath, tooltip, id.ToString()) { }

    /// <summary>
    /// Initializes a new instance of <see cref="SystemTrayIcon"/> with an icon loaded from a
    /// file path.
    /// </summary>
    /// <param name="iconPath">The path to the icon file.</param>
    /// <param name="tooltip">The tooltip text.</param>
    /// <param name="id">The stable identifier for the tray icon.</param>
    /// <remarks>
    /// Construction prepares the icon resources and begins D-Bus initialization. The
    /// icon is not registered with the StatusNotifierWatcher until <see cref="Show"/> is called.
    /// </remarks>
    public SystemTrayIcon(string iconPath, string tooltip, string id)
    {
        ThrowHelper.ThrowIfNotLinux();
        _id = id;
        _iconPath = iconPath;
        _tooltip = tooltip;

        // Render the initial icon synchronously so that any SetIcon() call
        // during async initialization is not overwritten by the captured path.
        _currentIcon = RenderIcon(iconPath);

        _ = InitAsync();

        async Task InitAsync()
        {
            await InitializeAsync();
            _sniHandler!.ActivationDelegate += OnActivation;
            _sniHandler!.ContextMenuDelegate += OnContextMenu;
            _sniHandler!.SecondaryActivateDelegate += OnSecondaryActivate;
            _sniHandler!.ScrollDelegate += OnScroll;
        }
    }

    void OnActivation(int x, int y)
    {
        LeftClicked?.Invoke(this, new MouseEventReceivedEventArgs(new(x, y)));
    }

    void OnContextMenu(int x, int y)
    {
        RightClicked?.Invoke(this, new MouseEventReceivedEventArgs(new(x, y)));
    }

    void OnSecondaryActivate(int x, int y)
    {
        MiddleClicked?.Invoke(this, new MouseEventReceivedEventArgs(new(x, y)));
    }

    void OnScroll(int delta, string orientation)
    {
        Scrolled?.Invoke(this, new MouseScrollEventReceivedEventArgs(
            delta,
            orientation == "horizontal" ? MouseScrollOrientation.Horizontal : MouseScrollOrientation.Vertical
        ));
    }

    // ─── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the tooltip text shown for the tray icon.
    /// </summary>
    /// <value>The tray icon tooltip text.</value>
    /// <remarks>
    /// If the tray icon is already registered with the StatusNotifierWatcher, setting this property
    /// updates it immediately. Otherwise, the value is recorded and applied when <see cref="Show"/> is called.
    /// </remarks>
    public string Tooltip
    {
        get => _tooltip;
        set
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SystemTrayIcon));

            _tooltip = value;
            if (_sniHandler?.Connection is not null)
                _sniHandler.SetTitleAndTooltip(value ?? "");
        }
    }

    /// <summary>
    /// Gets the full path to the current icon file.
    /// </summary>
    /// <value>The full path to an icon file.</value>
    public string IconPath => _iconPath;

    /// <summary>
    /// Replaces the current icon with one loaded from a file path.
    /// </summary>
    /// <param name="iconPath">The path to the icon file.</param>
    /// <exception cref="FileNotFoundException">
    /// Thrown when <paramref name="iconPath"/> cannot be found or loaded as an icon file.
    /// </exception>
    public void SetIcon(string iconPath)
    {
        _iconPath = iconPath;
        UpdateIcon(iconPath);
    }

    /// <summary>
    /// Makes the tray icon visible and synchronizes its state to the StatusNotifierWatcher.
    /// </summary>
    /// <remarks>
    /// If the tray icon has not yet been registered, this method registers it. If it
    /// already exists, this method updates its current state.
    /// </remarks>
    public void Show()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SystemTrayIcon));

        _isVisible = true;
        if (_serviceConnected)
            _ = CreateTrayIconAsync();
    }

    /// <summary>
    /// Removes the tray icon from the StatusNotifierWatcher.
    /// </summary>
    /// <remarks>
    /// This method is safe to call multiple times. It does not dispose the
    /// <see cref="SystemTrayIcon"/> object; call <see cref="Dispose()"/> to release all resources.
    /// </remarks>
    public void Destroy()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SystemTrayIcon));

        _isVisible = false;
        DestroyTrayIcon();
    }

    /// <summary>
    /// Releases the resources held by this <see cref="SystemTrayIcon"/>.
    /// </summary>
    /// <remarks>
    /// Call this method explicitly when the instance is no longer needed. If the tray icon is
    /// still registered, it is removed first. The D-Bus connection and associated watches are
    /// disposed. After disposal, the instance should not be used again.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _isVisible = false;

        DestroyTrayIcon();

        _serviceWatchDisposable?.Dispose();
        _serviceWatchDisposable = null;
        _connection?.Dispose();
        _connection = null;
        _dBus = null;
        _statusNotifierWatcher = null;
        _serviceConnected = false;

        GC.SuppressFinalize(this);
    }

    ~SystemTrayIcon() => Dispose();

    /// <summary>
    /// Gets or sets whether the tray icon is visible.
    /// </summary>
    /// <value><see langword="true"/> if the tray icon is visible; otherwise,
    /// <see langword="false"/>. The default is <see langword="true"/>.</value>
    /// <remarks>
    /// If the tray icon is already registered with the StatusNotifierWatcher, setting this property
    /// updates the icon state immediately. Otherwise, the value is recorded and applied when
    /// <see cref="Show"/> is called.
    /// </remarks>
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (value) Show();
            else Destroy();
        }
    }

    /// <summary>
    /// Gets the stable identifier used for the tray icon.
    /// </summary>
    /// <value>The identifier used by the StatusNotifierWatcher to identify this notification icon.</value>
    public string Id => _id;

    /// <summary>
    /// Occurs when the tray icon receives a left-click (activation).
    /// </summary>
    /// <remarks>
    /// The event argument contains the center point of the tray icon in physical screen pixels.
    /// </remarks>
    public event EventHandler<MouseEventReceivedEventArgs>? LeftClicked;

    /// <summary>
    /// Occurs when the tray icon receives a right-click (context menu).
    /// </summary>
    /// <remarks>
    /// The event argument contains the center point of the tray icon in physical screen pixels.
    /// </remarks>
    public event EventHandler<MouseEventReceivedEventArgs>? RightClicked;

    /// <summary>
    /// Occurs when the tray icon receives a middle-click (secondary activation).
    /// </summary>
    /// <remarks>
    /// The event argument contains the center point of the tray icon in physical screen pixels.
    /// </remarks>
    public event EventHandler<MouseEventReceivedEventArgs>? MiddleClicked;

    /// <summary>
    /// Occurs when the tray icon receives a scroll event.
    /// </summary>
    /// <remarks>
    /// The event argument contains the scroll delta and orientation.
    /// </remarks>
    public event EventHandler<MouseScrollEventReceivedEventArgs>? Scrolled;
    // Linux does not have this
    // public event EventHandler<MouseEventReceivedEventArgs>? LeftDoubleClicked;
    // public event EventHandler<MouseEventReceivedEventArgs>? RightDoubleClicked;

    // ─── D-Bus Initialization ─────────────────────────────────────

    async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SystemTrayIcon));

        _connection = new DBusConnection(DBusAddress.Session!);
        await _connection.ConnectAsync();

        _dBus = new(_connection, "org.freedesktop.DBus", "/org/freedesktop/DBus");

        _sniHandler = new X11StatusNotifierItemHandler(_connection, _id, _id);

        await WatchAsync(cancellationToken);
    }

    async Task WatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            _serviceWatchDisposable = await _dBus!.WatchNameOwnerChangedAsync(
                change =>
                {
                    if (change.A0 == "org.kde.StatusNotifierWatcher")
                        OnNameChange(change.A0, change.A2);
                },
                emitOnCapturedContext: false);

            var nameOwner = await _dBus.GetNameOwnerAsync("org.kde.StatusNotifierWatcher");
            OnNameChange("org.kde.StatusNotifierWatcher", nameOwner);
        }
        catch (DBusErrorReplyException ex) when (ex.ErrorName == "org.freedesktop.DBus.Error.NameHasNoOwner")
        {
        }
        catch
        {
            _serviceWatchDisposable = null;
        }
    }

    void OnNameChange(string name, string? newOwner)
    {
        if (_isDisposed || _connection is null || name != "org.kde.StatusNotifierWatcher")
            return;

        if (!_serviceConnected && !string.IsNullOrEmpty(newOwner))
        {
            _serviceConnected = true;
            _statusNotifierWatcher = new StatusNotifierWatcher(_connection, "org.kde.StatusNotifierWatcher", "/StatusNotifierWatcher");

            // Re-register the handler on the existing connection.
            // No need to DestroyTrayIcon() here — the handler was already
            // removed when the watcher went away, and DestroyTrayIcon()
            // would dispose the connection we still need.
            _connection.RemoveMethodHandler(_sniHandler!.Path);
            _connection.AddMethodHandler(_sniHandler);

            if (_isVisible)
                _ = CreateTrayIconAsync();
        }
        else if (_serviceConnected && string.IsNullOrEmpty(newOwner))
        {
            DestroyTrayIcon();
            _serviceConnected = false;
        }
    }

    async Task CreateTrayIconAsync()
    {
        if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierWatcher is null)
            return;

        try
        {
            _connection.RemoveMethodHandler(_sniHandler!.Path);
            _connection.AddMethodHandler(_sniHandler);

            await RegisterWithStatusNotifierWatcherAsync();

            ReEmitSignalsForGnomeShellAsync();
        }
        catch
        {
        }
    }

    async Task RegisterWithStatusNotifierWatcherAsync()
    {
        _sysTrayServiceName = _connection!.UniqueName!;

        try
        {
            await _statusNotifierWatcher!.RegisterStatusNotifierItemAsync(_sysTrayServiceName);
        }
        catch
        {
            throw;
        }

        _sniHandler!.SetTitleAndTooltip(_tooltip);
        _sniHandler.SetIcon(_currentIcon);
    }

    void ReEmitSignalsForGnomeShellAsync()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(GnomeShellInitialDelayMs);
            if (!_isDisposed && _sniHandler?.Connection is not null)
                _sniHandler.SetIcon(_currentIcon);

            await Task.Delay(GnomeShellSecondDelayMs);
            if (!_isDisposed && _sniHandler?.Connection is not null)
                _sniHandler.SetIcon(_currentIcon);
        });
    }

    void DestroyTrayIcon()
    {
        if (_sniHandler is null)
            return;

        try
        {
            _connection?.RemoveMethodHandler(_sniHandler.Path);
        }
        catch
        {
        }
    }

    // ─── Icon Management ──────────────────────────────────────────

    void UpdateIcon(string iconPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SystemTrayIcon));

        _currentIcon = RenderIcon(iconPath);

        if (_sniHandler?.Connection is not null)
            _sniHandler.SetIcon(_currentIcon);
    }

    // ─── Icon Rendering ───────────────────────────────────────────

    static (int width, int height, byte[] argbData) RenderIcon(string path, int size = 48)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Icon file not found: {path}");

        try
        {
            using var svg = new SKSvg();
            if (svg.Load(path) is { } picture)
            {
                using var bitmap = RenderSvgToBitmap(picture, size);
                return ConvertToArgb(bitmap);
            }
        }
        catch
        {
        }

        using var src = SKBitmap.Decode(path)
            ?? throw new InvalidOperationException($"Failed to decode image: {path}");

        using var scaled = ScaleBitmap(src, size);
        return ConvertToArgb(scaled);
    }

    static SKBitmap RenderSvgToBitmap(SKPicture picture, int size)
    {
        var bounds = picture.CullRect;
        var scale = Math.Min(size / bounds.Width, size / bounds.Height);
        var width = Math.Max(1, (int)(bounds.Width * scale));
        var height = Math.Max(1, (int)(bounds.Height * scale));

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
        return bitmap;
    }

    static SKBitmap ScaleBitmap(SKBitmap source, int size)
    {
        var scale = Math.Min((float)size / source.Width, (float)size / source.Height);
        var width = Math.Max(1, (int)(source.Width * scale));
        var height = Math.Max(1, (int)(source.Height * scale));

        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height));
        return bitmap;
    }

    static (int width, int height, byte[] argbData) ConvertToArgb(SKBitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var pixels = bitmap.Bytes;
        var argbData = new byte[width * height * 4];

        for (int i = 0; i < width * height; i++)
        {
            var srcIdx = i * 4;
            argbData[srcIdx] = pixels[srcIdx + 3];
            argbData[srcIdx + 1] = pixels[srcIdx];
            argbData[srcIdx + 2] = pixels[srcIdx + 1];
            argbData[srcIdx + 3] = pixels[srcIdx + 2];
        }

        return (width, height, argbData);
    }
}
#endif
